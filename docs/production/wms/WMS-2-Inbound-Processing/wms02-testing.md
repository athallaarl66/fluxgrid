# Testing Scenarios: Inbound Processing (WMS-2)

## 1. Test Strategy Overview
Testing for Inbound Processing focuses on workflow completion, data validation against external POs, and the critical integration point with the Stock Ledger.

## 2. Test Cases

### TC-01: Full Inbound Flow (Happy Path)
- **Given** a valid PO for 100 units of SKU-A
- **When** a user creates a Purchase Receipt, passes QA for 100 units, and confirms Putaway to Bin-A1
- **Then** the Receipt status should be "Completed"
- **And** the Stock Ledger shows 100 units in Bin-A1.

### TC-02: Partial Receiving (Happy Path)
- **Given** a PO for 100 units
- **When** a user creates a Receipt for only 60 units (due to supplier shortage)
- **Then** the Receipt is processed for 60 units.
- **And** the system allows a subsequent Receipt to be created for the remaining 40 units later.

### TC-03: Over-receiving Warning/Block (Negative Testing)
- **Given** a PO for 50 units
- **When** a user attempts to receive 60 units
- **Then** the system should display a validation error blocking the receipt (or a warning, based on configuration).
- **And** the Receipt cannot be finalized without manager override.

### TC-04: Quality Check Failure
- **Given** a receipt of 100 units
- **When** QA records 90 units as Passed and 10 units as Failed
- **Then** 90 units are routed to Putaway in standard bins.
- **And** 10 units are automatically routed to a Quarantine location.

### TC-05: Putaway Location Assignment
- **Given** a confirmed receipt waiting in the Receiving Dock
- **When** the user assigns Putaway to a valid bin and confirms
- **Then** the ledger creates a transfer entry: Debit Bin, Credit Receiving Dock.

### TC-06: PO Reference Validation (Negative Testing)
- **Given** a user creating a Purchase Receipt
- **When** they enter a non-existent PO number
- **Then** the system should show an error "PO Not Found".

### TC-07: Double Putaway Prevention (Edge Case)
- **Given** a receipt that has already been putaway
- **When** a concurrent user attempts to submit the putaway form for the same receipt again
- **Then** the system rejects the second request.

### TC-08: Domain Event Verification (Integration)
- **Given** a Purchase Receipt is confirmed
- **When** the transaction commits
- **Then** the `ReceiptProcessed` event is published.
- **And** the Finance module creates a pending Accounts Payable entry.

## 3. Performance Testing
- Ensure the Receipt confirmation and ledger update transaction completes in < 3 seconds to avoid blocking dock operations.

## 4. Security & Access Testing
- Only `Warehouse Staff` and `Warehouse Manager` roles can create/process Receipts.
- Read-only users cannot confirm putaways.

## 5. Test Data Requirements
- Mock Purchase Orders representing various states (Open, Partially Fulfilled).
- Valid location/bin codes for testing putaway assignments.
