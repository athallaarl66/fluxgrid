# task-project03-prod.md — User Story TP-3: Task Dependency Management

> **Module:** Task & Project Management (TaskProject)
> **Story ID:** TP-3
> **Priority:** Must Have
> **Status:** Ready for Development
> **Last Updated:** 2026-07-02

---

## 1. Feature Overview

Task Dependency Management enables Project Managers to define formal predecessor/successor relationships between tasks within a project. Once a dependency is established, the system enforces execution order by blocking any task whose predecessor has not yet been marked complete. The feature also includes a visual Directed Acyclic Graph (DAG) that renders real-time dependency chains, and a Critical Path Method (CPM) calculation engine that identifies the longest path of dependent tasks — the sequence that determines the project's earliest possible completion date.

This feature is critical infrastructure for professional project scheduling in FluxGrid ERP. It replaces informal, ad hoc dependency tracking (usually done in spreadsheets or verbal communication) with a structured, system-enforced mechanism that directly reduces project overruns caused by out-of-order task execution.

---

## 2. User Story

| Field | Value |
|---|---|
| **Story ID** | TP-3 |
| **As a** | Project Manager |
| **I want to** | Define task dependencies (predecessor/successor relationships) |
| **So that** | Tasks are executed in the correct order and the project completes on schedule |
| **Priority** | Must Have |
| **Epic** | Task & Project Management |
| **Business Rules** | BR-TP-001 (Dependency Blocking), BR-TP-003 (Critical Path Notification) |

---

## 3. Business Value & ROI

### Business Value

| Value Driver | Description |
|---|---|
| **Reduced Project Overruns** | Enforcing execution order eliminates the most common source of rework: tasks started before their prerequisites are ready |
| **Risk Visibility** | Critical path calculation exposes the highest-risk task chain, enabling proactive resource allocation |
| **Manager Efficiency** | Visual dependency graph eliminates the need for offline dependency tracking, saving 1–2 hours per manager per project week |
| **Audit Compliance** | System-enforced sequencing creates a defensible audit trail for project governance and ISO/CMMI compliance |
| **Stakeholder Transparency** | Real-time graph views allow stakeholders to understand project structure without attending status meetings |

### ROI Indicators

- **Reduction in Rework Rate:** Target 30% decrease in tasks that must be re-executed due to out-of-order execution within 6 months of go-live.
- **Planning Time Saved:** Project Managers report saving approximately 45 minutes per project kickoff session when dependency graph is auto-generated from task definitions.
- **On-Time Delivery Rate:** Projects using dependency enforcement target ≥ 85% on-time milestone delivery vs. baseline of ~62%.

---

## 4. Success Metrics

| Metric | Target | Measurement Method |
|---|---|---|
| Dependency creation rate | ≥ 80% of projects use dependencies within 60 days | Database count: task_dependencies rows per project |
| Graph load time (P95) | ≤ 1.5 seconds for graphs with ≤ 100 nodes | Server-side APM trace on `/api/task-dependencies/graph` |
| Circular dependency rejection rate | 100% (zero false negatives) | Automated regression suite + production error monitoring |
| Critical path accuracy | 100% match to manual CPM calculation on test projects | QA validation suite with known project fixtures |
| User adoption (graph view) | ≥ 60% of Project Managers open graph view ≥ once per week | Analytics event: `dependency_graph_viewed` |
| Dependency-blocked task rate | < 5% of blocked tasks are disputed (wrong block) | Support ticket tagging `dep-block-dispute` |

---

## 5. User Persona Details

### Primary Persona: Pak Rudi — Senior Project Manager

| Attribute | Detail |
|---|---|
| **Name** | Pak Rudi Hartono |
| **Role** | Senior Project Manager, Manufacturing ERP Division |
| **Age** | 42 |
| **Location** | Surabaya, Indonesia |
| **Experience** | 15 years project management, 6 years using ERP systems |
| **Tech Comfort** | Moderate — comfortable with web apps, prefers clear visual interfaces |
| **Team Size** | Manages 5–8 project teams simultaneously, 30–120 tasks per project |
| **Pain Points** | (1) Team members start downstream tasks before upstream blockers are done; (2) Has to manually calculate the critical path in Excel; (3) No visual overview of project dependency chains |
| **Goals** | Deliver projects on time, communicate task order clearly to team, identify bottlenecks early |
| **Device** | MacBook Pro + 27" external monitor (primary), iPad (meetings), iPhone (notifications) |
| **Languages** | Bahasa Indonesia (primary), English (technical terms) |

