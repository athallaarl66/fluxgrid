# task-project02-prod.md
# Production Requirements — TP-2: Time Tracking
## FluxGrid ERP | Module: Task & Project Management (TaskProject)

---

## 1. Feature Overview

| Attribute         | Detail                                                                 |
|-------------------|------------------------------------------------------------------------|
| **User Story ID** | TP-2                                                                   |
| **Feature Name**  | Time Tracking                                                          |
| **Module**        | Task & Project Management (`TaskProject`)                              |
| **Priority**      | Must Have                                                              |
| **Epic**          | Project Execution & Effort Visibility                                  |
| **Release Target**| MVP v1.0                                                               |
| **Last Updated**  | 2026-07-02                                                             |

### User Story

> **As a** Team Member,  
> **I want to** log time spent on tasks,  
> **So that** project effort can be tracked and reported accurately.

---

## 2. Business Value & ROI

### Business Problem
Indonesian project-based enterprises (construction, consulting, IT services) currently track effort through manual spreadsheets or verbal reporting. This results in:
- **Billing inaccuracy**: Untracked billable hours lead to revenue leakage of an estimated 8–15% per project.
- **Productivity opacity**: Managers cannot assess real-time team workload or identify over/under-allocated members.
- **HR disconnect**: Payroll and productivity analytics in the HR module receive no authoritative effort data, forcing manual reconciliation.

### Solution Value
The Time Tracking feature introduces a structured, auditable time entry system tightly integrated with the HR Productivity Analytics module. It enables:
- **Accurate billing**: All billable time is captured at the task level with approval workflow.
- **Real-time visibility**: Project managers see live effort burn vs. estimates.
- **Automated HR sync**: Approved time logs fire a `TimeLogUpdated` domain event consumed by the HR module.

### ROI Indicators
| KPI | Baseline (Without Feature) | Target (With Feature) |
|-----|----------------------------|-----------------------|
| Billable hour capture rate | ~75% | ≥95% |
| Time-to-payroll-data reconciliation | 3–5 days manual | <1 hour automated |
| Manager effort report generation time | 2–4 hours/week | <5 minutes/week |
| Employee time-entry compliance rate | ~60% | ≥90% |

---

## 3. Success Metrics

| Metric | Definition | Target |
|--------|------------|--------|
| Daily Active Loggers | Unique users submitting ≥1 time log per working day | ≥80% of active project members |
| Timer Usage Rate | % of time logs created via timer vs. manual entry | ≥50% within 60 days of launch |
| Approval SLA | % of pending logs reviewed within 24 hours | ≥85% |
| HR Sync Success Rate | % of `TimeLogUpdated` events successfully consumed by HR module | ≥99.5% |
| Time Log Accuracy | % of logs not edited after submission | ≥90% |
| System Availability | Uptime of time-tracking endpoints | ≥99.9% |

---

## 4. User Personas

### Persona 1: Team Member (Primary Actor)

| Attribute | Detail |
|-----------|--------|
| **Role** | Software Developer / Consultant / Field Engineer |
| **RBAC Role** | `team_member` |
| **Goals** | Quickly log time without friction; track personal effort per task |
| **Pain Points** | Forgetting to log time; manual entry is tedious; no live timer |
| **Behavior** | Uses both mobile and desktop; often logs at end of day |
| **Tech Comfort** | Moderate to high |

### Persona 2: Project Manager (Approver)

| Attribute | Detail |
|-----------|--------|
| **Role** | Project Manager / Team Lead |
| **RBAC Role** | `project_manager` |
| **Goals** | Review and approve team time logs; monitor effort vs. plan |
| **Pain Points** | Approving inaccurate logs wastes time; no context on what task was done |
| **Behavior** | Reviews pending approvals morning and end-of-day |
| **Tech Comfort** | High |

### Persona 3: HR Administrator (Consumer)

| Attribute | Detail |
|-----------|--------|
| **Role** | HR Payroll / Productivity Analyst |
| **RBAC Role** | `hr_admin` |
| **Goals** | Receive accurate approved effort data for productivity reports and payroll |
| **Pain Points** | Receiving incomplete or wrong data from project teams |
| **Behavior** | Accesses HR module; does not directly interact with TaskProject |
| **Tech Comfort** | Moderate |

---

## 5. User Journey

### Journey A: Manual Time Entry

**Trigger**: Team Member completes work on a task and wants to record hours.

