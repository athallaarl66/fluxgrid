# Testing Scenarios: Stock Ledger Management (WMS-1)

## 1. Test Strategy Overview
Testing will focus heavily on data consistency and concurrency. The core of this feature is the double-entry accounting principle applied to inventory, so verifying that total debits always equal total credits across the system is paramount.

## 2. Test Cases

### TC-01: Valid Inbound Movement (Happy Path)
- **Given** an empty warehouse and an approved Purchase Receipt for 100 units of SKU-A
- **When** the putaway is processed
- **Then** the ledger should show +100 (Debit) in Warehouse Location and -100 (Credit) in Supplier Transit.
- **And** the total balance for SKU-A is 100.

### TC-02: Valid Outbound Movement (Happy Path)
- **Given** 100 units of SKU-A in the warehouse
- **When** 20 units are shipped out
- **Then** the ledger should show -20 (Credit) from Warehouse Location and +20 (Debit) to Customer Transit.
- **And** the total balance for SKU-A is 80.

### TC-03: Internal Transfer
- **Given** 50 units of SKU-A in Location A
- **When** a transfer of 10 units to Location B is executed
- **Then** the ledger should show -10 for Location A and +10 for Location B.
- **And** the overarching warehouse balance remains 50.

### TC-04: Negative Stock Prevention (Negative Testing)
- **Given** 5 units of SKU-B in the warehouse
- **When** an outbound movement of 10 units is attempted
- **Then** the system should reject the transaction.
- **And** the ledger must not record any partial movement.

### TC-05: Double-Entry Validation (Negative Testing)
- **Given** a direct API call attempting to create a ledger entry with only a Debit record
- **When** the payload is submitted
- **Then** the API should return a 400 Bad Request.
- **And** no data is saved to the database.

### TC-06: FIFO Valuation Calculation
- **Given** 10 units bought at $10, and later 10 units bought at $15
- **When** 5 units are sold and FIFO valuation is requested
- **Then** the Cost of Goods Sold (COGS) should be calculated at $10 per unit ($50 total).

### TC-07: Average Cost Valuation Calculation
- **Given** 10 units bought at $10, and later 10 units bought at $15 (Total 20 units, Total Value $250)
- **When** 5 units are sold and Average Cost valuation is requested
- **Then** the COGS should be calculated at $12.50 per unit ($62.50 total).

### TC-08: Immutable Audit Trail Enforcement
- **Given** an existing ledger entry
- **When** an UPDATE or DELETE SQL command is simulated/attempted via the API layer
- **Then** the action must be rejected (403 or 405 error).
- **And** corrections must only be possible via reversing entries.

### TC-09: Filter by Date Range
- **Given** ledger entries spanning 3 months
- **When** the user filters for the current month
- **Then** only current month entries are displayed.
- **And** the opening balance for the period is calculated correctly from past entries.

### TC-10: Concurrency Handling (Performance/Edge Case)
- **Given** 10 simultaneous API requests deducting 1 unit each from a stock of 10
- **When** the requests hit the server at the exact same millisecond
- **Then** the final stock balance must be exactly 0.
- **And** no negative stock or dropped transactions should occur.

## 3. Performance Testing
- **Volume Test**: Generate 1,000,000 ledger entries for a single SKU. Verify that calculating the real-time balance takes < 1 second.
- **Stress Test**: 100 concurrent users writing to the ledger simultaneously.

## 4. Security & Access Testing
- **Role Test**: Ensure `Warehouse Staff` can only append to the ledger via operational workflows (Putaway, Pick), while `Warehouse Manager` can view the ledger and issue adjustment workflows.
- Tenant isolation (RLS): Ensure User from Tenant A cannot query ledger data from Tenant B.

## 5. Test Data Requirements
- Pre-populated master data for SKUs, Locations, and Suppliers.
- Mock scripts to generate historical ledger entries for valuation calculations.
