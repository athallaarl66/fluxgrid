# Technical Specifications: Budget Management Dashboard (FIN-5)

## 1. System Architecture
- **Frontend**: Next.js Client Components with React Query for data fetching, Recharts for charting.
- **Backend**: .NET 8 Minimal API (Modular Monolith) with Clean Architecture.
- **Database**: PostgreSQL (Neon).
- **ORM**: Entity Framework Core 8.0 with Npgsql.
- **Auth**: JWT in httpOnly cookie `token`. Super Admin bypasses permission checks.

## 2. Database Schema

### Table: `budgets`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `Id` | UUID | PRIMARY KEY | Unique identifier |
| `AccountId` | UUID | NOT NULL, FK → `chart_of_accounts.Id` | Account being budgeted |
| `PeriodId` | UUID | NOT NULL, FK → `periods.Id` | Budget period |
| `PlannedAmount` | DECIMAL(18,2) | NOT NULL | Budgeted amount |
| `Notes` | TEXT | | Optional notes |
| `TenantId` | UUID | NOT NULL | Multi-tenancy isolation |
| `CreatedAt` | TIMESTAMP | DEFAULT NOW() | |
| `UpdatedAt` | TIMESTAMP | DEFAULT NOW() | |

**Constraints**:
- `UNIQUE (TenantId, AccountId, PeriodId)`: One budget per account per period.
- `FK → chart_of_accounts(Id)` with `ON DELETE RESTRICT`.
- `FK → periods(Id)` with `ON DELETE RESTRICT`.

## 3. Entity Framework Core Entities

