# task-project02-design.md
# Design Specifications — TP-2: Time Tracking
## FluxGrid ERP | Module: Task & Project Management (TaskProject)

---

## 1. Screen Overview

The Time Tracking feature spans three primary UI surfaces:

| Screen / Component | Route / Location | Primary Actor |
|-------------------|-----------------|---------------|
| **Task Detail — Time Log Tab** | `/projects/[projectId]/tasks/[taskId]` → "Time Logs" tab | Team Member |
| **Timer Widget (Floating)** | Global persistent widget (bottom-right corner) | Team Member |
| **Log Time Modal / Drawer** | Modal overlaid on Task Detail | Team Member |
| **Time Approval Page** | `/time-approvals` | Project Manager |
| **My Time Logs Page** | `/my-time` | Team Member |

---

## 2. Design System References

| Token Category | Value |
|---------------|-------|
| **Design System** | shadcn/ui + Tailwind CSS v4 |
| **Primary Color** | `hsl(221, 83%, 53%)` — FluxGrid Blue (`--primary`) |
| **Success/Approve** | `hsl(142, 71%, 45%)` — Green (`--success`) |
| **Warning/Pending** | `hsl(43, 96%, 50%)` — Amber (`--warning`) |
| **Danger/Reject** | `hsl(0, 84%, 60%)` — Red (`--destructive`) |
| **Timer Active** | `hsl(221, 83%, 53%)` pulsing ring |
| **Timer Paused** | `hsl(43, 96%, 50%)` static ring |
| **Font Family** | Inter (body), JetBrains Mono (timer digits) |
| **Timer Font** | `font-mono text-3xl font-bold tabular-nums` |
| **Border Radius** | `--radius: 0.5rem` (base), `0.75rem` (cards) |
| **Spacing Unit** | 4px base (Tailwind default) |

---

## 3. Screen Layouts & Wireframe Descriptions

### 3.1 Task Detail Page — Time Log Tab

