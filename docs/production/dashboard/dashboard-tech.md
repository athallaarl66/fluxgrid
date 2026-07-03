# Technical Specifications: Dashboard (dashboard)

## 1. System Architecture
- **Frontend**: Next.js 15 page `pages/dashboard.jsx` renders a grid of module cards. Data fetched via React Query (TanStack Query) from the new backend endpoint.
- **Backend**: .NET 8 Minimal API `GET /api/dashboard` returns JSON array of modules with `name`, `path`, `description`, `icon`.
- **Auth**: Existing JWT middleware validates token; RBAC permission `Dashboard:Read` required. Unauthorized users receive 403.
- **Caching**: Results cached server‑side for 30 seconds using `IMemoryCache` and client‑side with TanStack Query stale‑time.
- **Event Flow**: No domain events needed for the dashboard; it simply aggregates static module metadata.

## 2. API Contract
- **Endpoint**: `GET /api/dashboard`
- **Headers**: `Authorization: Bearer <JWT>`
- **Response (200)**:
```json
[
  {
    "name": "WMS",
    "path": "/wms",
    "description": "Warehouse Management",
    "icon": "wms.svg"
  },
  {
    "name": "Finance",
    "path": "/finance",
    "description": "General Ledger & Reporting",
    "icon": "finance.svg"
  },
  {
    "name": "HR",
    "path": "/hr",
    "description": "Human Resources & Payroll",
    "icon": "hr.svg"
  },
  {
    "name": "Projects",
    "path": "/projects",
    "description": "Task & Project Management",
    "icon": "projects.svg"
  }
]
```
- **Error Responses**:
  - `401 Unauthorized` – missing or invalid JWT.
  - `403 Forbidden` – user lacks `Dashboard:Read` permission.
  - `500 Internal Server Error` – unexpected failure (returns `{ "error": "Unable to retrieve dashboard data" }`).

## 3. Permissions & RBAC
- Permission required: `Dashboard:Read` (granted to all standard roles: Admin, Manager, Staff). Adjusted in `Shared/RBAC/Permissions.cs`.

## 4. Security Considerations
- **Input Validation**: No request body; only header token.
- **Rate Limiting**: Inherited from global API rate‑limit middleware (max 100 requests/minute per user).
- **CORS**: Allowed origins limited to the frontend domain.
- **Data Exposure**: Only module metadata; no sensitive data.

## 5. Performance & Scalability
- **Response Size**: < 1 KB (static list).
- **Caching**: 30 s server cache reduces DB load.
- **Horizontal Scaling**: Stateless endpoint; works with Koyeb auto‑scaling.

## 6. Error Handling & Resilience
- Graceful fallback on client side: show skeleton cards with a retry button.
- Server returns `500` with generic error message; client logs to Sentry.

## 7. Deployment Notes
- Add the new controller `DashboardController.cs` to the backend project.
- Update Dockerfile to expose the new route (no changes needed).
- Ensure the new permission is seeded in the DB migration.

## 8. Monitoring
- Instrument endpoint with OpenTelemetry metrics: request count, latency, error rate.
- Frontend telemetry via Vercel/Koyeb logs for page load times.

---
