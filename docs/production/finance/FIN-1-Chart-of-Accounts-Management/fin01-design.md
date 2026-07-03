# Design Specifications: Chart of Accounts Management (FIN-1)

## 1. Screen Overview
**Page 1:** Chart of Accounts Tree Dashboard
**Page 2:** Account Creation/Edit Modal

## 2. Wireframe Description
```text
=== Chart of Accounts Dashboard ===
[Header: Chart of Accounts] [Button: + New Account]
[Search Bar: "Search by code or name..."]

[List / Tree View]
[-] 1000 - Assets 
  [-] 1100 - Current Assets
      [Line] 1110 - Cash in Bank (Active) [Edit]
      [Line] 1120 - Accounts Receivable (Active) [Edit]
  [+] 1200 - Fixed Assets
[+] 2000 - Liabilities
[+] 3000 - Equity
[+] 4000 - Revenue
[+] 5000 - Expenses

=== New Account Modal ===
Title: Create Account
[Input: Account Code (e.g., 1110)]
[Input: Account Name (e.g., Cash in Bank)]
[Dropdown: Parent Account (Searchable)]
[Dropdown: Account Type (Asset, Liability, etc.)] *Auto-filled if Parent is selected
[Toggle: Is Active]
[Button: Cancel] [Button: Save]
```

## 3. Component Hierarchy
- `ChartOfAccountsPage`
  - `CoaToolbar` (Search and Create buttons)
  - `CoaTreeView` (Recursive component to render hierarchy)
    - `CoaTreeItem`
      - `Badge` (Status active/inactive)
      - `ActionMenu` (Edit, Deactivate)
  - `AccountFormModal` (React Hook Form + Zod)
    - `Combobox` (For parent account selection)

## 4. UI Components (shadcn/ui)
- `Accordion` or custom Collapsible list for the tree view.
- `Dialog` (Modal for forms).
- `Command` (Inside Combobox for searching thousands of potential parent accounts).
- `Form`, `Input`, `Select`, `Switch`.
- `Badge` for status indication.

## 5. Visual Guidelines
- **Indentation is key**: The tree view must clearly show hierarchy through indentation (e.g., `ml-4` or `pl-4` in Tailwind per depth level).
- Use distinct icons for different account types (e.g., Green arrow for Revenue, Red arrow for Expenses, Bank icon for Assets) to make the list scannable.

## 6. Responsive Design
- The tree view on mobile can be difficult to navigate. Consider a flat list with breadcrumbs (e.g., `Assets > Current > Cash`) for mobile screens, while retaining the tree structure for Desktop/Tablet.

## 7. States & Interactions
- **Optimistic UI**: When searching the COA, the tree should instantly filter and auto-expand nodes that match the query.
- **Form Auto-fill**: Selecting a Parent Account automatically locks the "Account Type" field to match the parent.

## 8. Accessibility
- Tree nodes must be keyboard navigable (`Up/Down` arrows) and expandable (`Right/Left` arrows or `Enter`).
- `aria-expanded` attributes must be used on tree nodes.
