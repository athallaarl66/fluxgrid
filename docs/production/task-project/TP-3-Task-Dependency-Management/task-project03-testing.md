# task-project03-testing.md — Testing Scenarios: TP-3 Task Dependency Management

> **Module:** Task & Project Management (TaskProject)
> **Story ID:** TP-3
> **Test Author:** QA Team / FluxGrid ERP
> **Last Updated:** 2026-07-02
> **Framework:** Playwright (E2E), Vitest (Unit/Integration)

---

## 1. Test Strategy Overview

### Approach

| Layer | Tool | Scope |
|---|---|---|
| **Unit** | Vitest | CPM algorithm, topological sort, cycle detection logic, domain event handlers |
| **Integration** | Vitest + Drizzle ORM (test DB) | Repository layer, API route handlers, dependency create/delete flows |
| **E2E** | Playwright | Full user journeys: add dependency, view graph, blocking behavior, unblock on completion, critical path notification |
| **Performance** | Playwright + k6 | Graph load time with 100–500 task nodes; CPM calculation time under load |
| **Security** | Playwright + manual | RBAC enforcement, input injection, unauthorized access |
| **Accessibility** | Playwright + axe-core | Graph keyboard navigation, ARIA roles, screen reader compatibility |

### Test Environment

| Setting | Value |
|---|---|
| **Base URL** | `http://localhost:3000` |
| **Test Database** | PostgreSQL (Neon) — isolated test schema |
| **Redis** | Upstash Redis (test instance) |
| **Seed Data** | `seed/task-project03.ts` — provides test projects, tasks, users |
| **Test Users** | `pm_user@test.com` (PM role), `member_user@test.com` (MEMBER role), `admin_user@test.com` (ADMIN role) |

### Coverage Targets

| Category | Target |
|---|---|
| Unit test coverage (algorithm layer) | ≥ 95% |
| Integration test coverage (API routes) | ≥ 85% |
| E2E critical path coverage | 100% of ACs |
| Performance budget compliance | 100% (all within defined thresholds) |

---

## 2. Test Data Requirements

### Pre-conditions (Seed Data)

```typescript
// Seed project: "Data Center Migration"
project: {
  id: "proj-dc-001",
  name: "Data Center Migration",
  status: "ACTIVE",
  owner: pm_user
}

// Tasks (all within proj-dc-001)
tasks: [
  { id: "task-001", name: "Procure Hardware", status: "IN_PROGRESS", estimated_hours: 24 },
  { id: "task-002", name: "Install Server Rack", status: "TO_DO", estimated_hours: 8 },
  { id: "task-003", name: "Configure Network", status: "TO_DO", estimated_hours: 16 },
  { id: "task-004", name: "Install OS", status: "TO_DO", estimated_hours: 4 },
  { id: "task-005", name: "Deploy Application", status: "TO_DO", estimated_hours: 6 },
  { id: "task-006", name: "User Acceptance Test", status: "TO_DO", estimated_hours: 12 },
  { id: "task-007", name: "Go Live", status: "TO_DO", estimated_hours: 2 }
]

// Initial dependency chain (for blocking & CPM tests)
dependencies: [
  { predecessor: "task-001", successor: "task-002" }, // Procure → Install Rack
  { predecessor: "task-002", successor: "task-003" }, // Install Rack → Configure Network
  { predecessor: "task-002", successor: "task-004" }, // Install Rack → Install OS
  { predecessor: "task-003", successor: "task-005" }, // Configure Network → Deploy App
  { predecessor: "task-004", successor: "task-005" }, // Install OS → Deploy App
  { predecessor: "task-005", successor: "task-006" }, // Deploy App → UAT
  { predecessor: "task-006", successor: "task-007" }  // UAT → Go Live
]
```

### Users

| User | Email | Role | Used In |
|---|---|---|---|
| Pak Rudi | `pm_rudi@test.com` | PROJECT_MANAGER | All PM-action tests |
| Bu Sari | `sari@test.com` | MEMBER | Blocking behavior tests |
| Admin | `admin@test.com` | ADMIN | Override tests |
| Guest | `guest@test.com` | VIEWER | Read-only tests |

---

## 3. Test Cases

---

### TC-TP3-001: Add Predecessor Dependency — Happy Path

