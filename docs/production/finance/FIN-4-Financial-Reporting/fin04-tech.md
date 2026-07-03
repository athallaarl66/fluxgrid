# Technical Specifications: Financial Reporting (FIN-4)

## 1. System Architecture
- **Frontend**: Next.js Client Components (renders nested JSON structure recursively).
- **Backend**: API Routes acting as a pass-through to complex Postgres queries.
- **Database**: PostgreSQL (Neon). The heavy lifting is done entirely via SQL aggregations, not in application memory.

## 2. Database Schema
No new tables are required for FIN-4. It relies entirely on `chart_of_accounts`, `journal_entries`, and `journal_entry_lines`.

## 3. SQL Query Design (The Core Engine)

To calculate balances efficiently, we use a concept of "Normal Balance".
- **Assets & Expenses**: Normal balance is Debit. `(Debit - Credit) = Balance`
- **Liabilities, Equity, Revenue**: Normal balance is Credit. `(Credit - Debit) = Balance`

### Trial Balance Aggregation Query (Concept)
```sql
SELECT 
    coa.id, coa.code, coa.name, coa.type,
    SUM(jel.debit) as total_debit,
    SUM(jel.credit) as total_credit
FROM chart_of_accounts coa
LEFT JOIN journal_entry_lines jel ON coa.id = jel.account_id
JOIN journal_entries je ON jel.entry_id = je.id
WHERE je.status = 'POSTED' 
  AND je.tenant_id = $1
  AND je.transaction_date BETWEEN $2 AND $3
GROUP BY coa.id, coa.code, coa.name, coa.type
ORDER BY coa.code;
```
*Note: This flat result is then formatted into a hierarchical tree (Parent/Child) by the Application Layer.*

### Retained Earnings Calculation (Balance Sheet specific)
The Balance Sheet must include "Current Year Earnings". This is dynamically calculated by running the P&L query (Revenue - Expenses) for the current year and injecting it into the Equity section of the Balance Sheet.

## 4. API Endpoints

### GET `/api/v1/finance/reports/trial-balance`
- **Query Params**: `start_date`, `end_date`, `include_drafts`

### GET `/api/v1/finance/reports/pl`
- **Query Params**: `start_date`, `end_date`, `include_drafts`
- **Action**: Filters only accounts where `type IN ('REVENUE', 'EXPENSE')`.

### GET `/api/v1/finance/reports/balance-sheet`
- **Query Params**: `as_of_date` (Balance sheets are a snapshot in time, not a range), `include_drafts`
- **Action**: 
  1. Calculates balances for `type IN ('ASSET', 'LIABILITY', 'EQUITY')` from the beginning of time up to `as_of_date`.
  2. Calculates Current Year Net Income and appends it to Equity.

## 5. Domain Events
- **Consumed**: `JournalEntryPosted` and `PeriodClosed` can trigger caching invalidation if we implement a Redis cache for reports.

## 6. Permissions (RBAC)
- `finance.report.read`: Required to access any of these endpoints.

## 7. Performance Considerations
- **Materialized Views**: If a tenant exceeds 1 million journal entry lines, generating a report on the fly will be slow. Implement a PostgreSQL Materialized View `monthly_account_balances` that aggregates data up to the end of the previous month. The live API query then only needs to sum the Materialized View + the current month's live transactions.

## 8. Security Considerations
- Prevent SQL Injection in the Date params (use parameterized queries via Drizzle).

## 9. Export Functionality
- CSV export can be generated entirely client-side using a library like `papaparse` from the JSON data to save server bandwidth.
- PDF export can be handled client-side via `jspdf` or `react-pdf`.