| Step | Actor | Action | System Response |
|------|-------|--------|-----------------|
| 1 | Team Member | Opens Task Detail page for the assigned task | Displays task info + Time Log tab |
| 2 | Team Member | Clicks **"+ Log Time"** button | Opens Time Entry modal/drawer |
| 3 | Team Member | Fills in: Date, Hours (decimal, e.g. `2.5`), Description (optional), Billable toggle | Form validates in real-time |
| 4 | Team Member | Clicks **"Submit"** | Time log saved with status `pending_approval`; confirmation toast shown |
| 5 | System | Emits internal notification | Project Manager receives in-app notification of new pending log |
| 6 | Project Manager | Opens **"Time Approval"** page | Sees list of pending time logs with task context |
| 7 | Project Manager | Reviews log; clicks **"Approve"** or **"Reject"** (with reason) | Log status updated to `approved` or `rejected` |
| 8 | System | On approval | Emits `TimeLogUpdated` domain event → HR Productivity Analytics |
| 9 | Team Member | Receives in-app notification | Informed of approval/rejection status |

### Journey B: Timer-Based Time Entry

**Trigger**: Team Member starts working on a task and wants to track time live.

| Step | Actor | Action | System Response |
|------|-------|--------|-----------------|
| 1 | Team Member | Opens Task Detail page | Sees **Timer Widget** (idle state) |
| 2 | Team Member | Clicks **▶ Start Timer** | Timer begins counting (HH:MM:SS); state persisted in Redis with TTL; timer widget updates every second |
| 3 | Team Member | Optionally clicks **⏸ Pause** | Timer pauses; elapsed time saved to Redis; widget shows paused indicator |
| 4 | Team Member | Clicks **▶ Resume** | Timer resumes from paused time |
| 5 | Team Member | Clicks **⏹ Stop** | Timer stops; Review & Submit modal appears pre-filled with elapsed time |
| 6 | Team Member | Adds description, confirms billable status | Clicks **"Submit"** |
| 7 | System | Saves time log with `pending_approval` status | Timer widget resets to idle; confirmation shown |
| 8–9 | (Same as Journey A steps 6–9) | — | — |

### Journey C: Time Log Rejection & Resubmission

| Step | Actor | Action | System Response |
|------|-------|--------|-----------------|
| 1 | Project Manager | Rejects a time log with reason: "Hours seem inflated" | Log status set to `rejected`; Team Member notified |
| 2 | Team Member | Opens notification; views rejected log | Can see rejection reason; "Edit & Resubmit" button available |
| 3 | Team Member | Edits hours/description; clicks **"Resubmit"** | Creates new log version (previous version archived); status reset to `pending_approval` |
| 4 | Project Manager | Reviews corrected log | Can approve or reject again |

---

## 6. Detailed Acceptance Criteria

### AC-TP2-01: Manual Time Entry
- **Given** a Team Member is viewing a task detail page they are assigned to,
- **When** they click "+ Log Time" and fill the form with valid date, hours (0.25–24), and click Submit,
- **Then** a time log record is created with status `pending_approval`, a success toast is shown, and the time log appears in the task's time log list.

### AC-TP2-02: Timer Start
- **Given** a Team Member is on a task detail page with no active timer for that task,
- **When** they click "Start Timer",
- **Then** the timer widget begins counting from 00:00:00, the button changes to "Pause / Stop", and the timer state is persisted in Redis keyed to `timer:{userId}:{taskId}`.

### AC-TP2-03: Timer Pause & Resume
- **Given** the timer is running,
- **When** the Team Member clicks "Pause",
- **Then** the counter freezes and accumulated time is saved. When they click "Resume", the timer continues from the saved time.

### AC-TP2-04: Timer Stop & Submit
- **Given** the timer has elapsed time ≥ 1 minute,
- **When** the Team Member clicks "Stop",
- **Then** a pre-filled submission modal appears with hours rounded to 2 decimal places. On Submit, a time log record is created and the timer state is cleared from Redis.

### AC-TP2-05: Only One Active Timer Per User
- **Given** a Team Member has an active timer on Task A,
- **When** they attempt to start a timer on Task B,
- **Then** a confirmation dialog warns them that the Task A timer will be paused, and they must confirm before proceeding.

### AC-TP2-06: Approval Workflow — Approve
- **Given** a Project Manager opens the Time Approval queue with ≥1 pending log,
- **When** they click "Approve" on a log,
- **Then** the log status changes to `approved`, the Team Member receives a notification, and a `TimeLogUpdated` domain event is published.

### AC-TP2-07: Approval Workflow — Reject
- **Given** a Project Manager opens the Time Approval queue,
- **When** they click "Reject" and provide a mandatory rejection reason (min 10 characters),
- **Then** the log status changes to `rejected`, the rejection reason is stored, and the Team Member is notified.

### AC-TP2-08: Resubmission After Rejection
- **Given** a Team Member views a rejected time log,
- **When** they click "Edit & Resubmit" and modify the log,
- **Then** the original log version is archived (audit trail preserved), a new log with `pending_approval` is created, and the manager is notified.

### AC-TP2-09: HR Integration
- **Given** a time log is approved by a Project Manager,
- **When** the `TimeLogUpdated` event is published,
- **Then** the HR Productivity Analytics module receives the event within 30 seconds and updates the employee's effort record for the relevant period.