**Category:** Integration / E2E
**AC Coverage:** AC-TP-3-01
**Priority:** Critical

**Preconditions:**
- User `pm_rudi` is authenticated
- Project "Data Center Migration" has tasks A (`task-001`) and B (`task-008`, isolated, no deps)

**Steps:**
1. Navigate to Task Detail for `task-008` ("Setup Monitoring")
2. Click "Dependencies" tab
3. Click "+ Add Predecessor"
4. Search "Install OS" → select `task-004`
5. Confirm selection

**Expected Results:**
- Row inserted in `task_dependencies`: `{ predecessor_id: 'task-004', successor_id: 'task-008' }`
- `task-008` status changes to `BLOCKED` (task-004 is `TO_DO`, not `DONE`)
- Dependency chip appears: "Blocked by: Install OS ✗"
- `TaskDependencyCreated` domain event fired (verified via event log)
- API response: `201 Created` with dependency object

**Cleanup:** Delete `task-008`; remove dependency row

---

### TC-TP3-002: Add Successor Dependency — Happy Path

**Category:** Integration / E2E
**AC Coverage:** AC-TP-3-01
**Priority:** High

**Preconditions:** Same project, tasks exist

**Steps:**
1. Navigate to Task Detail for `task-001` ("Procure Hardware")
2. Click "Dependencies" tab
3. Click "+ Add Successor"
4. Search and select `task-008`

**Expected Results:**
- Row inserted: `{ predecessor_id: 'task-001', successor_id: 'task-008' }`
- `task-008` status set to `BLOCKED`
- `task-001` detail panel shows "Blocking: Setup Monitoring"

---

### TC-TP3-003: Circular Dependency Detection — Direct Cycle

**Category:** Unit + E2E
**AC Coverage:** AC-TP-3-05
**Priority:** Critical

**Preconditions:** Dependency A → B already exists (`task-001` → `task-002`)

**Steps:**
1. Navigate to Task Detail for `task-001`
2. Click "+ Add Predecessor"
3. Search and select `task-002`

**Expected Results:**
- API returns `422 Unprocessable Entity`
- Response body: `{ error: "CIRCULAR_DEPENDENCY", message: "Circular dependency detected: Adding this dependency would create a cycle." }`
- Toast notification: "❌ Circular dependency detected: Adding this dependency would create a cycle."
- No row inserted in `task_dependencies`
- Unit test: `detectCycle(graph, 'task-002', 'task-001')` returns `true`

---

### TC-TP3-004: Circular Dependency Detection — Transitive Cycle

**Category:** Unit + E2E
**AC Coverage:** AC-TP-3-06
**Priority:** Critical

**Preconditions:** Chain A→B→C exists (`task-001`→`task-002`→`task-003`)

**Steps:**
1. Attempt to add `task-001` as a successor of `task-003`
2. API: `POST /api/task-dependencies { predecessor_id: 'task-003', successor_id: 'task-001' }`

**Expected Results:**
- API returns `422 Unprocessable Entity`
- Cycle path returned in error response: `"cycle_path": ["task-001", "task-002", "task-003", "task-001"]`
- No row inserted

**Unit Test Coverage:**
```typescript
describe('detectCycle - transitive', () => {
  it('detects A→B→C→A cycle', () => {
    const graph = buildAdjacencyList([
      { predecessor: 'A', successor: 'B' },
      { predecessor: 'B', successor: 'C' },
    ]);
    expect(detectCycle(graph, 'C', 'A')).toBe(true);
  });
});
```

---

### TC-TP3-005: Self-Dependency Prevention

**Category:** Unit + E2E
**AC Coverage:** AC-TP-3-07
**Priority:** High

**Steps:**
1. API: `POST /api/task-dependencies { predecessor_id: 'task-001', successor_id: 'task-001' }`

**Expected Results:**
- API returns `422`
- Message: "A task cannot depend on itself."
- UI: In the add-predecessor dropdown, the current task is excluded from search results (gray out / filtered)

---

### TC-TP3-006: Blocking Behavior — Status Transition Rejected

**Category:** E2E (Playwright)
**AC Coverage:** AC-TP-3-02
**Priority:** Critical

**Preconditions:**
- `task-002` ("Install Server Rack") is `BLOCKED` by `task-001` ("Procure Hardware")
- User `sari@test.com` (MEMBER) is assigned to `task-002`
- `task-001` status = `IN_PROGRESS`

