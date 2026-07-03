# Production Requirements: Attendance Management (HR-2)

## 1. Feature Overview
- **Feature Name**: Attendance Management
- **Module**: HR & Payroll
- **User Story**: As an HR Staff, I want to track employee attendance so that payroll calculation is accurate.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Time is money. Accurate attendance tracking ensures employees are paid correctly for overtime and penalized appropriately for lateness or unauthorized absences, directly impacting the bottom line (payroll costs).
- **ROI Estimation**: Reduces time theft and manual timesheet calculation errors, saving HR up to 2 days of work per month during payroll preparation.

## 3. Success Metrics
- 100% of clock-in/out records correctly calculate hours worked.
- Overtime and Late penalties are automatically flagged based on company rules without manual intervention.

## 4. User Persona
- **Employee**: Uses the system daily to clock in and out, and to request leave.
- **HR Staff / Manager**: Reviews attendance logs, approves overtime, and manages shift schedules.

## 5. User Journey
1. **Clock In**: Employee logs in from their mobile phone or laptop when they arrive at work and clicks "Clock In". The system records the timestamp and IP address/Location.
2. **Late Detection**: If the employee clocks in at 09:15 AM (shift starts at 09:00 AM, with 10 mins tolerance), the system flags the entry as "Late" and calculates 5 minutes of lateness.
3. **Clock Out**: Employee clicks "Clock Out" at 18:30 PM (shift ends 18:00 PM).
4. **Overtime Processing**: The system detects 30 minutes of extra time. It flags it as "Pending Overtime Approval". The Manager reviews and approves it.
5. **Payroll Prep**: At the end of the month, the attendance engine aggregates Total Days Worked, Total Late Minutes, and Total Approved Overtime Hours, passing this data to the Payroll Engine (HR-3).

## 6. Acceptance Criteria
- [ ] Clock-in and Clock-out functionality with timestamping.
- [ ] Ability to define Shifts (Start time, End time, Late tolerance).
- [ ] Automatic calculation of "Late" status based on shift rules.
- [ ] Automatic calculation of "Overtime" requiring manager approval.
- [ ] Leave request and approval workflow (Sick, Annual, Unpaid).
- [ ] Monthly attendance aggregation view per employee.

## 7. Edge Cases and Constraints
- **Forgot to Clock Out**: If an employee clocks in but never clocks out, the system should auto-clock them out at a defined cutoff time (e.g., midnight) and flag the record as "Invalid - Needs Review".
- **Timezone Differences**: Remote employees working in different timezones must be evaluated against the company's base timezone or their specific shift timezone.

## 8. Dependencies on Other Modules
- Dependent on **HR-1 (Employee Data)** for active employee lists and manager hierarchy.
- Feeds data directly into **HR-3 (Payroll Processing)**.
- Integrated with **TaskProject (Time Tracking)** for productivity correlations (HR-7).

## 9. Out of Scope
- Biometric hardware integration (e.g., fingerprint scanners) is out of scope. Attendance is strictly software/web-based for now.
- Geofencing validation using GPS coordinates.
