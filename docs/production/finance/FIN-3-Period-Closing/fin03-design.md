# Design Specifications: Period Closing (FIN-3)

## 1. Screen Overview
**Page:** Accounting Periods Dashboard

## 2. Wireframe Description
```text
=== Accounting Periods ===
[Header] [Button: Generate Next Year Periods]

| Year | Month | Start Date | End Date | Status | Actions |
| 2026 | May   | 01-05-26   | 31-05-26 | [Badge: CLOSED]| [Re-open] (Admin only) |
| 2026 | June  | 01-06-26   | 30-06-26 | [Badge: OPEN]  | [Close Period] |
| 2026 | July  | 01-07-26   | 31-07-26 | [Badge: OPEN]  | - |

=== Close Period Modal (June 2026) ===
Title: Close Accounting Period
[Checklist UI]
- [X] All journal entries posted? 
  *Warning: 2 entries are still pending approval.* [View Entries Link]
- [X] Depreciation calculated? (Manual Check)
- [X] Bank reconciled? (Manual Check)

[Input: "Type CLOSE to confirm"] (Disabled until checks pass)
[Button: Cancel] [Button: Confirm Close (Disabled)]

=== Re-open Period Modal ===
Title: Re-open Period (May 2026)
Warning: Re-opening a closed period is a highly audited action.
[Textarea: Reason for re-opening (Required)]
[Button: Cancel] [Button: Confirm Re-open (Destructive/Red)]
```

## 3. Component Hierarchy
- `PeriodsDashboard`
  - `PeriodsTable`
    - `StatusBadge` (Green for OPEN, Gray for CLOSED)
    - `ActionMenu`
  - `ClosePeriodDialog`
    - `ValidationChecklist` (Fetches live data for pending entries)
    - `ConfirmationInput`
  - `ReopenPeriodDialog`
    - `ReasonForm` (Zod validation for min length)

## 4. UI Components (shadcn/ui)
- `Table` for the main list.
- `Dialog` (Modal) for the highly-sensitive close and re-open actions.
- `Alert` / `AlertDescription` inside the modal to show warnings (e.g., pending entries).
- `Badge` for Open/Closed statuses.

## 5. Visual Guidelines
- The "Re-open" button should be styled as Destructive (Red) to indicate it's a risky action.
- The validation checklist inside the Close modal provides a reassuring UX, showing the CFO exactly what the system is checking.

## 6. Responsive Design
- The table should scroll horizontally on mobile. The modals should take up full screen width on mobile devices.

## 7. States & Interactions
- **Live Validation**: When the Close Period modal opens, it triggers an API call `GET /api/v1/finance/periods/{id}/validate`. The UI shows a skeleton loader for the checklist items until the API responds.
- **Double Confirmation**: Closing a period requires typing "CLOSE" to prevent accidental clicks.

## 8. Accessibility
- All dialogs must trap focus.
- The checklist items must be announced clearly by screen readers (e.g., "Warning: 2 pending entries found").
