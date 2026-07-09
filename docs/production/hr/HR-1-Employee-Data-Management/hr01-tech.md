# Technical Specifications: Employee Data Management (HR-1)

## 1. Change Overview

**Branch:** `feat/DB-init-testing`
**Commits**:

| # | Hash | Message |
|---|------|---------|
| 1 | `e3d4b82` | feat(hr-1): implement backend Domain, Infrastructure, and Application layers |
| 2 | `9f8dfae` | feat(hr-1): frontend Section 7-10 — layout, directory, profile, form modal |

**Total**: ~45 files changed across backend + frontend

---

## 2. Complete File Inventory

### 2.1 Backend (.NET 8) — 15 files

```
Modules/HR/
├── API/
│   ├── HrDtos.cs                   ← CreateEmployeeRequest, EmployeeResponse,
│   │                                  EmployeeDetailResponse, DepartmentResponse, OrgChartNode
│   └── HrEndpoints.cs              ← /api/v1/hr/employees, /departments, /org-chart
├── Application/
│   ├── DepartmentService.cs        ← CRUD + max depth + circular ref + employee guard
│   ├── EmployeeService.cs          ← CRUD + search/filter/paginate + salary exclusion
│   └── OrgChartService.cs          ← GET flat active employees
├── Domain/
│   ├── Entities/
│   │   ├── Department.cs           ← Id, Name, ParentId, TenantId, IsActive
│   │   └── Employee.cs             ← Id, EmployeeNo, Name, Email, Status, Salary, etc.
│   └── Events/
│       ├── EmployeeHired.cs        ← Domain event on create
│       ├── EmployeeTerminated.cs   ← Domain event on terminate
│       └── EmployeeUpdated.cs      ← Domain event on update
```

Backend infrastructure changes:
```
M  Program.cs                       ← Register EmployeeService, DepartmentService, OrgChartService
M  Shared/Domain/Entities/User.cs   ← (existing) referenced by Employee.UserId
M  Shared/Infrastructure/Data/AppDbContext.cs  ← DbSet<Employee>, DbSet<Department>
M  Shared/Infrastructure/Audit/AuditService.cs ← ReferenceHandler.IgnoreCycles fix
```

### 2.2 Frontend (Next.js 16) — 22 files

```
app/hr/
├── layout.tsx                      ← AuthProvider + Sidebar + Header + Footer
├── page.tsx                        ← Redirect to /hr/employees
├── employees/
│   ├── page.tsx                    ← Directory page (search, filter, pagination)
│   └── [id]/page.tsx              ← Profile page (tabbed: personal, employment, payroll)
├── org-chart/
│   └── page.tsx                    ← Org chart with zoom/pan (desktop) / indented list (mobile)
└── departments/
    └── page.tsx                    ← Department list with CRUD

components/hr/
├── EmployeeTable.tsx               ← Dense data table (36px row, #9CAB84 border)
├── EmployeeGrid.tsx                ← Card grid view
├── EmployeeCard.tsx                ← Avatar initials with hash color
├── EmployeeToolbar.tsx             ← Search, filter dropdowns, view toggle, Add button
├── EmployeeFormModal.tsx           ← Create/edit employee modal form
├── ProfileHeader.tsx               ← Avatar, name, employee no, status badge
├── PersonalInfoTab.tsx             ← Editable personal details (Zod validation)
├── EmploymentTab.tsx               ← Department, manager, hire date + timeline
├── EmploymentTimeline.tsx          ← Vertical timeline (hire, promotions, transfers)
├── PayrollTab.tsx                  ← Masked salary with eye toggle (permission-gated)
├── MaskedField.tsx                 ← Reusable masked sensitive data component
├── OrgChartNode.tsx                ← Employee node with avatar, name, title
├── OrgChartTree.tsx                ← Recursive tree with drag-to-pan, ctrl+wheel zoom
├── OrgChartMobileList.tsx          ← Indented hierarchical list (mobile <768px)
├── DepartmentTable.tsx             ← Hierarchy-indented table with tree building
└── DepartmentFormModal.tsx         ← Create/edit department with parent selector

hooks/
├── useEmployees.ts                 ← TanStack Query: list, detail, create, update, terminate
├── useDepartments.ts               ← TanStack Query: list, create, update, delete
└── useOrgChart.ts                  ← TanStack Query: fetch flat list + buildTree()

lib/
└── hr-types.ts                     ← Employee, EmployeeDetail, Department, OrgChartNode,
                                       PaginatedResponse, CreateEmployeeRequest, etc.
```

