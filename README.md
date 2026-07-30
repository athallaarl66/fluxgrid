# FluxGrid ERP

> Modular Monolith ERP untuk industri berat: Mining, Oil & Gas, Logistics, Manufacturing.
> Industrial Modern design system — parchment-based, high-density, engineered for long shifts.

**Tech Stack:** .NET 8 (C# Minimal API) + Next.js 16 (TypeScript) + shadcn/ui + TanStack Query

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional)

### Without Docker

**Terminal 1 — Backend:**
```bash
cd backend/FluxGrid.Api
dotnet restore
dotnet run
# → http://localhost:5000 | Swagger: http://localhost:5000/swagger
```

**Terminal 2 — Frontend:**
```bash
cd frontend
npm install
npm run dev
# → http://localhost:3000
```

Or run both concurrently from root:
```bash
npm install   # installs concurrently
npm run dev
```

### With Docker
```bash
docker-compose up --build
# → Frontend: http://localhost:3000
# → Backend:  http://localhost:5000
```

---

## Login Credentials (MVP)

| Username | Password | Role  |
| -------- | -------- | ----- |
| `admin`  | `admin123` | **Super Admin** — bypasses all permission checks, has every permission |

> **Catatan:** Super Admin dapat membuat akun baru dan mengelola role melalui UI di `/admin/users` dan `/admin/roles`.

---

## Architecture

```
FLEXMNG/
├── frontend/                    # Next.js 16 (App Router)
│   ├── app/
│   │   ├── (auth)/login/        # Login page
│   │   ├── api/auth/            # Auth API routes (login, me)
│   │   ├── dashboard/           # Dashboard page
│   │   ├── settings/            # Settings (Profile, Security, Theme)
│   │   ├── support/             # Support (FAQ, contact)
│   │   ├── help/                # Help & Documentation
│   │   ├── projects/            # Projects placeholder
│   │   ├── admin/users/         # User management (Super Admin)
│   │   ├── admin/roles/         # Role management (Super Admin)
│   │   ├── wms/transfers/       # Transfer log
│   │   └── globals.css          # Industrial Modern design tokens
│   ├── components/
│   │   ├── ui/                  # shadcn/ui primitives
│   │   ├── Sidebar.tsx          # Fixed sidebar (280px)
│   │   ├── Header.tsx           # Top bar with nav + search
│   │   ├── Footer.tsx           # Copyright footer
│   │   └── ModuleCard.tsx       # KPI card with metric badge
│   ├── hooks/useDashboard.ts    # TanStack Query hook
│   ├── lib/
│   │   ├── api-client.ts        # Fetch wrapper + JWT cookie
│   │   ├── auth-context.tsx     # Auth provider
│   │   └── providers.tsx        # QueryClient provider
│   ├── public/icons/            # Module SVG icons
│   └── proxy.ts                 # Route protection (Next.js 16)
│
├── backend/FluxGrid.Api/        # .NET 8 Minimal API
│   ├── Auth/                    # JWT auth + RBAC (login, RequirePermission)
│   ├── Modules/
│   │   ├── Dashboard/           # Dashboard API (GET /api/dashboard)
│   │   ├── WMS/                 # (scaffold)
│   │   ├── Finance/             # Chart of Accounts CRUD + audit
│   │   └── HR/                  # (scaffold)
│   ├── Shared/
│   │   ├── Domain/              # Shared entities (User, Role, AuditLog)
│   │   ├── Domain/Events/       # IDomainEvent, AccountCreated, AccountUpdated
│   │   ├── Infrastructure/
│   │   │   ├── Audit/           # AuditService
│   │   │   ├── Caching/         # ICacheService, MemoryCacheService
│   │   │   ├── Data/            # AppDbContext + EF config
│   │   │   ├── Events/          # DomainEventDispatcher
│   │   │   └── Seed/            # DataSeeder, ChartOfAccountSeeder
│   │   └── RBAC/                # Permission constants
│   └── Program.cs               # DI, auth, CORS, endpoint registration
│
├── docs/
│   ├── DESIGN.md                # Industrial Modern design system
│   └── features/                # PRD, TDD, API Contract
│
├── docker-compose.yml           # Frontend (3000) + Backend (5000)
└── package.json                 # Root orchestration scripts
```