```
┌─────────────────────────────────────────────────────────────────┐
│ ◀ Back to Project      TASK-101: Implement Auth Flow       [●●●] │
│─────────────────────────────────────────────────────────────────│
│ [Overview] [Subtasks] [Comments] [Time Logs] [Files]            │
│─────────────────────────────────────────────────────────────────│
│  TIME LOGS                               [▶ Start Timer] [+ Log] │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ TIMER WIDGET (when active)                               │    │
│  │  ⏱ 01:23:45         [⏸ Pause]  [⏹ Stop]               │    │
│  │  Task: Implement Auth Flow                               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                   │
│  Total logged: 5.5h  │  Approved: 3.5h  │  Pending: 2.0h       │
│                                                                   │
│  ┌──────┬──────────┬───────┬───────────────┬────────────────┐   │
│  │ Date │ Duration │ Bill. │ Description   │ Status         │   │
│  ├──────┼──────────┼───────┼───────────────┼────────────────┤   │
│  │ Jul 2│  3.5h    │  ✓   │ Auth impl...  │ ✅ Approved    │   │
│  │ Jul 1│  2.0h    │  ✓   │ Code review   │ 🕐 Pending     │   │
│  └──────┴──────────┴───────┴───────────────┴────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Layout Notes**:
- Tab navigation uses shadcn `<Tabs>` component with underline variant
- Time Logs tab shows a summary banner with total/approved/pending hours
- The inline timer widget only appears within this tab when a timer is active for this specific task
- The table uses shadcn `<DataTable>` with sortable columns by Date and Duration
- Each row has an actions dropdown: Edit (if pending/draft), View Details

---

### 3.2 Log Time Modal

```
┌───────────────────────────────────────────┐
│  Log Time                           [✕]   │
│───────────────────────────────────────────│
│  Task: Implement Auth Flow                │
│                                           │
│  Date *                                   │
│  ┌─────────────────────────────────────┐  │
│  │  📅  July 2, 2026            [↓]   │  │
│  └─────────────────────────────────────┘  │
│                                           │
│  Duration *                               │
│  ┌──────────────────┐                     │
│  │  3.5             │  hours              │
│  └──────────────────┘                     │
│  Min: 0.25h (15 min) · Max: 24h          │
│                                           │
│  Description (optional)                   │
│  ┌─────────────────────────────────────┐  │
│  │  What did you work on?              │  │
│  │                                     │  │
│  └─────────────────────────────────────┘  │
│                                           │
│  Billable                                 │
│  ○━━━━━━━● Billable to client            │
│                                           │
│  ─────────────────────────────────────── │
│  [Cancel]                    [Submit Log] │
└───────────────────────────────────────────┘
```

**Component Breakdown**:
- `<Dialog>` from shadcn/ui (modal overlay with backdrop blur)
- Date field: shadcn `<DatePicker>` with disabled future dates and >30-day-past dates (grayed out)
- Duration field: shadcn `<Input>` type=number, step=0.25, helper text below
- Description: shadcn `<Textarea>` rows=3 with character count (max 500)
- Billable toggle: shadcn `<Switch>` with label, default ON
- Submit button: `variant="default"` (primary blue), disabled during loading

---

### 3.3 Timer Review & Submit Modal (After Stop)

```
┌───────────────────────────────────────────┐
│  Review & Submit Time                [✕]  │
│───────────────────────────────────────────│
│  ⏱ Tracked time:                         │
│                                           │
│         01:30:45                          │
│       → 1.51 hours                        │
│                                           │
│  Task: Implement Auth Flow (TASK-101)     │
│                                           │
│  Date: July 2, 2026 (auto-filled)        │
│                                           │
│  Adjust duration (optional)               │
│  ┌──────────────────┐                     │
│  │  1.51            │  hours              │
│  └──────────────────┘                     │
│                                           │
│  Description                              │
│  ┌─────────────────────────────────────┐  │
│  │                                     │  │
│  └─────────────────────────────────────┘  │
│                                           │
│  ○━━━━━━━● Billable to client            │
│                                           │
│  ─────────────────────────────────────── │
│  [Discard]               [Submit for Review] │
└───────────────────────────────────────────┘
```

**Notes**:
- Timer value shown in HH:MM:SS (monospace font) + converted hours in parentheses
- Duration field is pre-filled but editable (user can round up/down)
- "Discard" clears Redis timer state without creating a log
- A confirmation dialog appears on "Discard" to prevent accidental data loss

---

### 3.4 Timer Widget — Floating (Global Persistent)

```
┌─────────────────────────────────┐
│ ⏱  01:23:45                    │
│    TASK-101: Implement Auth...  │
│    [⏸ Pause]  [⏹ Stop]        │
└─────────────────────────────────┘
```

**Positioning**: Fixed, bottom-right corner, `z-index: 50`, `bottom: 1rem; right: 1rem`  
**Visibility**: Only shown when a timer is actively running or paused (any page)  
**Behavior**: Clicking the task name navigates to the task detail page

---

### 3.5 Time Approval Page

```
┌─────────────────────────────────────────────────────────────────┐
│ Time Approvals                                                    │
│─────────────────────────────────────────────────────────────────│
│ [All Projects ▼]  [All Members ▼]  [Date Range ▼]  [🔍 Search] │
│                                                                   │
│ PENDING APPROVAL (3)                                             │
│                                                                   │
│ ┌──────────┬────────┬──────┬───────┬──────────────┬──────────┐ │
│ │ Member   │ Task   │ Date │ Hours │ Description  │ Actions  │ │
│ ├──────────┼────────┼──────┼───────┼──────────────┼──────────┤ │
│ │ Budi S.  │TK-101  │Jul 2 │ 3.5h  │ Auth impl... │ [✓][✗]  │ │
│ │ Siti R.  │TK-102  │Jul 2 │ 8.0h  │ DB schema    │ [✓][✗]  │ │
│ │ Budi S.  │TK-101  │Jul 1 │ 2.0h  │ Code review  │ [✓][✗]  │ │
│ └──────────┴────────┴──────┴───────┴──────────────┴──────────┘ │
│                                                                   │
│ RECENTLY PROCESSED (Last 7 days)                                 │
│ [Collapsed accordion — click to expand]                          │
└─────────────────────────────────────────────────────────────────┘
```

**Component Breakdown**:
- Page-level filters: shadcn `<Select>` for project/member, `<DateRangePicker>`, `<Input>` for search
- Pending section uses shadcn `<DataTable>` with sortable columns
- `[✓]` = Approve button (green icon), `[✗]` = Reject button (red icon) — both with tooltips
- Inline approval: Clicking ✓ opens a confirmation popover; clicking ✗ opens rejection reason modal
- Bulk actions: Checkbox per row + "Approve Selected" button at top

---

### 3.6 My Time Page (Team Member)

```
┌─────────────────────────────────────────────────────────────────┐
│ My Time                                              [+ Log Time] │
│─────────────────────────────────────────────────────────────────│
│ [This Week ▼]  [All Projects ▼]                                 │
│                                                                   │
│  ┌─────────────────────────────────────────┐                    │
│  │  Week of June 30 – July 6, 2026         │                    │
│  │  Total: 18.5h  │  Approved: 14h  │ Pending: 4.5h  │        │
│  └─────────────────────────────────────────┘                    │
│                                                                   │
│  MON 30  ████████░░░░  4.0h                                     │
│  TUE 1   ████████████  6.0h                                     │
│  WED 2   ████░░░░░░░░  2.5h  ← Today                           │
│  THU 3   ░░░░░░░░░░░░  0.0h                                     │
│  FRI 4   ░░░░░░░░░░░░  0.0h                                     │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ TASK-101 │ Auth Flow  │ Jul 2 │ 3.5h │ ✅ Approved      │   │
│  │ TASK-102 │ DB Schema  │ Jul 2 │ 2.0h │ 🕐 Pending       │   │
│  │ TASK-101 │ Auth Flow  │ Jul 1 │ 6.0h │ ✅ Approved      │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Component Hierarchy