---

## 3. Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Browser                                                                  │
│  ┌────────────────┐  ┌─────────────────────────────────────────────────┐ │
│  │ /hr/employees  │  │ /hr/org-chart    │  /hr/departments              │ │
│  │ (dir page)     │  │ (zoom/pan tree)  │  (hierarchy table)           │ │
│  │ Table / Grid   │  │ Mobile: list     │  CRUD modal                   │ │
│  ├────────────────┤  │ Desktop: tree    │                               │ │
│  │ /hr/employees/ │  └──────────────────┴───────────────────────────────┘ │
│  │  [id] (profile)│                                                     │
│  └───────┬────────┘                                                     │
│          │ api-client.ts → http://localhost:5020                         │
└──────────┼──────────────────────────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────────────────────────┐
│  Program.cs (.NET 8 Minimal API)                                        │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  /api/v1/hr/employees     → EmployeeService                     │   │
│  │  /api/v1/hr/departments   → DepartmentService                   │   │
│  │  /api/v1/hr/org-chart     → OrgChartService                     │   │
│  │                                                                  │   │
│  │  Middleware: JWT Auth → Permission Claims → Tenant isolation     │   │
│  │  AuditService.LogAsync → DomainEventDispatcher → Event handlers  │   │
│  └──────────────────────────────┬───────────────────────────────────┘   │
│                                 │ EF Core (Npgsql)                      │
│                                 ▼                                        │
│  PostgreSQL 18 (fluxgrid)                                                │
│  Tables: Employees, Departments, Users, Roles, AuditLogs                │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Database Schema (EF Core)

### Table: `Departments`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `Name` | VARCHAR(100) | NOT NULL |
| `ParentId` | UUID | FK → Departments.Id (self-ref) |
| `TenantId` | UUID | NOT NULL |
| `IsActive` | BOOLEAN | DEFAULT true |

**Indexes**: `IX_Departments_TenantId`, `IX_Departments_ParentId`

### Table: `Employees`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `UserId` | UUID | FK → Users.Id (nullable) |
| `EmployeeNo` | VARCHAR(50) | NOT NULL, UNIQUE |
| `FirstName` | VARCHAR(100) | NOT NULL |
| `LastName` | VARCHAR(100) | NOT NULL |
| `Email` | VARCHAR(255) | NOT NULL, UNIQUE |
| `Phone` | VARCHAR(50) | |
| `Address` | TEXT | |
| `DateOfBirth` | DATE | |
| `Nik` | VARCHAR(50) | National ID |
| `EmergencyContact` | VARCHAR(200) | |
| `DepartmentId` | UUID | FK → Departments.Id |
| `ManagerId` | UUID | FK → Employees.Id (self-ref) |
| `JobTitle` | VARCHAR(100) | |
| `BaseSalary` | DECIMAL | Permission-gated (HR:PayrollRead) |
| `BankName` | VARCHAR(100) | |
| `BankAccount` | VARCHAR(50) | |
| `TaxId` | VARCHAR(50) | NPWP |
| `Status` | VARCHAR(20) | NOT NULL — ACTIVE / ON_LEAVE / TERMINATED |
| `HireDate` | DATE | NOT NULL |
| `TerminationDate` | DATE | |
| `TenantId` | UUID | NOT NULL |
| `CreatedAt` | TIMESTAMP | |
| `UpdatedAt` | TIMESTAMP | |

**Indexes**: `IX_Employees_TenantId`, `IX_Employees_EmployeeNo` (unique), `IX_Employees_Email` (unique), `IX_Employees_DepartmentId`, `IX_Employees_ManagerId`

---

## 5. API Contract

