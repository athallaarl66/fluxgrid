# Production Requirements: Journal Entry Management (FIN-2)

## 1. Feature Overview
- **Feature Name**: Journal Entry Management
- **Module**: Finance - General Ledger & Reporting
- **User Story**: As a Finance Staff, I want to create and manage journal entries so that all financial transactions are recorded accurately.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Journal entries are the core mechanism for recording financial transactions in a double-entry accounting system. It ensures that every transaction is balanced (Debit = Credit) and categorized correctly, which is required for accurate financial reporting and auditing.
- **ROI Estimation**: Reduces manual entry errors to 0% by strictly enforcing the double-entry accounting equation system-wide.

## 3. Success Metrics
- 100% of posted journal entries are balanced mathematically.
- Draft journal entries can be saved and continued later.
- High-value journal entries correctly trigger the managerial approval workflow.

## 4. User Persona
- **Finance Staff**: Inputs manual journal entries (e.g., accruals, adjustments, depreciation).
- **Finance Manager / CFO**: Reviews and approves high-value manual entries before they are posted to the ledger.

## 5. User Journey
1. **Initiate Entry**: Finance staff opens the "New Journal Entry" page.
2. **Input Data**: Staff enters a description, date, and adds multiple lines (Account, Debit, Credit).
3. **System Validation**: As lines are added, the system displays the total Debit and Credit. The "Submit" button remains disabled until Total Debit == Total Credit.
4. **Draft Mode**: The user can save it as a "Draft" to finish later.
5. **Approval Workflow**: Once submitted, if the amount exceeds Rp 50.000.000, it goes to "Pending Approval" state. The Manager reviews and clicks "Approve".
6. **Posting**: The entry status changes to "Posted", and the General Ledger balances are updated.

## 6. Acceptance Criteria
- [ ] Ability to create manual journal entries with multiple line items.
- [ ] Strict enforcement of double-entry validation (Total Debits = Total Credits).
- [ ] Ability to save drafts.
- [ ] Approval workflow for manual entries exceeding a defined threshold.
- [ ] Support for attaching reference documents (e.g., PDF invoices).
- [ ] Automatic posting capability (used by system-generated entries from WMS/HR).
- [ ] Prevent editing or deleting of "Posted" entries (must use a reversal entry instead).

## 7. Edge Cases and Constraints
- **Rounding Differences**: Multi-currency conversions or tax calculations might result in 1-cent differences. The UI should highlight imbalances clearly.
- **Closed Periods**: Cannot post a journal entry into an accounting period that has already been closed (See FIN-3).

## 8. Dependencies on Other Modules
- Dependent on **FIN-1 (COA)** for valid account codes.
- Generates the core data used by **FIN-4 (Reporting)**.

## 9. Out of Scope
- Automated recurring journal entries (e.g., auto-posting rent expense every 1st of the month) are out of scope for this initial phase.