### Secondary Persona: Bu Sari — Team Lead

| Attribute | Detail |
|---|---|
| **Role** | Team Lead / Senior Engineer |
| **Concern** | Needs to know which tasks she is blocked on and which tasks her completion will unlock |
| **Interaction** | Views task status; sees "blocked" indicator on her task card; receives notification when her blocker is cleared |

---

## 6. Full User Journey

### Journey 1: Defining a Task Dependency

**Entry Point:** Project Manager opens a task detail page within an active project.

| Step | Actor | Action | System Response |
|---|---|---|---|
| 1 | PM | Navigates to Project > Task Board; opens Task Detail for "Install Server Rack" | Task detail panel/modal opens |
| 2 | PM | Clicks the **"Dependencies"** tab within the Task Detail panel | Dependency section expands; shows empty list with "+ Add Predecessor" and "+ Add Successor" buttons |
| 3 | PM | Clicks **"+ Add Predecessor"** | Inline search field appears: "Search tasks in this project…" |
| 4 | PM | Types "Procure Hardware" | Filtered dropdown shows matching tasks with their status badges |
| 5 | PM | Selects "Procure Hardware" (status: In Progress) | System performs cycle detection. Dependency is valid — no cycle found. |
| 6 | System | Inserts row in `task_dependencies` (predecessor: Procure Hardware → successor: Install Server Rack) | Dependency card appears in the dependency list; a `TaskDependencyCreated` domain event fires |
| 7 | System | Sets "Install Server Rack" status to `BLOCKED` because predecessor is not yet `DONE` | Task card on board shows 🔒 Blocked indicator; assignee receives in-app notification |
| 8 | PM | Views updated dependency list | Two chips: "Blocked by: Procure Hardware ✗" and successor list |

**Success Condition:** Dependency is persisted, task is blocked, domain event fired, notification sent.

---

### Journey 2: Viewing the Visual Dependency Graph

**Entry Point:** Project Manager opens the Project Detail page.

| Step | Actor | Action | System Response |
|---|---|---|---|
| 1 | PM | Navigates to Project → selects "Data Center Migration" project | Project Overview page loads |
| 2 | PM | Clicks **"Dependency Graph"** tab | Graph canvas loads; nodes for all tasks render as boxes; edges (arrows) show dependencies |
| 3 | System | CPM calculation runs server-side | Tasks on the critical path are highlighted in amber/orange; non-critical paths in default color |
| 4 | PM | Hovers over node "Install Server Rack" | Tooltip shows: Task name, assignee, estimated duration, status, "Critical Path: Yes" |
| 5 | PM | Clicks node "Install Server Rack" | Right-side drawer opens with full Task Detail |
| 6 | PM | Uses zoom controls (scroll wheel / pinch-to-zoom) | Graph zooms in/out; minimap updates |
| 7 | PM | Drags graph canvas (pan) | Graph pans; node positions stable |
| 8 | PM | Clicks **"Export PNG"** | Graph canvas exported as PNG file download |

**Success Condition:** Graph renders within 1.5s for ≤ 100 nodes, critical path highlighted correctly.

---

### Journey 3: Dependency Blocking Enforcement (BR-TP-001)

**Entry Point:** Team member (Bu Sari) attempts to start a blocked task.

