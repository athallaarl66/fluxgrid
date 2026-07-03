# Technical Specifications: Journal Entry Management (FIN-2)

## 1. System Architecture
- **Frontend**: Next.js Client Components with Zod validation.
- **Backend**: .NET 8 Minimal API (Modular Monolith).
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

## 3. Entity Framework Core Schema Snippets

### Entity: `JournalEntry`
```csharp
namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntryNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT"; // DRAFT, PENDING_APPROVAL, POSTED
    public decimal TotalAmount { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<JournalEntryLine> Lines { get; set; } = [];
}
```

### Entity: `JournalEntryLine`
```csharp
namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class JournalEntryLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public JournalEntry Entry { get; set; } = null!;
    public Guid AccountId { get; set; }
    public ChartOfAccount Account { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
```

### EF Core Configuration (in `AppDbContext.OnModelCreating`)
```csharp
modelBuilder.Entity<JournalEntry>(entity =>
{
    entity.ToTable("journal_entries");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.EntryNo).IsUnique();
    entity.Property(e => e.EntryNo).HasMaxLength(50).IsRequired();
    entity.Property(e => e.TransactionDate).IsRequired();
    entity.Property(e => e.Description).IsRequired();
    entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("DRAFT");
    entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

    entity.HasMany(e => e.Lines)
          .WithOne(e => e.Entry)
          .HasForeignKey(e => e.EntryId)
          .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<JournalEntryLine>(entity =>
{
    entity.ToTable("journal_entry_lines");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Debit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
    entity.Property(e => e.Credit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
    entity.HasOne(e => e.Account)
          .WithMany()
          .HasForeignKey(e => e.AccountId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

## 4. API Endpoints

### GET `/api/v1/finance/journal-entries`
- **Description**: List journal entries with pagination and filtering.
- **Query Params**: `status` (optional), `from_date` (optional), `to_date` (optional), `page`, `page_size`.
- **Required Permission**: `finance.journal.view`

### GET `/api/v1/finance/journal-entries/{id}`
- **Description**: Get a single journal entry with its lines.
- **Required Permission**: `finance.journal.view`

### POST `/api/v1/finance/journal-entries`
- **Description**: Create a new journal entry.
- **Request Body**: Header data + Array of line items.
- **Action**:
  1. Calculate sum of debits and credits from request body.
  2. Throw `422 Unprocessable Entity` if sum(Debit) != sum(Credit).
  3. Query `periods` table to ensure `transaction_date` is in an Open period.
  4. If `total_amount > 50,000,000`, set status to `PENDING_APPROVAL`, else `POSTED`.
  5. Wrap insertion of Header and Lines in a DB transaction.
- **Required Permission**: `finance.journal.create`

### PUT `/api/v1/finance/journal-entries/{id}/approve`
- **Description**: Approve a pending entry.
- **Validation**: Enforce Segregation of Duties (approver `user_id` !== `created_by`).
- **Action**: Updates status to `POSTED` and sets `approved_by`. Dispatches `JournalEntryPosted`.
- **Required Permission**: `finance.journal.approve`

### DELETE `/api/v1/finance/journal-entries/{id}`
- **Description**: Void/cancel a draft journal entry. Cannot void POSTED entries.
- **Action**: Soft-delete (sets status to `VOID`), not actually removed from DB.
- **Required Permission**: `finance.journal.create`

## 5. Domain Events

### Raised
```csharp
public record JournalEntryPosted(Guid JournalEntryId, decimal TotalAmount, DateTime PostedDate) : IDomainEvent;
```

### Consumed
| Event | Source Module | Action |
|-------|--------------|--------|
| `ReceiptProcessed` | WMS | Auto-generates purchase journal entry |
| `ShipmentProcessed` | WMS | Auto-generates COGS journal entry |
| `PayrollProcessed` | HR | Auto-generates salary & tax journal entries |

## 6. Permissions (RBAC)
Defined in `Shared/RBAC/Permissions.cs`:
```csharp
public const string FinanceJournalView = "finance.journal.view";
public const string FinanceJournalCreate = "finance.journal.create";
public const string FinanceJournalApprove = "finance.journal.approve";
```

| Permission | Description |
|------------|-------------|
| `finance.journal.view` | Read-only access to journal entries |
| `finance.journal.create` | Can create draft/pending entries and void drafts |
| `finance.journal.approve` | Can approve pending entries (must differ from creator) |

## 7. Performance Considerations
- Database index on `transaction_date` and `status` in `journal_entries` for fast filtering.
- Database index on `account_id` in `journal_entry_lines` for fast ledger aggregation during reporting.

## 8. Security Considerations
- Transaction Atomicity: If line insertion fails, the header must roll back to prevent "orphaned" unbalanced headers.

## 9. Error Handling

| HTTP Status | Condition | Error Code |
|-------------|-----------|------------|
| 400 | Debit/Credit mismatch | `UNBALANCED_ENTRY` |
| 400 | Period is closed | `PERIOD_CLOSED` |
| 400 | Account is inactive | `ACCOUNT_INACTIVE` |
| 400 | Account not found | `ACCOUNT_NOT_FOUND` |
| 400 | Self-approval attempt | `SELF_APPROVAL_DENIED` |
| 400 | Cannot void posted entry | `CANNOT_VOID_POSTED` |
| 409 | Duplicate entry number | `DUPLICATE_ENTRY_NO` |

- Prevent Division by Zero or Overflow when dealing with massive monetary values (use DECIMAL type, not float).
- Wrap header + lines insert in a DB transaction — if line insert fails, header rolls back.

## 10. Seed Data
- Create a standard Opening Balance journal entry to initialize the tenant's ledger.
