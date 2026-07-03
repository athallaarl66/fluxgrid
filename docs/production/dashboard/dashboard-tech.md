# Technical Specifications: Dashboard (dashboard)

## 1. Change Overview

**Branch**: `feat/DB-init-testing`
**Commits**: 4 (from `1c52c28` to `d9d97cd`)

| # | Hash | Message |
|---|------|---------|
| 1 | `1c52c28` | feat: implement frontend auth, dashboard layout, and backend dashboard API |
| 2 | `2bce5d1` | feat: implement dashboard UI (sidebar, topbar, KPI cards, charts) and Docker |
| 3 | `f88efe8` | fix: dark mode, sidebar spacing, CSS variables on all components |
| 4 | `d9d97cd` | feat: PostgreSQL + EF Core setup, DB-backed auth with BCrypt |

**Total**: 45 files changed, 1,851 insertions, 438 deletions

---

## 2. Complete File Inventory

### 2.1 Root
```
M  .gitignore                     ← added .env / .next / node_modules
M  package.json                   ← root package with docker scripts
M  README.md                      ← project overview
A  docker-compose.yml             ← backend (5020) + frontend (3000)
```

### 2.2 Backend (.NET 8) — 15 files

```
M  FluxGrid.Api.csproj            ← added Npgsql, EF Core Design, BCrypt.Net-Next
M  Program.cs                     ← DbContext, auto-migrate, seed, CORS, auth
M  appsettings.json               ← added ConnectionStrings:DefaultConnection
A  Dockerfile                     ← multi-stage .NET 8 build

A  Auth/AuthEndpoints.cs          ← POST /api/auth/login (DB lookup + BCrypt)
A  Modules/Dashboard/API/
│  └── DashboardEndpoints.cs      ← GET /api/dashboard (RequireAuthorization)
A  Modules/Dashboard/Application/
│  └── DashboardService.cs        ← hardcoded module data (MVP)

A  Shared/Domain/Entities/
│  ├── User.cs                    ← Id, Username, PasswordHash, Email, IsActive, Roles
│  └── Role.cs                    ← Id, Name, Description, Permissions (text[])
A  Shared/Infrastructure/Data/
│  └── AppDbContext.cs            ← EF Core context, Fluent API config
A  Shared/Infrastructure/Seed/
│  └── DataSeeder.cs              ← 3 roles + admin/admin123 seed

A  Migrations/
│  ├── 20260703062837_InitialCreate.cs
│  ├── 20260703062837_InitialCreate.Designer.cs
│  └── AppDbContextModelSnapshot.cs
```

### 2.3 Frontend (Next.js 15) — 22 files

```
M  next.config.ts                 ← API URL rewrite
A  proxy.ts                       ← route protection middleware (skip /api/, redirect to /login)

A  app/layout.tsx                 ← root layout with Providers + suppressHydrationWarning
M  app/globals.css                ← full DESIGN.md theme: light + dark mode CSS variables
M  app/page.tsx                   ← redirect to /login

A  app/(auth)/login/page.tsx      ← login form: password show/hide, error state, demo hint

A  app/api/auth/
│  ├── login/route.ts             ← proxies POST to backend, returns token
│  └── me/route.ts                ← decodes JWT cookie, returns user info

A  app/dashboard/
│  ├── layout.tsx                 ← AuthProvider + Sidebar + Header + Footer wrapper
│  └── page.tsx                   ← KPI grid, charts, loading/error/permission states

A  components/
│  ├── Sidebar.tsx                ← nav items, active state, New Task button
│  ├── Header.tsx                 ← search, nav tabs, dark mode toggle, actions
│  ├── Footer.tsx                 ← copyright + help link
│  └── ModuleCard.tsx             ← module card with icon, badge, permission overlay

M  components/ui/
│  └── button.tsx                 ← added cursor-pointer

A  hooks/
│  └── useDashboard.ts            ← TanStack Query hook for GET /api/dashboard

A  lib/
│  ├── api-client.ts              ← fetch wrapper (defaults to localhost:5020)
│  ├── auth-context.tsx           ← AuthProvider context
│  └── providers.tsx              ← QueryClientProvider wrapper

A  public/icons/
│  ├── dashboard.svg, finance.svg, hr.svg, projects.svg, wms.svg
```

### 2.4 Docs & Infra — 5 files

```
M  docs/DESIGN.md                 ← Industrial Modern ERP design system (parchment, sage, olive)
M  docs/production/dashboard/
│  └── dashboard-tech.md          ← this file
```

---

