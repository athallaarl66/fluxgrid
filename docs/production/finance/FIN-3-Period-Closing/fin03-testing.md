# Testing Scenarios: Period Closing (FIN-3)

## 1. Test Strategy Overview
Testing for Period Closing validates the strict lock on historical data, ensuring that no module (Finance, WMS, HR) can bypass the closed status to insert or modify transactions.

## 2. Test Cases

### TC-01: Successful Period Close
- **Given** a period "May 2026" with all journal entries in "Posted" status
- **When** the CFO attempts to close the period
- **Then** the validation passes.
- **And** the period status updates to "CLOSED".

### TC-02: Block Closing Due to Pending Entries (Negative Testing)
- **Given** a period "June 2026" containing 1 entry in "Pending Approval" status
- **When** the CFO attempts to close the period
- **Then** the system blocks the action.
- **And** displays an error detailing the pending entry ID that must be resolved.

### TC-03: System-Wide Lock Enforcement (Integration)
- **Given** period "May 2026" is "CLOSED"
- **When** a Warehouse Manager confirms a purchase receipt backdated to May 28, 2026
- **Then** the WMS module's domain event attempts to create a Journal Entry for May 28.
- **And** the Finance API rejects the creation.
- **And** the WMS transaction rolls back, displaying an error: "Cannot process backdated transaction into a closed financial period."

### TC-04: Re-open Period Workflow
- **Given** period "May 2026" is "CLOSED"
- **When** the CFO clicks "Re-open" and provides the reason "Audit adjustment required"
- **Then** the period status changes back to "OPEN".
- **And** the `audit_logs` table records the re-opening event and the provided reason.

### TC-05: Re-open Period without Reason (Negative Testing)
- **Given** a closed period
- **When** the CFO attempts to re-open it but leaves the reason field blank
- **Then** the UI form validation prevents submission.
- **And** the API returns a 400 Bad Request if bypassed.

## 3. Performance Testing
- The pre-close validation check queries all journal entries for a month. Ensure this query is indexed on `transaction_date` and `status` to return results in under 500ms, even with 100,000 entries.

## 4. Security & Access Testing
- Only `Finance:Admin` can view the "Close Period" and "Re-open" buttons.
- Standard `Finance:Write` users attempting to hit the close API endpoint receive a 403 Forbidden.
