# Technical Specifications: Chart of Accounts Management (FIN-1)

## 1. System Architecture
- **Frontend**: Next.js Client Components (Tree rendering is client-side for interactivity).
- **Backend**: API Routes handling validation (circular reference checks).
- **Database**: PostgreSQL (Neon) using adjacency list model (`parent_id`) for hierarchy.

## 2. Database Schema

### Table: `chart_of_accounts`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `code` | VARCHAR(20) | NOT NULL | Account code (e.g., "1110") |
| `name` | VARCHAR(100) | NOT NULL | Account name |
| `parent_id` | UUID | FK | Self-referencing FK to `chart_of_accounts.id` |
| `type` | VARCHAR(20) | NOT NULL | Enum: ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE |
| `is_active` | BOOLEAN | DEFAULT TRUE| |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (tenant_id, code)`: Account codes must be unique per tenant.

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, boolean, timestamp, uniqueIndex, AnyPgColumn } from "drizzle-orm/pg-core";

export const chartOfAccounts = pgTable("chart_of_accounts", {
  id: uuid("id").primaryKey().defaultRandom(),
  code: varchar("code", { length: 20 }).notNull(),
  name: varchar("name", { length: 100 }).notNull(),
  parentId: uuid("parent_id").references((): AnyPgColumn => chartOfAccounts.id),
  type: varchar("type", { length: 20 }).notNull(), // ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
  isActive: boolean("is_active").default(true).notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
  updatedAt: timestamp("updated_at").defaultNow().notNull(),
}, (table) => {
  return {
    tenantCodeIdx: uniqueIndex("tenant_code_idx").on(table.tenantId, table.code),
  };
});
```

## 4. API Endpoints

### GET `/api/v1/finance/chart-of-accounts`
- **Description**: Fetch the COA.
- **Query Params**: `parent_id` (optional, to fetch a specific branch), `flat` (boolean, returns flat list vs nested JSON).
- **Action**: Returns accounts ordered by `code`.

### POST `/api/v1/finance/chart-of-accounts`
- **Description**: Create a new account.
- **Request Body**: `code`, `name`, `parent_id`, `type`.
- **Validation**: Enforce uniqueness of `code` within `tenant_id`. If `parent_id` is provided, verify it exists and inherit its `type`.

### PUT `/api/v1/finance/chart-of-accounts/{id}`
- **Description**: Update account details.
- **Validation**: 
  - Cannot change `type` if there are existing journal entries.
  - Cycle Detection: Use a recursive query (CTE) in Postgres or traverse up the tree to ensure the new `parent_id` is not a descendant of `{id}`.

### DELETE `/api/v1/finance/chart-of-accounts/{id}`
- **Description**: Soft-delete (or block deletion if used).
- **Validation**: Check `journal_entry_lines` to see if `account_id` is in use. If used -> return 400 "Account in use".

## 5. Domain Events
- **Raised**: `AccountCreated`, `AccountUpdated`
- **Consumed**: None directly.

## 6. Permissions (RBAC)
- `finance.coa.read`: View the COA.
- `finance.coa.manage`: Create/Update/Deactivate accounts.

## 7. Performance Considerations
- Use caching (Upstash Redis) for the full COA list per tenant, as it rarely changes but is queried frequently by dropdowns in other modules. Invalidate cache on PUT/POST/DELETE.

## 8. Security Considerations
- Prevent SQL injection in the cycle detection CTE if using raw queries.
- Validate `tenant_id` meticulously.

## 9. Error Handling Strategy
- Return `409 Conflict` if the `tenantCodeIdx` uniqueness constraint is violated.

## 10. Seed Data
- Provide a standard IFRS/GAAP baseline COA template when a new tenant registers (e.g., standard 1000-5000 structure).