| Method | Endpoint | Auth | Body | Response | Notes |
|--------|----------|------|------|----------|-------|
| GET | `/api/v1/hr/employees` | HR:EmployeeRead | — | `{ items[], total, page, pageSize }` | Query: `search`, `status`, `departmentId`, `page`, `pageSize`. Salary excluded unless HR:PayrollRead |
| GET | `/api/v1/hr/employees/{id}` | HR:EmployeeRead | — | EmployeeDetail | Salary null unless HR:PayrollRead |
| POST | `/api/v1/hr/employees` | HR:EmployeeManage | `CreateEmployeeRequest` | EmployeeDetail | Auto-generates EMP-NNN, provisions User account |
| PUT | `/api/v1/hr/employees/{id}` | HR:EmployeeManage | `UpdateEmployeeRequest` | EmployeeDetail | Validates circular manager reference |
| POST | `/api/v1/hr/employees/{id}/terminate` | HR:EmployeeManage | — | EmployeeDetail | Sets TERMINATED, deactivates User |
| GET | `/api/v1/hr/departments` | HR:EmployeeRead | — | Department[] | |
| POST | `/api/v1/hr/departments` | HR:EmployeeManage | `CreateDepartmentRequest` | Department | Validates max depth (5 levels) |
| PUT | `/api/v1/hr/departments/{id}` | HR:EmployeeManage | `UpdateDepartmentRequest` | Department | Validates circular ref, max depth |
| DELETE | `/api/v1/hr/departments/{id}` | HR:EmployeeManage | — | 204 | Blocked if employees or children exist |
| GET | `/api/v1/hr/org-chart` | HR:EmployeeRead | — | OrgChartNode[] | Flat list of ACTIVE employees, ordered by EmployeeNo |

---

## 6. Domain Events

| Event | Raised By | Consumer | Payload |
|-------|-----------|----------|---------|
| `EmployeeHired` | EmployeeService.CreateAsync | Logged only (MVP) | employeeId, employeeNo, name, departmentId, managerId, jobTitle |
| `EmployeeUpdated` | EmployeeService.UpdateAsync | Logged only (MVP) | employeeId, employeeNo, before/after jobTitle, departmentId, managerId |
| `EmployeeTerminated` | EmployeeService.TerminateAsync | deactivates User account | employeeId, employeeNo, userId, terminationDate |

---

## 7. Key Business Logic

| Rule | Implementation |
|------|----------------|
| **Employee No** | Auto-generated `EMP-NNN` format; increments per tenant |
| **Circular Manager** | Traverses ancestor chain — rejects if candidate is descendant |
| **Salary Gating** | Backend: DTO projection omits `base_salary` unless `HR:PayrollRead` claim exists |
| **Department Depth** | Max 5 levels; check depth of candidate parent before assignment |
| **Delete Guard** | Department deletion blocked if employees or child departments exist |
| **Termination** | Sets `status=TERMINATED`, `terminationDate=UtcNow`, deactivates linked User |
| **User Provisioning** | On employee create: creates User with default "Staff" role + random password |
| **Audit Trail** | All mutating endpoints call `AuditService.LogAsync` |
| **Tenant Isolation** | All queries filter by `TenantId` |

---

## 8. Frontend State Management

```
TanStack Query Key Hierarchy:
├── ["employees", {search, departmentId, status, page, pageSize}]  → useEmployeeList
├── ["employee", id]                                               → useEmployee
├── ["departments"]                                                → useDepartmentList
├── ["org-chart"]                                                  → useOrgChart (builds tree client-side)
```

**Org Chart Tree Construction** (client-side):
```typescript
function buildTree(employees: OrgChartNode[]): OrgChartNode[] {
  const map = new Map(employees.map(e => [e.id, { ...e, children: [] }]));
  const roots: OrgChartNode[] = [];
  for (const emp of employees) {
    const node = map.get(emp.id)!;
    if (emp.managerId && map.has(emp.managerId))
      map.get(emp.managerId)!.children.push(node);
    else
      roots.push(node);
  }
  return roots;
}
```

---

## 9. Dependencies

### Backend (.NET 8)

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.0 |
| `BCrypt.Net-Next` | 4.0.3 |

### Frontend (Next.js 16)

| Package | Version |
|---------|---------|
| `next` | 16.2.10 |
| `@tanstack/react-query` | ^5.101.2 |
| `lucide-react` | ^1.23.0 |
| `recharts` | ^2.15.0 |

---

## 10. Local Dev Setup

```bash
# Backend
cd backend/FluxGrid.Api
dotnet run
# → http://localhost:5020

# Frontend (separate terminal)
cd frontend
npm run dev
# → http://localhost:3000

# Test employee list
curl -X GET http://localhost:5020/api/v1/hr/employees?page=1\&page_size=20 \
  -H "Authorization: Bearer <token>"

# Run HR unit tests
cd tests/unit/hr/hr-1-employee-data-management.Test
dotnet test
```

---

## 11. Known Limitations (MVP)

- No self-service portal (employee editing own profile) — deferred
- No bulk import/export (CSV/Excel)
- No drag-and-drop org chart editing — read-only tree
- No advanced reporting (turnover, headcount analytics)
- Event consumers are log-only; no cross-module side effects yet
- User provisioning uses default "Staff" role — no role selection on create