```
TimeTrackingFeature/
├── TaskDetail/
│   ├── TimeLogsTab/
│   │   ├── TimerCard (inline, conditional)
│   │   │   ├── TimerDisplay (HH:MM:SS, animated)
│   │   │   ├── TimerControls (Start/Pause/Resume/Stop buttons)
│   │   │   └── TimerStatusBadge
│   │   ├── TimeLogSummaryBanner
│   │   │   ├── TotalHoursBadge
│   │   │   ├── ApprovedHoursBadge
│   │   │   └── PendingHoursBadge
│   │   ├── TimeLogTable
│   │   │   ├── TimeLogTableRow[]
│   │   │   │   ├── StatusBadge
│   │   │   │   └── TimeLogActions (Edit, View)
│   │   │   └── EmptyTimeLogState
│   │   └── LogTimeButton
│   └── LogTimeModal/
│       ├── DatePickerField
│       ├── DurationInputField
│       ├── DescriptionTextarea
│       ├── BillableSwitch
│       ├── SubmitButton (with loading state)
│       └── FormValidationErrors
├── TimerWidget (global floating)/
│   ├── TimerDisplay
│   ├── TaskReference (link)
│   ├── PauseButton
│   └── StopButton
├── TimerReviewModal/
│   ├── TrackedTimeDisplay
│   ├── HoursConversion
│   ├── DurationOverrideInput
│   ├── DescriptionInput
│   ├── BillableSwitch
│   ├── SubmitButton
│   └── DiscardButton
├── TimeApprovalPage/
│   ├── ApprovalFilters
│   ├── PendingLogsTable
│   │   ├── ApprovalTableRow[]
│   │   │   ├── ApproveButton (with popover confirmation)
│   │   │   └── RejectButton (opens RejectModal)
│   │   └── BulkApproveButton
│   ├── RejectReasonModal
│   └── RecentlyProcessedAccordion
└── MyTimePage/
    ├── WeekSelector
    ├── ProjectFilter
    ├── WeekSummaryCard
    ├── DailyHoursBar
    └── TimeLogTable (personal view)
```

