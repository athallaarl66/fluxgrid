# task-project02-testing.md
# Testing Scenarios — TP-2: Time Tracking
## FluxGrid ERP | Module: Task & Project Management (TaskProject)

---

## 1. Test Strategy Overview

### Scope
This document covers all testing scenarios for the **Time Tracking** feature (TP-2), including:
- Manual time entry (happy path, validation, edge cases)
- Timer functionality (start, pause, resume, stop, auto-stop)
- Approval workflow (approve, reject, resubmit)
- HR module integration via domain event
- RBAC access control
- Performance and load testing
- Security testing

### Testing Levels

| Level | Tools | Scope |
|-------|-------|-------|
| Unit Tests | Jest + ts-jest | Domain entities, use cases, validators |
| Integration Tests | Jest + Supertest | API endpoints, Drizzle ORM queries, Redis operations |
| E2E Tests | Playwright | Full user flows in browser |
| Performance Tests | k6 | Concurrent timer operations, approval queue load |
| Security Tests | Manual + OWASP ZAP | RBAC enforcement, injection, IDOR |

### Test Environment Requirements
- Next.js 15 dev server running locally or on staging
- Neon PostgreSQL test database (separate from production)
- Upstash Redis test instance
- Seeded test data (see Section 7)
- Test users: `team_member_01`, `project_manager_01`, `hr_admin_01`, `admin_01`

---

## 2. Test Cases

---

### TC-TP2-001: Manual Time Entry — Happy Path

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-01

**Given**:
- User `team_member_01` is logged in with role `team_member`
- Task `TASK-101` exists and is assigned to `team_member_01` with status `in_progress`

**When**:
- User navigates to `/projects/proj-001/tasks/TASK-101`
- User clicks the **"+ Log Time"** button
- User fills in: Date = today, Hours = `3.5`, Description = `Implemented authentication flow`, Billable = ON
- User clicks **"Submit"**

**Then**:
- HTTP 201 response from `POST /api/time-logs`
- A new record exists in `time_logs` table with: `task_id = TASK-101`, `user_id = team_member_01`, `hours = 3.5`, `status = pending_approval`, `is_billable = true`
- A success toast notification appears: "Time log submitted for approval"
- The Time Logs tab shows the new entry
- `project_manager_01` (PM of `proj-001`) receives an in-app notification

---

### TC-TP2-002: Manual Time Entry — Validation (Zero Hours)

**Category**: Validation | **Priority**: P1 | **AC Reference**: AC-TP2-01, Edge Cases

**Given**:
- User `team_member_01` is on the "+ Log Time" modal for `TASK-101`

**When**:
- User enters Hours = `0` and clicks **"Submit"**

**Then**:
- Form submission is blocked (button disabled or error on submit)
- Inline validation error shown: "Hours must be at least 0.25 (15 minutes)"
- No record is created in `time_logs` table
- No notification is sent

---

### TC-TP2-003: Manual Time Entry — Validation (Exceeds 24 Hours)

**Category**: Validation | **Priority**: P1 | **AC Reference**: AC-TP2-01, Edge Cases

**Given**:
- User `team_member_01` is on the "+ Log Time" modal

**When**:
- User enters Hours = `25` and clicks **"Submit"**

**Then**:
- Inline validation error: "Hours cannot exceed 24 for a single entry"
- No database record created

---

### TC-TP2-004: Manual Time Entry — Date Restriction (Past >30 Days)

**Category**: Validation | **Priority**: P1 | **AC Reference**: AC-TP2-12

**Given**:
- User `team_member_01` is on the "+ Log Time" modal

**When**:
- User selects a date 31 days ago and attempts to submit

**Then**:
- Validation error: "You cannot log time for dates more than 30 days in the past"
- Submission blocked

---

### TC-TP2-005: Manual Time Entry — Date Restriction (Future Date)

**Category**: Validation | **Priority**: P1 | **AC Reference**: AC-TP2-12

**Given**:
- User `team_member_01` is on the "+ Log Time" modal

**When**:
- User selects tomorrow's date and submits

**Then**:
- Validation error: "You cannot log time for future dates"
- Submission blocked

---

### TC-TP2-006: Timer Start — Happy Path

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-02

