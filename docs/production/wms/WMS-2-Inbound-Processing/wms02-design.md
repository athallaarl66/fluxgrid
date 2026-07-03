# Design Specifications: Inbound Processing (WMS-2)

## 1. Screen Overview
**Page 1:** Inbound List Dashboard
**Page 2:** Receive Goods Form
**Page 3:** Putaway Assignment Form

## 2. Wireframe Description
```text
=== Inbound List ===
[Header] [New Receipt Button]
| Receipt No | PO Ref | Date | Status | Actions |
| REC-001 | PO-55 | Today | Pending Putaway | [Process Putaway] |

=== Receive Goods Form ===
[Input: PO Number] [Search Button]

[PO Details Card]
Supplier: ABC Corp | Date: ...

[Line Items Table]
| SKU | Name | Ordered | Received | Passed QA | Failed QA |
| SKU-1 | Widget | 100 | [ 100 ] | [ 90 ] | [ 10 ] |

[Confirm Receipt Button]

=== Putaway Assignment Form ===
[Receipt: REC-001 Summary]
| SKU | Name | Qty to Putaway | Target Bin |
| SKU-1 | Widget | 90 | [ Dropdown: Bin-A1 ] |
| SKU-1 | Widget | 10 | [ Auto: Quarantine ] |

[Confirm Putaway Button]
```

## 3. Component Hierarchy
- `InboundDashboard`
- `CreateReceiptPage`
  - `POSearchForm`
  - `ReceiptLineItems`
    - `NumberInput` (for Qty received/passed/failed)
  - `SubmitReceipt`
- `PutawayPage`
  - `BinSelectionTable`
    - `ComboBox` (searchable select for locations)
  - `SubmitPutaway`

## 4. UI Components (shadcn/ui)
- `Card` (for layout grouping)
- `Input` / `Button`
- `Table` (for line items)
- `Command` & `Popover` (Combobox for searching thousands of bin locations)
- `Badge` (Status indicators: e.g., yellow for "Pending Putaway", green for "Completed")

## 5. Visual Guidelines
- Fast data entry is prioritized: Use large touch targets for tablet users on the warehouse floor.
- Clear error states (red outlines) for over-receiving quantities.
- Emphasize the "Passed QA" and "Target Bin" fields as they dictate stock ledger accuracy.

## 6. Responsive Design
- The forms must be highly responsive. Warehouse staff often use tablets. Stack table rows into cards on narrow screens if necessary.

## 7. States & Interactions
- **Auto-fill**: When PO is searched, auto-fill the "Received" and "Passed QA" columns with the ordered quantity to speed up happy-path entry.
- **Validation feedback**: Instant inline warning if `Passed + Failed != Received`.
- **Loading State**: Disable submit buttons and show spinners while transaction processes.

## 8. Accessibility
- All numeric inputs must have `aria-labels` mapping to the row/SKU they belong to.
- Allow keyboard form submission (`Enter` key) for rapid barcode scanner compatibility (scanners often append an Enter keystroke).