---

## 5. UI States

### Timer Widget States

| State | Visual | Controls |
|-------|--------|----------|
| **Idle** (no timer) | No floating widget | Start button in task detail |
| **Running** | Green pulsing ring animation; counting upward | Pause, Stop |
| **Paused** | Amber static ring; frozen counter; blinking "PAUSED" badge | Resume, Stop |
| **Submitting** | Spinner overlay on widget | — |

### Time Log Row States

| Status | Badge Style | Color | Icon |
|--------|-------------|-------|------|
| `draft` | Outline | Gray | ✏️ |
| `pending_approval` | Soft amber | Amber | 🕐 |
| `approved` | Solid green | Green | ✅ |
| `rejected` | Soft red + strikethrough | Red | ✗ |
| `archived` | Muted gray italic | Gray | 🗄️ |

### Log Time Modal States

| State | Visual |
|-------|--------|
| **Empty / Initial** | Empty form, Submit button disabled |
| **Filling** | Real-time validation, Submit enabled when valid |
| **Loading** | Submit button shows spinner, inputs disabled |
| **Error** | Inline field error messages in red, form shakes |
| **Success** | Modal closes, success toast shown |

### Approval Page States

| State | Description |
|-------|-------------|
| **Empty queue** | Illustration: "All caught up! No pending logs." |
| **Confirming approve** | Popover: "Approve 3.5h log for Budi Santoso?" [Confirm] [Cancel] |
| **Rejection modal open** | Dialog with text area for reason |
| **Processing** | Row dimmed with spinner; action buttons disabled |
| **Approved** | Row briefly highlighted green, then fades out |
| **Rejected** | Row briefly highlighted red, then fades out |

---

## 6. Timer Micro-Animations

### Running State — Pulsing Ring
```css
/* Applied to the timer container border */
@keyframes timer-pulse {
  0%, 100% { box-shadow: 0 0 0 0 hsl(221 83% 53% / 0.4); }
  50%       { box-shadow: 0 0 0 8px hsl(221 83% 53% / 0); }
}
.timer-running {
  animation: timer-pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
```

### Digit Tick Animation (HH:MM:SS)
- Each digit group (hours, minutes, seconds) uses `tabular-nums` to prevent layout shift
- Seconds digit updates with a subtle `translateY(-2px) → translateY(0)` slide on each tick
- Uses `requestAnimationFrame` for smooth 60fps updates

### Paused State — Blinking Badge
```css
@keyframes blink-badge {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.3; }
}
.paused-badge {
  animation: blink-badge 1.2s ease-in-out infinite;
}
```

### Approval Confirmation — Row Flash
- On approve: Row background transitions `bg-green-50 → transparent` over 1.5s
- On reject: Row background transitions `bg-red-50 → transparent` over 1.5s
- Uses Framer Motion `animate` with `opacity: [1, 0]` for fade-out from list

### Log Time Button — Hover Effect
- `transform: scale(1.02)` on hover, `transition: all 150ms ease`
- Icon rotates `+90deg` on hover (plus to X metaphor)

---

## 7. Responsive Design

### Breakpoints (Tailwind)

| Breakpoint | Width | Behavior |
|------------|-------|----------|
| `sm` | 640px | Single column layout |
| `md` | 768px | 2-column table with condensed columns |
| `lg` | 1024px | Full table; sidebar visible |
| `xl` | 1280px | Full layout with filters sidebar |

### Mobile Adaptations (< 768px)

- **Task Detail Time Logs Tab**: Table replaced by card list (each log is a `<Card>`)
- **Log Time Modal**: Full-screen drawer (`<Drawer>` from shadcn) instead of dialog
- **Timer Widget**: Positions at bottom, full-width strip instead of corner widget
- **Approval Page**: Swipe-to-approve gesture on log cards (HammerJS touch events)
- **Duration Input**: Numeric keypad-friendly (`inputmode="decimal"`)

