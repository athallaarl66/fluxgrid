# Test Report: FIN-4 Financial Reporting

## Execution Summary

| | |
|---|---|
| **Test Date** | 2026-07-08 |
| **Project** | `tests/unit/Finance/finanance-report-04.Test/FluxGrid.Api.Tests.csproj` |
| **Framework** | xUnit + EF Core InMemory + Moq |
| **Total Tests** | 51 |
| **Passed** | 51 |
| **Failed** | 0 |
| **Skipped** | 0 |
| **Duration** | 4s |

## Test Files

| File | Type | Tests |
|------|------|-------|
| `ReportServiceTests.cs` | Unit (InMemory DB) | 43 |
| `ReportEndpointsTests.cs` | Integration (WebApplicationFactory) | 8 |

## Test Categories

### Trial Balance (14 tests)

| Test | What It Verifies |
|------|------------------|
| `GetTrialBalanceAsync_ReturnsAllAccountsWithBalances` | TB returns all accounts with correct aggregated debit/credit totals |
| `GetTrialBalanceAsync_TotalDebitEqualsTotalCredit` | For balanced journal entries, total debit = total credit |
| `GetTrialBalanceAsync_ExcludesDraftsWhenIncludeDraftsFalse` | `includeDrafts=false` filters out DRAFT entries |
| `GetTrialBalanceAsync_IncludesDraftsWhenIncludeDraftsTrue` | `includeDrafts=true` includes DRAFT entries |
| `GetTrialBalanceAsync_IncludesPendingApprovalWhenIncludeDraftsFalse` | `includeDrafts=false` also excludes PENDING_APPROVAL |
| `GetTrialBalanceAsync_IncludesPendingApprovalWhenIncludeDraftsTrue` | `includeDrafts=true` includes PENDING_APPROVAL |
| `GetTrialBalanceAsync_FiltersByDateRange` | Only entries within the date range are included |
| `GetTrialBalanceAsync_ReturnsHierarchicalTree` | Tree matches COA parent-child structure with correct depth |
| `GetTrialBalanceAsync_ParentAggregatesChildBalances` | Parent accounts roll up children's debit/credit |
| `GetTrialBalanceAsync_ReturnsEmptyWhenNoAccounts` | No COA → empty tree |
| `GetTrialBalanceAsync_ReturnsZeroBalancesWhenNoEntries` | COA exists but no JE → zero balances |
| `GetTrialBalanceAsync_RespectsTenantIsolation` | Other tenant's data is excluded |
| `GetTrialBalanceAsync_OnlyIncludesActiveAccounts` | Inactive accounts are excluded |
| `GetTrialBalanceAsync_UsesCorrectBalanceSign_DebitNormal` | ASSET (debit-normal) balance = debit - credit |
| `GetTrialBalanceAsync_UsesCorrectBalanceSign_CreditNormal` | LIABILITY (credit-normal) balance = credit - debit |
| `GetTrialBalanceAsync_NetIncomeIsNull` | TB returns `netIncome=null` |

### Profit & Loss (7 tests)

| Test | What It Verifies |
|------|------------------|
| `GetProfitLossAsync_OnlyIncludesRevenueAndExpense` | Only REVENUE and EXPENSE accounts appear |
| `GetProfitLossAsync_ExcludesAssetLiabilityEquity` | ASSET, LIABILITY, EQUITY accounts are excluded |
| `GetProfitLossAsync_NetIncomeEqualsRevenueMinusExpenses` | Net Income = Revenue balance - Expense balance |
| `GetProfitLossAsync_ReturnsZeroNetIncomeWhenNoEntries` | No entries → NI = 0 |
| `GetProfitLossAsync_NetIncomeNegativeWhenExpensesExceedRevenue` | Expense > Revenue → negative NI |
| `GetProfitLossAsync_RevenueBalanceIsCreditNormal` | Revenue uses credit-normal balance calculation |
| `GetProfitLossAsync_ExpenseBalanceIsDebitNormal` | Expense uses debit-normal balance calculation |

### Balance Sheet (6 tests)

