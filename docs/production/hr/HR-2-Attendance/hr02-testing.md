# Testing Scenarios: Attendance Management (HR-2)

## 1. Test Strategy Overview
Testing for Attendance focuses on time calculation accuracy, specifically around boundary conditions (exactly on time vs 1 minute late), tolerance rules, and state transitions (e.g., auto-clock-out).

## 2. Test Cases

### TC-01: On-Time Clock In (Happy Path)
- **Given** a shift that starts at 09:00 with a 15-minute tolerance.
- **When** an employee clocks in at 08:55.
- **Then** the status is marked as "On Time".
- **And** lateness minutes = 0.

### TC-02: Late Clock In with Tolerance (Happy Path)
- **Given** the same shift (09:00, 15m tolerance).
- **When** the employee clocks in at 09:10.
- **Then** the status is marked as "On Time" (within tolerance).
- **And** lateness minutes = 0.

### TC-03: Late Clock In (Negative Testing)
- **Given** the same shift.
- **When** the employee clocks in at 09:16.
- **Then** the status is marked as "Late".
- **And** lateness minutes = 16 (calculation usually starts from 09:00, not from the end of tolerance, based on company rules).

### TC-04: Overtime Calculation & Approval
- **Given** a shift ending at 18:00.
- **When** the employee clocks out at 19:30.
- **Then** the system records 1.5 hours of potential overtime.
- **And** marks it as "Pending Approval".
- **When** the Manager approves 1.0 hours (rejecting 0.5h).
- **Then** the approved overtime is strictly 1.0 hours for payroll.

### TC-05: Missing Clock Out (Edge Case)
- **Given** an employee clocks in at 09:00 but never clocks out.
- **When** the daily cron job runs at 23:59.
- **Then** the system auto-clocks out the employee at 18:00 (end of shift).
- **And** flags the record as "Requires HR Review".

### TC-06: Duplicate Clock In Prevention (Negative Testing)
- **Given** an employee who is already clocked in.
- **When** they attempt to hit the "Clock In" endpoint again on the same day.
- **Then** the API returns a 409 Conflict: "Already clocked in today."

### TC-07: Leave Request Overlap
- **Given** an employee has an approved Annual Leave for May 15.
- **When** they attempt to Clock In on May 15.
- **Then** the system allows it (they might have come in for an emergency) but warns HR of the conflict.

## 3. Performance Testing
- At 09:00 AM, a company with 1,000 employees might have 800 people clicking "Clock In" simultaneously. The `POST /attendance/clock-in` endpoint must handle this burst traffic gracefully (e.g., using Redis caching/queuing if Postgres locks become a bottleneck).

## 4. Security & Access Testing
- An employee cannot clock in for another employee (ensure `user_id` is extracted from the JWT, not passed in the request body).
- Clock times rely on the server timestamp `NOW()`, not the client's device time (which can be manipulated).