### Entity: `Budget`
```csharp
namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
    public Guid PeriodId { get; set; }
    public Period? Period { get; set; }
    public decimal PlannedAmount { get; set; }
    public string? Notes { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### EF Core Configuration (in `AppDbContext.OnModelCreating`)
```csharp
modelBuilder.Entity<Budget>(entity =>
{
    entity.ToTable("budgets");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.TenantId, e.AccountId, e.PeriodId }).IsUnique();
    entity.Property(e => e.PlannedAmount).HasColumnType("decimal(18,2)").IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
    entity.HasOne(e => e.Account)
          .WithMany()
          .HasForeignKey(e => e.AccountId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.Period)
          .WithMany()
          .HasForeignKey(e => e.PeriodId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

## 4. API Endpoints

### GET `/api/v1/finance/budgets`
- **Description**: List budgets with pagination and filtering.
- **Query Params**: `period_id`, `account_id`, `page` (default 1), `page_size` (default 10).
- **Response**: Paginated list with `total`, `page`, `page_size`, `total_pages`, `items`.
- **Required Permission**: `finance.budget.read`

### POST `/api/v1/finance/budgets`
- **Description**: Create a new budget.
- **Request Body**: `account_id`, `period_id`, `planned_amount`, `notes` (optional).
- **Validation**:
  - Account must exist and be active in tenant.
  - Period must exist and be open.
  - Duplicate (account_id + period_id) → HTTP 409.
- **Required Permission**: `finance.budget.manage`

### PUT `/api/v1/finance/budgets/{id}`
- **Description**: Update budget planned amount or notes.
- **Validation**: Budget must exist in tenant. Cannot change account or period.
- **Required Permission**: `finance.budget.manage`

### DELETE `/api/v1/finance/budgets/{id}`
- **Description**: Delete a budget.
- **Response**: HTTP 204 No Content.
- **Required Permission**: `finance.budget.manage`

### GET `/api/v1/finance/budgets/report`
- **Description**: Budget vs Actual report for a period.
- **Query Params**: `period_id` (required).
- **Response**: Array of `{ account_code, account_name, planned_amount, actual_amount, variance, variance_percentage, is_flagged }`.
- **Logic**: LEFT JOIN budgets with aggregated actuals from `journal_entry_lines` where `journal_entries.status IN ('POSTED', 'APPROVED')`. Variance = planned - actual. Flagged if |variance_percentage| > 20%.
- **Required Permission**: `finance.budget.read`

### GET `/api/v1/finance/dashboard`
- **Description**: Financial dashboard KPIs.
- **Response**: `{ total_assets, total_liabilities, total_equity, revenue_mtd, expenses_mtd, net_income_mtd, journal_entry_count, period_id, recent_entries: [...], monthly_trend: [...] }`.
- **Logic**:
  - `total_assets`: Sum of debit balances for ASSET type accounts in current period.
  - `total_liabilities`: Sum of credit balances for LIABILITY type accounts.
  - `total_equity`: Sum of credit balances for EQUITY type accounts.
  - `revenue_mtd`: Sum of credit amounts in REVENUE accounts for current month.
  - `expenses_mtd`: Sum of debit amounts in EXPENSE accounts for current month.
  - `net_income_mtd`: revenue_mtd - expenses_mtd.
  - `recent_entries`: Last 10 posted/approved journal entries.
  - `monthly_trend`: Revenue and expense totals per month for current year.
- **Required Permission**: `finance.read`

## 5. Service Layer

### `BudgetService` (`Modules/Finance/Application/BudgetService.cs`)
- `CreateAsync(CreateBudgetRequest)` → validates account/period existence, checks duplicate, saves.
- `UpdateAsync(Guid, UpdateBudgetRequest)` → loads, validates, updates, saves.
- `DeleteAsync(Guid)` → loads, validates, deletes.
- `GetListAsync(period_id, account_id, page, page_size)` → filtered paginated query.
- `GetBudgetVsActualAsync(period_id)` → LEFT JOIN query with aggregation.

### `DashboardService` (`Modules/Finance/Application/DashboardService.cs`)
- `GetDashboardAsync()` → runs 4 EF queries (KPI aggregation, recent entries, monthly trend, current period lookup), assembles response.

## 6. Permissions (RBAC)

Defined in `Shared/RBAC/Permissions.cs`:
```csharp
public const string FinanceBudgetRead = "finance.budget.read";
public const string FinanceBudgetManage = "finance.budget.manage";
```

| Permission | Description |
|------------|-------------|
| `finance.budget.read` | View budgets and variance reports |
| `finance.budget.manage` | Create, update, delete budgets |
| `finance.read` | View dashboard (existing) |

Pre-seeded roles:
- **Admin**: All permissions (via `Permissions.All`)
- **Manager**: `finance.budget.read`, `finance.budget.manage`, `finance.read`
- **Staff**: `finance.budget.read`, `finance.read`

## 7. Error Handling

| HTTP Status | Condition | Error Code |
|-------------|-----------|------------|
| 400 | Invalid account or period | `INVALID_REFERENCE` |
| 404 | Budget not found | `BUDGET_NOT_FOUND` |
| 404 | Account not found | `ACCOUNT_NOT_FOUND` |
| 404 | Period not found | `PERIOD_NOT_FOUND` |
| 409 | Duplicate budget for account+period | `BUDGET_DUPLICATE` |

## 8. Frontend — Route Structure

| Route | File | Description |
|-------|------|-------------|
| `/finance` | `app/finance/page.tsx` | Financial dashboard with KPI cards, charts, recent entries |
| `/finance/budgets` | `app/finance/budgets/page.tsx` | Budget list with create/edit/delete |
| — | `components/finance/FinanceNav.tsx` | Secondary nav bar for sub-module links |

## 9. Frontend — Data Flow

### Types (`lib/budget-types.ts`)
```typescript
interface BudgetResponse {
  id: string; accountId: string; accountCode: string; accountName: string;
  periodId: string; plannedAmount: number; notes: string | null;
  createdAt: string; updatedAt: string;
}

interface CreateBudgetRequest {
  accountId: string; periodId: string;
  plannedAmount: number; notes?: string;
}

interface UpdateBudgetRequest {
  plannedAmount?: number; notes?: string;
}

interface BudgetVsActualRow {
  accountCode: string; accountName: string;
  plannedAmount: number; actualAmount: number;
  variance: number; variancePercentage: number;
  isFlagged: boolean;
}
```

### Types (`lib/dashboard-types.ts`)
```typescript
interface DashboardResponse {
  totalAssets: number; totalLiabilities: number; totalEquity: number;
  revenueMtd: number; expensesMtd: number; netIncomeMtd: number;
  journalEntryCount: number; periodId: string;
  recentEntries: RecentEntryRow[];
  monthlyTrend: MonthlyTrendRow[];
}

interface RecentEntryRow {
  id: string; entryNo: string; description: string;
  transactionDate: string; totalDebit: number; totalCredit: number; status: string;
}

interface MonthlyTrendRow {
  month: number; revenue: number; expenses: number;
}
```

### TanStack Query Hooks

| Hook | Method | Query Key | Invalidates |
|------|--------|-----------|-------------|
| `useBudgets(params)` | GET | `["budgets", params]` | — |
| `useBudgetReport(periodId)` | GET | `["budgets", "report", periodId]` | — |
| `useCreateBudget()` | POST | — | `["budgets"]` |
| `useUpdateBudget()` | PUT | — | `["budgets"]` |
| `useDeleteBudget()` | DELETE | — | `["budgets"]` |
| `useFinanceDashboard()` | GET | `["finance-dashboard"]` | — |

### API Endpoints Consumed
```
GET    /api/v1/finance/budgets          → PaginatedResult<BudgetResponse>
POST   /api/v1/finance/budgets          → BudgetResponse (201)
PUT    /api/v1/finance/budgets/{id}     → BudgetResponse (200)
DELETE /api/v1/finance/budgets/{id}     → (204)
GET    /api/v1/finance/budgets/report   → BudgetVsActualRow[]
GET    /api/v1/finance/dashboard        → DashboardResponse
```

## 10. Frontend — Components

### Budget Management
- `BudgetTable.tsx` — paginated table with search/filter, action buttons per row.
- `BudgetFormModal.tsx` — create/edit dialog with account combobox, period selector, amount input.
- `BudgetVarianceReport.tsx` — report table showing planned vs actual with color-coded variance badges.

### Dashboard
- `DashboardKpiCard.tsx` — metric card with label, value, icon, trend indicator.
- `DashboardChart.tsx` — Recharts bar/line chart for monthly revenue/expense trend.
- `FinanceNav.tsx` — horizontal secondary nav bar with links to Budgets, COA, JE, Periods, Reports.

## 11. Frontend — State Handling

```
AUTH_LOADING → [Skeleton]
    ↓
NO_USER → redirect /login
    ↓
FORBIDDEN (403) → "Access Denied" with permission name
    ↓
ERROR → error state with Retry button
    ↓
LOADING → skeleton placeholders
    ↓
EMPTY → "No budgets yet" / "No data available"
    ↓
DATA → rendered components
```