| Test | What It Verifies |
|------|------------------|
| `GetBalanceSheetAsync_OnlyIncludesAssetLiabilityEquity` | Only ASSET, LIABILITY, EQUITY accounts appear |
| `GetBalanceSheetAsync_InjectsCurrentYearEarnings` | CYE synthetic row injected under top-level Equity |
| `GetBalanceSheetAsync_CurrentYearEarningsHasCorrectAmount` | CYE amount matches netIncome parameter |
| `GetBalanceSheetAsync_DoesNotInjectWhenNetIncomeNull` | No CYE when netIncome is null |
| `GetBalanceSheetAsync_HandlesNegativeNetIncome` | Negative NI → CYE shows as debit |
| `GetBalanceSheetAsync_UsesAsOfDateSnapshot` | Transactions after asOfDate are excluded |
| `GetBalanceSheetAsync_DoesNotInjectWhenNoTopLevelEquity` | No top-level Equity account → no CYE injection |

### Ledger Drill-down (11 tests)

| Test | What It Verifies |
|------|------------------|
| `GetAccountLedgerAsync_ReturnsLinesForSpecificAccount` | Filter by account ID works |
| `GetAccountLedgerAsync_RespectsPagination` | Skip/Take works correctly |
| `GetAccountLedgerAsync_FiltersByDateRange` | Only lines within date range returned |
| `GetAccountLedgerAsync_ExcludesDraftsWhenIncludeDraftsFalse` | DRAFT entries excluded when requested |
| `GetAccountLedgerAsync_IncludesDraftsWhenIncludeDraftsTrue` | DRAFT entries included when requested |
| `GetAccountLedgerAsync_ReturnsEmptyForNonExistentAccount` | Non-existent account → empty result |
| `GetAccountLedgerAsync_OrdersByTransactionDateDescending` | Results sorted newest first |
| `GetAccountLedgerAsync_RespectsTenantIsolation` | Other tenant data excluded |
| `GetAccountLedgerAsync_ReturnsCorrectTotalCount` | Total count matches unfiltered results |
| `GetAccountLedgerAsync_ReturnsEmptyWhenNoEntries` | No entries → empty |

### Tree Building (3 tests)

| Test | What It Verifies |
|------|------------------|
| `BuildTree_MultipleChildrenPerParent` | Parent correctly aggregates multiple children |
| `BuildTree_EmptyChildrenWhenNoSubAccounts` | Leaf accounts have empty children list |
| `BuildTree_ExcludesAccountsWithInvalidParent` | Accounts with non-existent ParentId are excluded |

### Endpoint Integration (8 tests)

| Test | What It Verifies |
|------|------------------|
| `TrialBalance_WithoutAuth_Returns401` | Unauthenticated request returns 401 |
| `TrialBalance_WithAuth_ReturnsReport` | Authenticated request returns report data |
| `PL_WithoutAuth_Returns401` | Unauthenticated request returns 401 |
| `PL_WithAuth_ReturnsReport` | Authenticated request returns report data |
| `BalanceSheet_WithoutAuth_Returns401` | Unauthenticated request returns 401 |
| `BalanceSheet_WithAuth_ReturnsReport` | Authenticated request returns report data |
| `Ledger_WithoutAuth_Returns401` | Unauthenticated request returns 401 |
| `Ledger_WithAuth_ReturnsRows` | Authenticated request returns paginated rows |

## Bug Fixed During Testing

### `ReportService.Sum()` double-counting

**Location**: `backend/FluxGrid.Api/Modules/Finance/Application/ReportService.cs:140`

**Symptom**: `TotalDebit` and `TotalCredit` in report responses were 3x the correct value when accounts had parent-child hierarchy.

**Root cause**: `Sum()` and `SumByType()` recursively summed children, but parent `ReportRow.Debit`/`ReportRow.Credit` already included children's amounts via the rollup in `BuildLevel()`. This caused triple-counting (grandparent + parent + child).

**Fix**: Removed recursive calls. Since parent rows already aggregate descendants, summing only top-level rows is correct.

## Related Documentation

- [Technical Spec](docs/production/finance/FIN-4-Financial-Reporting/fin04-tech.md)
- [Feature Design](openspec/changes/fin-4-financial-reporting/design.md)
- [Requirements Spec](openspec/changes/fin-4-financial-reporting/specs/financial-reports/spec.md)
- [Tasks](openspec/changes/fin-4-financial-reporting/tasks.md)
