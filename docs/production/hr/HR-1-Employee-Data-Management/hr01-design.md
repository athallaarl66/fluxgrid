# Design Specifications: Employee Data Management (HR-1)

## 1. Screen Overview
**Page 1:** Employee Directory (Table/Grid view)
**Page 2:** Employee Profile (Detailed view with Tabs)
**Page 3:** Organization Chart (Visual tree view)

## 2. Wireframe Description
```text
=== Employee Directory ===
[Search by Name, Role, Department] [Filter: Active/Terminated] [Btn: + Add Employee]

[Grid View]
+-----------------------+ +-----------------------+
| [Avatar] John Doe     | | [Avatar] Jane Smith   |
| Senior Developer      | | HR Manager            |
| IT Department         | | HR Department         |
| [View Profile]        | | [View Profile]        |
+-----------------------+ +-----------------------+

=== Employee Profile (John Doe) ===
[Header: John Doe - Senior Developer (Active)]

[Tabs: Personal Info | Employment | Payroll Details | Documents]

(Tab: Personal Info)
Email: john@company.com
Phone: +62 812...
Address: Jakarta...

(Tab: Employment)
Department: IT
Direct Manager: [Link: Tech Lead]
Hire Date: 01 Jan 2024

=== Organization Chart ===
      [CEO]
        |
  +-----+-----+
[HR]        [CTO]
              |
        [Senior Dev]
```

## 3. Component Hierarchy
- `EmployeeDirectory`
  - `Toolbar` (Search, Filters)
  - `EmployeeGrid` / `EmployeeTable`
    - `EmployeeCard` (Avatar, Name, Role)
- `EmployeeProfile`
  - `ProfileHeader`
  - `Tabs`
    - `PersonalInfoForm` (Zod validated)
    - `EmploymentHistory` (Timeline component)
    - `PayrollSettings` (Secured component, requires elevated auth check)
- `OrgChart`
  - `TreeNode` (Recursive component using a library like React Flow or customized CSS)

## 4. UI Components (shadcn/ui)
- `Tabs` for organizing dense profile data.
- `Avatar` for employee photos.
- `Card` for directory grid.
- `Form` elements (Input, Select, DatePicker).
- `Badge` for employment status.

## 5. Visual Guidelines
- **Avatar Fallbacks**: If no photo is uploaded, use the employee's initials with a distinct background color generated from a hash of their name.
- **Data Protection**: Sensitive fields (like Salary) should be masked by default (e.g., `Rp ***.***.***`) with an "eye" icon to reveal them, preventing shoulder-surfing.

## 6. Responsive Design
- The Organization Chart is notoriously hard on mobile. Implement pinch-to-zoom and panning, or fallback to a standard indented list on small screens.
- Employee profile tabs should become a scrollable horizontal list or a vertical accordion on mobile.

## 7. States & Interactions
- **Timeline**: Employment history (promotions, transfers) should be displayed as a vertical timeline to easily trace career progression.

## 8. Accessibility
- Use descriptive `alt` tags for Avatars.
- Ensure the org chart can be navigated using the `Tab` and `Arrow` keys.
