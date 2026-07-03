# Testing Scenarios: Financial Reporting (FIN-4)

## 1. Test Strategy Overview
Testing for Financial Reports is essentially validating massive aggregation queries. The key is ensuring that debit/credit logic (normal balances) applies correctly based on the account type (e.g., Asset normally debit, Revenue normally credit).

## 2. Test Cases

### TC-01: Trial Balance Accuracy (Happy Path)
- **Given** a ledger with 50 posted journal entries
- **When** the user generates a Trial Balance for the current year
- **Then** the total of all Debit balances must exactly equal the total of all Credit balances.

### TC-02: Balance Sheet Equation (Mathematical Validation)
- **Given** historical transaction data spanning 2 years
- **When** the user generates a Balance Sheet as of today
- **Then** Total Assets MUST EXACTLY EQUAL (Total Liabilities + Total Equity).
- **And** the "Current Year Earnings" calculation matches the Net Income on the P&L for the same period.

### TC-03: Profit & Loss Date Range Filtering
- **Given** transactions occurring in Jan, Feb, and March
- **When** the user generates a P&L strictly for Feb 1 to Feb 28
- **Then** the report only includes Revenue and Expenses recorded in February.
- **And** ignores any transactions from Jan or March.

### TC-04: Exclude Draft Entries (Validation)
- **Given** a journal entry for $10,000 Revenue in "Draft" status
- **When** the user generates the P&L with "Include Drafts = false"
- **Then** the $10,000 Revenue is NOT included in the total.

### TC-05: Drill-down Capability (Integration)
- **Given** an "Office Supplies" expense total of $5,000 on the P&L
- **When** the user clicks on the $5,000 value
- **Then** a modal opens displaying the 5 individual journal entry lines (e.g., $1000 each) that sum up to exactly $5,000.

### TC-06: Handling Zero-Balance Accounts
- **Given** an active account "Vehicle Maintenance" with 0 transactions this year
- **When** the report generates with default settings
- **Then** "Vehicle Maintenance" is hidden from the P&L view to reduce clutter.

## 3. Performance Testing
- Financial reports are notoriously slow in legacy ERPs. Ensure the SQL aggregation query uses materialized views or heavily optimized `SUM()` queries with `GROUP BY` to generate a yearly P&L across 100,000 lines in < 3 seconds.

## 4. Security & Access Testing
- Only users with `finance.report.read` can view these reports.
- Data isolation: Tenant A must never see Tenant B's financial aggregations.
