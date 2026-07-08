# Testing Scenarios: Budget Management Dashboard (FIN-5)

## 1. Test Strategy Overview
Testing focuses on budget CRUD integrity, budget-vs-actual variance calculations, dashboard KPI accuracy, and permission enforcement.

## 2. Test Cases

### TC-01: Create Budget Successfully (Happy Path)
- **Given** a user with `finance.budget.manage` permission
- **When** they create a budget with `account_id`, `period_id`, `planned_amount`
- **Then** the system returns HTTP 201 with the created budget object
- **And** the budget is saved with the user's tenant

### TC-02: Duplicate Budget Rejected (Negative Testing)
- **Given** an existing budget for account X and period Y
- **When** a user attempts to create another budget for the same account and period
- **Then** the system returns HTTP 409 Conflict

### TC-03: Budget for Non-Existent Account Rejected
- **Given** an account ID that does not exist in the tenant
- **When** a user attempts to create a budget with that account
- **Then** the system returns HTTP 404 Not Found

### TC-04: Update Budget Amount
- **Given** an existing budget
- **When** a user updates the `planned_amount`
- **Then** the system returns HTTP 200 with the updated budget
- **And** `updated_at` is refreshed

### TC-05: Delete Budget
- **Given** an existing budget
- **When** a user deletes it
- **Then** the system returns HTTP 204 No Content
- **And** the budget is removed from the database

### TC-06: Delete Non-Existent Budget
- **Given** an invalid budget ID
- **When** a user attempts to delete it
- **Then** the system returns HTTP 404 Not Found

### TC-07: Budget vs Actual Report — Correct Variance Calculation
- **Given** a budget with `planned_amount = 100,000,000` and actual journal entry lines summing to `85,000,000`
- **When** the report is generated
- **Then** `variance` = -15,000,000 and `variance_percentage` = -15.0%

### TC-08: Budget Item Flagged When Variance Exceeds Threshold
- **Given** a budget where |variance_percentage| > 20%
- **When** the report is generated
- **Then** `is_flagged` = true

### TC-09: Draft Entries Excluded from Actuals
- **Given** a journal entry with status `DRAFT` exists for a budgeted account
- **When** the report is generated
- **Then** the draft entry is NOT included in `actual_amount`

### TC-10: No Budgets in Period Returns Empty Array
- **Given** no budgets exist for a given period
- **When** the report is requested
- **Then** the system returns HTTP 200 with an empty array

### TC-11: Dashboard Returns All KPIs
- **Given** journal entries exist for the current period
- **When** a user requests the dashboard
- **Then** all KPIs are returned: `total_assets`, `total_liabilities`, `total_equity`, `revenue_mtd`, `expenses_mtd`, `net_income_mtd`, `journal_entry_count`

### TC-12: Dashboard Respects Tenant Isolation
- **Given** two tenants with different data
- **When** users from both tenants request the dashboard
- **Then** each receives KPIs scoped to their own tenant's data

### TC-13: Recent Entries Limited to Posted/Approved
- **Given** a mix of DRAFT, POSTED, and APPROVED entries
- **When** the dashboard loads
- **Then** only POSTED and APPROVED entries appear in recent entries

### TC-14: Unauthorized Access Rejected
- **Given** a user without `Finance:Read` permission
- **When** they request `/api/v1/finance/dashboard`
- **Then** the system returns HTTP 403 Forbidden

### TC-15: Budget List Pagination
- **Given** 25 budgets exist
- **When** a user requests `?page=2&page_size=10`
- **Then** the response includes items 11-20, with `total=25`, `page=2`, `page_size=10`, `total_pages=3`

## 3. Performance Testing
- Dashboard aggregation queries (4 separate queries) must complete within 500ms for tenants with up to 10,000 journal entries.
- Budget vs Actual report must load within 300ms for periods with up to 500 budgeted accounts.
- Budget list pagination must respond within 200ms.

## 4. Security & Access Testing
- Users without `finance.budget.read` cannot view budgets or reports.
- Users without `finance.budget.manage` cannot create, update, or delete budgets.
- Users without `finance.read` cannot access dashboard.
- Cross-tenant testing: Tenant A cannot see or query Tenant B's budgets or dashboard data.
