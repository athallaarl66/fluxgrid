# Technical Specifications: Financial Reporting (FIN-4)

## 1. System Architecture
- **Frontend**: Next.js Client Components (renders nested JSON structure recursively).
- **Backend**: .NET 8 Minimal API with EF Core. `ReportService` handles core logic (aggregation → flat rows → tree).
- **Database**: PostgreSQL (Neon). Aggregation is done via EF Core LINQ queries (GROUP BY + SUM), not raw SQL.
- **Query Pattern**: Flat aggregation via EF Core → Hierarchy built in application layer (`BuildTree`), identical to COA's `GetTreeAsync`.

```
┌──────────────┐     ┌────────────────┐     ┌──────────────┐
│  PostgreSQL  │────▶│  ReportService │────▶│  API Response│
│  EF Core     │     │  (C# LINQ)     │     │  (nested     │
│  SUM/GROUP BY│     │  BuildTree()   │     │   JSON tree) │
└──────────────┘     └────────────────┘     └──────────────┘
```

## 2. Database Schema
No new tables are required for FIN-4. It relies entirely on `chart_of_accounts`, `journal_entries`, and `journal_entry_lines`.

## 3. Query & Aggregation Design

### Normal Balance Convention
To calculate balances efficiently, we use a concept of "Normal Balance" implemented as a `HashSet<string>` in code:
- **Assets & Expenses**: Normal balance is Debit. `Balance = SUM(debit) - SUM(credit)`
- **Liabilities, Equity, Revenue**: Normal balance is Credit. `Balance = SUM(credit) - SUM(debit)`

### Aggregation Logic (EF Core LINQ)
The core aggregation is done in `GetAggregatedRowsAsync`:

1. Load active accounts for the tenant (optionally filtered by type).
2. Query `journal_entry_lines` joined with `journal_entries`, grouped by `account_id`, summing `debit` and `credit`.
3. Status filter: if `includeDrafts=false`, only `POSTED` or `APPROVED` entries are included. If `includeDrafts=true`, all statuses are included.
4. Date filter: `transaction_date BETWEEN start AND end` (UTC-normalized via `DateTime.SpecifyKind(dt, DateTimeKind.Utc)`).
5. Map aggregation into a `List<FlatReportRow>` — accounts with no entries get `0` debit/credit.

```csharp
var aggregation = await (
    from jel in _db.JournalEntryLines
    join je in _db.JournalEntries on jel.EntryId equals je.Id
    where accountIds.Contains(jel.AccountId)
          && je.TenantId == tenantId
          && je.TransactionDate >= startDate
          && je.TransactionDate <= endDate
          && (includeDrafts || je.Status == "POSTED" || je.Status == "APPROVED")
    group new { jel.Debit, jel.Credit } by jel.AccountId into g
    select new { AccountId = g.Key, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) }
).ToDictionaryAsync(x => x.AccountId);
```

### Tree Builder (Application Layer)
`BuildTree` converts flat rows into a nested hierarchy matching the COA parent-child structure:

```csharp
static List<ReportRow> BuildTree(List<FlatReportRow> flatRows)
{
    var lookup = flatRows.ToLookup(r => r.ParentId);
    return BuildLevel(lookup, null, 0);
}

static List<ReportRow> BuildLevel(ILookup<Guid?, FlatReportRow> lookup, Guid? parentId, int depth)
{
    return lookup[parentId].OrderBy(r => r.Code).Select(r =>
    {
        var children = BuildLevel(lookup, r.Id, depth + 1);
        // Children balances are rolled up into parent
        var totalDebit = r.Debit + children.Sum(c => c.Debit);
        var totalCredit = r.Credit + children.Sum(c => c.Credit);
        var balance = NormalDebitTypes.Contains(r.Type)
            ? totalDebit - totalCredit
            : totalCredit - totalDebit;
        return new ReportRow(..., totalDebit, totalCredit, balance, children);
    }).ToList();
}
```

- Parents aggregate all descendant balances (deep rollup).
- Each row carries `Depth` (0 = top-level), used by frontend for indentation.
- Balance sign depends on account type (debit-normal vs credit-normal).

### Retained Earnings / Current Year Earnings (Balance Sheet)
The Balance Sheet receives `netIncome` as a query parameter (calculated by the frontend from the P&L endpoint). If provided, a synthetic "Current Year Earnings" row is injected under the top-level Equity account:

```csharp
if (netIncome.HasValue)
{
    var topEquity = rows.FirstOrDefault(r => r.Type == "EQUITY" && r.ParentId == null);
    if (topEquity != null)
        rows.Add(new FlatReportRow(Guid.Empty, "CYE", "Current Year Earnings",
            "EQUITY", topEquity.Id,
            netIncome < 0 ? -netIncome.Value : 0m,  // debit if negative
            netIncome > 0 ? netIncome.Value : 0m)); // credit if positive
}
```

## 4. API Endpoints

All endpoints are registered under `/api/v1/finance/reports` in `ReportEndpoints.cs`, requiring `finance.report.read` permission.

