# Production Requirements: Payroll Processing (HR-3)

## 1. Feature Overview
- **Feature Name**: Payroll Processing Engine
- **Module**: HR & Payroll
- **User Story**: As an HR Manager, I want to process payroll automatically so that employees are paid accurately and on time, and the finance ledger is updated.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Automates the most tedious and error-prone monthly HR task. Ensures accurate calculation of allowances, overtime, deductions (lateness, unpaid leave), and statutory taxes (PPh 21). Crucially, it bridges HR and Finance by automatically posting payroll journals, eliminating dual-entry.
- **ROI Estimation**: Reduces payroll processing time from 5 days to 1 day per month. Eliminates calculation errors that lead to employee dissatisfaction or tax penalties.

## 3. Success Metrics
- 100% mathematical accuracy in Net Pay calculations.
- Payroll journals are posted to the Finance module seamlessly without manual intervention.
- Payslips are generated and accessible by employees immediately after processing.

## 4. User Persona
- **HR Manager / Payroll Officer**: Runs the payroll engine, reviews the draft calculations, and approves the final run.
- **Employee**: Views and downloads their monthly payslip.
- **Finance Staff**: Receives the automated journal entry (View only).

## 5. User Journey
1. **Initiation**: At the end of the month, the HR Manager creates a new "Payroll Run" for May 2026.
2. **Data Aggregation**: The system automatically pulls `Base Salary` (from HR-1), `Approved Overtime` & `Late Minutes` (from Task App Attendance).
3. **Calculation (Draft)**: The engine calculates Gross Pay, subtracts statutory deductions and tax, resulting in Net Pay. The HR Manager reviews this draft list.
4. **Approval & Posting**: HR Manager clicks "Finalize Payroll".
5. **System Actions**:
   - Payslips are generated (PDF format).
   - Domain Event `PayrollProcessed` is fired.
   - Finance module listens and creates a Journal Entry (Debit: Salary Expense, Credit: Cash/Bank and Tax Payable).

## 6. Acceptance Criteria
- [ ] Ability to define dynamic payroll components (Allowances, Deductions).
- [ ] Automatic aggregation of attendance data from Task App (Lateness penalties, Overtime pay).
- [ ] Tax (PPh 21) calculation placeholder/logic.
- [ ] Approval workflow (Draft -> Finalized).
- [ ] Auto-generation of individual payslips.
- [ ] Automatic posting of Journal Entries to Finance upon finalization.

## 7. Edge Cases and Constraints
- **Mid-month Hires/Terminations**: Proration logic must apply if an employee didn't work the full calendar month.
- **Negative Net Pay**: If deductions exceed gross pay (e.g., massive unpaid leave), the system must cap deductions at Gross = 0, or carry forward the debt.

## 8. Dependencies on Other Modules
- Dependent on **HR-1** (Base Salary) and **Task App** (Attendance data via API).
- Severely impacts **Finance (FIN-2)**. The `PayrollProcessed` event MUST post balanced journal entries.

## 9. Out of Scope
- Direct API integration with Banks for automated transfer (Disbursement). The system will only generate a Bank Transfer CSV file.
