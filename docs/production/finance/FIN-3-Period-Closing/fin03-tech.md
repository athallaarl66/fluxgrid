# Technical Specifications: Period Closing (FIN-3)

## 1. System Architecture
- **Frontend**: Next.js Client Components for the Dashboard and Modals.
- **Backend API**: .NET 8 Minimal APIs (Carter pattern) for validation and state updates.
- **Database**: PostgreSQL (Neon). The `accounting_periods` table dictates the open/close boundaries for the entire ERP system.

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
| `row_version` | BYTEA | | Optimistic concurrency token |

**Constraints**:
- `UNIQUE (tenant_id, start_date, end_date)`: Prevent overlapping periods within a tenant.

## 3. EF Core Entity Configuration

The `AccountingPeriod` entity is mapped to the `accounting_periods` table via EF Core Fluent API in `AppDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<AccountingPeriod>(entity =>
{
    entity.ToTable("accounting_periods");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.TenantId, e.StartDate, e.EndDate }).IsUnique();
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.Property(e => e.StartDate).IsRequired();
    entity.Property(e => e.EndDate).IsRequired();
    entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("OPEN");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
});
```

The entity class includes a `RowVersion` property (annotated with `[Timestamp]`) for optimistic concurrency control:

```csharp
public class AccountingPeriod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
```

## 4. API Endpoints

### GET `/api/v1/finance/periods`
- **Description**: List all accounting periods.

### GET `/api/v1/finance/periods/{id}/validate`
- **Description**: Pre-close validation check.
- **Action**: Queries `journal_entries` where `transaction_date` is between `start_date` and `end_date` and `status` IN ('DRAFT', 'PENDING_APPROVAL').
- **Returns**:
  ```json
  {
    "canClose": true,
    "blockingEntryIds": [],
    "message": "Period can be closed"
  }
  ```
  When blocking entries exist: `canClose` = false, `blockingEntryIds` contains the IDs, `message` explains the count.

### POST `/api/v1/finance/periods/{id}/close`
- **Description**: Close the period.
- **Action**: 
  1. Re-runs the validation query to prevent race conditions.
  2. Updates `status` to CLOSED, sets `closed_by` and `closed_at`.
  3. Publishes `PeriodClosed` event.

### POST `/api/v1/finance/periods/{id}/reopen`
- **Description**: Re-open a closed period.
- **Request Body**: `reason` (string, required, min 10 characters).
- **Action**:
  1. Updates `status` to OPEN, clears `closed_by` and `closed_at`.
  2. Writes a critical entry to the `audit_logs` including the reason.
  3. Raises `PeriodReopened` domain event.

### POST `/api/v1/finance/periods/generate`
- **Description**: Auto-generate missing accounting periods for the tenant.
- **Action**: Scans for missing months across previous, current, and next fiscal year (36 months total). Creates only periods that do not already exist by name.
- **Returns**: `{ "generated": 3 }` — count of newly created periods.

## 5. Domain Events
- **Raised** (via `DomainEventDispatcher`):
  - `PeriodClosed` (PeriodId, PeriodName, StartDate, EndDate, ClosedBy, TenantId) -> Signals to reporting services to cache final snapshots.
  - `PeriodReopened` (PeriodId, PeriodName, StartDate, EndDate, Reason, ReopenedBy, TenantId) -> Signals to invalidate cached reports.
- **Consumed**: None.

## 6. Permissions (RBAC)
- `finance.period.read`: View periods.
- `finance.period.admin`: Close or Re-open periods. (Highly restricted).

## 7. Performance Considerations
- The validation query requires an index on `(transaction_date, status)` in the `journal_entries` table. Without it, validating a heavy month with millions of transactions will cause the API to time out.
- The `accounting_periods` table has a unique composite index on `(tenant_id, start_date, end_date)` to enforce domain integrity and speed up tenant-scoped queries.

## 8. Security Considerations
- **Global middleware check**: A shared `PeriodValidator` service (registered as scoped in DI) exposes `ValidateOpenPeriod(date)` that must be injected into all creation endpoints in WMS (Receipts, Shipments), HR (Payroll), and Finance (Journal Entries) to ensure no inserts bypass the lock.
- **Optimistic concurrency**: The `RowVersion` column on `accounting_periods` prevents race conditions when two admins attempt to close the same period simultaneously. A `DbUpdateConcurrencyException` results in a user-friendly error message.

## 9. Error Handling Strategy
- Return `403 Forbidden` if a transaction attempts to insert data outside of an OPEN period.

## 10. Seed Data
- `AccountingPeriodSeeder.SeedAsync(db, tenantId)` generates 12 monthly periods for the current calendar year when a new tenant is initialized.
- The `DataSeeder` calls this seeder after seeding Chart of Accounts.
- A separate `POST /api/v1/finance/periods/generate` endpoint fills gaps across 3 fiscal years (previous, current, next) on demand.