### Tablet Adaptations (768px – 1023px)

- Table is horizontally scrollable
- Approval page filters collapse into a single "Filters" button → slide-out panel
- Timer widget remains in corner

---

## 8. Accessibility Requirements

### WCAG 2.1 AA Compliance

| Requirement | Implementation |
|-------------|---------------|
| Color contrast | All text meets 4.5:1 ratio; status badges use color + icon + text |
| Keyboard navigation | Timer controls fully keyboard operable (Tab, Space, Enter) |
| Screen reader support | Timer announces time via `aria-live="polite"` region updating every second |
| Focus management | Modal opens with focus on first field; closes with focus returned to trigger button |
| Error announcements | Validation errors use `role="alert"` |
| Timer visual alternative | Elapsed time also shown as text "X hours Y minutes elapsed" for screen readers |
| Button labels | All icon-only buttons have `aria-label` (e.g., `aria-label="Pause timer"`) |
| Loading states | `aria-busy="true"` on loading containers |

### ARIA Markup Highlights
```html
<!-- Timer display -->
<div 
  role="timer" 
  aria-label="Elapsed time" 
  aria-live="polite"
  aria-atomic="true"
>
  01:23:45
</div>

<!-- Status badge -->
<span aria-label="Status: Pending approval">
  🕐 Pending
</span>

<!-- Approve button -->
<button aria-label="Approve time log for Budi Santoso, 3.5 hours on July 2">
  ✓
</button>
```

---

## 9. Visual Guidelines

### Color Usage

| Context | Color Token | Usage |
|---------|------------|-------|
| Timer active | `--primary` + `ring` | Pulsing ring around timer |
| Approve action | `--success` (#22c55e) | Approve button, approved badge |
| Reject / Danger | `--destructive` (#ef4444) | Reject button, rejected badge |
| Pending | `--warning` (#f59e0b) | Pending badge, paused timer |
| Billable icon | `--primary` | Checkmark icon for billable |
| Non-billable | `--muted` | Dash icon |

### Typography

| Element | Classes |
|---------|---------|
| Timer digits | `font-mono text-3xl font-bold tabular-nums tracking-wider` |
| Log hours | `font-mono text-sm tabular-nums` |
| Log date | `text-sm text-muted-foreground` |
| Status badge | `text-xs font-medium` |
| Modal title | `text-lg font-semibold` |
| Section header | `text-sm font-medium uppercase tracking-wide text-muted-foreground` |

### Iconography (Lucide Icons)

| Action | Icon |
|--------|------|
| Start timer | `Play` |
| Pause timer | `Pause` |
| Resume timer | `Play` |
| Stop timer | `Square` |
| Log time | `Clock` |
| Approved | `CheckCircle2` |
| Rejected | `XCircle` |
| Pending | `Clock3` |
| Billable | `DollarSign` |
| Draft | `FilePen` |

---

## 10. Empty States & Loading States

### Empty: No Time Logs Yet
```
    ⏱
  No time logged yet

  Track your effort on this task by starting a timer
  or manually logging time.

  [▶ Start Timer]  [+ Log Time]
```

### Empty: No Pending Approvals
```
    ✅
  All caught up!

  There are no time logs waiting for your approval.
```

### Loading: Time Logs Table
- Skeleton rows: 3 rows of animated `<Skeleton>` components (shimmer effect)
- Width pattern: `w-24 w-32 w-16 w-48 w-20` per column

---

## 11. Design References

| Reference | Type | Notes |
|-----------|------|-------|
| shadcn/ui DataTable | Component | Base for all tables |
| shadcn/ui Dialog & Drawer | Component | Log time modal |
| shadcn/ui DatePicker | Component | Date selection |
| Framer Motion | Library | Row animations, widget slide-in |
| Lucide React | Icons | All iconography |
| Tailwind CSS v4 | Styling | Utility classes |
| FluxGrid Design Tokens | Internal | Colors, typography, spacing |

---

*Document Version: 1.0 | Generated: 2026-07-02 | Author: FluxGrid SDD Agent*
