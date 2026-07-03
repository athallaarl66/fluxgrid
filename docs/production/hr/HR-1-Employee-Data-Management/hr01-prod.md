# Production Requirements: Employee Data Management (HR-1)

## 1. Feature Overview
- **Feature Name**: Employee Data Management (HR Core)
- **Module**: HR & Payroll
- **User Story**: As an HR Manager, I want to manage employee master data so that employee information is centralized and up-to-date.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Centralizes the source of truth for all employee records, organizational structures, and job hierarchies. This prevents scattered spreadsheets and forms the basis for automated payroll calculations, access control, and recruitment matching.
- **ROI Estimation**: Reduces administrative HR time by 40%. Ensures regulatory compliance with employment records retention.

## 3. Success Metrics
- 100% of active employees have complete master data records (Bank accounts for payroll, Job titles for hierarchy).
- Organizational structure dynamically updates when reporting lines change.

## 4. User Persona
- **HR Manager / Admin**: Full access to create, edit, and terminate employees. Manages salary grades.
- **Employee**: Read-only access to their own profile (self-service).

## 5. User Journey
1. **Onboarding**: A candidate is hired (from HR-6). HR Manager navigates to the Employee Dashboard and clicks "Convert to Employee".
2. **Data Entry**: HR inputs personal details, assigns a Department (e.g., IT), a Job Title (e.g., Senior Engineer), a Manager, and Salary Grade.
3. **Account Creation**: The system automatically provisions a user account with appropriate RBAC permissions based on the Job Title.
4. **Maintenance**: When an employee is promoted, HR updates their Job Title and Salary Grade. The system keeps an immutable history of these changes.
5. **Offboarding**: When an employee resigns, HR changes their status to "Terminated" and sets an effective date. Access is automatically revoked on that date.

## 6. Acceptance Criteria
- [ ] CRUD operations for Employee Profiles (Personal, Contact, Bank Details).
- [ ] Management of Organizational Units (Departments/Divisions).
- [ ] Management of Job Positions and Salary Grades.
- [ ] Track historical changes to employment (Promotions, Transfers) via Audit Trail.
- [ ] Soft deletion / Termination workflow (Archive data, revoke access).

## 7. Edge Cases and Constraints
- **Data Privacy**: Personal Identifiable Information (PII) like NIK (National ID), Bank Account numbers, and Base Salary must be heavily restricted and only visible to authorized HR/Payroll personnel.
- **Circular Reporting Lines**: Prevent Employee A from reporting to Employee B, while Employee B reports to Employee A.

## 8. Dependencies on Other Modules
- Feeds into **HR-2 (Attendance)** and **HR-3 (Payroll)**.
- Domain Event `EmployeeHired` triggers tasks in the **TaskProject** module (e.g., "Setup Laptop", "Create Email").

## 9. Out of Scope
- Complex Performance Appraisal systems (360-degree feedback).
