# Testing Scenarios: Employee Data Management (HR-1)

## 1. Test Strategy Overview
Testing focuses on structural integrity (organizational hierarchy), data privacy boundaries, and ensuring lifecycle changes (like termination) correctly propagate to system access.

## 2. Test Cases

### TC-01: Create Employee (Happy Path)
- **Given** an HR Manager on the New Employee form
- **When** they fill in valid personal details, assign a department, and click save
- **Then** the employee record is created.
- **And** a corresponding system User account is provisioned with default employee roles.

### TC-02: Prevent Circular Reporting Structure
- **Given** Employee A manages Employee B
- **When** HR attempts to edit Employee A's profile to report to Employee B
- **Then** the system detects a circular reference and blocks the update.

### TC-03: Data Privacy (Negative Testing)
- **Given** an Employee (non-HR) logged into the system
- **When** they attempt to view the profile of a colleague (Employee B) via direct API call or UI
- **Then** the API returns a 403 Forbidden for sensitive fields (Salary, Bank Account, NIK).
- **And** they can only see public fields (Name, Department, Email).

### TC-04: Termination Workflow Integration
- **Given** an active employee "John Doe"
- **When** the HR Manager changes their status to "Terminated" with an effective date of today
- **Then** the system marks the employee as inactive.
- **And** their associated User account is deactivated, preventing login.
- **And** the `EmployeeTerminated` domain event is fired.

### TC-05: Organizational Unit Deletion
- **Given** a Department "Marketing" that currently has 5 active employees assigned to it
- **When** an HR Admin attempts to delete the "Marketing" department
- **Then** the system blocks deletion, requiring employees to be reassigned first.

## 3. Performance Testing
- Fetching the Organizational Chart for a company with 1,000+ employees must complete in under 2 seconds. The tree structure should be built efficiently using nested sets or adjacency lists in Postgres.

## 4. Security & Access Testing
- PII fields must be encrypted at rest if required by local regulations, or strictly controlled via Row-Level Security (RLS) policies.