| Step | Actor | Action | System Response |
|---|---|---|---|
| 1 | Bu Sari | Sees task "Install Server Rack" on her board with 🔒 Blocked badge | Badge clearly visible; status column shows `BLOCKED` |
| 2 | Bu Sari | Attempts to change status to "In Progress" via status dropdown | System validates dependency chain — predecessor "Procure Hardware" is still `IN_PROGRESS` |
| 3 | System | Rejects the status transition | Error toast: "Task cannot be started: 'Procure Hardware' must be completed first." |
| 4 | Bu Sari | Checks predecessor status | She can see the blocking task and its current assignee from the dependency panel |
| 5 | Predecessor Assignee | Marks "Procure Hardware" as `DONE` | `TaskStatusChanged` domain event fires |
| 6 | System | Re-evaluates all successors of "Procure Hardware" | "Install Server Rack" has all predecessors complete → status auto-transitions from `BLOCKED` to `TO_DO`; Bu Sari receives notification: "Task unblocked: Install Server Rack is now ready to start" |

**Success Condition:** Transition rejected correctly; auto-unblock triggers on predecessor completion; notification delivered.

---

### Journey 4: Critical Path Calculation & Notification (BR-TP-003)

**Entry Point:** System recalculates critical path whenever a task's duration or status changes.

| Step | Actor | Action | System Response |
|---|---|---|---|
| 1 | PM | Updates estimated duration of task "Configure Network" from 3 days to 7 days | `TaskUpdated` domain event fires |
| 2 | System | Background CPM worker re-executes critical path algorithm | New critical path identified |
| 3 | System | Detects that "Configure Network" is now on the critical path | `CriticalPathChanged` domain event fires |
| 4 | PM | Receives in-app + email notification | "⚠️ Critical Path Updated: 'Configure Network' is now on the critical path. Project end date may be affected." |
| 5 | PM | Opens Dependency Graph | New critical path highlighted; "Configure Network" node rendered in orange with ⚡ icon |
| 6 | PM | Clicks on "Configure Network" | Task detail shows: "Critical Path: YES | Float: 0 days" |

**Success Condition:** CPM recalculates on task change; notification sent within 30 seconds; graph reflects new state.

---

### Journey 5: Removing a Dependency

| Step | Actor | Action | System Response |
|---|---|---|---|
| 1 | PM | Opens Task Detail > Dependencies tab for "Install Server Rack" | Dependency list shows "Blocked by: Procure Hardware" |
| 2 | PM | Clicks the ✕ (remove) button on the predecessor chip | Confirmation dialog: "Remove dependency? 'Install Server Rack' will no longer be blocked by 'Procure Hardware'." |
| 3 | PM | Confirms removal | Row deleted from `task_dependencies`; `TaskDependencyRemoved` domain event fires |
| 4 | System | Re-evaluates "Install Server Rack" blocking status | No remaining predecessors → task transitions from `BLOCKED` to `TO_DO` |
| 5 | PM | Sees updated dependency list (now empty) | Graph updates in real-time (if graph view is open) |

---

## 7. Acceptance Criteria (Detailed & Testable)

### AC-TP-3-01: Add Predecessor Dependency
- **Given** a Project Manager is viewing a Task Detail for Task B within a project
- **When** they select Task A as a predecessor for Task B (and Task A is not already a successor of Task B)
- **Then** a new row is inserted in `task_dependencies` with `predecessor_id = Task A`, `successor_id = Task B`, and the dependency is visible in the Task B dependency list within 1 second

### AC-TP-3-02: Dependency Blocks Successor Task
- **Given** Task B has Task A as a predecessor
- **When** Task A's status is NOT `DONE` (i.e., it is `TO_DO`, `IN_PROGRESS`, `REVIEW`, or `BLOCKED`)
- **Then** Task B's status must be `BLOCKED`, and any attempt to manually transition Task B to `IN_PROGRESS` or `DONE` must be rejected with error message "Task cannot be started: '[Task A name]' must be completed first."

### AC-TP-3-03: Auto-Unblock on Predecessor Completion
- **Given** Task B is `BLOCKED` by Task A as its only predecessor
- **When** Task A's status is changed to `DONE`
- **Then** Task B automatically transitions from `BLOCKED` to `TO_DO` within 5 seconds, and the assignee of Task B receives an in-app notification

