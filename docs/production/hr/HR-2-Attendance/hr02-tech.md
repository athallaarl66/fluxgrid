# Technical Specifications: Attendance Management (HR-2)

## 1. System Architecture
- **Frontend**: Next.js Client Components for the Dashboard.
- **Backend API**: Next.js API Routes.
- **Database**: PostgreSQL (Neon).
- **Scheduled Jobs**: A daily cron job runs at 23:59 to auto-clock-out missing records and calculate daily lateness/absences.

## 2. Database Schema

### Table: `shifts`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `name` | VARCHAR(50) | NOT NULL | e.g., "Regular 09-18" |
| `start_time` | TIME | NOT NULL | 09:00:00 |
| `end_time` | TIME | NOT NULL | 18:00:00 |
| `late_tolerance_mins` | INT | DEFAULT 0 | e.g., 15 |
| `tenant_id` | UUID | NOT NULL, FK| |

### Table: `attendance_logs`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `employee_id` | UUID | NOT NULL, FK | Reference to `employees` |
| `shift_id` | UUID | NOT NULL, FK | Reference to `shifts` |
| `date` | DATE | NOT NULL | 2026-05-15 |
| `clock_in` | TIMESTAMP | | |
| `clock_out` | TIMESTAMP | | |
| `status` | VARCHAR(20) | NOT NULL | ON_TIME, LATE, ABSENT, ON_LEAVE |
| `late_minutes` | INT | DEFAULT 0 | Calculated value |
| `overtime_minutes` | INT | DEFAULT 0 | Calculated potential value |
| `approved_overtime`| INT | DEFAULT 0 | Value approved by manager |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (employee_id, date)`: An employee can only have one main attendance record per day.

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, time, integer, date, timestamp, uniqueIndex } from "drizzle-orm/pg-core";
import { employees } from "./hr01";

export const shifts = pgTable("shifts", {
  id: uuid("id").primaryKey().defaultRandom(),
  name: varchar("name", { length: 50 }).notNull(),
  startTime: time("start_time").notNull(),
  endTime: time("end_time").notNull(),
  lateToleranceMins: integer("late_tolerance_mins").default(0).notNull(),
  tenantId: uuid("tenant_id").notNull(),
});

export const attendanceLogs = pgTable("attendance_logs", {
  id: uuid("id").primaryKey().defaultRandom(),
  employeeId: uuid("employee_id").references(() => employees.id).notNull(),
  shiftId: uuid("shift_id").references(() => shifts.id).notNull(),
  date: date("date").notNull(),
  clockIn: timestamp("clock_in"),
  clockOut: timestamp("clock_out"),
  status: varchar("status", { length: 20 }).notNull().default("ON_TIME"),
  lateMinutes: integer("late_minutes").default(0).notNull(),
  overtimeMinutes: integer("overtime_minutes").default(0).notNull(),
  approvedOvertime: integer("approved_overtime").default(0).notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
}, (table) => {
  return {
    employeeDateIdx: uniqueIndex("employee_date_idx").on(table.employeeId, table.date),
  };
});
```

## 4. API Endpoints

### POST `/api/v1/hr/attendance/clock-in`
- **Description**: Records clock in.
- **Action**:
  1. Identifies `employee_id` from the JWT token.
  2. Gets current server timestamp.
  3. Checks `shifts` table to calculate if `clock_in > start_time + tolerance`.
  4. Inserts into `attendance_logs` with status (ON_TIME or LATE) and calculated `late_minutes`.

### POST `/api/v1/hr/attendance/clock-out`
- **Description**: Records clock out.
- **Action**:
  1. Updates the `clock_out` field for today's record.
  2. Calculates `overtime_minutes` = `clock_out - end_time`. If > 0, sets it (awaits approval).

### PUT `/api/v1/hr/attendance/{id}/approve-overtime`
- **Description**: Manager approves overtime.
- **Request Body**: `approved_minutes`.

## 5. Domain Events
- **Raised**: None immediately. Data is aggregated during payroll processing.
- **Consumed**: None.

## 6. Permissions (RBAC)
- **Employee**: Can only call clock-in/out for themselves.
- `hr.attendance.manage`: View all team logs and approve overtime (usually mapped to the `manager_id`).

## 7. Performance Considerations
- Database index on `(employee_id, date)` ensures fast upserts when clocking out.
- Caching: The current shift status (in/out) should be fetched quickly to display the correct button state on the dashboard.

## 8. Security Considerations
- **Time Spoofing**: Trust only the backend server `NOW()` function. Never accept a timestamp payload from the client for clock-in/out.
- **Location Spoofing**: Capture IP address in `audit_logs` for dispute resolution, even if geofencing isn't actively blocking.

## 9. Error Handling
- Prevent clocking out before clocking in (unless it's an overnight shift logic, which needs special date handling).

## 10. Seed Data
- Create a standard `09:00 - 18:00` shift for the initial tenant setup.
