# Testing Scenarios: Journal Entry Management (FIN-2)

## 1. Test Strategy Overview
Testing for Journal Entries is critical. The primary focus is mathematical validation, ensuring the ledger is never corrupted with unbalanced transactions, and enforcing immutability after posting.

## 2. Test Cases

### TC-01: Create Balanced Entry (Happy Path)
- **Given** a Finance Staff user
- **When** they create an entry with:
  - Line 1: Debit 1000
  - Line 2: Credit 1000
- **Then** the submission is successful.
- **And** the entry status becomes "Posted" (assuming amount is below approval threshold).

### TC-02: Prevent Unbalanced Entry (Negative Testing)
- **Given** an entry form
- **When** the user inputs:
  - Line 1: Debit 1000
  - Line 2: Credit 900
- **Then** the Submit button is disabled in UI.
- **And** if bypassed via API, the backend returns a 422 Unprocessable Entity error: "Debits must equal Credits".

### TC-03: Single Sided Entry Prevention (Negative Testing)
- **Given** an entry form
- **When** a user tries to submit an entry with only one line (e.g., Debit 1000, no credit line)
- **Then** the system rejects it, requiring at least two lines for a valid double-entry transaction.

### TC-04: High Value Approval Workflow
- **Given** an approval threshold of Rp 50.000.000
- **When** a staff member submits a balanced entry for Rp 100.000.000
- **Then** the status is set to "Pending Approval".
- **And** the ledger balances are NOT updated yet.

### TC-05: Manager Approval
- **Given** an entry in "Pending Approval" state
- **When** a Manager clicks "Approve"
- **Then** the status changes to "Posted".
- **And** the ledger balances are updated accordingly.

### TC-06: Prevent Edits to Posted Entries (Negative Testing)
- **Given** a "Posted" journal entry
- **When** a user attempts to edit the amount or account
- **Then** the UI hides the edit button.
- **And** the backend rejects any PUT requests to that ID.

### TC-07: Closed Period Enforcement
- **Given** the accounting period for May 2026 is marked as "Closed"
- **When** a user attempts to post a journal entry with a transaction date of May 15, 2026
- **Then** the system throws an error: "Cannot post to a closed period."

## 3. Performance Testing
- Ensure the API can handle batch ingestion of 1,000 journal entry lines (e.g., from an automated Payroll run) within 5 seconds without timing out.

## 4. Security & Access Testing
- Only `Finance:Write` can create draft entries.
- Only `Finance:Admin` or `Finance:Approve` can approve high-value entries.
- Standard users cannot approve their own high-value entries (Segregation of Duties).