**Given**:
- User `team_member_01` is on `TASK-101` detail page
- No active timer exists for `team_member_01` in Redis

**When**:
- User clicks **"▶ Start Timer"**

**Then**:
- `POST /api/time-logs/timer/start` returns HTTP 200
- Redis key `timer:team_member_01:TASK-101` is created with fields: `started_at`, `elapsed_seconds = 0`, `status = running`
- Timer widget displays `00:00:00` and starts counting
- Start button changes to "Pause" and "Stop" buttons
- Page title/favicon shows an animated indicator (timer active)

---

### TC-TP2-007: Timer Pause & Resume

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-03

**Given**:
- Timer is running for `team_member_01` on `TASK-101` with elapsed time ~2 minutes

**When**:
- User clicks **"⏸ Pause"**

**Then**:
- `POST /api/time-logs/timer/pause` returns HTTP 200
- Redis key updated: `status = paused`, `elapsed_seconds` = actual elapsed seconds
- Widget displays the frozen elapsed time with a blinking pause indicator

**And When**:
- User clicks **"▶ Resume"**

**Then**:
- `POST /api/time-logs/timer/resume` returns HTTP 200
- Timer resumes counting from the saved `elapsed_seconds`
- Redis key updated: `status = running`, `resumed_at` = current timestamp

---

### TC-TP2-008: Timer Stop & Submit

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-04

**Given**:
- Timer has been running for `team_member_01` on `TASK-101` for 1 hour 30 minutes 45 seconds

**When**:
- User clicks **"⏹ Stop"**

**Then**:
- Timer stops
- Review & Submit modal appears with: Hours = `1.51` (rounded to 2 decimal places), Date = today (pre-filled)
- User adds description: `Backend API development`
- User clicks **"Submit"**
- `POST /api/time-logs` returns HTTP 201
- Time log created in DB with `hours = 1.51`, `source = timer`
- Redis key `timer:team_member_01:TASK-101` is deleted
- Timer widget resets to idle state

---

### TC-TP2-009: Only One Active Timer Per User

**Category**: Business Rule | **Priority**: P1 | **AC Reference**: AC-TP2-05

**Given**:
- Timer is actively running for `team_member_01` on `TASK-101`

**When**:
- User navigates to `TASK-102` and clicks **"▶ Start Timer"**

**Then**:
- Confirmation dialog appears: "You have an active timer on [TASK-101: Implement Auth]. Starting this timer will pause that one. Continue?"
- If user clicks **"Cancel"**: Nothing changes; TASK-101 timer remains running
- If user clicks **"Continue"**: TASK-101 timer is paused; TASK-102 timer starts

---

### TC-TP2-010: Timer Auto-Stop After 12 Hours

**Category**: Business Rule | **Priority**: P1 | **AC Reference**: Edge Cases

**Given**:
- Timer has been running for `team_member_01` on `TASK-101` for 12 hours

**When**:
- The system scheduled job runs (every 15 minutes) and detects elapsed time ≥ 43200 seconds

**Then**:
- Timer is automatically stopped
- A time log draft is created in DB with `status = draft` and `source = auto_stopped`
- In-app notification sent to user: "Your timer on [TASK-101] was auto-stopped after 12 hours. Please review and submit."
- Redis key is cleaned up

---

### TC-TP2-011: Approval Workflow — Project Manager Approves Log

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-06

**Given**:
- Time log `TL-001` exists with `status = pending_approval`, `user_id = team_member_01`, `task_id = TASK-101`, `hours = 3.5`
- `project_manager_01` is the PM of the project containing `TASK-101`

**When**:
- `project_manager_01` navigates to `/time-approvals`
- Clicks **"Approve"** on `TL-001`

**Then**:
- `PATCH /api/time-logs/TL-001/approve` returns HTTP 200
- `TL-001` status updated to `approved` in DB
- Audit trail entry created: `{action: "approved", performed_by: "project_manager_01", timestamp: now}`
- `TimeLogUpdated` domain event published with payload: `{timeLogId, userId, taskId, projectId, hours, approvedAt, isApproved: true}`
- `team_member_01` receives in-app notification: "Your 3.5h log for [TASK-101] was approved"
- Log disappears from pending queue; appears in approved logs view

