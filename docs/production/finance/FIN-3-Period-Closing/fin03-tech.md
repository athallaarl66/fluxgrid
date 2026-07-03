# Technical Specifications: Period Closing (FIN-3)

## 1. System Architecture
- **Frontend**: Next.js Client Components for the Dashboard and Modals.
- **Backend API**: Next.js API Routes for validation and state updates.
- **Database**: PostgreSQL (Neon). The `periods` table dictates the open/close boundaries for the entire ERP system.

## 2. Database Schema

### Table: `accounting_periods`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `name` | VARCHAR(50) | NOT NULL | e.g., "June 2026" |
| `start_date` | DATE | NOT NULL | 2026-06-01 |
| `end_date` | DATE | NOT NULL | 2026-06-30 |
| `status` | VARCHAR(20) | NOT NULL | OPEN, CLOSED |
| `closed_by` | UUID | FK | Reference to users |
| `closed_at` | TIMESTAMP | | |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (tenant_id, start_date, end_date)`: Prevent overlapping periods within a tenant.

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, date, timestamp, uniqueIndex } from "drizzle-orm/pg-core";

export const accountingPeriods = pgTable("accounting_periods", {
  id: uuid("id").primaryKey().defaultRandom(),
  name: varchar("name", { length: 50 }).notNull(),
  startDate: date("start_date").notNull(),
  endDate: date("end_date").notNull(),
  status: varchar("status", { length: 20 }).notNull().default("OPEN"),
  closedBy: uuid("closed_by"),
  closedAt: timestamp("closed_at"),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
}, (table) => {
  return {
    tenantDatesIdx: uniqueIndex("tenant_dates_idx").on(table.tenantId, table.startDate, table.endDate),
  };
});
```

## 4. API Endpoints

### GET `/api/v1/finance/periods`
- **Description**: List all accounting periods.

### GET `/api/v1/finance/periods/{id}/validate`
- **Description**: Pre-close validation check.
- **Action**: Queries `journal_entries` where `transaction_date` is between `start_date` and `end_date` and `status` IN ('DRAFT', 'PENDING_APPROVAL').
- **Returns**: Array of blocking entry IDs or `[]` if clear.

### POST `/api/v1/finance/periods/{id}/close`
- **Description**: Close the period.
- **Action**: 
  1. Re-runs the validation query to prevent race conditions.
  2. Updates `status` to CLOSED, sets `closed_by` and `closed_at`.
  3. Publishes `PeriodClosed` event.

### POST `/api/v1/finance/periods/{id}/reopen`
- **Description**: Re-open a closed period.
- **Request Body**: `reason` (string, required).
- **Action**:
  1. Updates `status` to OPEN, clears `closed_by` and `closed_at`.
  2. Writes a critical entry to the `audit_logs` including the reason.

## 5. Domain Events
- **Raised**: 
  - `PeriodClosed` -> Signals to reporting services to cache final snapshots.
  - `PeriodReopened` -> Signals to invalidate cached reports.
- **Consumed**: None.

## 6. Permissions (RBAC)
- `finance.period.read`: View periods.
- `finance.period.admin`: Close or Re-open periods. (Highly restricted).

## 7. Performance Considerations
- The validation query requires an index on `transaction_date` and `status` in the `journal_entries` table. Without it, validating a heavy month with millions of transactions will cause the API to time out.

## 8. Security Considerations
- **Global Middleware check**: A shared utility function `validateOpenPeriod(date)` must be injected into all creation endpoints in WMS (Receipts, Shipments), HR (Payroll), and Finance (Journal Entries) to ensure no inserts bypass the lock.

## 9. Error Handling Strategy
- Return `403 Forbidden` if a transaction attempts to insert data outside of an OPEN period.

## 10. Seed Data
- A script to auto-generate 12 monthly periods for the current fiscal year when a new tenant is created.
