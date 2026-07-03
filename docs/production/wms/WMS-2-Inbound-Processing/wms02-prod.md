# Production Requirements: Inbound Processing (WMS-2)

## 1. Feature Overview
- **Feature Name**: Inbound Processing
- **Module**: Warehouse Management System (WMS)
- **User Story**: As a Warehouse Staff, I want to process purchase receipts and putaway so that incoming goods are properly recorded and stored.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Ensures that goods arriving at the warehouse are verified against purchase orders, checked for quality, and correctly placed into specific warehouse bins. This prevents receiving unrecorded goods and speeds up the availability of goods for outbound operations.
- **ROI Estimation**: Reduces receiving time per truckload by 20%. Reduces misplaced inventory by 90%.

## 3. Success Metrics
- 100% of purchase receipts have a valid Purchase Order (PO) reference.
- Putaway operations are completed within 4 hours of receipt.
- Zero discrepancies between physical receipts and system receipts.

## 4. User Persona
- **Warehouse Staff**: Physically receives the goods, inspects them, and inputs the data into the system. Needs a fast, barcode-friendly, error-proof interface.
- **Warehouse Manager**: Monitors inbound queue and resolves discrepancies (e.g., received less than ordered).

## 5. User Journey
1. **Receive Goods**: Staff creates a `Purchase Receipt` referencing a `Purchase Order`.
2. **Quality Check**: Staff inspects the goods. If damaged, records the quantity as rejected/quarantine.
3. **Receipt Confirmation**: Staff confirms the receipt. The goods are now in the `Receiving Dock` (Transit).
4. **Putaway Assignment**: System suggests (or staff selects) a bin/location for the goods.
5. **Putaway Execution**: Staff physically moves goods and confirms putaway in the system. The stock ledger updates automatically (Debit Bin, Credit Receiving Dock).

## 6. Acceptance Criteria
- [ ] Ability to create a Purchase Receipt with reference to a PO number.
- [ ] Ability to record pass/fail quantities during the Quality Check phase.
- [ ] Ability to assign a specific warehouse bin location for Putaway.
- [ ] Putaway confirmation must automatically trigger a stock ledger update (double-entry).
- [ ] Support partial receiving (receiving less than the PO quantity).

## 7. Edge Cases and Constraints
- **Over-receiving**: The system must warn (or block, based on configuration) if the received quantity exceeds the ordered PO quantity.
- **Damaged Goods**: Goods failing quality check must not be available for picking; they should be directed to a Quarantine location.

## 8. Dependencies on Other Modules
- **Finance Module**: Once a Purchase Receipt is confirmed, a domain event (`ReceiptProcessed`) is sent to Finance to acknowledge the liability (Accounts Payable).

## 9. Out of Scope
- Automated Barcode scanning via native mobile app (Web UI only for now, though barcode scanner keyboard emulation is supported).
- Multi-stage cross-docking workflows.
