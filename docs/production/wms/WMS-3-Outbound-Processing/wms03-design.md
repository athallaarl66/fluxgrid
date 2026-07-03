# Design Specifications: Outbound Processing (WMS-3)

## 1. Screen Overview
**Page 1:** Outbound Dashboard (Kanban style columns for Pending, Picking, Packing, Shipped)
**Page 2:** Pick List Execution View (Mobile/Tablet optimized)
**Page 3:** Packing & Shipping Verification

## 2. Wireframe Description
```text
=== Outbound Dashboard ===
[Header] [Generate Pick List from Sales Orders]

[Kanban Board View]
Column: To Pick       | Column: To Pack    | Column: Shipped
----------------------|--------------------|----------------
[Card: SO-123]        | [Card: SO-122]     | 
Lines: 5              | Status: QA Check   |
----------------------|--------------------|

=== Pick List Execution View (Tablet optimized) ===
[Back] Pick List: PL-001 (For SO-123)
[Progress Bar: 0 / 5 Items Picked]

[Item Card: 1]
Location: Bin-A1 | SKU: SKU-X | Qty: 10
[Input: Qty Picked (Default: 10)] [Btn: Confirm Pick] [Btn: Short Pick]

=== Packing Verification ===
Order: SO-123
| SKU | Expected | Verified | Status |
| SKU-X | 10 | [ 10 ] | [Green Check] |

[Button: Confirm Packing] [Button: Mark as Shipped]
```

## 3. Component Hierarchy
- `OutboundDashboard`
  - `OutboundKanbanBoard` (using dnd-kit for moving orders visually if needed)
    - `OrderCard`
- `PickListExecution`
  - `ProgressTracker`
  - `PickItemCard` (Large touch targets)
    - `NumberInput`
    - `ActionButtons`
- `PackingShipmentForm`
  - `VerificationTable`
  - `ShipmentConfirmationDialog`

## 4. UI Components (shadcn/ui)
- `Progress` (for pick completion percentage)
- `Card` (for kanban tickets and pick items)
- `Dialog` (for confirming shipment and capturing courier details if any)
- `Table` (for packing verification)
- `Input` (Numeric inputs for quantities)

## 5. Visual Guidelines
- **Tablet First for Picking**: The picking interface must be designed for an iPad/Tablet held vertically or horizontally by a warehouse worker. Large fonts, clear contrast, large buttons.
- **Success Cues**: When a line item is packed/verified, the row should flash green or highlight clearly to provide instant visual feedback.

## 6. Responsive Design
- The Dashboard and Packing views are desktop-first (though responsive).
- The Picking view is Tablet-first.

## 7. States & Interactions
- **Short Pick Interaction**: If a user clicks "Short Pick", a modal pops up asking for a reason code (e.g., Damaged, Missing) before allowing them to proceed.
- **Auto-Advance**: Upon clicking "Confirm Pick" for an item card, the UI automatically scrolls to the next item card.

## 8. Accessibility
- All status colors (Green/Red) must be accompanied by icons (Check/Cross) for colorblind users.
