# Technical Specifications: Productivity Analytics (HR-7)

## 1. System Architecture
- **Backend API**: Next.js API Routes querying a materialized view.
- **Database**: PostgreSQL (Neon).
- **Event Bus**: MediatR pattern for decoupling. HR listens to events fired by TaskProject.

## 2. Database Schema

### Table: `employee_productivity_stats`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `employee_id` | UUID | NOT NULL, FK | Reference to `employees` |
| `date` | DATE | NOT NULL | e.g., 2026-05-15 |
| `hours_worked`| DECIMAL | DEFAULT 0 | Copied from Task App Attendance API |
| `tasks_completed`| INT | DEFAULT 0 | Incremented via Domain Event |
| `hours_logged`| DECIMAL | DEFAULT 0 | Incremented via Domain Event (Time Tracking) |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (employee_id, date)`: Upsert constraint.

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, date, decimal, integer, timestamp, uniqueIndex } from "drizzle-orm/pg-core";
import { employees } from "./hr01";

export const employeeProductivityStats = pgTable("employee_productivity_stats", {
  id: uuid("id").primaryKey().defaultRandom(),
  employeeId: uuid("employee_id").references(() => employees.id).notNull(),
  date: date("date").notNull(),
  hoursWorked: decimal("hours_worked").default('0').notNull(),
  tasksCompleted: integer("tasks_completed").default(0).notNull(),
  hoursLogged: decimal("hours_logged").default('0').notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
}, (table) => {
  return {
    empDateIdx: uniqueIndex("emp_date_idx").on(table.employeeId, table.date),
  };
});
```

## 4. API Endpoints

### GET `/api/v1/hr/analytics/productivity`
- **Description**: Fetch aggregated data for the dashboard.
- **Query Params**: `start_date`, `end_date`, `department_id`.
- **Action**: Runs a `GROUP BY date` or `GROUP BY employee_id` query against `employee_productivity_stats`.

## 5. Domain Events
- **Consumed**: 
  - `TaskCompleted` (from TaskProject) -> Upserts `employee_productivity_stats`, incrementing `tasks_completed`.
  - `TimeLogUpdated` (from TaskProject) -> Upserts `employee_productivity_stats`, incrementing `hours_logged`.

## 6. Permissions (RBAC)
- `hr.analytics.read`: Required to view the dashboard.

## 7. Performance Considerations
- Writing to `employee_productivity_stats` on every single task completion might cause database locks if an employee completes 50 tasks in a minute. 
- **Optimization**: Use Redis to buffer the counters during the day, and flush them to PostgreSQL in a nightly batch job, OR just calculate it dynamically in a nightly Materialized View refresh instead of real-time event listening. Given the requirement for an Event-Driven architecture, real-time upserts with `ON CONFLICT DO UPDATE` are acceptable for small-medium scale.

## 8. Security Considerations
- Data isolation is critical. Ensure `tenant_id` is always passed into the aggregation queries.

## 9. Error Handling
- If the event handler fails to upsert, the message should be returned to a Dead Letter Queue (DLQ) in Upstash for manual replay, ensuring no productivity data is permanently lost.