---

### TC-TP2-012: Approval Workflow — Project Manager Rejects Log

**Category**: Functional | **Priority**: P0 | **AC Reference**: AC-TP2-07

**Given**:
- Time log `TL-002` exists with `status = pending_approval`
- `project_manager_01` is the approver

**When**:
- PM clicks **"Reject"** on `TL-002`
- Rejection modal appears; PM enters reason: `"Hours seem too high for this task scope"`
- PM confirms rejection

**Then**:
- `PATCH /api/time-logs/TL-002/reject` returns HTTP 200
- `TL-002.status` = `rejected`, `rejection_reason` stored
- Audit trail entry created
- `team_member_01` notified: "Your time log for [TASK-102] was rejected: 'Hours seem too high...'"

---

### TC-TP2-013: Rejection Without Reason — Blocked

**Category**: Validation | **Priority**: P1 | **AC Reference**: AC-TP2-07

**Given**:
- PM is in the rejection modal for `TL-002`

**When**:
- PM leaves rejection reason empty or enters fewer than 10 characters
- PM clicks **"Confirm Rejection"**

**Then**:
- Validation error: "Rejection reason must be at least 10 characters"
- Rejection not submitted
- Log remains in `pending_approval` status

---

### TC-TP2-014: Resubmission After Rejection

**Category**: Functional | **Priority**: P1 | **AC Reference**: AC-TP2-08

**Given**:
- Log `TL-002` has `status = rejected`
- `team_member_01` is viewing the rejected log

**When**:
- User clicks **"Edit & Resubmit"**
- Edits hours from `8.0` to `5.5` and updates description
- Clicks **"Resubmit"**

**Then**:
- Original `TL-002` is archived (not deleted): `status = archived`, `archived_at` timestamp set
- New log `TL-003` created with `status = pending_approval`, referencing `parent_log_id = TL-002`
- `project_manager_01` receives notification of resubmission
- Audit trail shows full history: original → rejected → resubmitted

---

### TC-TP2-015: HR Integration Event — TimeLogUpdated Published

**Category**: Integration | **Priority**: P0 | **AC Reference**: AC-TP2-09

**Given**:
- Time log `TL-001` is in `pending_approval` state

**When**:
- PM approves `TL-001`

**Then**:
- Within 5 seconds, `TimeLogUpdated` event is published to the MediatR event bus
- HR module event handler is invoked with correct payload:
  ```json
  {
    "eventType": "TimeLogUpdated",
    "timeLogId": "TL-001",
    "userId": "team_member_01",
    "taskId": "TASK-101",
    "projectId": "proj-001",
    "hours": 3.5,
    "date": "2026-07-02",
    "isApproved": true,
    "isBillable": true,
    "approvedAt": "2026-07-02T08:30:00Z",
    "approvedBy": "project_manager_01"
  }
  ```
- HR Productivity Analytics module updates `employee_time_records` for the period
- Event delivery is confirmed (no retry triggered)

---

### TC-TP2-016: RBAC — Team Member Cannot Access Others' Logs

**Category**: Security/Authorization | **Priority**: P0 | **AC Reference**: AC-TP2-10

**Given**:
- `team_member_01` and `team_member_02` are both on `proj-001`
- `team_member_02` has submitted log `TL-010`

**When**:
- `team_member_01` makes direct API call: `GET /api/time-logs/TL-010`

**Then**:
- HTTP 403 Forbidden response
- Error: `{"error": "FORBIDDEN", "message": "You do not have permission to view this resource"}`
- No data returned

---

### TC-TP2-017: RBAC — PM Cannot Approve Own Time Log

**Category**: Business Rule / Security | **Priority**: P0 | **AC Reference**: BR-TP2-03

**Given**:
- `project_manager_01` has submitted their own time log `TL-PM-001`
- Log is in `pending_approval` status

**When**:
- `project_manager_01` attempts to approve `TL-PM-001` via `PATCH /api/time-logs/TL-PM-001/approve`

**Then**:
- HTTP 422 Unprocessable Entity response
- Error: `{"error": "SELF_APPROVAL_NOT_ALLOWED", "message": "You cannot approve your own time log"}`
- Log status remains `pending_approval`
- Audit trail entry created for the failed attempt

