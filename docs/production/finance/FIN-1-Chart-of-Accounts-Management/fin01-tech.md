# Technical Specifications: Chart of Accounts Management (FIN-1)

## 1. System Architecture
- **Frontend**: Next.js Client Components (Tree rendering is client-side for interactivity).
- **Backend**: .NET 8 Minimal API (Modular Monolith) with Clean Architecture.
- **Database**: PostgreSQL (Neon) using adjacency list model (`parent_id`) for hierarchy.
- **ORM**: Entity Framework Core 8.0 with Npgsql.
- **Auth**: JWT disimpan di httpOnly cookie `token` oleh frontend, backend baca dari cookie lewat `OnMessageReceived` event. Role `Admin` bersifat super admin — bypass semua permission check (logic `RequireAssertion` di `Program.cs`).

## 2. Database Schema

### Table: `chart_of_accounts`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `Id` | UUID | PRIMARY KEY | Unique identifier |
| `Code` | VARCHAR(20) | NOT NULL | Account code (e.g., "1110") |
| `Name` | VARCHAR(100) | NOT NULL | Account name |
| `ParentId` | UUID | FK | Self-referencing FK to `chart_of_accounts.Id` |
| `Type` | VARCHAR(20) | NOT NULL | ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE |
| `IsActive` | BOOLEAN | DEFAULT TRUE | |
| `TenantId` | UUID | NOT NULL | Multi-tenancy isolation |
| `CreatedAt` | TIMESTAMP | DEFAULT NOW() | |
| `UpdatedAt` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (TenantId, Code)`: Account codes must be unique per tenant.

### Table: `audit_logs`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `Id` | UUID | PRIMARY KEY | |
| `Timestamp` | TIMESTAMP | DEFAULT NOW() | |
| `UserId` | UUID | NOT NULL | Actor |
| `TenantId` | UUID | NOT NULL | |
| `Action` | VARCHAR(50) | NOT NULL | CREATE, UPDATE, DEACTIVATE |
| `ResourceType` | VARCHAR(100) | NOT NULL | "chart_of_accounts" |
| `ResourceId` | UUID | NOT NULL | Account ID |
| `IpAddress` | VARCHAR(45) | | |
| `UserAgent` | VARCHAR(500) | | |
| `ChangesJson` | TEXT | | Before/after snapshots |

## 3. Entity Framework Core Entities

### Entity: `ChartOfAccount`
```csharp
namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class ChartOfAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public ChartOfAccount? Parent { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChartOfAccount> Children { get; set; } = [];
}
```

### Entity: `AuditLog`
```csharp
namespace FluxGrid.Api.Shared.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ChangesJson { get; set; }
}
```

### EF Core Configuration (in `AppDbContext.OnModelCreating`)
```csharp
modelBuilder.Entity<ChartOfAccount>(entity =>
{
    entity.ToTable("chart_of_accounts");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
    entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.Property(e => e.Type).HasMaxLength(20).IsRequired();
    entity.Property(e => e.IsActive).HasDefaultValue(true);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
    entity.HasOne(e => e.Parent)
          .WithMany(e => e.Children)
          .HasForeignKey(e => e.ParentId)
          .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<AuditLog>(entity =>
{
    entity.ToTable("audit_logs");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
    entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
    entity.Property(e => e.IpAddress).HasMaxLength(45);
    entity.Property(e => e.UserAgent).HasMaxLength(500);
    entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
    entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
    entity.HasIndex(e => e.Timestamp);
});
```

## 4. API Endpoints

### GET `/api/v1/finance/chart-of-accounts`
- **Description**: Fetch the COA as nested tree or flat list.
- **Query Params**: `flat` (boolean? — nullable, default false kalo tidak dikirim).
- **Action**: Returns all accounts for the tenant ordered by `code`, built into a recursive tree via lookup.
- **Required Permission**: `finance.coa.read` (super admin role "Admin" bypass)

### POST `/api/v1/finance/chart-of-accounts`
- **Description**: Create a new account.
- **Request Body**: `code`, `name`, `parent_id` (optional), `type`, `is_active` (optional, default true).
- **Validation**:
  - Account type must be one of: ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE.
  - `code` must be unique within `tenant_id` (explicit check before DB constraint).
  - If `parent_id` provided: parent must exist and be active, depth must be < 5 levels, type auto-inherited from parent.
- **Audit**: Creates audit log entry with `action=CREATE` and new value snapshot.
- **Required Permission**: `finance.coa.manage` (super admin role "Admin" bypass)

### PUT `/api/v1/finance/chart-of-accounts/{id}`
- **Description**: Update account details.
- **Request Body**: Partial update — any of `code`, `name`, `parent_id`, `type`, `is_active`.
- **Validation**:
  - Code uniqueness check on change (exclude self).
  - Cycle detection: traverses ancestors of candidate parent to ensure it's not a descendant of the target account.
  - Can set `is_active=false` to deactivate (cascades to all children).
- **Audit**: Captures before/after snapshots, logs `action=UPDATE`.
- **Required Permission**: `finance.coa.manage` (super admin role "Admin" bypass)

### DELETE `/api/v1/finance/chart-of-accounts/{id}`
- **Description**: Deactivate an account (soft-delete). Cannot deactivate if already inactive.
- **Validation**: Checks `journal_entry_lines` for existing references (placeholder until FIN-2). If has entries → 400 "Account in use".
- **Action**: Sets `is_active=false` on the account and all descendants (cascade).
- **Audit**: Captures before/after snapshots, logs `action=DEACTIVATE`.
- **Required Permission**: `finance.coa.manage` (super admin role "Admin" bypass)

## 5. Service Layer

Located at `Modules/Finance/Application/ChartOfAccountService.cs`.

Key validations:
- **Code uniqueness**: Explicit `AnyAsync` check on `(TenantId, Code)` before create and before update.
- **Circular reference**: `IsDescendantAsync` — traverses parent chain upwards from candidate parent, returns true if it reaches the target account (O(depth) loop, max 5 iterations).
- **Hierarchy depth**: `GetDepthAsync` — traverses parent chain upwards, counts levels. Rejects if depth >= 4 (meaning level 5).
- **Type inheritance**: On create with `parent_id`, account type is auto-set from parent. On top-level create, type must be explicitly provided.
- **Cascade deactivation**: `DeactivateCascadeAsync` — recursively finds all children and sets `is_active=false`.
- **Deactivation guard**: `HasAssociatedEntriesAsync` — checks `journal_entry_lines` for references. Placeholder until FIN-2 is built.

## 6. Domain Events

Domain events implement `IDomainEvent` interface (`Shared/Domain/Events/IDomainEvent.cs`). Events are raised via `DomainEventDispatcher` (`Shared/Infrastructure/Events/DomainEventDispatcher.cs`) — a lightweight in-memory dispatcher that can be replaced with MediatR in the future.

### Raised

```csharp
// File: Shared/Domain/Events/AccountCreated.cs
public sealed record AccountCreated(
    Guid AccountId, string Code, string Name, string Type,
    Guid? ParentId, Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// File: Shared/Domain/Events/AccountUpdated.cs
public sealed record AccountUpdated(
    Guid AccountId, string Code, string Name, string Type,
    bool IsActive, Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

| Event | Raised By | When |
|-------|-----------|------|
| `AccountCreated` | `POST /chart-of-accounts` | After account is saved to DB |
| `AccountUpdated` | `PUT /chart-of-accounts/{id}` | After account details are updated |
| `AccountUpdated` | `DELETE /chart-of-accounts/{id}` | After account is deactivated |

- **Consumed**: None directly in this iteration. Future modules may subscribe (e.g., Reporting read model refresh, WMS integration).

## 7. Permissions (RBAC)

Defined in `Shared/RBAC/Permissions.cs`:
```csharp
public const string FinanceCoaRead = "finance.coa.read";
public const string FinanceCoaManage = "finance.coa.manage";
```

| Permission | Description |
|------------|-------------|
| `finance.coa.read` | View the COA tree and account details |
| `finance.coa.manage` | Create, update, and deactivate accounts |

Pre-seeded roles:
- **Admin**: Both permissions (via `Permissions.All`)
- **Manager**: Both permissions
- **Staff**: `finance.coa.read` only

## 8. Audit Trail

Every mutation (CREATE, UPDATE, DEACTIVATE) on `chart_of_accounts` is logged to the `audit_logs` table via `AuditService` (`Shared/Infrastructure/Audit/AuditService.cs`).

Each entry captures:
- **Actor**: `UserId` from JWT claim `NameIdentifier`
- **Tenant**: `TenantId` from JWT claim `tenant_id`
- **Action**: `CREATE`, `UPDATE`, or `DEACTIVATE`
- **Resource**: `resource_type="chart_of_accounts"`, `resource_id=<account UUID>`
- **Snapshot**: `ChangesJson` containing `{ old_value: {...}, new_value: {...} }` serialized as JSON
- **Metadata**: `IpAddress`, `UserAgent` from HTTP request

## 9. Caching

Caching is implemented via `ICacheService` interface (`Shared/Infrastructure/Caching/ICacheService.cs`) with an initial `MemoryCacheService` backed by `IMemoryCache` (`Shared/Infrastructure/Caching/MemoryCacheService.cs`).

| Aspect | Detail |
|--------|--------|
| Cache key | `finance:chart-of-accounts:{tenantId}` |
| Strategy | Cache-aside — read from cache, fall back to DB |
| Invalidation | On POST, PUT, DELETE — remove cache entry |
| TTL | 30 minutes sliding expiration |
| Future swap | Replace `MemoryCacheService` registration with `StackExchange.Redis` implementation |

- COA rarely changes (< 500 accounts per tenant) — full tree fetch is acceptable for most queries.

## 10. Error Handling

| HTTP Status | Condition | Error Code |
|-------------|-----------|------------|
| 400 | Invalid account type | `INVALID_ACCOUNT_TYPE` |
| 400 | Duplicate account code | `ACCOUNT_CODE_DUPLICATE` |
| 400 | Parent account not found | `PARENT_NOT_FOUND` |
| 400 | Max hierarchy depth (5) exceeded | `MAX_DEPTH_EXCEEDED` |
| 400 | Deactivated parent | `PARENT_INACTIVE` |
| 400 | Self-reference as parent | `SELF_PARENT_REFERENCE` |
| 400 | Circular reference detected | `CIRCULAR_REFERENCE` |
| 400 | Account has journal entries | `ACCOUNT_IN_USE` |
| 400 | Account already deactivated | `ALREADY_DEACTIVATED` |
| 404 | Account not found | `ACCOUNT_NOT_FOUND` |

## 11. Seed Data

Seeded via `ChartOfAccountSeeder` (`Shared/Infrastructure/Seed/ChartOfAccountSeeder.cs`) — idempotent (skips if accounts already exist for tenant).

- **Tenant**: Uses `DefaultTenantId` (`00000000-0000-0000-0000-000000000001`) for initial tenant.
- **Trigger**: Called from `DataSeeder.SeedAsync` every startup (even if roles already exist).
- **Format**: Declarative tuple array `(code, name, type, parentCode)` — parent di-refer via kode, bukan object reference.
- **Template**: Standard IFRS/GAAP baseline COA — 33 accounts total (5 top-level + 28 sub-accounts):

```
Level 1          Level 2               Level 3
1000 Assets
├── 1100 Current Assets
│   ├── 1110 Cash in Bank
│   ├── 1120 Accounts Receivable
│   ├── 1130 Inventory
│   └── 1140 Prepaid Expenses
└── 1200 Fixed Assets
    ├── 1210 Land
    ├── 1220 Buildings
    ├── 1230 Machinery & Equipment
    └── 1240 Accumulated Depreciation

2000 Liabilities
├── 2100 Current Liabilities
│   ├── 2110 Accounts Payable
│   ├── 2120 Accrued Expenses
│   └── 2130 Short-Term Debt
└── 2200 Long-term Liabilities
    ├── 2210 Long-Term Debt
    └── 2220 Deferred Tax Liabilities

3000 Equity
├── 3100 Share Capital
├── 3200 Retained Earnings
└── 3300 Current Year Earnings

4000 Revenue
├── 4100 Sales Revenue
│   ├── 4110 Product Sales
│   └── 4120 Service Revenue
└── 4200 Other Income

5000 Expenses
├── 5100 Cost of Goods Sold
├── 5200 Operating Expenses
│   ├── 5210 Salaries Expense
│   ├── 5220 Rent Expense
│   ├── 5230 Utilities Expense
│   └── 5240 Depreciation Expense
└── 5300 Other Expenses
```

- **Future**: On new tenant registration, call `ChartOfAccountSeeder.SeedAsync(db, tenantId)` with the new tenant's ID.

## 12. Frontend — Route & Page

### Route Structure
| Route | File | Description |
|-------|------|-------------|
| `/finance/layout.tsx` | `app/finance/layout.tsx` | Authenticated shell: `AuthProvider` + `Sidebar` + `Header` + `Footer` (mirrors `dashboard/layout.tsx`) |
| `/finance/chart-of-accounts` | `app/finance/chart-of-accounts/page.tsx` | Main COA page — client component that orchestrates all sub-components |

### Sidebar Navigation
- **Nav item**: `Wallet` icon → label "Finance" → href `/finance`
- **Sub-item**: "Chart of Accounts" → href `/finance/chart-of-accounts`
- Rendered with chevron indicator when active; sub-items shown in an indented, bordered list below the parent.
- Active state: parent highlights when a child is active (`pathname.startsWith(child.href)`).

### Header
- **Right side**: Theme toggle, Notifications, Grid menu, User avatar (Logout dropdown).
- **Logout**: POST `/api/auth/logout` → clear httpOnly cookie `token` → redirect `/login`.

### Route Guard
```
UNAUTHENTICATED (no user in context)
  → useEffect redirect /login?redirect=/finance/chart-of-accounts

FORBIDDEN (API returns 403)
  → show "Access Denied" page with:
      - AlertTriangle icon
      - "Access Denied" heading
      - "You do not have the required permission (finance.coa.read)"
      - "Contact your administrator" note

OTHER ERRORS (network error, 500)
  → show error state with Retry button

LOADING
  → show Skeleton placeholders

EMPTY (no accounts exist)
  → show empty state in CoaTable:
      - "No accounts yet" / "No accounts match your search"
```

### Page State Machine
```
authLoading ──→ [Skeleton]
    ↓
no user ──→ redirect /login
    ↓
isError ──→ error state (403 → permission denied, else retry)
    ↓
isLoading ──→ skeleton rows
    ↓
data ──→ CoaTable (desktop) / CoaMobileList (mobile)
```

## 13. Frontend — Components

### Component Tree
```
page.tsx
├── CoaTable (desktop, ≥768px)
│   ├── Search input (filter by code/name)
│   ├── Type filter dropdown (All / ASSET / LIABILITY / EQUITY / REVENUE / EXPENSE)
│   ├── Refresh button
│   ├── "New Account" Button (→ opens AccountFormModal)
│   ├── Table header: Code | Name | Parent | Type | Status | Actions
│   ├── Table rows:
│   │   ├── Code (tabular-nums monospace)
│   │   ├── Name (bold for top-level)
│   │   ├── Parent name (from breadcrumb path)
│   │   ├── Type badge (color-coded per category)
│   │   ├── Status dot (green=active / red=inactive)
│   │   └── Actions (Edit pencil + Deactivate trash, show on hover)
│   └── Pagination footer
│       ├── Rows per page selector (5 / 10 / 20 / 50)
│       ├── Page info (1–10 of 33)
│       └── Page number buttons (first, prev, 1…n…last, next)
├── CoaMobileList (mobile, <768px)
│   └── Flat list item
│       ├── Code + status badge
│       ├── Name (truncated)
│       ├── Breadcrumb path (e.g., "Assets > Current Assets > Cash")
│       └── Chevron (→ opens edit)
├── AccountFormModal (dialog overlay)
│   ├── Code input (required)
│   ├── Name input (required)
│   ├── Type select (auto-filled from parent, disabled if parent set)
│   ├── Combobox (parent account selector)
│   ├── Active/Inactive toggle (dot hijau/merah)
│   └── Cancel / Create|Update buttons
└── DeactivateConfirmModal (dialog overlay)
    ├── "Are you sure?" message
    └── Cancel / Deactivate buttons
```

### File Inventory

| File | Lines | Type | Responsibility |
|------|-------|------|----------------|
| `components/finance/CoaTable.tsx` | ~190 | Client | Flat table with search, type filter, pagination, hover actions |
| `components/finance/CoaMobileList.tsx` | ~75 | Client | Flat list using `flattenTree()` utility, renders breadcrumb path per item |
| `components/finance/AccountFormModal.tsx` | ~135 | Client | Controlled form: `useState` per field, `useEffect` to reset on open, `findAccount` recursive lookup for parent type inheritance |
| `components/finance/Combobox.tsx` | ~100 | Client | Searchable dropdown: `useState` for open/query, `useEffect` outside-click listener, filters by code/name |
| `components/Header.tsx` | ~120 | Client | Header bar with theme toggle, notifications, grid menu, user avatar with Logout dropdown |
| `components/Sidebar.tsx` | ~160 | Client | Fixed sidebar with nav items, expandable children, active state tracking |

### Key Behaviors

**Pagination** (CoaTable)
- Page sizes: 5, 10, 20, 50 rows per page.
- Page number buttons with ellipsis for large page counts.
- Reset to page 0 on search query or type filter change.

**Search & Filter** (CoaTable)
- Search by code or name (client-side, instant).
- Type filter dropdown (All, ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE).
- Both filters stack.

**Form Type Inheritance** (AccountFormModal)
- When `parentId` changes and is non-null, `handleParentChange` looks up the parent's type and auto-sets it.
- Type dropdown is disabled when a parent is selected.
- User can override by clearing parent first, then selecting type.

**Combobox Click-Outside** (Combobox)
- `useEffect` with `document.addEventListener("mousedown", ...)` dismisses dropdown.
- Cleanup on unmount.

**Logout** (Header)
- Klik avatar user → dropdown menu → "Logout".
- POST `/api/auth/logout` → server hapus httpOnly cookie `token` → redirect `/login`.

**Responsive** (page.tsx)
- Desktop (≥768px): `hidden md:block` → `CoaTable`
- Mobile (<768px): `md:hidden` → `CoaMobileList`
- Mobile items show breadcrumb path instead of table.

## 14. Frontend — Data Flow

### Types (`lib/coa-types.ts`)
```typescript
type AccountType = "ASSET" | "LIABILITY" | "EQUITY" | "REVENUE" | "EXPENSE";

interface AccountResponse {
  id: string; code: string; name: string;
  parentId: string | null;
  type: AccountType;
  isActive: boolean;
  children: AccountResponse[];
}

interface CreateAccountRequest {
  code: string; name: string;
  parentId?: string | null;
  type?: string; isActive?: boolean;
}

type UpdateAccountRequest = Partial<CreateAccountRequest>;

// Utility
function flattenTree(accounts, level, parentPath): FlatAccount[];
interface FlatAccount extends AccountResponse {
  level: number; path: string;  // e.g. "Assets > Current Assets > Cash"
}
```

### TanStack Query Hooks (`hooks/useCoa.ts`)

| Hook | Method | Mutation Key | Query Invalidation |
|------|--------|-------------|-------------------|
| `useCoaTree()` | GET | `["coa"]` | — |
| `useCreateAccount()` | POST | — | `["coa"]` on success |
| `useUpdateAccount()` | PUT | — | `["coa"]` on success |
| `useDeactivateAccount()` | DELETE | — | `["coa"]` on success |

- All use `apiClient` from `@/lib/api-client` with `credentials: "include"`.
- Query config: `staleTime: 30_000`, `retry: 1` (from global `<Providers>`).
- On mutation success, the `["coa"]` query is invalidated → tree auto-refetches.

### API Endpoints Consumed
```
GET    /api/v1/finance/chart-of-accounts   → AccountResponse[]
POST   /api/v1/finance/chart-of-accounts   → AccountResponse  (201)
PUT    /api/v1/finance/chart-of-accounts/{id} → AccountResponse (200)
DELETE /api/v1/finance/chart-of-accounts/{id} → AccountResponse (200)
```

---

## 15. Ringkasan Fitur

**Chart of Accounts (COA)** adalah fondasi sistem finance ERP — daftar akun yang mengkategorikan setiap transaksi keuangan perusahaan ke dalam 5 tipe: ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE.

### Kenapa ini penting?
- **Akuntansi standar**: Setiap perusahaan butuh COA buat mencatat transaksi sesuai standar akuntansi (PSAK/IFRS).
- **Hierarki**: Akun punya parent-child (misal "1110 Cash in Bank" anak dari "1100 Current Assets").
- **Multi-tenant**: Setiap tenant punya COA sendiri, terisolasi.
- **Audit trail**: Setiap perubahan COA tercatat immutable — siapa, kapan, apa yang berubah.
- **Seed data**: Tenant baru langsung dapet template COA standar IFRS (33 akun) jadi tinggal pakai.

### Siapa yang akses?
| Role | Akses |
|------|-------|
| **Super Admin** (`admin`) | Bypass semua permission — bisa create/edit/deactivate akun |
| **Manager** | Bisa lihat dan manage COA |
| **Staff** | Cuma bisa lihat (read-only) |

### Frontend
- **CoaTable** di `/finance/chart-of-accounts` — flat table dengan kolom Code, Name, Parent, Type, Status, Actions
- Type badge warna per kategori (biru=ASSET, amber=LIABILITY, ungu=EQUITY, hijau=REVENUE, merah=EXPENSE)
- Status dot hijau (active) / merah (inactive)
- Pagination (5/10/20/50 rows) dengan page number buttons
- Search real-time + filter by Type dropdown
- Responsive: table di desktop, flat list dengan breadcrumb di mobile
- Form create/edit dengan parent combobox, auto-inherit account type
- Logout via avatar dropdown di header (POST `/api/auth/logout`)
- Toast notification sukses/gagal
- Fade-in animations di page load
- Loading skeleton, error state dengan retry, empty state

## 16. Future: User & Role Management

Saat ini cuma ada 1 akun seed (`admin`). Ke depannya Super Admin dapat:
- Membuat akun baru (CRUD user)
- Membuat role dengan permission picker
- Assign role ke user
- Melihat audit log perubahan RBAC