---

## Routes

| Path              | Auth Required | Description                |
| ----------------- | ------------- | -------------------------- |
| `/login`          | No            | Login form                 |
| `/dashboard`      | Yes           | Dashboard with KPI grid    |
| `/settings`       | Yes           | Profile, Security, Theme   |
| `/support`        | Yes           | FAQ & support              |
| `/help`           | Yes           | Help & documentation       |
| `/projects`       | Yes           | Projects (placeholder)     |
| `/wms`            | Yes           | WMS module                 |
| `/wms/transfers`  | Yes           | Warehouse transfer log     |
| `/finance`        | Yes           | Finance module landing     |
| `/finance/chart-of-accounts` | Yes | Chart of Accounts tree view |
| `/hr`             | Yes           | HR module                  |
| `/hr/recruitment/kanban` | Yes | Candidate pipeline (Kanban) |
| `/admin/users`    | Super Admin   | User management            |
| `/admin/roles`    | Super Admin   | Role management            |

### API Endpoints

| Method | Path                                    | Auth                     | Description                     |
| ------ | --------------------------------------- | ------------------------ | ------------------------------- |
| POST   | `/api/auth/login`                       | Anonymous                | Login → returns JWT             |
| GET    | `/api/health`                           | Anonymous                | Health check                    |
| GET    | `/api/dashboard`                        | `Dashboard:Read`         | Module metadata + KPIs          |
| GET    | `/api/auth/profile`                     | Authenticated            | Get current user profile        |
| PUT    | `/api/auth/profile`                     | Authenticated            | Update user profile             |
| GET    | `/api/v1/finance/chart-of-accounts`     | `finance.coa.read`       | Get COA tree (flat? query)      |
| POST   | `/api/v1/finance/chart-of-accounts`     | `finance.coa.manage`     | Create account                  |
| PUT    | `/api/v1/finance/chart-of-accounts/{id}`| `finance.coa.manage`     | Update account                  |
| DELETE | `/api/v1/finance/chart-of-accounts/{id}`| `finance.coa.manage`     | Deactivate account (soft-delete)|
| PUT    | `/api/v1/hr/recruitment/candidates/{id}/status` | `HR:RecruitmentManage` | Change candidate status |
| GET    | `/api/v1/hr/recruitment/candidates/{id}/activities` | `HR:RecruitmentManage` | Get activity log |
| POST   | `/api/v1/hr/recruitment/candidates/{id}/activities` | `HR:RecruitmentManage` | Add activity note |
| POST   | `/api/v1/hr/recruitment/candidates/{id}/jobs` | `HR:RecruitmentManage` | Assign to job |
| DELETE | `/api/v1/hr/recruitment/candidates/{id}/jobs/{jobId}` | `HR:RecruitmentManage` | Unassign from job |
| GET    | `/api/v1/hr/recruitment/candidates/{id}/jobs` | `HR:RecruitmentManage` | List assigned jobs |
| POST   | `/api/v1/hr/recruitment/candidates/bulk-assign` | `HR:RecruitmentManage` | Bulk assign to job |
| PUT    | `/api/v1/hr/recruitment/candidates/{id}` | `HR:RecruitmentManage` | Update candidate data |
| GET    | `/api/v1/wms/pick-lists/by-order/{orderId}` | `WMS:Read` | Get pick list by order |
| GET    | `/api/v1/wms/stock-ledger/transfers`    | `WMS:Read`               | Warehouse transfer log          |
| GET    | `/api/admin/users`                      | Super Admin              | List users                      |
| POST   | `/api/admin/users`                      | Super Admin              | Create user                     |
| PUT    | `/api/admin/users/{id}`                 | Super Admin              | Update user                     |
| DELETE | `/api/admin/users/{id}`                 | Super Admin              | Deactivate user                 |
| GET    | `/api/admin/roles`                      | Super Admin              | List roles                      |
| POST   | `/api/admin/roles`                      | Super Admin              | Create role                     |
| PUT    | `/api/admin/roles/{id}`                 | Super Admin              | Update role                     |
| DELETE | `/api/admin/roles/{id}`                 | Super Admin              | Delete role                     |
| GET    | `/api/admin/permissions`                | Super Admin              | List all permissions            |
| GET    | `/api/notifications/unread`             | Authenticated            | Unread notifications            |
| PUT    | `/api/notifications/{id}/read`          | Authenticated            | Mark notification read          |
| PUT    | `/api/notifications/read-all`           | Authenticated            | Mark all read                   |
| POST   | `/api/support/contact`                  | Authenticated            | Submit support message          |

