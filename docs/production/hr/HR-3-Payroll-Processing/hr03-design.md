# Design Specifications: Payroll Processing (HR-3)

## 1. Screen Overview
**Page 1:** Payroll Runs Dashboard
**Page 2:** Payroll Run Details (Draft/Finalized)
**Page 3:** Employee Payslip View

## 2. Wireframe Description
```text
=== Payroll Runs Dashboard ===
[Header] [Button: + Run Payroll]

| Period   | Total Employees | Total Gross | Status   | Actions |
| May 2026 | 150             | 1.5B        | DRAFT    | [Review]|
| Apr 2026 | 148             | 1.4B        | POSTED   | [View]  |

=== Payroll Run Details (May 2026 - DRAFT) ===
[Status: Draft] [Button: Recalculate] [Button: Finalize & Post to Ledger]

[Table]
| Emp ID | Name   | Base Salary | Overtime | Deductions | Net Pay |
| 001    | J. Doe | 10M         | +1M      | -500k      | 10.5M   | [Edit Adjustments]
| 002    | S. Lee | 8M          | 0        | 0          | 8M      | [Edit Adjustments]
-------------------------------------------------------------------
TOTAL                                                   | 18.5M

=== Employee Payslip View (Mobile Optimized) ===
[Company Logo]
Payslip for May 2026
---------------------------------
EARNINGS
Base Salary:        Rp 10,000,000
Overtime:           Rp  1,000,000
Total Earnings:     Rp 11,000,000

DEDUCTIONS
Tax (PPh 21):       Rp    500,000
Total Deductions:   Rp    500,000
---------------------------------
NET TAKE HOME PAY:  Rp 10,500,000

[Button: Download PDF]
```

## 3. Component Hierarchy
- `PayrollDashboard`
  - `PayrollRunsTable`
  - `NewRunDialog`
- `PayrollDetailsPage`
  - `SummaryMetricsCards` (Total Gross, Net, Taxes)
  - `PayrollRecordsTable` (Highly dense data table)
    - `AdjustmentModal` (For manual additions like bonuses)
- `EmployeePayslipPage`
  - `PayslipDocument` (Rendered cleanly for print/PDF conversion)

## 4. UI Components (shadcn/ui)
- `Table` for dense payroll data. Requires sticky headers and horizontal scrolling.
- `Card` for summary metrics at the top of the details page.
- `Dialog` for manual adjustments.
- `Button` (Destructive/Warning style for the "Finalize & Post" action since it's irreversible).

## 5. Visual Guidelines
- **Density**: The Payroll Details table needs to show a lot of numbers. Use a compact font, right-align all currency, and minimize padding so HR can scan the list without endless scrolling.
- **Irreversibility Warning**: When clicking "Finalize", the confirmation modal must make it extremely clear that this will lock the payroll and post financial ledgers.

## 6. Responsive Design
- The HR Payroll processing screens are strictly Desktop.
- The Employee Payslip screen must be perfectly readable on Mobile, as 90% of employees will check their payslip on their phones.

## 7. States & Interactions
- **Long-Running Process**: "Run Payroll" could take 10-30 seconds. The UI must show a progress bar or polling skeleton state while the backend calculates.
- **Locking**: Once Finalized, the "Edit Adjustments" and "Recalculate" buttons completely disappear.

## 8. Accessibility
- Ensure the PDF generated for the payslip includes text-selectable layers, not just a flattened image, so screen readers can parse it.
