# Production Requirements: Stock Ledger Management (WMS-1)

## 1. Feature Overview
- **Feature Name**: Stock Ledger Management
- **Module**: Warehouse Management System (WMS)
- **User Story**: As a Warehouse Manager, I want to maintain a double-entry stock ledger so that every inventory movement is tracked with proper debit/credit entries.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Provides a single source of truth for inventory valuation and stock balances. By enforcing a double-entry system (debit/credit) similar to accounting, it eliminates "ghost stock" and ensures traceability of every single item.
- **ROI Estimation**: 40% reduction in stock-out incidents, 30% improvement in inventory turnover, and significantly reduced discrepancies during stock opname.

## 3. Success Metrics
- 100% of inventory movements map to a balanced double-entry record.
- < 1 second latency for calculating real-time stock balance of any SKU.
- Zero undocumented manual adjustments allowed (every adjustment requires a documented movement).

## 4. User Persona
- **Warehouse Manager**: Needs an overarching view of inventory health, valuation, and discrepancies. Needs to audit stock movements.
- **Finance Staff (Indirect)**: Relies on the stock ledger's valuation (FIFO/Average) to be synced to the General Ledger accurately.

## 5. User Journey
1. **View Ledger**: Warehouse Manager navigates to the Stock Ledger page.
2. **Filter & Search**: Manager filters by specific SKU or Date Range.
3. **View Movement Details**: Manager clicks on a specific entry to view the paired journal entry (where did it come from, where did it go).
4. **Change Valuation Method**: Manager toggles the view between FIFO and Average Cost to see valuation impacts.

## 6. Acceptance Criteria
- [ ] Every stock movement MUST create paired journal entries (e.g., In from Supplier = Debit Warehouse Inventory, Credit In-Transit/Supplier).
- [ ] Support calculation for multiple valuation methods (FIFO and Average Cost).
- [ ] Real-time calculation of stock balance from the ledger entries (no arbitrary balance overrides).
- [ ] Immutable audit trail for all changes (if a mistake is made, a reversing entry must be posted, no hard deletes).

## 7. Edge Cases and Constraints
- **Negative Stock Prevention**: System must prevent outbound movements that would cause a location's balance to drop below zero.
- **Concurrency**: High volume of simultaneous inbound and outbound transactions must not cause race conditions resulting in inaccurate balance calculations.

## 8. Dependencies on Other Modules
- **Finance Module**: `StockMovement` domain event must be published so the Finance module can update inventory valuation in the General Ledger.

## 9. Out of Scope
- Integration with external supplier ERPs via EDI.
- RFID integration for automated ledger entries (ledger relies on UI inputs for now).
