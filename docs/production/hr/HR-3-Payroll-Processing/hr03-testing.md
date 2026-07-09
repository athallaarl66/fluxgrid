# Testing Scenarios: Payroll Processing (HR-3)

## 1. Test Strategy Overview
Testing for Payroll is high-risk. Focus heavily on mathematical assertions, proration logic for mid-month state changes, and the integration boundary where HR Domain Events trigger Finance Journal Entries.

## 2. Test Cases

### TC-01: Standard Full-Month Payroll (Happy Path)
- **Given** an employee with Base Salary 10,000,000, 0 late minutes, and 0 overtime.
- **When** the HR Manager runs payroll for the month.
- **Then** Gross Pay is calculated as 10,000,000.
- **And** Tax and standard deductions are applied correctly to yield Net Pay.

### TC-02: Proration for Mid-Month Hire
- **Given** an employee hired on the 15th of a 30-day month. Base Salary is 10,000,000.
- **When** the payroll is run.
- **Then** the Gross Pay is prorated (e.g., 5,000,000 based on working days).

### TC-03: Variable Components (Overtime & Lateness)
- **Given** an employee with 2 hours of approved overtime and 60 minutes of lateness.
- **When** the payroll aggregates attendance from Task App API.
- **Then** the Overtime Allowance is added to Gross Pay.
- **And** the Lateness Penalty is added to Deductions.

### TC-04: Journal Entry Posting (Integration)
- **Given** a finalized payroll run totaling 100,000,000 in Gross Pay, 10,000,000 in Tax, and 90,000,000 Net Pay.
- **When** the `PayrollProcessed` event is dispatched.
- **Then** the Finance module automatically creates a "Posted" journal entry:
  - Debit: Salary Expense (100M)
  - Credit: Tax Payable (10M)
  - Credit: Bank/Cash (90M)
- **And** the journal entry mathematically balances.

### TC-05: Prevent Re-running Finalized Payroll (Negative Testing)
- **Given** a Payroll Run for May 2026 that has been "Finalized".
- **When** HR attempts to click "Re-calculate" or "Finalize" again.
- **Then** the API rejects the request to prevent double-posting to the Finance ledger.

### TC-06: Closed Financial Period (Integration)
- **Given** the Finance module has marked May 2026 as "CLOSED".
- **When** HR attempts to finalize the May payroll.
- **Then** the system blocks the finalization because the resulting journal entry would be rejected by Finance.

## 3. Performance Testing
- Processing payroll for 2,000 employees involves thousands of database reads (Task App attendance API calls, salary history, tax brackets). Ensure the calculation engine can process a batch of 2,000 employees in under 30 seconds using background jobs or optimized bulk queries.

## 4. Security & Access Testing
- Only `hr.payroll.process` can initiate or finalize a run.
- Payslips endpoints must rigorously check that `user_id` matches the token; Employee A cannot download Employee B's PDF.