**Steps:**
1. Login as `sari@test.com`
2. Navigate to Task Board
3. Attempt to drag `task-002` card from "Blocked" column to "In Progress" column
   - (Also test via status dropdown in Task Detail: select "In Progress")

**Expected Results:**
- Status transition rejected
- Task card returns to "Blocked" column (visual rollback)
- Toast error: "Task cannot be started: 'Procure Hardware' must be completed first."
- `task-002` status remains `BLOCKED` in database

---

### TC-TP3-007: Auto-Unblock When Single Predecessor Completes

**Category:** E2E (Playwright) + Integration
**AC Coverage:** AC-TP-3-03
**Priority:** Critical

**Preconditions:**
- `task-002` is `BLOCKED` by `task-001` only
- `task-001` status = `IN_PROGRESS`
- `sari@test.com` is assigned to `task-002`

**Steps:**
1. Login as `pm_rudi@test.com`
2. Mark `task-001` as `DONE`

**Expected Results:**
- `task-002` status transitions to `TO_DO` within 5 seconds
- `sari@test.com` receives in-app notification: "✅ Task unblocked: 'Install Server Rack' is now ready to start."
- Task card on board updates (real-time via Redis pub/sub)
- `TaskUnblocked` domain event logged

---

### TC-TP3-008: Auto-Unblock — Multi-Predecessor (All Must Be DONE)

**Category:** Integration
**AC Coverage:** AC-TP-3-04
**Priority:** High

**Preconditions:**
- `task-005` ("Deploy Application") is blocked by `task-003` (DONE) and `task-004` (IN_PROGRESS)

**Steps:**
1. Mark `task-004` as `DONE`

**Expected Results:**
- `task-005` unblocks → transitions to `TO_DO`
- Only after BOTH predecessors are `DONE`

**Negative Test:**
- Mark only `task-003` as `DONE` (task-004 still IN_PROGRESS) → `task-005` remains `BLOCKED`

---

### TC-TP3-009: Visual Dependency Graph Rendering

**Category:** E2E (Playwright)
**AC Coverage:** AC-TP-3-08, AC-TP-3-09
**Priority:** High

**Steps:**
1. Login as `pm_rudi@test.com`
2. Navigate to "Data Center Migration" project
3. Click "Dependency Graph" tab
4. Wait for graph to fully render

**Expected Results:**
- All 7 task nodes are visible on the canvas
- All 7 dependency edges are rendered as arrows
- Page load (graph render) ≤ 1.5 seconds (measured via `performance.now()`)
- Critical path nodes (task-001 → task-002 → task-003 → task-005 → task-006 → task-007) are highlighted in amber/orange
- Non-critical path nodes (task-004: Install OS) are in default blue
- Screenshot taken and compared to visual baseline

**Playwright Snippet:**
```typescript
await expect(page.locator('[data-testid="graph-canvas"]')).toBeVisible();
const nodeCount = await page.locator('[data-testid="graph-node"]').count();
expect(nodeCount).toBe(7);
const start = performance.now();
await page.waitForSelector('[data-testid="graph-node"]');
expect(performance.now() - start).toBeLessThan(1500);
```

---

### TC-TP3-010: Remove Dependency

**Category:** E2E + Integration
**AC Coverage:** AC-TP-3-10
**Priority:** High

**Preconditions:**
- `task-002` has predecessor `task-001`
- `task-001` is `IN_PROGRESS`, so `task-002` is `BLOCKED`

**Steps:**
1. Open Task Detail for `task-002`
2. Click ✕ on "Blocked by: Procure Hardware"
3. Confirm removal in dialog

**Expected Results:**
- `task_dependencies` row deleted
- `task-002` transitions from `BLOCKED` to `TO_DO` (no remaining predecessors)
- Graph edge (task-001 → task-002) removed in real-time
- `TaskDependencyRemoved` domain event emitted
- Audit trail entry created: `{ action: 'DEPENDENCY_REMOVED', actor: 'pm_rudi', task_id: 'task-002', predecessor_id: 'task-001' }`

---

### TC-TP3-011: RBAC — Member Cannot Create Dependency

**Category:** Security + E2E
**AC Coverage:** AC-TP-3-11
**Priority:** Critical