---

### TC-TP2-018: Timer State Persistence Across Browser Sessions

**Category**: Functional | **Priority**: P1 | **AC Reference**: AC-TP2-02

**Given**:
- `team_member_01` started a timer on `TASK-101` (elapsed: 45 minutes)
- User closes the browser completely

**When**:
- User reopens the browser and navigates back to `TASK-101`

**Then**:
- Timer widget shows the correct elapsed time (~45 min + time since browser closed, if paused or running)
- Redis key `timer:team_member_01:TASK-101` still exists with correct state
- User can continue to pause/stop/submit as normal

---

### TC-TP2-019: Approved Log Cannot Be Edited

**Category**: Business Rule | **Priority**: P1 | **AC Reference**: BR-TP2-04

**Given**:
- Time log `TL-001` has `status = approved`

**When**:
- `team_member_01` attempts `PUT /api/time-logs/TL-001` with modified hours

**Then**:
- HTTP 422 response
- Error: `{"error": "IMMUTABLE_LOG", "message": "Approved time logs cannot be edited"}`
- No changes made to DB

---

### TC-TP2-020: HR Module Unavailable — Event Retry

**Category**: Resilience | **Priority**: P1 | **AC Reference**: AC-TP2-09, Edge Cases

**Given**:
- PM approves log `TL-005`
- HR module event handler is simulated as unavailable (throws exception)

**When**:
- `TimeLogUpdated` event is published

**Then**:
- First delivery attempt fails; error logged
- Retry with exponential backoff: 5s → 15s → 45s → 2min → 5min
- After 5 failed retries, event is moved to dead-letter queue in Redis (`dlq:TimeLogUpdated`)
- Alert fired to system admin
- Approval status remains `approved`; no rollback

---

## 3. Performance Testing Requirements

### Scenario P-01: Concurrent Time Log Submissions

**Tool**: k6  
**Objective**: Validate system handles simultaneous submissions without data corruption

| Parameter | Value |
|-----------|-------|
| Concurrent users | 100 |
| Ramp-up time | 30 seconds |
| Test duration | 5 minutes |
| Target endpoint | `POST /api/time-logs` |

**Acceptance Criteria**:
- 95th percentile response time ≤ 500ms
- 0 duplicate or corrupted records
- Error rate < 0.1%

### Scenario P-02: Timer Heartbeat Throughput (Redis)

**Tool**: k6  
**Objective**: Simulate 500 concurrent active timers syncing heartbeats every 5 seconds

| Parameter | Value |
|-----------|-------|
| Concurrent timers | 500 |
| Heartbeat interval | 5 seconds |
| Test duration | 3 minutes |

**Acceptance Criteria**:
- Redis write latency p95 ≤ 10ms
- No dropped heartbeat events
- Redis memory usage stable (no unbounded growth)

### Scenario P-03: Approval Queue Load

**Tool**: k6  
**Objective**: Simulate 10 PMs simultaneously approving from a queue of 1,000 pending logs

| Parameter | Value |
|-----------|-------|
| Concurrent PMs | 10 |
| Queue size | 1,000 logs |
| Duration | Until queue empty |

**Acceptance Criteria**:
- No approval processed twice
- Optimistic locking prevents race conditions
- 0 duplicate `TimeLogUpdated` events

---

## 4. Security Testing

### S-01: Insecure Direct Object Reference (IDOR)
- Attempt to access `GET /api/time-logs/{id}` with a valid ID belonging to another user
- **Expected**: HTTP 403 — IDOR blocked

### S-02: Horizontal Privilege Escalation
- `team_member_01` attempts to call `PATCH /api/time-logs/{id}/approve`
- **Expected**: HTTP 403 — Only `project_manager` or `admin` role allowed

### S-03: Injection via Description Field
- Submit time log with description: `'; DROP TABLE time_logs; --`
- **Expected**: Field sanitized; no SQL execution; stored as literal string

### S-04: Timer Manipulation via Direct Redis Write
- Attempt to directly manipulate timer elapsed time via unauthorized Redis key modification (simulated)
- **Expected**: Redis is not publicly accessible; only backend service can write timer keys. All timer operations go through authenticated API.

