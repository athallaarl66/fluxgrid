# Testing Scenarios: Outbound Processing (WMS-3)

## 1. Test Strategy Overview
Testing for Outbound Processing ensures that the strict sequence (Pick -> Pack -> Ship) is followed, stock is safely reserved during the process, and the ledger is updated correctly only upon shipment.

## 2. Test Cases

### TC-01: Full Outbound Flow (Happy Path)
- **Given** a valid Sales Order for 10 units of SKU-A and 10 units available in stock.
- **When** a user generates a Pick List, completes Picking, verifies Packing, and confirms Shipment
- **Then** the Shipment status is "Completed".
- **And** the Stock Ledger shows -10 units from the specific picking bin.

### TC-02: Insufficient Stock Allocation (Negative Testing)
- **Given** a Sales Order for 50 units but only 40 are available in the warehouse.
- **When** the user attempts to generate a Pick List
- **Then** the system throws an "Insufficient Stock" validation error.
- **And** the Pick List generation is blocked or placed in a backorder state.

### TC-03: Short Pick Workflow
- **Given** a Pick List for 10 units, but the picker only finds 8 physically.
- **When** the picker enters a Short Pick of 8 units
- **Then** the packing stage only expects 8 units.
- **And** the system generates an anomaly alert for the 2 missing units for inventory reconciliation.

### TC-04: Packing Verification Mismatch
- **Given** a completed pick list of 10 units.
- **When** the packer inputs they only verified 9 units during packing
- **Then** the system blocks the shipment confirmation until the discrepancy is resolved.

### TC-05: Double Shipment Prevention (Edge Case)
- **Given** a shipment that has just been confirmed.
- **When** a concurrent user attempts to submit the shipment form again.
- **Then** the system rejects the second request.
- **And** the ledger is not double-deducted.

### TC-06: Order Cancellation Un-reserves Stock
- **Given** a Pick List generated (stock is reserved).
- **When** the Sales Order is canceled before shipment.
- **Then** the pick list is voided.
- **And** the stock becomes available for other orders again (reservation removed).

## 3. Performance Testing
- Ensure the Pick List generation engine can handle allocating stock for an order with 100+ different line items across different bins in under 2 seconds.

## 4. Security & Access Testing
- Only users with `wms.outbound.process` permission can transition states from Pick -> Pack -> Ship.

## 5. Test Data Requirements
- Pre-populated Sales Orders.
- Sufficient stock levels seeded in the `stock_ledger` for successful picking tests.