---

## RBAC — Role-Based Access Control

### Super Admin
- Role **Admin** bersifat **super admin** — semua permission bypass.
- Cek di `Program.cs`: policy authorization menerima `permission claim` **ATAU** `role = "Admin"`.
- Admin dapat mengakses endpoint apapun tanpa perlu permission spesifik.

### Permission Model
| Namespace          | Permissions                                      |
| ------------------ | ------------------------------------------------ |
| `Dashboard`        | `Dashboard:Read`                                 |
| `WMS`              | `WMS:Read`, `WMS:Write`, `WMS:Admin`            |
| `Finance`          | `Finance:Read`, `Finance:Write`, `Finance:Admin` |
| `Finance COA`      | `finance.coa.read`, `finance.coa.manage`        |
| `HR`               | `HR:Read`, `HR:Write`, `HR:PayrollProcess`       |
| `HR Recruitment`   | `HR:RecruitmentManage`, `HR:CVRead`, `HR:CVWrite`, `HR:CandidateManage` |
| `Admin`            | `Admin:Manage` (Super Admin only)                |
| `Notification`     | `Notification:Read`, `Notification:Manage`       |

### User & Role Management
Super Admin dapat membuat akun, mengelola role, dan assign permission secara dinamis melalui UI di `/admin/users` dan `/admin/roles`.

---

## Design System

Lihat [`docs/DESIGN.md`](./docs/DESIGN.md) untuk dokumentasi lengkap:
- **Palette:** Parchment base (#fdf8f5), Sage accents (#c5d89d), Olive typography
- **Typography:** Inter — headlines 18-32px, body 13-16px, labels 11-12px
- **Layout:** 4px grid, fixed sidebar (260px) + fluid content
- **Shapes:** 4px radius default, 8px for cards, pill for badges
- **Elevation:** 1px borders, no shadows, tonal layering

---

## Modules

| Module             | Description                                           |
| ------------------ | ----------------------------------------------------- |
| **WMS**            | Warehouse Management — inbound/outbound, stock ledger |
| **Finance**        | General Ledger — double-entry, P&L, balance sheet     |
| **HR**             | Employee data, payroll, recruitment AI                |

> **Catatan:** Modul **Task & Project** (kanban, time tracking, task dependencies) telah di-extract menjadi standalone app terpisah. Lihat [`TASK-APP.md`](./TASK-APP.md) untuk dokumentasi lengkap.

Dokumentasi detail: [`docs/features/`](./docs/features/)

- [PRD — Product Requirements](./docs/features/PRD.md)
- [TDD — Technical Design](./docs/features/TECHNICAL.md)
- [API Contract](./docs/features/API-CONTRACT.md)

---

## Scripts

| Command                  | Description                            |
| ------------------------ | -------------------------------------- |
| `npm run dev`            | Start backend + frontend concurrently  |
| `npm run dev:backend`    | Start .NET backend only                |
| `npm run dev:frontend`   | Start Next.js frontend only            |
| `npm run build`          | Build frontend for production          |
| `npm run docker:up`      | Build & start with Docker Compose      |
| `npm run docker:down`    | Stop Docker services                   |

---

_FluxGrid ERP — Mini ERP for industries where data integrity isn't optional._