### S-05: JWT Token Expiry During Timer
- Start timer with valid JWT; wait for token expiry (simulate with short-lived token); attempt to stop timer
- **Expected**: HTTP 401; user must re-authenticate; timer state in Redis preserved for 24 hours so user can stop timer after re-login

### S-06: Mass Assignment Attack
- Submit `POST /api/time-logs` with extra fields: `{"status": "approved", "approvedBy": "hacker"}`
- **Expected**: Status field ignored; log created with `status = pending_approval` regardless of payload

---

## 5. Acceptance Criteria Verification Mapping

| Acceptance Criteria | Test Cases |
|--------------------|------------|
| AC-TP2-01: Manual time entry | TC-TP2-001, TC-TP2-002, TC-TP2-003 |
| AC-TP2-02: Timer start | TC-TP2-006 |
| AC-TP2-03: Timer pause & resume | TC-TP2-007 |
| AC-TP2-04: Timer stop & submit | TC-TP2-008 |
| AC-TP2-05: One active timer per user | TC-TP2-009 |
| AC-TP2-06: Approval — approve | TC-TP2-011 |
| AC-TP2-07: Approval — reject | TC-TP2-012, TC-TP2-013 |
| AC-TP2-08: Resubmission | TC-TP2-014 |
| AC-TP2-09: HR integration | TC-TP2-015, TC-TP2-020 |
| AC-TP2-10: Visibility scoping | TC-TP2-016 |
| AC-TP2-11: Billable flag | TC-TP2-001 (billable=ON) |
| AC-TP2-12: Date restriction | TC-TP2-004, TC-TP2-005 |
| BR-TP2-03: No self-approval | TC-TP2-017 |
| BR-TP2-04: Immutable approved logs | TC-TP2-019 |
| Edge: Auto-stop at 12h | TC-TP2-010 |
| Edge: Browser session persistence | TC-TP2-018 |

---

## 6. Regression Test Suite

After any change to the Time Tracking module, the following regression tests **must pass**:

1. TC-TP2-001 (Manual entry happy path)
2. TC-TP2-006 (Timer start)
3. TC-TP2-007 (Timer pause/resume)
4. TC-TP2-008 (Timer stop/submit)
5. TC-TP2-011 (Approval happy path)
6. TC-TP2-015 (HR event published)
7. TC-TP2-016 (RBAC IDOR protection)
8. TC-TP2-017 (No self-approval)

---

## 7. Test Data Requirements

### Users (Pre-seeded)

| User ID | Name | Role | Project |
|---------|------|------|---------|
| `user-tm-01` | Budi Santoso | `team_member` | proj-001 |
| `user-tm-02` | Siti Rahma | `team_member` | proj-001 |
| `user-pm-01` | Ahmad Fauzi | `project_manager` | proj-001 |
| `user-hr-01` | Dewi Lestari | `hr_admin` | N/A |
| `user-admin` | System Admin | `admin` | All |

### Projects & Tasks (Pre-seeded)

| ID | Type | Name | Status | Assigned To |
|----|------|------|--------|-------------|
| `proj-001` | Project | Website Redesign PT Maju | `active` | user-pm-01 |
| `TASK-101` | Task | Implement Auth Flow | `in_progress` | user-tm-01 |
| `TASK-102` | Task | Database Schema Design | `in_progress` | user-tm-02 |
| `TASK-103` | Task | Archived Task | `archived` | user-tm-01 |

### Time Logs (Pre-seeded for approval workflow tests)

| Log ID | Task | User | Hours | Status | Date |
|--------|------|------|-------|--------|------|
| `TL-001` | TASK-101 | user-tm-01 | 3.5 | pending_approval | today |
| `TL-002` | TASK-102 | user-tm-02 | 8.0 | pending_approval | today |
| `TL-003` | TASK-101 | user-tm-01 | 2.0 | approved | yesterday |
| `TL-004` | TASK-102 | user-tm-02 | 4.5 | rejected | yesterday |

---

*Document Version: 1.0 | Generated: 2026-07-02 | Author: FluxGrid SDD Agent*
