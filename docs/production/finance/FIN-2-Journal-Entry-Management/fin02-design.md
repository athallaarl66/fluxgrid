# Design Specifications: Journal Entry Management (FIN-2)

## 1. Screen Overview
**Page 1:** Journal Entries Dashboard (List view of all entries with status filters)
**Page 2:** Create/Edit Journal Entry Form

## 2. Wireframe Description
```text
=== Journal Entries Dashboard ===
[Header] [Button: + New Journal Entry]
Filter: [All] [Draft] [Pending Approval] [Posted]

| Date | Entry No | Description | Total Amount | Status | Actions |
| 15/05| JE-001   | Accrual     | 1,000,000    | Posted | [View]  |

=== Create Journal Entry ===
[Header: New Entry]

Transaction Date: [DatePicker]
Description: [TextInput]

[Line Items Table]
| Account | Description (Line) | Debit | Credit | Actions |
| [Combobox: Search COA] | [Text] | [Num] | [Num] | [X] |
| [Combobox: Search COA] | [Text] | [Num] | [Num] | [X] |
[Button: + Add Line]

[Summary Footer]
Total Debit: 1,000,000 | Total Credit: 900,000
[Warning Text: Out of balance by 100,000]

[Button: Save as Draft] [Button: Submit (Disabled)]
```

## 3. Component Hierarchy
- `JournalEntryDashboard`
  - `FilterBar`
  - `DataTable`
- `JournalEntryForm`
  - `HeaderSection` (Date, Description)
  - `LineItemsManager` (useFieldArray from react-hook-form)
    - `JournalLineRow`
      - `AccountCombobox` (Searchable COA)
      - `CurrencyInput` (Formats numbers with thousands separators as you type)
  - `BalanceFooter` (Calculates totals in real-time)

## 4. UI Components (shadcn/ui)
- `Table` for the main list and the entry lines.
- `Combobox` (Popover + Command) for selecting accounts smoothly.
- `DatePicker` for transaction date.
- `Alert` / `Badge` to show out-of-balance warnings.

## 5. Visual Guidelines
- **Debit/Credit alignment**: In the line items table, Debit and Credit columns MUST be right-aligned so decimal points line up vertically, allowing accountants to scan numbers quickly.
- **Color Coding**: Status badges: Gray (Draft), Yellow (Pending), Green (Posted). Out-of-balance warning in bold Red.

## 6. Responsive Design
- Journal Entry forms are notoriously difficult on mobile due to the wide table format.
- On mobile: Convert the line item table into a series of Cards (one card per line).
- Prioritize Desktop/Tablet for this specific screen.

## 7. States & Interactions
- **Auto-balancing Helper**: If the user has entered Debit 1000, and they add a new line, automatically fill the Credit column of the new line with 1000 to save typing.
- **Real-time validation**: The "Submit" button turns active only when `Sum(Debit) === Sum(Credit)` and `Sum(Debit) > 0`.

## 8. Accessibility
- Tab navigation order is critical. The user should be able to `Tab` from Account -> Desc -> Debit -> Credit -> Next Row Account seamlessly without using the mouse.
