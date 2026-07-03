# Technical Specifications: Dashboard (dashboard)

## 1. System Architecture
- **Frontend**: Next.js 15 app router page `app/dashboard/page.tsx` renders a responsive grid of module cards. Data fetched via TanStack Query hook `useDashboard()`.
- **Backend**: .NET 8 Minimal API `GET /api/dashboard` returns JSON array of modules with `name`, `path`, `description`, `icon`, `metric`.
- **Auth**: JWT middleware validates token from header; RBAC permission `Dashboard:Read` required. Credentials verified against PostgreSQL via EF Core + BCrypt.
- **Database**: PostgreSQL 18 with EF Core (Npgsql). Connection via `appsettings.json`.
- **Caching**: None yet (static data from `DashboardService`). TanStack Query `staleTime` default applied client-side.
- **Event Flow**: No domain events needed for dashboard.

## 2. API Contract
- **Endpoint**: `GET /api/dashboard`
- **Headers**: `Authorization: Bearer <JWT>`
- **Response (200)**:
```json
[
  {
    "name": "WMS",
    "path": "/wms",
    "description": "Warehouse Management System — inventory, picking, shipping",
    "icon": "package",
    "metric": "1,234"
  },
  {
    "name": "Finance",
    "path": "/finance",
    "description": "Financial management — invoices, budgets, reporting",
    "icon": "wallet",
    "metric": "$892K"
  },
  {
    "name": "HR",
    "path": "/hr",
    "description": "Human Resources — payroll, attendance, employee records",
    "icon": "users",
    "metric": "156"
  },
  {
    "name": "Projects",
    "path": "/projects",
    "description": "Task & Project management — timelines, milestones, tasks",
    "icon": "clipboard",
    "metric": "23"
  }
]
```
- **Error Responses**:
  - `401 Unauthorized` – missing or invalid JWT.
  - `403 Forbidden` – user lacks `Dashboard:Read` permission.
  - `500 Internal Server Error` – unexpected failure (returns `{ "error": "Unable to retrieve dashboard data" }`).

## 3. Auth & RBAC
- **Login**: `POST /api/auth/login` — validates username/password against `Users` table via BCrypt, returns JWT.
- **Permission required**: `Dashboard:Read` (granted to all roles: Admin, Manager, Staff).
- **Permission definition**: `Shared/RBAC/Permissions.cs` — static string constants.
- **Database storage**: `Role.Permissions` stored as `text[]` column in PostgreSQL, assigned via `DataSeeder`.

## 4. Security Considerations
- **Input Validation**: No request body for dashboard; login payload validated server-side.
- **CORS**: Allowed origins limited to `http://localhost:3000`.
- **Data Exposure**: Only module metadata; no sensitive data.
- **Password Storage**: BCrypt hash with random salt.

## 5. Performance & Scalability
- **Response Size**: < 1 KB (static list).
- **Caching**: Module data is static (hardcoded in `DashboardService` for MVP).
- **Horizontal Scaling**: Stateless endpoint; works with Koyeb auto-scaling.

## 6. Error Handling & Resilience
- Client side: skeleton loading cards with retry button on failure.
- Server returns 500 with generic error message.

## 7. Deployment Notes
- **Database**: PostgreSQL 18 required. Run `dotnet ef database update` or let `Program.cs` auto-migrate on startup.
- **Connection String**: Configure in `appsettings.json` under `ConnectionStrings:DefaultConnection`.
- **Seed Data**: `DataSeeder` runs on startup — creates 3 roles (Admin, Manager, Staff) + admin user.
- **Frontend**: `NEXT_PUBLIC_API_URL` env var pointing to backend (default `http://localhost:5020`).

## 8. Monitoring
- Backend health check at `GET /api/health`.
- Frontend uses TanStack Query devtools in development for request debugging.

---