## 3. Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│  Browser                                                              │
│  ┌─────────────┐  ┌───────────────────────────────────────────────┐ │
│  │ /login      │  │ /dashboard                                    │ │
│  │ (LoginForm) │  │ ┌────────┐ ┌─────────────────────────────┐   │ │
│  └──────┬──────┘  │ │Sidebar │ │ Header (search, nav,       │   │ │
│         │         │ │(nav,   │ │ dark toggle, actions)       │   │ │
│         │         │ │ New    │ ├─────────────────────────────┤   │ │
│         │         │ │ Task)  │ │ ModuleCard grid (2x2)      │   │ │
│         │         │ │        │ │ ┌──────┐ ┌──────┐          │   │ │
│         │         │ │        │ │ │ WMS  │ │FIN   │          │   │ │
│         ▼         │ │        │ │ ├──────┤ ├──────┤          │   │ │
│  ┌────────────┐   │ │        │ │ │ HR   │ │ PRJ  │          │   │ │
│  │ proxy.ts   │───│ │        │ │ └──────┘ └──────┘          │   │ │
│  │ (protect   │   │ │        │ │ Charts section (2 column)  │   │ │
│  │  routes)   │   │ │        │ └─────────────────────────────┘   │ │
│  └────────────┘   │ │        │ Footer                            │ │
│                   │ └────────┴───────────────────────────────────┘ │
└───────────────────┴────────────────────────────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │  POST /api/auth/*   │
         │  GET  /api/dashboard│
         │  (via api-client.ts)│
         └──────────┬──────────┘
                    │
┌───────────────────▼────────────────────────────────────────────────┐
│  Program.cs (.NET 8 Minimal API)                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  AuthEndpoints ── AppDbContext ── DataSeeder                │  │
│  │  DashboardEndpoints ── DashboardService (hardcoded)        │  │
│  │  JWT Bearer Auth ── CORS (localhost:3000)                  │  │
│  └─────────────────────────┬───────────────────────────────────┘  │
│                            │ EF Core (Npgsql)                     │
│                            ▼                                       │
│  PostgreSQL 18 (localhost:5432)                                    │
│  Database: fluxgrid                                                 │
│  Tables: Users, Roles, UserRoles, __EFMigrationsHistory            │
└────────────────────────────────────────────────────────────────────┘
```

## 4. Database Schema

```
Users ──── UserRoles ──── Roles
┌──────────┐   ┌─────────────┐   ┌──────────────┐
│ Id (PK)  │──▶│ UserId      │◀──│ Id (PK)      │
│ Username │   │ RoleId      │   │ Name         │
│ (unique) │   └─────────────┘   │ Description  │
│ Password │                     │ Permissions  │ ← text[]
│ Hash     │                     └──────────────┘
│ Email    │
│ IsActive │
│ CreatedAt│
└──────────┘
```

### Seed Data

| Role | Permissions |
|------|-------------|
| **Admin** | All 10 permissions |
| **Manager** | Dashboard:Read, WMS:Read/Write, Finance:Read/Write, HR:Read/Write, Task:Read/Write |
| **Staff** | Dashboard:Read, WMS:Read, Finance:Read, HR:Read, Task:Read/Write |

**Default user**: `admin` / `admin123` (BCrypt hashed)

## 5. Auth Flow

```
Login Page            proxy.ts               Backend                 DB
    │                    │                       │                    │
    │  POST /api/auth/  │                       │                    │
    │  login            │  → forward as-is →    │                    │
    │  {user, pass}     │                       │                    │
    │                    │                       │  SELECT FROM Users │
    │                    │                       │  WHERE Username=.. │
    │                    │                       │──────────────────▶│
    │                    │                       │◀──────────────────│
    │                    │                       │  user + roles     │
    │                    │                       │                    │
    │                    │                       │  BCrypt.Verify()   │
    │                    │                       │  Generate JWT      │
    │                    │                       │  (permissions as   │
    │                    │                       │   claims[])        │
    │                    │                       │                    │
    │  {token, expires} │◀──────────────────────│                    │
    │                    │                       │                    │
    │  Store token       │                       │                    │
    │  Redirect /dashboard                       │                    │
```

## 6. API Contract

| Method | Endpoint | Auth | Body | Response |
|--------|----------|------|------|----------|
| POST | `/api/auth/login` | None | `{ username, password }` | `{ token, expiresAt }` |
| GET | `/api/auth/me` | Bearer | — | `{ id, email, roles }` |
| GET | `/api/dashboard` | `Dashboard:Read` | — | ModuleInfo[] |
| GET | `/api/health` | None | — | `{ status: "healthy" }` |

## 7. Design System (DESIGN.md)

### Light Mode
```
Background  #fdf8f5 (parchment)
Sidebar     #efe9cf (cream)
Primary     #625f4b (olive)
Accent      #c5d89d (sage)
Ring        #9cab84 (muted sage)
Border      #e5debf (parchment border)
```

### Dark Mode
```
Background  #1a1a18 (near-black olive)
Sidebar     #1e1e1c
Card        #242422
Accent      #4a5a2a (dark sage)
Border      #353532
```

### Typography
- Font: **Inter** (sans-serif + mono)
- Labels: 11px / 600 weight
- Body: 13-14px / 400 weight
- Headings: 18-32px / 600 weight

### Shapes
- Buttons/Inputs: 4px radius
- Cards: 8px radius
- Badges: pill (fully rounded)

### Elevation
- No shadows — 1px `#E5DEBF` borders
- Active state: `inset 0 1px 2px rgba(0,0,0,0.15)`
- Hover: brightness 95%

## 8. Dependencies

### Backend (.NET 8)

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.0 |
| `BCrypt.Net-Next` | 4.0.3 |
| `Swashbuckle.AspNetCore` | 6.6.2 |

### Frontend (Next.js 15)

| Package | Version |
|---------|---------|
| `next` | 16.2.10 |
| `@tanstack/react-query` | ^5.101.2 |
| `@base-ui/react` | ^1.6.0 |
| `lucide-react` | ^1.23.0 |
| `shadcn` | ^4.12.0 |
| `tw-animate-css` | ^1.4.0 |

## 9. Connection String

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=fluxgrid;Username=postgres;Password=postgres"
```

**Migrasi ke Neon**: ganti `Host`, `Username`, `Password` — EF Core provider tetap `Npgsql`.

## 10. Local Dev Setup

```bash
# Backend
cd backend/FluxGrid.Api
dotnet run                       # auto migrate + seed on startup
# → http://localhost:5020

# Frontend (terminal lain)
cd frontend
npm run dev
# → http://localhost:3000

# Test login
curl -X POST http://localhost:5020/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# New migration
dotnet ef migrations add Add<NamaEntity>
```

## 11. Known Limitations (MVP)

- Dashboard module data masih hardcoded (belum pake DB)
- No refresh token
- No user registration endpoint
- No password change
- Permission pake `text[]` — not normalized
- No rate limiting middleware
- No audit trail

---

