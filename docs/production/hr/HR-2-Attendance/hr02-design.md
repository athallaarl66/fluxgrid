# Design Specifications: Attendance Management (HR-2)

## 1. Screen Overview
**Page 1:** Employee Dashboard (Self-Service: Clock In/Out)
**Page 2:** Manager Attendance Review (Team view, Overtime approval)
**Page 3:** HR Attendance Master (Company-wide view)

## 2. Wireframe Description
```text
=== Employee Dashboard (Mobile Optimized) ===
[Current Time: 08:55 AM]
[Shift: 09:00 - 18:00]

      [BIG GREEN BUTTON: CLOCK IN]
      (Disabled if already clocked in)

Recent Activity:
- Yesterday: In 08:50, Out 18:05
- Monday: In 09:15 (Late), Out 18:00

=== Manager Review Dashboard ===
[Header] Pending Approvals

[Table: Overtime Requests]
| Employee | Date | Shift End | Clock Out | Requested | Action |
| J. Doe   | 15/5 | 18:00     | 19:30     | 1.5 hrs   | [Approve] [Edit] [Reject] |

[Table: Team Attendance Today]
| Employee | Status | Clock In | Lateness |
| J. Doe   | In     | 08:50    | -        |
| S. Smith | Late   | 09:30    | 30 mins  |
| M. Lee   | Absent | -        | -        |
```

## 3. Component Hierarchy
- `EmployeeDashboard`
  - `ClockWidget` (Displays live time)
  - `ActionPanel`
    - `ClockButton` (Toggles In/Out state based on fetched status)
  - `AttendanceHistoryList`
- `ManagerDashboard`
  - `OvertimeApprovalTable`
    - `ApprovalModal` (Allows manager to override hours before approving)
  - `TeamStatusTable` (Real-time view of who is currently in the office)

## 4. UI Components (shadcn/ui)
- `Button` (Extra large, prominent CTA for clocking in).
- `Table` for manager and HR views.
- `Dialog` for overtime overrides or leave requests.
- `Badge` for statuses (Green for On-Time, Red for Late/Absent).

## 5. Visual Guidelines
- **Clock Button**: Must be the most obvious element on the page. Use a large touch target area since many employees will tap this on their phones while walking through the door.
- **Lateness Indicators**: Highlight lateness in a noticeable but non-punitive color (e.g., Orange instead of harsh Red).

## 6. Responsive Design
- The Employee Dashboard MUST be mobile-first.
- Manager and HR dashboards are desktop-first.

## 7. States & Interactions
- **Live Clock**: The current time should tick visually on the screen.
- **Button Loading**: When "Clock In" is clicked, disable the button and show a spinner until the API returns a 200 OK. Prevent double-taps.
- **Geolocation Feedback**: If geofencing was in scope, prompt the user if they are outside the allowed radius. (Out of scope for this iteration, but UI should be prepared for future warnings).

## 8. Accessibility
- The Clock In button must be easily triggerable via keyboard (`Space` or `Enter`).
- State changes (e.g., "Successfully Clocked In at 08:55") must be announced via ARIA live regions.
