# Technical Specifications: Payroll Processing (HR-3)

## 1. System Architecture
- **Backend API**: Next.js Server Actions or API Routes to trigger the calculation.
- **Async Processing**: Payroll calculation for hundreds of employees takes time. Use Upstash QStash to queue calculation tasks per employee or in chunks, then aggregate results.
- **Database**: PostgreSQL (Neon).
- **Integration**: MediatR pattern for Domain Events to decouple HR and Finance.

## 2. Database Schema

### Table: `payroll_runs`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `period_name` | VARCHAR(50) | NOT NULL | e.g., "May 2026" |
| `start_date` | DATE | NOT NULL | |
| `end_date` | DATE | NOT NULL | |
| `status` | VARCHAR(20) | NOT NULL | DRAFT, FINALIZED |
| `total_gross` | DECIMAL | DEFAULT 0 | |
| `total_net` | DECIMAL | DEFAULT 0 | |
| `processed_by`| UUID | FK | Reference to users |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `payroll_records`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `run_id` | UUID | NOT NULL, FK | Reference to `payroll_runs` |
| `employee_id` | UUID | NOT NULL, FK | Reference to `employees` |
| `base_salary` | DECIMAL | NOT NULL | Snapshot of current salary |
| `overtime_pay`| DECIMAL | DEFAULT 0 | Calculated from Task App attendance data |
| `gross_pay` | DECIMAL | NOT NULL | |
| `tax_deduction`| DECIMAL| DEFAULT 0 | PPh 21 |
| `net_pay` | DECIMAL | NOT NULL | |
| `tenant_id` | UUID | NOT NULL, FK | |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, date, decimal, timestamp, uniqueIndex } from "drizzle-orm/pg-core";
import { employees } from "./hr01";

export const payrollRuns = pgTable("payroll_runs", {
  id: uuid("id").primaryKey().defaultRandom(),
  periodName: varchar("period_name", { length: 50 }).notNull(),
  startDate: date("start_date").notNull(),
  endDate: date("end_date").notNull(),
  status: varchar("status", { length: 20 }).notNull().default("DRAFT"),
  totalGross: decimal("total_gross").default('0').notNull(),
  totalNet: decimal("total_net").default('0').notNull(),
  processedBy: uuid("processed_by").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const payrollRecords = pgTable("payroll_records", {
  id: uuid("id").primaryKey().defaultRandom(),
  runId: uuid("run_id").references(() => payrollRuns.id).notNull(),
  employeeId: uuid("employee_id").references(() => employees.id).notNull(),
  baseSalary: decimal("base_salary").notNull(),
  overtimePay: decimal("overtime_pay").default('0').notNull(),
  grossPay: decimal("gross_pay").notNull(),
  taxDeduction: decimal("tax_deduction").default('0').notNull(),
  netPay: decimal("net_pay").notNull(),
  tenantId: uuid("tenant_id").notNull(),
}, (table) => {
  return {
    runEmployeeIdx: uniqueIndex("run_employee_idx").on(table.runId, table.employeeId),
  };
});
```

## 4. API Endpoints

### POST `/api/v1/hr/payroll/calculate`
- **Description**: Generates a Draft payroll run.
- **Action**: Aggregates attendance data from Task App API and employee data from `employees`. Calculates tax. Saves to DB.

### PUT `/api/v1/hr/payroll/{id}/finalize`
- **Description**: Locks the run.
- **Action**:
  1. Checks if Finance period is OPEN.
  2. Sets status to FINALIZED.
  3. Dispatches `PayrollProcessed` Event containing Total Gross, Taxes, and Net.

### GET `/api/v1/hr/payroll/my-payslips`
- **Description**: Employee endpoint to view their history.

## 5. Domain Events
- **Raised**: `PayrollProcessed`
- **Consumed**: Finance Module listens to `PayrollProcessed` -> Creates Journal Entry (Debit Expense, Credit Payable/Bank).

## 6. Permissions (RBAC)
- `hr.payroll.process`: Admin/HR Manager only.
- **Employee**: Can only GET their own `employee_id` payslips.

## 7. Performance Considerations
- Payroll calculation fetches attendance summary from Task App API, then joins with employee data. Do NOT run this synchronously in a single request if employees > 50. Use background workers.

## 8. Security Considerations
- The `PayrollProcessed` event handler in the Finance module must be idempotent. If it receives the same event twice, it should not create duplicate journal entries.

## 9. Error Handling
- If Finance API fails to create the journal entry (e.g., closed period), the payroll finalization must rollback the status to DRAFT and alert the user.

## 10. Document Generation
- Use `react-pdf` or a similar server-side library to generate the PDF payslip on the fly when requested, rather than storing thousands of PDF files in an S3 bucket.