### AC-TP-3-04: Multi-Predecessor Unblocking
- **Given** Task C has two predecessors: Task A and Task B, and Task A is `DONE` but Task B is `IN_PROGRESS`
- **When** Task B's status is changed to `DONE`
- **Then** Task C transitions from `BLOCKED` to `TO_DO` (all predecessors now complete)

### AC-TP-3-05: Circular Dependency Detection — Direct
- **Given** Task A already has Task B as a successor
- **When** a Project Manager attempts to add Task A as a successor of Task B (creating A→B→A cycle)
- **Then** the system rejects the request with error: "Circular dependency detected: Adding this dependency would create a cycle."

### AC-TP-3-06: Circular Dependency Detection — Transitive
- **Given** a dependency chain A→B→C exists
- **When** a Project Manager attempts to add Task A as a successor of Task C (creating A→B→C→A cycle)
- **Then** the system rejects the request with error: "Circular dependency detected: Adding this dependency would create a cycle."

### AC-TP-3-07: Self-Dependency Prevention
- **Given** a Project Manager is on the dependency management UI for Task A
- **When** they attempt to add Task A as a predecessor of itself
- **Then** the system rejects the request with error: "A task cannot depend on itself."

### AC-TP-3-08: Visual Dependency Graph Rendering
- **Given** a project with ≥ 2 tasks that have defined dependencies
- **When** the Project Manager opens the "Dependency Graph" view
- **Then** all tasks are rendered as nodes, all dependencies as directed edges (arrows), and the graph loads within 1.5 seconds for projects with ≤ 100 tasks

### AC-TP-3-09: Critical Path Highlighting
- **Given** a project with a defined dependency network and task durations
- **When** the Dependency Graph is displayed
- **Then** all tasks on the critical path (longest path from start to finish) are visually highlighted in amber/orange color, and non-critical tasks in their default color

### AC-TP-3-10: Remove Dependency
- **Given** a dependency between Task A (predecessor) and Task B (successor) exists
- **When** the Project Manager removes the dependency via the Task B detail panel
- **Then** the `task_dependencies` row is deleted, Task B's blocking status is re-evaluated, and the graph is updated in real-time

### AC-TP-3-11: RBAC — Only Authorized Roles Can Manage Dependencies
- **Given** a user with role `MEMBER` (not `PROJECT_MANAGER` or `ADMIN`)
- **When** they attempt to create or delete a dependency via the API
- **Then** the system returns HTTP 403 Forbidden

### AC-TP-3-12: Critical Path Notification (BR-TP-003)
- **Given** a task's estimated duration changes such that it shifts the critical path
- **When** the CPM is recalculated
- **Then** the Project Manager receives an in-app notification within 30 seconds: "Critical Path Updated: [task name] is now on the critical path"

---

## 8. Business Rules

### BR-TP-001: Dependency Blocking
- A task with at least one predecessor that is not in `DONE` status must have its own status set to `BLOCKED`.
- Status transitions OUT of `BLOCKED` are only permissible by the system (automatic, when all predecessors reach `DONE`) or by a user with `ADMIN` or `PROJECT_MANAGER` role via an explicit "override block" action (which is logged to the audit trail).
- A `DONE` task that has its predecessor re-opened (e.g., status changed back to `IN_PROGRESS` due to rework) must be automatically re-evaluated. If the predecessor is no longer `DONE`, the successor must be re-blocked.

### BR-TP-003: Critical Path Notification
- The CPM must be recalculated whenever: (a) a task dependency is added or removed, (b) a task's `estimated_hours` or `due_date` changes, (c) a task's status changes.
- If the recalculation results in a different critical path than the previous calculation, a `CriticalPathChanged` domain event is emitted.
- Project Managers of the affected project must receive an in-app notification within 30 seconds.
- The notification must name the specific tasks that have been added to or removed from the critical path.

---

## 9. Edge Cases