### AC-TP2-10: Time Log Visibility
- **Given** a Team Member views their task detail,
- **When** they click the "Time Logs" tab,
- **Then** they see all their own time logs for that task (date, hours, status, description). They cannot see other members' logs unless they have `project_manager` or `admin` role.

### AC-TP2-11: Billable Flag
- **Given** a time log entry form,
- **When** the user toggles the "Billable" switch,
- **Then** the time log is stored with `is_billable = true/false`. This flag must be factored into project billing reports.

### AC-TP2-12: Date Restriction
- **Given** the manual time entry form,
- **When** a user enters a date more than 30 days in the past or any future date,
- **Then** a validation error is shown and submission is blocked.

---

## 7. Edge Cases & Constraints

| Edge Case | Expected Behavior |
|-----------|-------------------|
| User submits 0 hours | Validation error: "Hours must be at least 0.25 (15 minutes)" |
| User submits >24 hours | Validation error: "Hours cannot exceed 24 for a single entry" |
| Timer running when browser tab closes | Redis TTL keeps state; on reopening the task, timer resumes from saved state |
| Timer running >12 hours without stop | System auto-stops timer, sends notification: "Your timer has been auto-stopped after 12 hours" |
| Network loss during timer | Timer continues client-side; on reconnect, heartbeat syncs elapsed time to Redis |
| Duplicate log for same date+task | Warning shown (not blocked): "You already have X hours logged for this task on this date" |
| Project Manager approves own time log | Blocked by business rule; system shows error: "You cannot approve your own time log" |
| HR module unavailable | `TimeLogUpdated` event queued in Redis; retried with exponential backoff up to 5 times |
| Task is archived/deleted | Existing time logs preserved; no new logs can be added to archived tasks |
| User role changes mid-approval | System re-checks permissions at approval time; stale tokens rejected |

---

## 8. Dependencies

### Internal Module Dependencies
| Module | Dependency Type | Detail |
|--------|----------------|--------|
| Task Management (TP-1) | Hard prerequisite | Time logs must be linked to an existing Task; Task ID is mandatory FK |
| User & RBAC Module | Hard prerequisite | Role-based access enforcement; user identity for `logged_by` field |
| Notification Module | Soft dependency | In-app notifications for approval/rejection events |
| HR Productivity Analytics | Event consumer | Receives `TimeLogUpdated` event; must implement event handler |

### Infrastructure Dependencies
| Component | Purpose |
|-----------|---------|
| PostgreSQL (Neon) | Persistent storage for time logs and approval history |
| Upstash Redis | Timer state persistence (key-value with TTL) |
| Domain Event Bus (MediatR) | Publishing `TimeLogUpdated` events |

---

## 9. Out of Scope (TP-2 v1.0)

The following items are explicitly **excluded** from this user story:

- **Automatic billing invoice generation** from time logs (planned for Billing module)
- **GPS-based time tracking** or location check-in (field operations feature, future release)
- **Bulk time entry** (logging multiple tasks in one submission)
- **Time log export to Excel/PDF** (reporting module feature)
- **Integration with third-party tools** (Jira, Toggl, Harvest) — future enhancement
- **Payroll calculation** from time logs (HR Payroll module responsibility)
- **Client-facing time log portal** (Client Portal module, v2.0)
- **Overtime calculation and flagging** (HR Payroll module v2.0)
- **Time budget vs. actual comparison at project level** (project reporting TP-7)

---

## 10. Business Rules

| Rule ID | Rule Description |
|---------|-----------------|
| BR-TP2-01 | A time log must be linked to exactly one Task and one User |
| BR-TP2-02 | Only the log owner (Team Member) or an Admin can create/edit/delete their own time log |
| BR-TP2-03 | A Project Manager cannot approve their own time logs |
| BR-TP2-04 | Approved time logs cannot be edited; they must be rejected first |
| BR-TP2-05 | Time logs older than 30 days cannot be submitted via manual entry |
| BR-TP2-06 | Only `approved` time logs trigger the HR integration event |
| BR-TP2-07 | A user can have at most one active (running) timer at a time across all tasks |
| BR-TP2-08 | Rejection requires a mandatory reason (minimum 10 characters) |
| BR-TP2-09 | An audit trail entry is created for every status transition of a time log |
| BR-TP2-10 | Timer auto-stops after 12 continuous hours and notifies the user |

---

## 11. Regulatory & Compliance Notes

- **Indonesian Labor Law (UU No. 13/2003)**: Working hours are capped at 40 hours/week. Time logs may be used as evidence; audit trail immutability is required.
- **Data Retention**: Time log records must be retained for a minimum of 5 years per Indonesian financial/HR regulation.
- **PDPA Compliance**: Time log data constitutes personal productivity data; access must be restricted by RBAC and logged in the audit trail.

---

*Document Version: 1.0 | Generated: 2026-07-02 | Author: FluxGrid SDD Agent*