**Steps:**
1. Login as `sari@test.com` (MEMBER role)
2. API: `POST /api/task-dependencies { predecessor_id: 'task-001', successor_id: 'task-008' }`

**Expected Results:**
- HTTP 403 Forbidden
- Body: `{ error: "FORBIDDEN", message: "Insufficient permissions to manage task dependencies." }`
- UI: "+ Add Predecessor" button is disabled or hidden for MEMBER role
- No row inserted in `task_dependencies`

---

### TC-TP3-012: Critical Path Notification on Duration Change (BR-TP-003)

**Category:** Integration + E2E
**AC Coverage:** AC-TP-3-12
**Priority:** High

**Preconditions:**
- Initial critical path: task-001→task-002→task-003→task-005→task-006→task-007 (total 62h)
- `task-004` is non-critical (float = 8h)

**Steps:**
1. Login as `pm_rudi@test.com`
2. Update `task-004` estimated_hours from 4h to 16h (equal to task-003)

**Expected Results:**
- CPM recalculated within 500ms
- Now two equal critical paths (task-004 is now also critical)
- `CriticalPathChanged` domain event emitted
- In-app notification delivered to `pm_rudi` within 30 seconds
- Notification text: "⚠️ Critical Path Updated: 'Install OS' is now on the critical path. Review project schedule."
- Graph updates to show `task-004` also highlighted in amber

---

### TC-TP3-013: Duplicate Dependency Prevention

**Category:** Integration
**Priority:** Medium

**Steps:**
1. Dependency A→B already exists
2. API: `POST /api/task-dependencies { predecessor_id: 'task-001', successor_id: 'task-002' }` (duplicate)

**Expected Results:**
- HTTP 409 Conflict
- Message: "Dependency already exists between these tasks."
- No duplicate row inserted

---

### TC-TP3-014: Dependency on Completed Predecessor — No Block

**Category:** Integration
**Priority:** High

**Preconditions:**
- `task-001` status = `DONE`

**Steps:**
1. Add `task-001` as predecessor of `task-008`

**Expected Results:**
- Dependency row inserted successfully
- `task-008` is NOT blocked (predecessor is already `DONE`)
- `task-008` status remains `TO_DO`
- No "BLOCKED" badge shown for `task-008`

---

### TC-TP3-015: Graph Interaction — Hover Tooltip

**Category:** E2E
**Priority:** Medium

**Steps:**
1. Open Dependency Graph
2. Hover over graph node for "Configure Network"

**Expected Results:**
- Tooltip visible with:
  - Task name: "Configure Network"
  - Assignee: "Bu Sari"
  - Status: "TO_DO"
  - Estimated Hours: "16h"
  - Critical Path: "Yes"
  - Float: "0 days"

---

### TC-TP3-016: Cancelled Task — Skip-Through in Dependency Chain

**Category:** Integration
**Priority:** Medium

**Preconditions:**
- Chain: A→B→C. B is cancelled.

**Steps:**
1. Mark `task-002` ("Install Server Rack") as `CANCELLED`
2. Evaluate blocking status of `task-003`

**Expected Results:**
- `task-003` is NOT blocked by `task-002` (cancelled tasks are transparent)
- `task-003` remains blocked by `task-001` if `task-001` is not `DONE`
- Graph renders `task-002` as a greyed-out node with strikethrough label

---

## 4. Performance Testing

### PT-TP3-001: Graph Load Time — 100 Tasks

**Tool:** Playwright + custom timer
**Threshold:** ≤ 1.5 seconds P95

**Setup:**
- Seed project with 100 tasks and 150 dependency edges (generated programmatically)

**Test:**
1. Measure time from route navigation to `[data-testid="graph-canvas"][data-loaded="true"]`
2. Run 20 iterations; record P50, P95, P99

**Pass Criteria:** P95 ≤ 1500ms; P99 ≤ 2500ms

---

### PT-TP3-002: CPM Calculation Time — 200 Tasks

**Tool:** Vitest + `performance.now()`
**Threshold:** ≤ 500ms server-side

**Test:**
```typescript
const graph = generateProjectGraph(200, 300); // 200 tasks, 300 dependencies
const start = performance.now();
const result = calculateCriticalPath(graph);
const duration = performance.now() - start;
expect(duration).toBeLessThan(500);
```

---

