# Production Requirements: Budget Management Dashboard (FIN-5)

## 1. Feature Overview
- **Feature Name**: Budget Management & Financial Dashboard
- **Module**: Finance - General Ledger & Reporting
- **User Story**: As a CFO, I want to manage budgets per account and period and view a financial dashboard with KPIs so that I can track performance against plan at a glance.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Budget management is a core ERP capability that enables companies to plan, monitor, and control financial performance. The financial dashboard replaces a static navigation page with actionable KPIs, reducing the time spent navigating between reports.
- **ROI Estimation**: Reduces monthly budget review time by 60%. Provides at-a-glance financial health visibility, enabling faster decision-making.

## 3. Success Metrics
- 100% of budgets are scoped to a specific account and period combination.
- Budget vs Actual variance reports load in under 500ms for periods with up to 500 budgeted accounts.
- Dashboard KPIs reflect real-time data (no stale caches beyond 30 seconds).
- Zero duplicate budgets for the same account+period combination.

## 4. User Persona
- **CFO / Finance Manager**: Creates budgets for each account per period, reviews variance reports to track performance.
- **Finance Staff**: Views the financial dashboard daily for quick KPI checks, reads budget reports.

## 5. User Journey
1. **Period Opening**: At the start of a new period, the CFO navigates to Budget Management and creates budgets for key accounts.
2. **Budget Creation**: CFO selects an account (e.g., "Office Supplies"), sets a planned amount of Rp 50,000,000 for the period, and adds notes.
3. **Daily Operations**: Finance staff posts journal entries against accounts throughout the period.
4. **Variance Review**: Mid-period, the CFO opens the Budget vs Actual report. The system shows planned vs actual with variance percentages. Accounts exceeding 20% variance are flagged.
5. **Dashboard Access**: Finance staff visits `/finance` and sees KPI cards (Total Assets, Revenue MTD, Expenses MTD, Net Income), recent entries, and trend charts.

## 6. Acceptance Criteria
- [ ] Users with `finance.budget.manage` permission can create, update, and delete budgets per account and period.
- [ ] Duplicate budget for same account+period is rejected with HTTP 409.
- [ ] Budget vs Actual report returns planned vs actual amounts with variance percentage.
- [ ] Accounts with variance exceeding 20% threshold are flagged.
- [ ] Dashboard endpoint returns KPIs: total_assets, total_liabilities, total_equity, revenue_mtd, expenses_mtd, net_income_mtd, journal_entry_count.
- [ ] Dashboard includes 10 most recent posted journal entries and monthly revenue/expense trend data.
- [ ] Only POSTED/APPROVED journal entries are counted in actual amounts and recent entries.
- [ ] Dashboard respects tenant isolation.

## 7. Edge Cases and Constraints
- **No budgets exist yet**: Report returns empty array gracefully, not an error.
- **Account with activity but no budget**: Excluded from variance report (report only shows budgeted accounts).
- **Draft entries**: Never counted in actuals or recent entries list.
- **Cold start for first period**: No historical budget data — users create budgets first, reports populate after.

## 8. Dependencies on Other Modules
- Dependent on **FIN-1** (Chart of Accounts) for account selection.
- Dependent on **FIN-2** (Journal Entries) for actual amount calculations and recent entries.
- Dependent on **FIN-3** (Period Closing) for period selection and MTD filtering.

## 9. Out of Scope
- Multi-year budget rollup or forecasting.
- Approval workflow for budgets (simple CRUD only).
- Real-time dashboard updates (on-load fetch via React Query).
- Line-item budget breakdown (single planned_amount per account+period).
