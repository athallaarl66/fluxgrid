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
| `admin`  | `admin123` | Admin (all permissions) |

---

## Architecture

```
FLEXMNG/
├── frontend/                    # Next.js 16 (App Router)
│   ├── app/
│   │   ├── (auth)/login/        # Login page
│   │   ├── api/auth/            # Auth API routes (login, me)
│   │   ├── dashboard/           # Dashboard page
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
│   │   ├── Finance/             # (scaffold)
│   │   ├── HR/                  # (scaffold)
│   │   └── TaskProject/         # (scaffold)
│   ├── Shared/RBAC/             # Permission constants
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
| `/wms`            | Yes           | WMS module (scaffold)      |
| `/finance`        | Yes           | Finance module (scaffold)   |
| `/hr`             | Yes           | HR module (scaffold)       |
| `/projects`       | Yes           | Projects module (scaffold) |

### API Endpoints

| Method | Path               | Auth                | Description                     |
| ------ | ------------------ | ------------------- | ------------------------------- |
| POST   | `/api/auth/login`  | Anonymous           | Login → returns JWT             |
| GET    | `/api/dashboard`   | `Dashboard:Read`    | Module metadata + KPIs          |
| GET    | `/api/health`      | Anonymous           | Health check                    |

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
| **HR**             | Employee data, attendance, payroll, recruitment AI    |
| **Task & Project** | Kanban board, time tracking, dependency graph         |

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
