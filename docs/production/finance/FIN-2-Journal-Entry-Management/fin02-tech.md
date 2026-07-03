# Technical Specifications: Journal Entry Management (FIN-2)

## 1. System Architecture
- **Frontend**: Next.js Client Components with Zod validation.
- **Backend API**: Next.js Server Actions / API Routes.
- **Database**: PostgreSQL (Neon). Journal Entries use a Header-Detail pattern (`journal_entries` and `journal_entry_lines`).
- **Domain Logic**: Business logic verifies mathematical balance and checks against the `periods` table to ensure the transaction date falls in an open period.

## 2. Database Schema

### Table: `journal_entries`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `entry_no` | VARCHAR(50) | UNIQUE, NOT NULL | Auto-generated standard number |
| `transaction_date`| DATE | NOT NULL | Accounting date |
| `description` | TEXT | NOT NULL | General description |
| `status` | VARCHAR(20) | NOT NULL | DRAFT, PENDING_APPROVAL, POSTED |
| `total_amount`| DECIMAL | NOT NULL | Sum of debits (should equal sum of credits) |
| `created_by` | UUID | NOT NULL, FK | Reference to users |
| `approved_by` | UUID | FK | Reference to users |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `journal_entry_lines`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `entry_id` | UUID | NOT NULL, FK | Reference to `journal_entries` |
| `account_id` | UUID | NOT NULL, FK | Reference to `chart_of_accounts` |
| `description` | TEXT | | Line specific description |
| `debit` | DECIMAL | NOT NULL | Default 0 |
| `credit` | DECIMAL | NOT NULL | Default 0 |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, date, text, decimal, timestamp } from "drizzle-orm/pg-core";
import { chartOfAccounts } from "./fin01";

export const journalEntries = pgTable("journal_entries", {
  id: uuid("id").primaryKey().defaultRandom(),
  entryNo: varchar("entry_no", { length: 50 }).notNull().unique(),
  transactionDate: date("transaction_date").notNull(),
  description: text("description").notNull(),
  status: varchar("status", { length: 20 }).notNull().default("DRAFT"),
  totalAmount: decimal("total_amount").notNull(),
  createdBy: uuid("created_by").notNull(),
  approvedBy: uuid("approved_by"),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const journalEntryLines = pgTable("journal_entry_lines", {
  id: uuid("id").primaryKey().defaultRandom(),
  entryId: uuid("entry_id").references(() => journalEntries.id).notNull(),
  accountId: uuid("account_id").references(() => chartOfAccounts.id).notNull(),
  description: text("description"),
  debit: decimal("debit").notNull().default('0'),
  credit: decimal("credit").notNull().default('0'),
});
```

## 4. API Endpoints

### POST `/api/v1/finance/journal-entries`
- **Description**: Create a new journal entry.
- **Request Body**: Header data + Array of line items.
- **Action**:
  1. Calculate sum of debits and credits from request body.
  2. Throw `422 Unprocessable Entity` if sum(Debit) != sum(Credit).
  3. Query `periods` table to ensure `transaction_date` is in an Open period.
  4. If `total_amount > 50,000,000`, set status to `PENDING_APPROVAL`, else `POSTED`.
  5. Wrap insertion of Header and Lines in a DB transaction.

### PUT `/api/v1/finance/journal-entries/{id}/approve`
- **Description**: Approve a pending entry.
- **Validation**: Enforce Segregation of Duties (approver `user_id` !== `created_by`).
- **Action**: Updates status to `POSTED` and sets `approved_by`. Dispatches `JournalEntryPosted`.

## 5. Domain Events
- **Raised**: `JournalEntryPosted` (EntryID, Amount) -> Triggers async update of Dashboard/Reporting Read Models if materialized views are used.
- **Consumed**: 
  - `ReceiptProcessed` (from WMS) -> Auto-generates a Journal Entry.
  - `ShipmentProcessed` (from WMS) -> Auto-generates a Journal Entry.
  - `PayrollProcessed` (from HR) -> Auto-generates a Journal Entry.

## 6. Permissions (RBAC)
- `finance.journal.create`: Can create draft/pending entries.
- `finance.journal.approve`: Can approve pending entries.
- `finance.journal.view`: Read-only access.

## 7. Performance Considerations
- Database index on `transaction_date` and `status` in `journal_entries` for fast filtering.
- Database index on `account_id` in `journal_entry_lines` for fast ledger aggregation during reporting.

## 8. Security Considerations
- Transaction Atomicity: If line insertion fails, the header must roll back to prevent "orphaned" unbalanced headers.

## 9. Error Handling
- Prevent Division by Zero or Overflow when dealing with massive monetary values (use DECIMAL type, not float).

## 10. Seed Data
- Create a standard Opening Balance journal entry to initialize the tenant's ledger.
