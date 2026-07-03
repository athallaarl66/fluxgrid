# Design Specifications: Financial Reporting (FIN-4)

## 1. Screen Overview
**Page:** Financial Reports Dashboard (Tabs for P&L, Balance Sheet, Trial Balance)
**Modal:** Drill-down Ledger View

## 2. Wireframe Description
```text
=== Financial Reports ===
[Tabs: Profit & Loss | Balance Sheet | Trial Balance]

[Header Controls]
Date Range: [ 01 Jan 2026 - 31 Dec 2026 ] [Btn: Generate]
[Toggle: Show Zero Balances] [Btn: Export CSV] [Btn: PDF]

=== Profit & Loss View ===
[Accordion Table]
[-] 4000 - Revenue .............................. Rp 1,500,000,000
    [Link] 4100 - Product Sales ................. Rp 1,000,000,000
    [Link] 4200 - Service Revenue ............... Rp   500,000,000
------------------------------------------------------------------
Total Revenue                                     Rp 1,500,000,000

[-] 5000 - Cost of Goods Sold ................... Rp   800,000,000
    [Link] 5100 - Raw Materials ................. Rp   800,000,000
------------------------------------------------------------------
GROSS PROFIT                                      Rp   700,000,000

... (Operating Expenses) ...

------------------------------------------------------------------
NET INCOME                                        Rp   300,000,000

=== Drill-down Modal ===
Title: Ledger Details (4100 - Product Sales)
| Date | Entry No | Description | Debit | Credit | Balance |
| 15/01| JE-101   | Invoice #1  | -     | 500k   | 500k (C)|
```

## 3. Component Hierarchy
- `FinancialReportsPage`
  - `ReportTabs`
  - `ReportControls` (Date pickers, toggles)
  - `ReportViewer`
    - `FinancialStatementTree` (Recursive rendering of COA with balances)
      - `StatementRow`
        - `HoverLink` (Triggers drill-down)
  - `LedgerDrilldownModal`
    - `DataTable`

## 4. UI Components (shadcn/ui)
- `Tabs` to switch between report types instantly (if cached client-side).
- `Table` / custom flex rows for the financial statement layout.
- `Dialog` for the drill-down view.
- `DatePickerWithRange` (Requires React DayPicker).

## 5. Visual Guidelines
- **Typography Alignment**: Numbers MUST be strictly right-aligned with fixed-width (tabular/monospaced) numerals so decimals align perfectly.
- **Hierarchy Styling**: Parent accounts should be bolded. Grand totals (Gross Profit, Net Income, Total Assets) should have a top and double-bottom border standard in accounting.
- **Negative Numbers**: Display negative values in red or wrapped in parentheses `(1,000)` based on user locale preference.

## 6. Responsive Design
- Reports are heavily Desktop-focused. On mobile, horizontal scrolling is inevitable. Provide a persistent floating "Export" button on mobile so users can just download the PDF instead of squinting.

## 7. States & Interactions
- **Collapsible Nodes**: Users can collapse "Operating Expenses" to hide the 50 underlying accounts and just see the summary line.
- **Loading State**: Full-page skeleton or spinner while the heavy aggregation query runs.

## 8. Accessibility
- Provide a clear `table` structure for screen readers, ensuring row headers (Account Name) are properly associated with data cells (Amount).
