# Design Specifications: Stock Ledger Management (WMS-1)

## 1. Screen Overview
**Page:** Stock Ledger Dashboard
**Purpose:** Provides a tabular view of all inventory movements, real-time balances, and valuation toggles.

## 2. Wireframe Description
```text
[Header: FluxGrid ERP] [Module: WMS] [User Profile]
---------------------------------------------------
[Breadcrumb: WMS / Stock Ledger]

[Title: Stock Ledger] 
[Toggle: Valuation Method (FIFO / Average Cost)]

[Search SKU...] [Filter: Date Range] [Filter: Location] [Export CSV]

[Data Table]
| Date       | Ref No.  | SKU   | Location   | Type     | Qty In | Qty Out | Balance | Val. (FIFO) |
|------------|----------|-------|------------|----------|--------|---------|---------|-------------|
| 2026-06-01 | PO-101   | SKU-A | Whse-1     | Putaway  | 100    | 0       | 100     | $1,000      |
| 2026-06-02 | SO-202   | SKU-A | Whse-1     | Pick     | 0      | 20      | 80      | $800        |
| 2026-06-03 | TR-303   | SKU-A | Transit    | Transfer | 0      | 10      | 70      | $700        |

[Pagination: < 1 2 3 ... 10 >]
```

## 3. Component Hierarchy
- `StockLedgerPage` (Server Component)
  - `LedgerHeader`
    - `ValuationToggle` (Client Component - switches state)
  - `LedgerFilters` (Client Component - updates URL search params)
    - `SearchInput`
    - `DateRangePicker`
    - `LocationSelect`
  - `LedgerTable` (Server Component with Suspense)
    - `TableRow`
    - `Pagination`

## 4. UI Components (shadcn/ui)
- `Table` (for the main ledger view)
- `Input` (for SKU search)
- `DatePicker` (for date ranges)
- `Select` (for location filtering)
- `Switch` or `ToggleGroup` (for Valuation Method)
- `Badge` (for Movement Type: Green for In, Red for Out, Blue for Transfer)

## 5. Visual Guidelines
- **Typography**: Inter (sans-serif), standard ERP sizing (14px for table data for density).
- **Colors**:
  - `Qty In` values text should be muted green to indicate addition.
  - `Qty Out` values text should be muted red to indicate subtraction.
  - Borders and dividers should use subtle grays (e.g., slate-200 in light mode).
- **Spacing**: Compact table density to allow maximum data visibility on desktop screens.

## 6. Responsive Design
- **Desktop (1024px+)**: Full table view.
- **Tablet (768px-1023px)**: Horizontal scroll on the table.
- **Mobile (<768px)**: Card-based layout instead of a table. Each ledger entry is a stacked card showing Date, SKU, and the net movement prominently.

## 7. States & Interactions
- **Empty State**: Illustration of an empty box with "No ledger entries found for the selected filters."
- **Loading State**: `Skeleton` rows matching the table layout during data fetch.
- **Error State**: Toast notification + inline error boundary "Failed to load ledger data. [Retry]"
- **Interaction**: Clicking a row opens a Side Panel (Sheet component) showing the exact double-entry journal (Debit/Credit breakdown) and reference document links (e.g., link to the Purchase Receipt).

## 8. Accessibility (WCAG 2.1 AA)
- Table must have proper `<th scope="col">` and `<th scope="row">` tags.
- Valuation toggle must be keyboard navigable with ARIA labels.
- Sufficient color contrast for the In/Out text colors (avoid light green on white).