### PT-TP3-003: Concurrent Dependency Creation — 50 Concurrent Requests

**Tool:** k6
**Threshold:** P95 latency ≤ 200ms; 0 errors

**Script:**
```javascript
import http from 'k6/http';
export default function() {
  http.post(`${BASE_URL}/api/task-dependencies`, JSON.stringify({
    predecessor_id: randomTaskId(),
    successor_id: randomTaskId()
  }), { headers: { 'Content-Type': 'application/json' } });
}
export const options = { vus: 50, duration: '30s' };
```

---

### PT-TP3-004: Redis Pub/Sub Propagation Latency

**Tool:** Integration test with timestamps
**Threshold:** Unblock notification delivered to subscriber within ≤ 5 seconds

**Test:**
1. Subscribe to channel `project:proj-dc-001:task-updates`
2. Mark predecessor task as `DONE`
3. Measure time until subscriber receives `task_unblocked` message

---

## 5. Security Testing

### ST-TP3-001: RBAC Enforcement — All Mutating Endpoints

| Endpoint | MEMBER Expected | VIEWER Expected | PM Expected | ADMIN Expected |
|---|---|---|---|---|
| `POST /api/task-dependencies` | 403 | 403 | 201 | 201 |
| `DELETE /api/task-dependencies/:id` | 403 | 403 | 200 | 200 |
| `GET /api/task-dependencies/graph/:projectId` | 200 | 200 | 200 | 200 |
| `GET /api/task-dependencies/critical-path/:projectId` | 200 | 200 | 200 | 200 |

---

### ST-TP3-002: SQL Injection in Task Search

**Steps:**
1. In the add-predecessor search field, enter: `'; DROP TABLE task_dependencies; --`

**Expected Results:**
- Input is parameterized via Drizzle ORM prepared statements
- No SQL error; search returns empty results
- Audit log entry for suspicious query pattern

---

### ST-TP3-003: IDOR — Access Dependencies of Another Tenant's Project

**Steps:**
1. Login as PM of Company A
2. API: `GET /api/task-dependencies/graph/proj-company-b-001`

**Expected Results:**
- HTTP 403 Forbidden (tenant isolation enforced)
- No data returned from Company B's project

---

### ST-TP3-004: Dependency Manipulation via Direct API Call

**Steps:**
1. Login as `sari@test.com` (MEMBER)
2. Craft direct `POST /api/task-dependencies` with valid JWT but MEMBER role

**Expected Results:**
- HTTP 403 regardless of JWT validity
- Role extracted from JWT claims, not client-supplied header

---

## 6. Acceptance Criteria Verification Mapping

| AC ID | Test Case(s) | Status |
|---|---|---|
| AC-TP-3-01 | TC-TP3-001, TC-TP3-002 | ⬜ Not Run |
| AC-TP-3-02 | TC-TP3-006 | ⬜ Not Run |
| AC-TP-3-03 | TC-TP3-007 | ⬜ Not Run |
| AC-TP-3-04 | TC-TP3-008 | ⬜ Not Run |
| AC-TP-3-05 | TC-TP3-003 | ⬜ Not Run |
| AC-TP-3-06 | TC-TP3-004 | ⬜ Not Run |
| AC-TP-3-07 | TC-TP3-005 | ⬜ Not Run |
| AC-TP-3-08 | TC-TP3-009 | ⬜ Not Run |
| AC-TP-3-09 | TC-TP3-009 | ⬜ Not Run |
| AC-TP-3-10 | TC-TP3-010 | ⬜ Not Run |
| AC-TP-3-11 | TC-TP3-011, ST-TP3-001 | ⬜ Not Run |
| AC-TP-3-12 | TC-TP3-012 | ⬜ Not Run |

---

## 7. Regression Test Checklist

After every related code change, verify:

- [ ] Adding a dependency does not break existing tasks' status
- [ ] CPM does not regress (result matches manual calculation on fixture projects)
- [ ] Blocking enforcement still applies after status field refactor
- [ ] Graph renders correctly with no orphan nodes after task deletion
- [ ] Redis pub/sub channel names unchanged (subscriber breakage risk)
- [ ] Domain events fire in correct order: `TaskDependencyCreated` before `TaskBlocked`

---

*Document Owner: QA Team / FluxGrid ERP*
*Linked PRD: TP-3 Task Dependency Management*
