# Production Requirements: Period Closing (FIN-3)

## 1. Feature Overview
- **Feature Name**: Period Closing
- **Module**: Finance - General Ledger & Reporting
- **User Story**: As a CFO, I want to close accounting periods so that financial statements can be generated reliably without fear of historical data changing.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: "Closing the books" is a mandatory accounting practice. It locks historical data so that reports (like Q1 P&L) generated today will look exactly the same 5 years from now. This is vital for external audits and tax reporting.
- **ROI Estimation**: Ensures 100% compliance with audit standards regarding data immutability, preventing fines and failed audits.

## 3. Success Metrics
- Zero transactions can be posted to a Closed period.
- Period closing process validates all pending transactions before allowing a lock.
- Re-opening a period is possible but heavily audited and restricted to Admin roles.

## 4. User Persona
- **CFO / Finance Controller**: The only roles authorized to execute the Period Closing and Re-opening functions.
- **Finance Staff**: Blocked from posting transactions to closed periods.

## 5. User Journey
1. **End of Month**: It is July 1st. The CFO navigates to the Period Management screen to close June.
2. **Validation**: CFO clicks "Close June 2026". The system runs a validation check:
   - Are there any 'Draft' or 'Pending Approval' journal entries in June?
   - If yes, the close is blocked. The CFO is shown a list of pending items to resolve.
3. **Closing**: Once validations pass, the CFO confirms the close.
4. **Locking**: The period status changes to "CLOSED". Any new journal entry attempted with a date in June will be rejected system-wide.
5. **Re-opening (Exception)**: If an auditor finds an error, the CFO can click "Re-open Period". They must input a mandatory reason. The system logs this action in the Audit Trail.

## 6. Acceptance Criteria
- [ ] Ability to define accounting periods (typically monthly: Start Date to End Date).
- [ ] Pre-close validation checks (no pending transactions).
- [ ] Lock mechanism preventing new entries or modifications in closed periods.
- [ ] Ability to re-open a period with mandatory reasoning.
- [ ] Automatic generation of Closing Journal Entries (transferring net income to Retained Earnings) - *Note: for this iteration, manual closing entries are acceptable, but the lock mechanism is mandatory.*

## 7. Edge Cases and Constraints
- **Timezone handling**: Transaction dates must be strictly evaluated based on the tenant's primary timezone to determine which period they belong to.
- **Cross-Period Reversals**: Reversing an entry from a closed period must post the reversal entry into the current *open* period, not the closed one.

## 8. Dependencies on Other Modules
- Blocks WMS (Inventory movements) and HR (Payroll posting) from backdating transactions into closed periods.

## 9. Out of Scope
- Soft-close vs Hard-close (For now, a period is either OPEN or CLOSED).
