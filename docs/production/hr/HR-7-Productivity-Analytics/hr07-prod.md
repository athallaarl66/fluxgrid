# Production Requirements: Productivity Analytics (HR-7)

## 1. Feature Overview
- **Feature Name**: Productivity Analytics Dashboard
- **Module**: HR & Payroll (Integrated with TaskProject)
- **User Story**: As an HR Manager, I want to view productivity analytics so that I can evaluate team performance objectively based on actual data.
- **Priority**: Could Have

## 2. Business Value & ROI
- **Business Value**: Bridges the gap between "Time spent in office" (Attendance) and "Actual work delivered" (Tasks completed). Identifies high performers and bottlenecks using hard data instead of subjective manager opinions.
- **ROI Estimation**: Improves workforce efficiency by identifying under-utilized resources or overloaded employees, potentially reducing overtime costs by 15%.

## 3. Success Metrics
- Dashboard visualizes the correlation between Attendance hours and Task completion rates.
- Generates a "Productivity Score" for employees based on completed tasks vs scheduled hours.

## 4. User Persona
- **HR Manager**: Uses the data for annual performance reviews and restructuring.
- **CFO**: Uses the data to analyze payroll ROI (Output per Rupiah spent).

## 5. User Journey
1. **Data Aggregation**: Behind the scenes, the TaskProject module fires `TaskCompleted` and `TimeLogUpdated` events. The HR module listens and stores these metrics.
2. **Dashboard Access**: HR Manager opens the "Analytics" tab.
3. **Filtering**: They select the "Engineering Department" and set the date range to "Last Quarter".
4. **Analysis**: The dashboard displays:
   - Average Tasks Completed per employee.
   - Time-tracking efficiency (Estimated Time vs Actual Logged Time from the TaskProject module).
   - "Flight Risk" indicators (e.g., employees who work high overtime but show dropping task completion rates).

## 6. Acceptance Criteria
- [ ] Consume `TaskCompleted` and `TimeLogUpdated` events from the TaskProject module.
- [ ] Aggregate task data against attendance records (HR-2).
- [ ] Visualize data via charts (Bar charts, Line graphs).
- [ ] Calculate a unified Productivity Score algorithm.

## 7. Edge Cases and Constraints
- **Unquantifiable Work**: Not all roles use the TaskProject module (e.g., Office Boy, Security). The system must allow HR to exclude specific job titles or departments from the analytics dashboard to prevent skewed "zero productivity" data.
- **Data Freshness**: Analytics queries can be extremely heavy. Data should be aggregated nightly via a batch job, not calculated on-the-fly.

## 8. Dependencies on Other Modules
- Strictly dependent on the **TaskProject** module. If TaskProject is not used, this feature is disabled.
- Dependent on **HR-2 (Attendance)** for the baseline denominator (Hours Worked).

## 9. Out of Scope
- AI-based prescriptive recommendations for firing/promoting (Keep the AI out of direct personnel actions due to ethical/legal risks). The system only provides deterministic data visualization.