### GET `/api/v1/finance/reports/trial-balance`
- **Query Params**: `start_date`, `end_date`, `include_drafts`
- **Return Type**: `ReportResponse` with nested `ReportRow` tree, `total_debit`, `total_credit`, `net_income` (null).

### GET `/api/v1/finance/reports/pl` (Profit & Loss)
- **Query Params**: `start_date`, `end_date`, `include_drafts`
- **Action**: Filters accounts to `type IN ('REVENUE', 'EXPENSE')`.
- **Return Type**: `ReportResponse` with `net_income` = `SumByType(REVENUE) - SumByType(EXPENSE)`.
- **Balance Sign**: Revenue accounts use credit-normal (positive balance when credited), Expense accounts use debit-normal (positive balance when debited). Net Income = Revenue Balance - Expense Balance.

### GET `/api/v1/finance/reports/balance-sheet`
- **Query Params**: `as_of_date` (snapshot), `include_drafts`, `net_income` (from P&L, nullable decimal)
- **Action**:
  1. Aggregates `type IN ('ASSET', 'LIABILITY', 'EQUITY')` from beginning of time up to `as_of_date`.
  2. Injects "Current Year Earnings" synthetic row under top-level Equity if `net_income` is provided.
- **Return Type**: `ReportResponse`. Equation `Total Assets = Total Liabilities + Total Equity` should hold.

### GET `/api/v1/finance/reports/{accountId}/ledger` (Drill-down)
- **Query Params**: `start_date`, `end_date`, `include_drafts`, `page` (default 1), `pageSize` (default 20)
- **Action**: Returns paginated list of `LedgerDetailRow` for a specific account. No aggregation — raw journal entry lines.
- **Return Type**:
  ```json
  { "rows": [LedgerDetailRow...], "total": 42, "page": 1, "pageSize": 20 }
  ```
- **Pagination**: Default 20 per page, max unconstrained (frontend controls page size).

## 5. ReportService Implementation

| Method | Dependencies | Key Internal Calls |
|--------|-------------|-------------------|
| `GetTrialBalanceAsync` | `AppDbContext` | `GetAggregatedRowsAsync(type=null)`, `BuildTree`, `Sum` |
| `GetProfitLossAsync` | `AppDbContext` | `GetAggregatedRowsAsync(type=REVENUE,EXPENSE)`, `BuildTree`, `SumByType` |
| `GetBalanceSheetAsync` | `AppDbContext` | `GetAggregatedRowsAsync(type=ASSET,LIABILITY,EQUITY)`, inject CYE row, `BuildTree` |
| `GetAccountLedgerAsync` | `AppDbContext` | Direct EF Core query with `CountAsync` + `Skip`/`Take` |

**Note**: Unlike other Finance services (COA, Period Closing), `ReportService` does NOT depend on `IAuditService`, `ICacheService`, or `IDomainEventDispatcher`. It is a pure read model.

## 6. Status Filter Logic

| `include_drafts` value | Statuses included |
|------------------------|-------------------|
| `false` (default) | `POSTED`, `APPROVED` |
| `true` | All statuses (`DRAFT`, `PENDING_APPROVAL`, `POSTED`, `APPROVED`, etc.) |

## 7. DTOs

```csharp
// Response for TB, P&L, Balance Sheet
public sealed record ReportRow(
    Guid AccountId, string Code, string Name, string Type, int Depth,
    decimal Debit, decimal Credit, decimal Balance,
    List<ReportRow> Children
);

public sealed record ReportResponse(
    List<ReportRow> Rows, decimal TotalDebit, decimal TotalCredit, decimal? NetIncome
);

// Response for drill-down
public sealed record LedgerDetailRow(
    Guid EntryId, string EntryNo, DateTime TransactionDate,
    string Description, decimal Debit, decimal Credit, DateTime CreatedAt
);
```

## 8. Permissions (RBAC)
- `finance.report.read`: Required to access any of these endpoints. Registered in `Permissions.cs` and seeded to Admin role in `DataSeeder.cs`.

## 9. Performance Considerations
- **Indexes**: Ensure composite indexes on `journal_entries(tenant_id, status, transaction_date)` and `journal_entry_lines(account_id, entry_id)`.
- **Materialized Views** (Deferred): If a tenant exceeds 1 million journal entry lines, implement a PostgreSQL Materialized View `monthly_account_balances` that aggregates data up to the end of the previous month. The live API query then only needs to sum the Materialized View + the current month's live transactions.
- **Tree Building**: `BuildTree` uses `ILookup` for O(n) hierarchy construction — no N+1 queries. For 500+ accounts, consider virtualized rendering on frontend.

## 10. Security Considerations
- All date/user input is passed via EF Core parameterized queries (no SQL injection risk).
- Tenant isolation enforced at query level (`je.TenantId == tenantId`).
- `finance.report.read` permission enforced via `RequireAuthorization`.

## 11. Export Functionality
- CSV export can be generated entirely client-side using a library like `papaparse` from the JSON data to save server bandwidth.
- PDF export can be handled client-side via `jspdf` or `react-pdf` (not implemented in MVP).
