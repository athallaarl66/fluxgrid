# Testing Scenarios: Productivity Analytics (HR-7)

## 1. Test Strategy Overview
Testing focuses on verifying that the Domain Events from external modules (TaskProject) are correctly consumed, aggregated, and accurately displayed on the dashboard without skewing data.

## 2. Test Cases

### TC-01: Correct Event Consumption (Integration)
- **Given** the TaskProject module fires a `TaskCompleted` event for Employee ID "123".
- **When** the HR event listener receives it.
- **Then** the `employee_productivity_stats` table for Employee "123" increments its `tasks_completed` counter by 1 for the current date.

### TC-02: Productivity Score Calculation (Happy Path)
- **Given** Employee A worked 160 hours this month (from Task App Attendance) and logged 150 hours of active task time (from TaskProject).
- **When** the dashboard calculates their Utilization Rate.
- **Then** the rate displayed is 93.75%.

### TC-03: Excluded Departments
- **Given** the "Security" department is marked as `exclude_from_analytics = true`.
- **When** the HR Manager views the company-wide average productivity score.
- **Then** the Security employees' 0% task completion rate does not drag down the company average.

### TC-04: Cross-Tenant Data Isolation (Security)
- **Given** Tenant A and Tenant B both use the system.
- **When** Tenant A views the Analytics dashboard.
- **Then** the aggregation queries strictly filter by `tenant_id = A`.
- **And** Tenant B's high performers do not appear in Tenant A's charts.

## 3. Performance Testing
- Analytics dashboards are notorious for slowing down databases. Ensure that the dashboard relies on a pre-aggregated `monthly_productivity_summary` materialized view or summary table, rather than querying raw Task App attendance data and `tasks` in real-time. The dashboard must load in < 2 seconds.

## 4. Security & Access Testing
- Only `hr.analytics.read` or `admin` roles can view this page. Individual employees cannot view the analytics dashboard, though they might have a personal summary on their own profile.
