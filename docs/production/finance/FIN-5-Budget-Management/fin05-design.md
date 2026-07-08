# Design Specifications: Budget Management Dashboard (FIN-5)

## 1. Screen Overview
**Page 1:** Financial Dashboard (`/finance`)
**Page 2:** Budget List (`/finance/budgets`)
**Page 3:** Budget Form Modal (overlay)
**Page 4:** Budget vs Actual Report (inline on budget page)

## 2. Wireframe Description
```text
=== Financial Dashboard (/finance) ===
[FinanceNav: COA | Journal | Periods | Budgets | Reports]

[KPI Cards Row]
[Total Assets] [Total Liabilities] [Total Equity]
[Revenue MTD]  [Expenses MTD]     [Net Income MTD]

[Chart: Monthly Revenue vs Expenses]
[Bar chart showing 12 months, revenue in green, expenses in red]

[Recent Entries Table]
| Entry No | Date | Description | Debit | Credit | Status |

=== Budget List (/finance/budgets) ===
[Header: Budget Management] [Button: + New Budget]
[Filter: Period dropdown] [Filter: Account search]

[Table]
| Account Code | Account Name | Period | Planned Amount | Notes | Actions |
| 4110         | Product Sales| Jan 25 | 500,000,000    | —     | [Edit] [Delete] |

=== Budget Form Modal ===
Title: [Create/Edit] Budget
[Combobox: Account (searchable)]
[Select: Period]
[Input: Planned Amount (number)]
[Textarea: Notes (optional)]
[Button: Cancel] [Button: Save]

=== Budget vs Actual Report ===
[Header: Budget vs Actual — Period: Jan 2025]
[Table]
| Account | Planned | Actual | Variance | Var % | Status |
| 4110    | 500M    | 420M   | -80M     | -16%  | ✓ Normal |
| 5210    | 100M    | 140M   | +40M     | +40%  | ⚠ Flagged |
```

## 3. Component Hierarchy
- `FinanceDashboardPage`
  - `FinanceNav`
  - `DashboardKpiCard` (×6)
  - `DashboardChart` (Recharts BarChart)
  - `RecentEntriesTable`
- `BudgetListPage`
  - `FinanceNav`
  - `BudgetToolbar` (filters + create button)
  - `BudgetTable`
  - `BudgetVarianceReport`
  - `BudgetFormModal` (dialog)

## 4. UI Components (shadcn/ui)
- `Card` for KPI metric cards.
- `Table` for budget list and variance report.
- `Dialog` for budget create/edit form.
- `Command` + `Popover` for account combobox.
- `Select` for period and type filters.
- `Input` for amount and search.
- `Badge` for variance status (green=normal, red=flagged).
- `Button` for actions.

## 5. Visual Guidelines
- **Color Palette**: Follow DESIGN.md (#fdf8f5 surface, #625f4b primary, Inter typography).
- **KPI Cards**: Use distinct icons per metric (bank for assets, scale for equity, chart for revenue).
- **Variance Colors**: Green for variance within threshold, red/amber for flagged overages.
- **Chart**: Revenue bars in green (#22c55e), expense bars in red (#ef4444).

## 6. Responsive Design
- Dashboard KPI cards stack to 2-column grid on tablets, single column on mobile.
- Charts render full-width on desktop, scrollable on mobile.
- Budget table is horizontally scrollable on mobile with sticky first column.
- FinanceNav collapses to a hamburger dropdown on mobile.

## 7. States & Interactions
- **Loading**: Skeleton cards for KPIs, skeleton rows for tables.
- **Empty State**: "No budgets yet. Create your first budget to get started." with CTA button.
- **Error State**: Error message with Retry button for API failures.
- **Optimistic Updates**: Budget create/edit/delete updates UI immediately, rollback on error.
- **Toast Notifications**: Success/error feedback after every mutation.
- **Hover Actions**: Edit/Delete buttons appear on table row hover.

## 8. Accessibility
- All interactive elements keyboard-navigable.
- KPI cards use `aria-label` for screen readers.
- Chart data includes `aria-label` descriptions.
- Color is not the only indicator for variance status (icons + text labels also used).
