# Technical Specifications: Employee Data Management (HR-1)

## 1. System Architecture
- **Frontend**: Next.js Client Components with TanStack Query for caching directory data.
- **Backend**: Next.js API Routes.
- **Database**: PostgreSQL (Neon). Uses Adjacency List (`manager_id`) for Org Chart.

## 2. Database Schema

### Table: `departments`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `name` | VARCHAR(100) | NOT NULL | e.g., "Engineering" |
| `parent_id` | UUID | FK | Reference to `departments.id` |
| `tenant_id` | UUID | NOT NULL, FK| |

### Table: `employees`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `user_id` | UUID | UNIQUE, FK | Links to Auth User |
| `employee_no` | VARCHAR(50) | UNIQUE | e.g., "EMP-001" |
| `first_name` | VARCHAR(100) | NOT NULL | |
| `last_name` | VARCHAR(100) | NOT NULL | |
| `email` | VARCHAR(255) | UNIQUE, NOT NULL | |
| `department_id` | UUID | FK | |
| `manager_id` | UUID | FK | Reference to `employees.id` |
| `job_title` | VARCHAR(100) | NOT NULL | |
| `base_salary` | DECIMAL | | (Requires strict RLS) |
| `status` | VARCHAR(20) | NOT NULL | ACTIVE, ON_LEAVE, TERMINATED |
| `hire_date` | DATE | NOT NULL | |
| `termination_date`| DATE | | |
| `tenant_id` | UUID | NOT NULL, FK| |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, date, decimal, timestamp, AnyPgColumn } from "drizzle-orm/pg-core";

export const departments = pgTable("departments", {
  id: uuid("id").primaryKey().defaultRandom(),
  name: varchar("name", { length: 100 }).notNull(),
  parentId: uuid("parent_id").references((): AnyPgColumn => departments.id),
  tenantId: uuid("tenant_id").notNull(),
});

export const employees = pgTable("employees", {
  id: uuid("id").primaryKey().defaultRandom(),
  userId: uuid("user_id").unique(),
  employeeNo: varchar("employee_no", { length: 50 }).notNull().unique(),
  firstName: varchar("first_name", { length: 100 }).notNull(),
  lastName: varchar("last_name", { length: 100 }).notNull(),
  email: varchar("email", { length: 255 }).notNull().unique(),
  departmentId: uuid("department_id").references(() => departments.id),
  managerId: uuid("manager_id").references((): AnyPgColumn => employees.id),
  jobTitle: varchar("job_title", { length: 100 }).notNull(),
  baseSalary: decimal("base_salary"),
  status: varchar("status", { length: 20 }).notNull().default("ACTIVE"),
  hireDate: date("hire_date").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});
```

## 4. API Endpoints

### GET `/api/v1/hr/employees`
- **Description**: Fetch employee directory.
- **Query Params**: `department_id`, `status`, `search`.
- **Security**: Exclude `base_salary` from the select statement unless the user has `hr.payroll.read`.

### POST `/api/v1/hr/employees`
- **Description**: Create new employee.
- **Action**: Inserts into `employees`. Dispatches `EmployeeHired` event. Optionally creates a `users` record if auth is handled internally.

### PUT `/api/v1/hr/employees/{id}/terminate`
- **Description**: Terminate employee.
- **Action**: Sets `status` = TERMINATED. Dispatches `EmployeeTerminated` event (which triggers auth revocation).

### GET `/api/v1/hr/org-chart`
- **Description**: Get flat list of employees to build the org chart.

## 5. Domain Events
- **Raised**: 
  - `EmployeeHired` -> TaskProject module listens to create onboarding tasks.
  - `EmployeeTerminated` -> TaskProject module listens to reassign open tasks.
- **Consumed**: `CandidateHired` (from Recruitment) -> Auto-populates POST endpoint.

## 6. Permissions (RBAC)
- `hr.employee.read`: View basic directory.
- `hr.employee.manage`: Create/Edit profiles.
- `hr.payroll.read`: View sensitive fields (salary, bank).

## 7. Performance Considerations
- Org chart generation should be cached or built client-side from a flat list to prevent heavy recursive SQL queries on every page load.

## 8. Security Considerations
- Use Drizzle ORM's specific column selection to ensure `baseSalary` is never accidentally sent to the frontend directory payload.

## 9. Error Handling
- Prevent circular references in `manager_id`.

## 10. Seed Data
- Seed a "CEO" employee and a basic department structure (HR, IT, Finance) to allow testing the org chart immediately.