| Edge Case | Expected Behavior |
|---|---|
| **Self-dependency** | Rejected at API level with 422; UI disables self-selection in dropdown |
| **Direct circular dependency** (A→B, attempt B→A) | Rejected with "Circular dependency detected" error |
| **Transitive circular dependency** (A→B→C, attempt C→A) | Topological sort detects cycle; rejected with same error |
| **Dependency on a task in another project** | Not allowed; task search is scoped to the current project only |
| **Dependency on a DONE task** | Allowed; successor is NOT blocked (predecessor already done) |
| **Predecessor re-opened after successor was unblocked** | System re-evaluates; if successor is now `TO_DO` or `IN_PROGRESS`, re-block enforced |
| **Deleting a task that is a predecessor** | All its successor dependencies are also deleted; successors are re-evaluated |
| **Deleting a task that is a successor** | All its predecessor dependencies are also deleted |
| **Project with zero tasks** | Dependency Graph shows empty state: "No tasks have been added to this project yet." |
| **Project with tasks but no dependencies** | Dependency Graph shows all tasks as isolated nodes with "No dependencies defined" hint |
| **Graph with 500+ tasks** | Graph renders with virtualization; performance warning if > 200 nodes on single graph |
| **Duplicate dependency attempt** (same A→B twice) | API returns 409 Conflict; UI prevents duplicate selection |
| **All tasks on critical path** (linear chain) | All nodes highlighted; notification suppressed if critical path was always the same |
| **Float = 0 for non-critical task** (near-critical) | Task displayed with float indicator: "Float: 0 days — near critical" |

---

## 10. Constraints

| Constraint | Description |
|---|---|
| **Scope** | Task dependencies are scoped to tasks within the same project. Cross-project dependencies are out of scope for TP-3. |
| **Status model** | Blocking is based on the `task_status` enum: `TO_DO`, `IN_PROGRESS`, `REVIEW`, `DONE`, `BLOCKED`, `CANCELLED`. Only `DONE` constitutes completion for dependency purposes. `CANCELLED` tasks are treated as transparent (skip-through) in the dependency chain. |
| **Graph library** | React Flow is selected as the graph rendering library (see tech spec). Server-side graph computation; client-side rendering only. |
| **Performance** | CPM must complete server-side within 500ms for projects with ≤ 200 tasks. |
| **Real-time** | Blocking/unblocking status changes must propagate to all connected clients via Upstash Redis pub/sub within 5 seconds. |
| **RBAC** | Only users with roles `PROJECT_MANAGER` or `ADMIN` may create or delete dependencies. All authenticated users may view the dependency graph. |

---

## 11. Out of Scope (TP-3)

| Item | Rationale |
|---|---|
| **Cross-project dependencies** | Adds significant complexity; deferred to TP-8 (External Dependencies) |
| **Lag/lead time on dependencies** (e.g., Start-to-Start + 2 days) | Standard Finish-to-Start only in TP-3; advanced dependency types in TP-8 |
| **Gantt chart integration** | Gantt view is a separate user story (TP-5); dependency data will be reused |
| **Automatic resource leveling** | Resource-constrained CPM is deferred |
| **Dependency import from MS Project (.mpp)** | Import feature is out of scope; manual entry only |
| **Sub-task dependencies** | Dependencies are defined at the task level only; sub-task hierarchy not included |
| **Manual override of critical path** | Critical path is always calculated algorithmically |

---

## 12. Definition of Done

- [ ] All 12 Acceptance Criteria pass automated tests
- [ ] Business Rules BR-TP-001 and BR-TP-003 enforced in production code
- [ ] Dependency graph renders for projects with 1–100 tasks within 1.5s (measured in staging)
- [ ] CPM calculation validated against 3 hand-verified test projects
- [ ] Circular dependency detection passes all edge case tests (direct + transitive)
- [ ] In-app notifications delivered for critical path changes
- [ ] Audit trail entries created for all dependency create/delete actions
- [ ] RBAC enforced: 403 returned for unauthorized role attempts
- [ ] Responsive design verified on 1440px, 1280px, 768px viewports
- [ ] Accessibility: keyboard navigation of graph nodes verified
- [ ] Code review completed; no critical or high-severity issues
- [ ] PM Persona (Pak Rudi) UAT sign-off received

---

*Document Owner: FluxGrid ERP Product Team*
*Review Cycle: Per sprint*
