# task-project02-tech.md
# Technical Specifications — TP-2: Time Tracking
## FluxGrid ERP | Module: Task & Project Management (TaskProject)

---

## 1. System Architecture

### Architecture Pattern
- **Clean Architecture + Domain-Driven Design (DDD)**
- **CQRS** with MediatR for command/query separation
- **Domain Events** via MediatR `INotification` + `INotificationHandler`
- **Repository Pattern** with Drizzle ORM over PostgreSQL (Neon)
- **Optimistic Locking** on time log updates via `version` column

### Layer Responsibility

```
┌─────────────────────────────────────────────────────────────────┐
│  Presentation Layer (Next.js 15 App Router)                      │
│  ├── Server Components: SSR for initial log list                 │
│  ├── Client Components: Timer widget, Log modal, Approval table  │
│  └── API Routes: /api/time-logs/*, /api/time-logs/timer/*        │
├─────────────────────────────────────────────────────────────────│
│  Application Layer (Use Cases / Commands / Queries)              │
│  ├── Commands: CreateTimeLog, ApproveTimeLog, RejectTimeLog,     │
│  │             StartTimer, PauseTimer, ResumeTimer, StopTimer    │
│  └── Queries: GetTimeLogsByTask, GetPendingApprovals,            │
│               GetTimerState, GetMyTimeLogs                       │
├─────────────────────────────────────────────────────────────────│
│  Domain Layer (Entities / Domain Events / Business Rules)        │
│  ├── Entities: TimeLog, TimerSession                             │
│  ├── Value Objects: Duration, TimerState                         │
│  ├── Domain Events: TimeLogCreated, TimeLogApproved,             │
│  │                  TimeLogRejected, TimeLogUpdated               │
│  └── Domain Services: TimeLogApprovalService                     │
├─────────────────────────────────────────────────────────────────│
│  Infrastructure Layer                                            │
│  ├── PostgreSQL (Neon): time_logs, time_log_audit                │
│  ├── Upstash Redis: Timer state (key-value, TTL 24h)             │
│  ├── MediatR: In-process domain event bus                        │
│  └── HR Event Publisher: Publishes TimeLogUpdated to HR module   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Database Schema

### 2.1 Table: `time_logs`

```sql
CREATE TABLE time_logs (
  id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id         UUID          NOT NULL REFERENCES tasks(id) ON DELETE RESTRICT,
  project_id      UUID          NOT NULL REFERENCES projects(id) ON DELETE RESTRICT,
  user_id         UUID          NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  hours           DECIMAL(5, 2) NOT NULL CHECK (hours >= 0.25 AND hours <= 24),
  date            DATE          NOT NULL,
  description     TEXT,
  is_billable     BOOLEAN       NOT NULL DEFAULT TRUE,
  source          VARCHAR(20)   NOT NULL DEFAULT 'manual'
                                CHECK (source IN ('manual', 'timer', 'auto_stopped')),
  status          VARCHAR(30)   NOT NULL DEFAULT 'pending_approval'
                                CHECK (status IN ('draft', 'pending_approval', 'approved', 'rejected', 'archived')),
  parent_log_id   UUID          REFERENCES time_logs(id) ON DELETE SET NULL,
  rejection_reason TEXT,
  approved_by     UUID          REFERENCES users(id),
  approved_at     TIMESTAMPTZ,
  submitted_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  version         INTEGER       NOT NULL DEFAULT 1,
  created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
  updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_time_logs_task_id       ON time_logs (task_id);
CREATE INDEX idx_time_logs_user_id       ON time_logs (user_id);
CREATE INDEX idx_time_logs_project_id    ON time_logs (project_id);
CREATE INDEX idx_time_logs_status        ON time_logs (status);
CREATE INDEX idx_time_logs_date          ON time_logs (date);
CREATE INDEX idx_time_logs_user_date     ON time_logs (user_id, date);
CREATE INDEX idx_time_logs_pending       ON time_logs (status, project_id) WHERE status = 'pending_approval';
```

### 2.2 Table: `time_log_audit`

```sql
CREATE TABLE time_log_audit (
  id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
  time_log_id     UUID          NOT NULL REFERENCES time_logs(id) ON DELETE CASCADE,
  action          VARCHAR(50)   NOT NULL, -- 'created', 'submitted', 'approved', 'rejected', 'archived', 'resubmitted', 'auto_stopped'
  performed_by    UUID          NOT NULL REFERENCES users(id),
  old_status      VARCHAR(30),
  new_status      VARCHAR(30),
  metadata        JSONB,        -- { rejection_reason, hours_before, hours_after, ... }
  created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_time_log_id ON time_log_audit (time_log_id);
CREATE INDEX idx_audit_performed_by ON time_log_audit (performed_by);
CREATE INDEX idx_audit_created_at   ON time_log_audit (created_at);
```

### 2.3 Entity-Relationship Diagram

```
users ──────< time_logs >──── tasks
              │                 │
              │                 └── projects
              └── time_log_audit
                  (audit trail per log)
```

---

## 3. Drizzle ORM Schema

```typescript
// src/infrastructure/database/schema/time-logs.schema.ts

import { 
  pgTable, uuid, decimal, date, text, boolean, varchar, 
  timestamp, integer, index, check 
} from 'drizzle-orm/pg-core';
import { sql } from 'drizzle-orm';
import { users } from './users.schema';
import { tasks } from './tasks.schema';
import { projects } from './projects.schema';

export const timeLogStatusEnum = ['draft', 'pending_approval', 'approved', 'rejected', 'archived'] as const;
export type TimeLogStatus = typeof timeLogStatusEnum[number];

export const timeLogSourceEnum = ['manual', 'timer', 'auto_stopped'] as const;
export type TimeLogSource = typeof timeLogSourceEnum[number];

export const timeLogs = pgTable(
  'time_logs',
  {
    id:              uuid('id').primaryKey().defaultRandom(),
    taskId:          uuid('task_id').notNull().references(() => tasks.id, { onDelete: 'restrict' }),
    projectId:       uuid('project_id').notNull().references(() => projects.id, { onDelete: 'restrict' }),
    userId:          uuid('user_id').notNull().references(() => users.id, { onDelete: 'restrict' }),
    hours:           decimal('hours', { precision: 5, scale: 2 }).notNull(),
    date:            date('date').notNull(),
    description:     text('description'),
    isBillable:      boolean('is_billable').notNull().default(true),
    source:          varchar('source', { length: 20 }).notNull().default('manual'),
    status:          varchar('status', { length: 30 }).notNull().default('pending_approval'),
    parentLogId:     uuid('parent_log_id').references((): any => timeLogs.id, { onDelete: 'set null' }),
    rejectionReason: text('rejection_reason'),
    approvedBy:      uuid('approved_by').references(() => users.id),
    approvedAt:      timestamp('approved_at', { withTimezone: true }),
    submittedAt:     timestamp('submitted_at', { withTimezone: true }).notNull().defaultNow(),
    version:         integer('version').notNull().default(1),
    createdAt:       timestamp('created_at', { withTimezone: true }).notNull().defaultNow(),
    updatedAt:       timestamp('updated_at', { withTimezone: true }).notNull().defaultNow(),
  },
  (table) => ({
    taskIdx:    index('idx_time_logs_task_id').on(table.taskId),
    userIdx:    index('idx_time_logs_user_id').on(table.userId),
    projectIdx: index('idx_time_logs_project_id').on(table.projectId),
    statusIdx:  index('idx_time_logs_status').on(table.status),
    dateIdx:    index('idx_time_logs_date').on(table.date),
    userDateIdx: index('idx_time_logs_user_date').on(table.userId, table.date),
    hoursCheck: check('hours_range', sql`hours >= 0.25 AND hours <= 24`),
  })
);

export const timeLogAudit = pgTable(
  'time_log_audit',
  {
    id:          uuid('id').primaryKey().defaultRandom(),
    timeLogId:   uuid('time_log_id').notNull().references(() => timeLogs.id, { onDelete: 'cascade' }),
    action:      varchar('action', { length: 50 }).notNull(),
    performedBy: uuid('performed_by').notNull().references(() => users.id),
    oldStatus:   varchar('old_status', { length: 30 }),
    newStatus:   varchar('new_status', { length: 30 }),
    metadata:    text('metadata'), // JSON stringified
    createdAt:   timestamp('created_at', { withTimezone: true }).notNull().defaultNow(),
  },
  (table) => ({
    timeLogIdx:  index('idx_audit_time_log_id').on(table.timeLogId),
    performedIdx: index('idx_audit_performed_by').on(table.performedBy),
  })
);

export type TimeLog = typeof timeLogs.$inferSelect;
export type NewTimeLog = typeof timeLogs.$inferInsert;
export type TimeLogAudit = typeof timeLogAudit.$inferSelect;
```

---

## 4. Redis Timer State Schema (Upstash)

### Key Pattern
```
timer:{userId}:{taskId}
```

### Value Structure (JSON stringified)
```typescript
interface TimerState {
  userId:       string;          // UUID
  taskId:       string;          // UUID  
  projectId:    string;          // UUID
  taskName:     string;          // Display name
  status:       'running' | 'paused';
  startedAt:    string;          // ISO8601 UTC timestamp
  pausedAt:     string | null;   // ISO8601 UTC timestamp (when paused)
  elapsedSeconds: number;        // Total accumulated seconds before current session
  lastHeartbeat:  string;        // ISO8601 UTC timestamp
}
```

### Redis Operations

| Operation | Command | TTL |
|-----------|---------|-----|
| Start timer | `SET timer:{userId}:{taskId} {json} EX 86400` | 24h |
| Heartbeat sync | `SET timer:{userId}:{taskId} {json} EX 86400` | Refreshed |
| Pause | Update `status`, `pausedAt`, `elapsedSeconds`; `SET ... EX 86400` | 24h |
| Resume | Update `status`, remove `pausedAt`, set `startedAt`; `SET ... EX 86400` | 24h |
| Stop/Submit | `DEL timer:{userId}:{taskId}` | N/A |
| Active check | `EXISTS timer:{userId}:*` | Read-only |

### User's Active Timer Index
```
active_timer:{userId}  → "{taskId}"   (TTL 24h)
```
Used to quickly find a user's active timer without pattern scanning.

---

## 5. API Endpoints

### Base URL: `/api/time-logs`

All endpoints require `Authorization: Bearer {JWT}` header.  
All responses follow: `{ data, meta, error }` envelope.

---

#### `POST /api/time-logs`
**Description**: Create a manual time log entry  
**Role Required**: `team_member`, `project_manager`, `admin`

**Request Body**:
```typescript
{
  taskId:      string;  // UUID, required
  hours:       number;  // 0.25–24, required
  date:        string;  // ISO 8601 date (YYYY-MM-DD), required
  description?: string; // max 500 chars
  isBillable?: boolean; // default: true
}
```

**Response 201**:
```typescript
{
  data: {
    id:          string;
    taskId:      string;
    hours:       number;
    date:        string;
    description: string | null;
    isBillable:  boolean;
    status:      "pending_approval";
    submittedAt: string;
  }
}
```

**Validations**:
- `taskId` must exist and be assigned to the requesting user
- `hours` must be 0.25–24
- `date` must be within last 30 days and not in future
- Task must not be `archived`

**Errors**:
| Code | HTTP | Message |
|------|------|---------|
| `TASK_NOT_FOUND` | 404 | Task not found |
| `NOT_ASSIGNED` | 403 | You are not assigned to this task |
| `INVALID_DATE` | 422 | Date is outside the allowed range |
| `INVALID_HOURS` | 422 | Hours must be between 0.25 and 24 |
| `TASK_ARCHIVED` | 422 | Cannot log time for archived tasks |

---

#### `GET /api/time-logs`
**Description**: List time logs (filtered by task, user, date range, status)  
**Role Required**: `team_member` (own), `project_manager` (team), `admin` (all)

**Query Parameters**:
```
taskId?:    string
userId?:    string  (PM/admin only)
projectId?: string
status?:    TimeLogStatus
dateFrom?:  YYYY-MM-DD
dateTo?:    YYYY-MM-DD
page?:      number (default 1)
pageSize?:  number (default 20, max 100)
sort?:      "date_desc" | "date_asc" | "hours_desc"
```

**Response 200**:
```typescript
{
  data: TimeLog[];
  meta: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
    summary: {
      totalHours: number;
      approvedHours: number;
      pendingHours: number;
    }
  }
}
```

---

#### `GET /api/time-logs/:id`
**Description**: Get a single time log  
**Role Required**: Owner, PM of the project, Admin

---

#### `PATCH /api/time-logs/:id`
**Description**: Update a pending/draft time log  
**Role Required**: Owner only  
**Constraint**: Cannot update if status is `approved` or `archived`

**Request Body** (all fields optional):
```typescript
{
  hours?:       number;
  date?:        string;
  description?: string;
  isBillable?:  boolean;
}
```

**Response 200**: Updated time log

**Errors**:
| Code | HTTP | Message |
|------|------|---------|
| `IMMUTABLE_LOG` | 422 | Approved time logs cannot be edited |
| `NOT_OWNER` | 403 | You can only edit your own time logs |

---

#### `PATCH /api/time-logs/:id/approve`
**Description**: Approve a time log  
**Role Required**: `project_manager`, `admin`  
**Constraint**: Cannot self-approve

**Request Body**: None

**Response 200**:
```typescript
{
  data: {
    id:         string;
    status:     "approved";
    approvedBy: string;
    approvedAt: string;
  }
}
```

**Side Effects**:
- Creates audit trail entry
- Fires `TimeLogUpdated` domain event
- Sends in-app notification to log owner

**Errors**:
| Code | HTTP | Message |
|------|------|---------|
| `SELF_APPROVAL_NOT_ALLOWED` | 422 | You cannot approve your own time log |
| `NOT_PENDING` | 422 | Only pending logs can be approved |
| `NOT_PROJECT_MANAGER` | 403 | Insufficient permissions |

---

#### `PATCH /api/time-logs/:id/reject`
**Description**: Reject a time log with reason  
**Role Required**: `project_manager`, `admin`

**Request Body**:
```typescript
{
  reason: string; // min 10, max 500 chars
}
```

**Response 200**:
```typescript
{
  data: {
    id:              string;
    status:          "rejected";
    rejectionReason: string;
  }
}
```

---

#### `POST /api/time-logs/:id/resubmit`
**Description**: Resubmit a rejected log (creates new log, archives old)  
**Role Required**: Owner only (log must be in `rejected` status)

**Request Body**:
```typescript
{
  hours:        number;
  date:         string;
  description?: string;
  isBillable?:  boolean;
}
```

**Response 201**: New time log with `parent_log_id` referencing original

---

### Timer Endpoints: `/api/time-logs/timer`

#### `POST /api/time-logs/timer/start`
**Description**: Start a new timer session  
**Role Required**: `team_member`, `project_manager`, `admin`

**Request Body**:
```typescript
{
  taskId: string; // UUID
}
```

**Business Logic**:
1. Check if user has an active timer (`active_timer:{userId}` Redis key)
2. If yes: Return `TIMER_CONFLICT` with existing task info — let client handle confirmation
3. Create Redis key `timer:{userId}:{taskId}` with initial state
4. Set `active_timer:{userId}` to `{taskId}`

**Response 200**:
```typescript
{
  data: {
    timerState: TimerState;
    conflictingTimer?: { taskId: string; taskName: string; elapsedSeconds: number; };
  }
}
```

---

#### `POST /api/time-logs/timer/pause`
**Description**: Pause the active timer for a task

**Request Body**:
```typescript
{ taskId: string; }
```

**Business Logic**:
1. Load timer state from Redis
2. Calculate `elapsedSeconds` = previous elapsed + (now - `startedAt`)
3. Update state: `status = 'paused'`, `pausedAt = now`, `elapsedSeconds`
4. Persist to Redis

---

#### `POST /api/time-logs/timer/resume`
**Description**: Resume a paused timer

**Request Body**:
```typescript
{ taskId: string; }
```

---

#### `POST /api/time-logs/timer/stop`
**Description**: Stop the timer and return elapsed time (does NOT create time log)

**Request Body**:
```typescript
{ taskId: string; }
```

**Response 200**:
```typescript
{
  data: {
    elapsedSeconds: number;
    elapsedHours:   number; // rounded to 2 decimal places
    taskId:         string;
    taskName:       string;
  }
}
```

**Side Effect**: Deletes Redis timer keys. Time log creation happens separately via `POST /api/time-logs`.

---

#### `GET /api/time-logs/timer/state`
**Description**: Get current timer state for a task (for page load recovery)

**Query**: `?taskId={taskId}`

**Response 200**:
```typescript
{
  data: TimerState | null; // null if no active timer
}
```

---

#### `POST /api/time-logs/timer/heartbeat`
**Description**: Client sends heartbeat every 30s to keep timer alive in Redis

**Request Body**:
```typescript
{ taskId: string; }
```

**Response 200**: Updated timer state

---

## 6. Domain Events

### 6.1 `TimeLogUpdated` Event

**Published when**: A time log is approved (status changes to `approved`)  
**Publisher**: `ApproveTimeLogCommandHandler`  
**Consumed by**: HR Productivity Analytics module

**Event Payload**:
```typescript
interface TimeLogUpdatedEvent {
  eventType:    "TimeLogUpdated";
  eventId:      string;          // UUID (for idempotency)
  occurredAt:   string;          // ISO8601 UTC
  timeLogId:    string;          // UUID
  userId:       string;          // UUID — the employee
  employeeCode: string;          // HR employee code
  taskId:       string;          // UUID
  taskName:     string;
  projectId:    string;          // UUID
  projectCode:  string;          // Project billing code
  hours:        number;          // Approved hours
  date:         string;          // YYYY-MM-DD — the work date
  isBillable:   boolean;
  isApproved:   true;
  approvedAt:   string;          // ISO8601 UTC
  approvedBy:   string;          // UUID of PM
}
```

**MediatR Implementation**:
```typescript
// Domain Event (in Domain layer)
export class TimeLogUpdatedDomainEvent implements INotification {
  constructor(public readonly payload: TimeLogUpdatedEvent) {}
}

// Event Handler (in Infrastructure layer — HR integration)
@Injectable()
export class TimeLogUpdatedHRHandler 
  implements INotificationHandler<TimeLogUpdatedDomainEvent> {
  
  constructor(
    private readonly hrProductivityService: HRProductivityService,
    private readonly redis: Redis,
  ) {}

  async handle(notification: TimeLogUpdatedDomainEvent): Promise<void> {
    const { payload } = notification;
    
    try {
      await this.hrProductivityService.recordTimeEntry({
        employeeCode: payload.employeeCode,
        projectCode:  payload.projectCode,
        hours:        payload.hours,
        date:         payload.date,
        isBillable:   payload.isBillable,
        sourceEventId: payload.eventId,
      });
    } catch (error) {
      // Queue for retry in Redis dead-letter
      await this.queueForRetry(payload);
      throw error; // Allow MediatR to log; do NOT rollback approval
    }
  }

  private async queueForRetry(payload: TimeLogUpdatedEvent): Promise<void> {
    const retryKey = `retry:TimeLogUpdated:${payload.timeLogId}`;
    await this.redis.lpush('dlq:TimeLogUpdated', JSON.stringify({
      payload,
      enqueuedAt: new Date().toISOString(),
      attempts: 0,
    }));
  }
}
```

### 6.2 Other Domain Events

| Event | When Published | Consumers |
|-------|---------------|-----------|
| `TimeLogCreated` | Time log submitted | Notification service (notify PM) |
| `TimeLogRejected` | Time log rejected | Notification service (notify team member) |
| `TimerAutoStopped` | Timer auto-stopped at 12h | Notification service (notify team member) |

---

## 7. RBAC Permissions Matrix

| Action | `team_member` | `project_manager` | `hr_admin` | `admin` |
|--------|:---:|:---:|:---:|:---:|
| Create time log (own) | ✅ | ✅ | ✗ | ✅ |
| View own time logs | ✅ | ✅ | ✗ | ✅ |
| View all time logs in project | ✗ | ✅ | ✗ | ✅ |
| View all time logs (all projects) | ✗ | ✗ | ✗ | ✅ |
| Edit own pending/draft log | ✅ | ✅ | ✗ | ✅ |
| Delete own log (draft only) | ✅ | ✅ | ✗ | ✅ |
| Approve time logs (other users) | ✗ | ✅ | ✗ | ✅ |
| Reject time logs (other users) | ✗ | ✅ | ✗ | ✅ |
| Approve own time log | ✗ | ✗ | ✗ | ✗ |
| Start/stop/pause timer (own) | ✅ | ✅ | ✗ | ✅ |
| View HR analytics (read-only) | ✗ | ✗ | ✅ | ✅ |

### RBAC Implementation Notes
- Permission checks run in the **Application Layer** Command/Query handlers
- RBAC middleware at the API route level handles coarse-grained auth (token validity, basic role check)
- Fine-grained checks (e.g., "is this user the PM of the project this task belongs to?") run in handlers
- All permission failures are logged in the audit trail

---

## 8. Performance Considerations

### Database Query Optimization

**Hot Path: Pending Approval Queue**
```sql
-- Partial index makes this O(1) lookup instead of full table scan
SELECT tl.*, u.name as user_name, t.name as task_name
FROM time_logs tl
JOIN users u ON tl.user_id = u.id
JOIN tasks t ON tl.task_id = t.id
WHERE tl.status = 'pending_approval'
  AND tl.project_id = ANY($1::uuid[])  -- PM's projects
ORDER BY tl.submitted_at ASC
LIMIT 50;
-- Uses: idx_time_logs_pending (partial index on status + project_id)
```

**Hot Path: User's Time Logs by Week**
```sql
SELECT * FROM time_logs
WHERE user_id = $1
  AND date BETWEEN $2 AND $3
  AND status != 'archived'
ORDER BY date DESC;
-- Uses: idx_time_logs_user_date
```

### Redis Timer Performance
- Timer heartbeat every 30s from client: ~200 concurrent users = ~6-7 Redis ops/second (negligible)
- Auto-stop job: Background job running every 15 minutes using `SCAN` with pattern `timer:*` + filter by TTL
- Timer key TTL = 24 hours (prevents Redis memory growth from abandoned timers)

### Response Time Targets

| Endpoint | P50 | P95 | P99 |
|----------|-----|-----|-----|
| `POST /api/time-logs` | ≤150ms | ≤300ms | ≤500ms |
| `GET /api/time-logs` (list) | ≤100ms | ≤250ms | ≤400ms |
| `PATCH .../approve` | ≤200ms | ≤400ms | ≤600ms |
| `POST /timer/start` | ≤50ms | ≤100ms | ≤200ms |
| `POST /timer/heartbeat` | ≤20ms | ≤50ms | ≤100ms |

### Caching Strategy
- **Time log list per task**: Invalidated on any log creation/status change for that task (Redis TTL 60s)
- **Approval queue count**: Cached in Redis, invalidated on approval/rejection. Used for badge count in UI.
- **User's weekly summary**: React Query on client, `staleTime: 30s`, `gcTime: 5min`

---

## 9. Security Considerations

### Authentication
- All API routes protected by Next.js middleware: validates JWT from `Authorization` header (Bearer) or `session` cookie (NextAuth.js)
- JWT `userId` and `roles` claims extracted server-side; never trusted from client request body

### Authorization (IDOR Prevention)
```typescript
// In GetTimeLogByIdQueryHandler
const log = await this.repo.findById(query.id);
if (!log) throw new NotFoundException('Time log not found');

const isOwner = log.userId === query.requestingUserId;
const isPM = await this.projectRepo.isProjectManager(log.projectId, query.requestingUserId);
const isAdmin = query.requestingUserRoles.includes('admin');

if (!isOwner && !isPM && !isAdmin) {
  throw new ForbiddenException('FORBIDDEN');
}
```

### Input Sanitization
- All text fields (description, rejection reason) sanitized via `DOMPurify` (client-side) and `sanitize-html` (server-side)
- Drizzle ORM uses parameterized queries exclusively — no raw SQL interpolation
- `hours` field: Strict `parseFloat()` with range check; never passed to SQL as string

### Rate Limiting (via Upstash Redis)
| Endpoint | Limit |
|----------|-------|
| `POST /api/time-logs` | 30 requests/min per user |
| `POST /api/time-logs/timer/*` | 60 requests/min per user |
| `PATCH .../approve` | 100 requests/min per user |
| Global per-IP | 300 requests/min |

### Audit Trail Immutability
- `time_log_audit` rows are INSERT-ONLY (no UPDATE or DELETE triggers)
- Application code never calls UPDATE/DELETE on `time_log_audit`
- PostgreSQL row-level security (RLS) policy enforces append-only at DB level:
```sql
ALTER TABLE time_log_audit ENABLE ROW LEVEL SECURITY;
CREATE POLICY audit_insert_only ON time_log_audit
  FOR INSERT TO app_role WITH CHECK (true);
-- No SELECT/UPDATE/DELETE policies → denied by default
CREATE POLICY audit_select_all ON time_log_audit
  FOR SELECT TO app_role USING (true);
```

---

## 10. Error Handling

### Standardized Error Response Format
```typescript
interface ErrorResponse {
  error: {
    code:     string;   // Machine-readable error code
    message:  string;   // Human-readable message
    field?:   string;   // Field name (for validation errors)
    details?: object;   // Additional context
  }
}
```

### Error Code Registry

| Code | HTTP | Description |
|------|------|-------------|
| `TASK_NOT_FOUND` | 404 | Task does not exist |
| `TIME_LOG_NOT_FOUND` | 404 | Time log does not exist |
| `NOT_ASSIGNED` | 403 | User not assigned to task |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `SELF_APPROVAL_NOT_ALLOWED` | 422 | PM cannot approve own log |
| `IMMUTABLE_LOG` | 422 | Cannot edit approved log |
| `NOT_PENDING` | 422 | Log is not in pending state |
| `INVALID_DATE` | 422 | Date out of allowed range |
| `INVALID_HOURS` | 422 | Hours out of valid range |
| `TASK_ARCHIVED` | 422 | Task is archived |
| `TIMER_NOT_FOUND` | 404 | No active timer for this task |
| `TIMER_CONFLICT` | 409 | User already has an active timer |
| `REJECTION_REASON_TOO_SHORT` | 422 | Reason min 10 chars |
| `OPTIMISTIC_LOCK_FAILED` | 409 | Concurrent modification conflict |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

### Optimistic Locking Pattern
```typescript
// Prevents race condition when two PMs approve same log simultaneously
const result = await db
  .update(timeLogs)
  .set({ 
    status: 'approved', 
    approvedBy: commanderId, 
    approvedAt: new Date(),
    version: sql`${timeLogs.version} + 1`,
    updatedAt: new Date(),
  })
  .where(
    and(
      eq(timeLogs.id, logId),
      eq(timeLogs.status, 'pending_approval'),
      eq(timeLogs.version, expectedVersion)
    )
  )
  .returning();

if (result.length === 0) {
  throw new ConflictException('OPTIMISTIC_LOCK_FAILED', 
    'This log was modified by another user. Please refresh and try again.');
}
```

---

## 11. Background Jobs

### Job 1: Timer Auto-Stop (Every 15 minutes)
```typescript
// src/infrastructure/jobs/timer-auto-stop.job.ts
export async function runTimerAutoStopJob(redis: Redis, db: Database) {
  const maxElapsedSeconds = 12 * 3600; // 12 hours
  
  // Scan for all active timer keys
  const keys = await redis.keys('timer:*');
  
  for (const key of keys) {
    const stateRaw = await redis.get(key);
    if (!stateRaw) continue;
    
    const state: TimerState = JSON.parse(stateRaw);
    const now = Date.now();
    
    let totalElapsed = state.elapsedSeconds;
    if (state.status === 'running') {
      totalElapsed += Math.floor((now - new Date(state.startedAt).getTime()) / 1000);
    }
    
    if (totalElapsed >= maxElapsedSeconds) {
      // Create draft time log
      await createDraftTimeLog(db, state, totalElapsed);
      
      // Delete Redis keys
      await redis.del(key);
      await redis.del(`active_timer:${state.userId}`);
      
      // Notify user
      await notifyUser(state.userId, 'TIMER_AUTO_STOPPED', { taskId: state.taskId });
    }
  }
}
```

### Job 2: Dead Letter Queue Retry (Every 5 minutes)
```typescript
// Retry failed TimeLogUpdated events from DLQ
export async function retryDeadLetterEvents(redis: Redis, hrService: HRProductivityService) {
  const maxRetries = 5;
  const backoffSeconds = [5, 15, 45, 120, 300];
  
  const items = await redis.lrange('dlq:TimeLogUpdated', 0, 9); // Process 10 at a time
  
  for (const item of items) {
    const entry = JSON.parse(item);
    if (entry.attempts >= maxRetries) {
      // Alert system admin and move to permanent failure log
      await alertAdmin('TimeLogUpdated DLQ max retries exceeded', entry);
      await redis.lrem('dlq:TimeLogUpdated', 1, item);
      continue;
    }
    
    const backoff = backoffSeconds[entry.attempts] * 1000;
    const nextRetryAt = new Date(entry.enqueuedAt).getTime() + backoff;
    
    if (Date.now() < nextRetryAt) continue; // Not yet time to retry
    
    try {
      await hrService.recordTimeEntry(entry.payload);
      await redis.lrem('dlq:TimeLogUpdated', 1, item); // Remove on success
    } catch {
      entry.attempts++;
      await redis.lrem('dlq:TimeLogUpdated', 1, item);
      await redis.lpush('dlq:TimeLogUpdated', JSON.stringify(entry));
    }
  }
}
```

---

## 12. Seed Data

```typescript
// src/infrastructure/database/seeds/time-logs.seed.ts

export async function seedTimeLogs(db: Database) {
  const now = new Date();
  const yesterday = new Date(now.getTime() - 86400000);
  const twoDaysAgo = new Date(now.getTime() - 2 * 86400000);

  const logs: NewTimeLog[] = [
    // Approved log (triggers HR sync in test)
    {
      id: '11111111-1111-1111-1111-111111111001',
      taskId: 'task-seed-001', // TASK-101: Implement Auth Flow
      projectId: 'proj-seed-001',
      userId: 'user-tm-seed-01', // Budi Santoso
      hours: '3.50',
      date: formatDate(yesterday),
      description: 'Implemented JWT authentication middleware and refresh token logic',
      isBillable: true,
      source: 'manual',
      status: 'approved',
      approvedBy: 'user-pm-seed-01',
      approvedAt: yesterday,
      submittedAt: yesterday,
      version: 2,
    },
    // Pending approval log
    {
      id: '11111111-1111-1111-1111-111111111002',
      taskId: 'task-seed-001',
      projectId: 'proj-seed-001',
      userId: 'user-tm-seed-01',
      hours: '2.00',
      date: formatDate(now),
      description: 'Code review and unit test fixes',
      isBillable: true,
      source: 'timer',
      status: 'pending_approval',
      submittedAt: now,
      version: 1,
    },
    // Rejected log (for resubmission tests)
    {
      id: '11111111-1111-1111-1111-111111111003',
      taskId: 'task-seed-002', // TASK-102: Database Schema Design
      projectId: 'proj-seed-001',
      userId: 'user-tm-seed-02', // Siti Rahma
      hours: '8.00',
      date: formatDate(yesterday),
      description: 'Database schema v1 design',
      isBillable: true,
      source: 'manual',
      status: 'rejected',
      rejectionReason: 'Hours seem too high for initial schema draft. Please revise.',
      submittedAt: twoDaysAgo,
      version: 1,
    },
    // Draft log from auto-stopped timer
    {
      id: '11111111-1111-1111-1111-111111111004',
      taskId: 'task-seed-001',
      projectId: 'proj-seed-001',
      userId: 'user-tm-seed-01',
      hours: '12.00',
      date: formatDate(twoDaysAgo),
      description: null,
      isBillable: false,
      source: 'auto_stopped',
      status: 'draft',
      submittedAt: twoDaysAgo,
      version: 1,
    },
  ];

  await db.insert(timeLogs).values(logs).onConflictDoNothing();

  // Seed audit trail
  const auditEntries: NewTimeLogAudit[] = [
    {
      timeLogId: '11111111-1111-1111-1111-111111111001',
      action: 'created',
      performedBy: 'user-tm-seed-01',
      oldStatus: null,
      newStatus: 'pending_approval',
      metadata: JSON.stringify({ source: 'manual' }),
    },
    {
      timeLogId: '11111111-1111-1111-1111-111111111001',
      action: 'approved',
      performedBy: 'user-pm-seed-01',
      oldStatus: 'pending_approval',
      newStatus: 'approved',
      metadata: JSON.stringify({ approvedAt: yesterday.toISOString() }),
    },
    {
      timeLogId: '11111111-1111-1111-1111-111111111003',
      action: 'created',
      performedBy: 'user-tm-seed-02',
      oldStatus: null,
      newStatus: 'pending_approval',
      metadata: JSON.stringify({ source: 'manual' }),
    },
    {
      timeLogId: '11111111-1111-1111-1111-111111111003',
      action: 'rejected',
      performedBy: 'user-pm-seed-01',
      oldStatus: 'pending_approval',
      newStatus: 'rejected',
      metadata: JSON.stringify({ rejectionReason: 'Hours seem too high for initial schema draft.' }),
    },
  ];

  await db.insert(timeLogAudit).values(auditEntries).onConflictDoNothing();
  
  console.log('✅ Time logs seed data inserted successfully');
}
```

---

## 13. Deployment Considerations

### Environment Variables Required

```env
# Database
DATABASE_URL=postgresql://user:pass@neon-host/fluxgrid_db?sslmode=require

# Redis (Upstash)
UPSTASH_REDIS_REST_URL=https://xxxxx.upstash.io
UPSTASH_REDIS_REST_TOKEN=AXxxxxxxxxxxxxxx

# HR Module Integration (internal service URL)
HR_MODULE_BASE_URL=https://api.internal.fluxgrid.id/hr
HR_MODULE_API_KEY=sk-xxxxxxxxxxxxxxxx

# Timer Configuration
TIMER_MAX_HOURS=12
TIMER_HEARTBEAT_INTERVAL_MS=30000
TIMER_AUTO_STOP_JOB_CRON=*/15 * * * *  # Every 15 minutes

# Rate Limiting
RATE_LIMIT_MAX_REQUESTS=300
RATE_LIMIT_WINDOW_MS=60000
```

### Database Migration Script
```bash
# Run Drizzle migration for time_logs tables
npx drizzle-kit migrate

# Apply seed data (development/staging only)
npx tsx src/infrastructure/database/seeds/index.ts --feature=time-logs
```

### Neon PostgreSQL Considerations
- Enable **connection pooling** (PgBouncer) via Neon console for high-throughput approval operations
- Use **Read Replicas** for `GET /api/time-logs` (read-heavy queries) — route to replica via `DATABASE_URL_READ`
- Set up **Neon Branch** for staging environment (branch from main → staging branch)
- Enable **Query Performance Insights** in Neon dashboard to monitor slow queries

### Upstash Redis Considerations
- Use **Upstash Redis REST API** (not TCP) for edge compatibility with Next.js middleware
- Enable **Upstash Redis Eviction** policy: `allkeys-lru` to prevent OOM on timer keys
- Monitor `dlq:TimeLogUpdated` list length — alert if > 50 items

### Vercel Deployment (Next.js)
- Background jobs (auto-stop, DLQ retry) deployed as **Vercel Cron Jobs** using `vercel.json`:
```json
{
  "crons": [
    {
      "path": "/api/jobs/timer-auto-stop",
      "schedule": "*/15 * * * *"
    },
    {
      "path": "/api/jobs/dlq-retry",
      "schedule": "*/5 * * * *"
    }
  ]
}
```
- Configure `VERCEL_CRON_SECRET` env var and validate in job API routes

### Monitoring & Observability
- **Sentry**: Capture all API errors with `timeLogId`, `userId`, `operation` tags
- **Vercel Analytics**: Track approval page load time and interaction latency
- **Custom Metrics** (via Upstash or custom logging):
  - Active timers count (gauge)
  - Pending approval queue depth (gauge)
  - `TimeLogUpdated` event delivery latency (histogram)
  - DLQ depth (gauge, alert if > 10)

---

## 14. Dependencies & Impact Analysis

### Upstream Dependencies (This feature requires)
| Dependency | Type | Impact if Unavailable |
|-----------|------|----------------------|
| Task Management (TP-1) | Hard | Cannot create time logs without tasks |
| User/Auth Module | Hard | No auth, no logging |
| RBAC Module | Hard | No permission enforcement |
| Notification Module | Soft | Silent failures; logs still saved |

### Downstream Dependencies (Other features require this)
| Consumer | Type | Impact |
|----------|------|--------|
| HR Productivity Analytics | Event consumer | Missing effort data for payroll/analytics |
| Project Billing (future) | Data consumer | Inaccurate billable hours reports |
| Project Dashboard (TP-7) | Data consumer | Effort burn charts will be empty |

---

*Document Version: 1.0 | Generated: 2026-07-02 | Author: FluxGrid SDD Agent*
