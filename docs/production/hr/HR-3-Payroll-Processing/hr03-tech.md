# Technical Specifications: Payroll Processing (HR-3)

## 1. System Architecture
- **Backend API**: .NET 8 Minimal API with EF Core (PostgreSQL). Synchronous calculation for MVP (< 1000 employees, < 30s per PRD).
- **Frontend**: Next.js 16 App Router + TanStack Query + shadcn/ui (Industrial Modern design system).
- **Database**: PostgreSQL via EF Core (`Npgsql` provider).
- **Integration**: `PayrollProcessed` domain event via `DomainEventDispatcher` → Finance module creates balanced journal entries.
- **ponytail:** When > 1000 employees, extract calculation to background job with polling progress endpoint.

## 2. Database Schema

### Table: `payroll_runs`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `period_name` | VARCHAR(50) | NOT NULL | e.g., "May 2026" |
| `start_date` | DATE | NOT NULL | |
| `end_date` | DATE | NOT NULL | |
| `status` | VARCHAR(20) | NOT NULL | DRAFT, FINALIZED |
| `total_gross` | DECIMAL | DEFAULT 0 | |
| `total_net` | DECIMAL | DEFAULT 0 | |
| `processed_by` | VARCHAR(100) | NOT NULL | User ID string |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `payroll_records`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `run_id` | UUID | NOT NULL, FK | References `payroll_runs` |
| `employee_id` | UUID | NOT NULL, FK | References `employees` |
| `base_salary` | DECIMAL | NOT NULL | Prorated snapshot |
| `overtime_pay` | DECIMAL | DEFAULT 0 | Calculated from Task App attendance |
| `lateness_deduction` | DECIMAL | DEFAULT 0 | Calculated from Task App attendance |
| `gross_pay` | DECIMAL | NOT NULL | Base + overtime - lateness |
| `tax_deduction` | DECIMAL | DEFAULT 0 | PPh 21 (5% flat) |
| `net_pay` | DECIMAL | NOT NULL | Gross - tax (capped at 0) |
| `tenant_id` | UUID | NOT NULL, FK | |

**Unique index:** `(run_id, employee_id)` — one record per employee per run.

## 3. Backend Layers

### Domain Entities
- `PayrollRun` — `backend/FluxGrid.Api/Modules/HR/Domain/Entities/PayrollRun.cs`
- `PayrollRecord` — `backend/FluxGrid.Api/Modules/HR/Domain/Entities/PayrollRecord.cs`
- Both carry `TenantId` for RLS enforcement, registered as `DbSet<>` in `AppDbContext`.

### DTOs (`PayrollDtos.cs`)
| DTO | Fields | Notes |
|---|---|---|
| `CreatePayrollRequest` | PeriodName, StartDate, EndDate | Incoming |
| `PayrollRunResponse` | Id, PeriodName, StartDate, EndDate, Status, TotalGross?, TotalNet?, ProcessedBy, TenantId, CreatedAt | `TotalGross`/`TotalNet` nullable — masked without `HR:PayrollRead` |
| `PayrollRecordResponse` | Id, RunId, EmployeeId, EmployeeNo, EmployeeName, BaseSalary?, OvertimePay?, LatenessDeduction?, GrossPay?, TaxDeduction?, NetPay?, TenantId | All salary fields nullable — masked without `HR:PayrollRead` |
| `PayrollRunDetailResponse` | Run, TotalRecords, Records | Wrapper for detail page |
| `PaginatedPayrollRunListResponse` | Items, Total, Page, PageSize | Generic pattern |

### PayrollService (`PayrollService.cs`)
| Method | Key Logic |
|---|---|
| `CalculatePayrollAsync` | Validates no duplicate period → fetches active employees → fetches attendance from Task App API → prorates salaries → calculates OT/lateness/tax → saves run + records → audits |
| `FinalizePayrollAsync` | Validates DRAFT status → checks Finance period is OPEN → sets FINALIZED → dispatches `PayrollProcessed` event → audits |
| `RecalculatePayrollAsync` | Validates DRAFT → deletes existing records → re-runs calculation → audits |
| `GetPayrollRunAsync` | Returns run + paginated records with employee join; respects `includeSalary` flag |
| `ListPayrollRunsAsync` | Paginated, filtered by status, ordered by `created_at` DESC; respects `includeSalary` flag |
| `GetMyPayslipsAsync` | Finds employee linked to current user → returns only FINALIZED run records |

### PayrollEndpoints (`PayrollEndpoints.cs`)
| Endpoint | Auth | Rate Limit | Error Codes |
|---|---|---|---|
| `POST /api/v1/hr/payroll/calculate` | `HR:PayrollProcess` | 10 req/min | 409 `DUPLICATE_PERIOD`, 400 generic |
| `PUT /api/v1/hr/payroll/{id}/finalize` | `HR:PayrollProcess` | 10 req/min | 409 `ALREADY_FINALIZED`, 400 `PERIOD_CLOSED`, 400 generic |
| `PUT /api/v1/hr/payroll/{id}/recalculate` | `HR:PayrollProcess` | 10 req/min | 409 `ALREADY_FINALIZED`, 400 generic |
| `GET /api/v1/hr/payroll/runs` | Authenticated (salary masked without `HR:PayrollRead`) | — | — |
| `GET /api/v1/hr/payroll/{id}` | Authenticated (salary masked without `HR:PayrollRead`) | — | 404 |
| `GET /api/v1/hr/payroll/my-payslips` | Authenticated (self-only) | — | 400 |
| `GET /api/v1/hr/payroll/{runId}/payslip/{employeeId}` | Authenticated (self or `HR:PayrollRead`) | — | 404, 403 |

### Audit Logging
All mutations (calculate, finalize, recalculate) log to `audit_logs` table via `AuditService.LogAsync` with:
- action, user_id, tenant_id, resource_type (`payroll_runs`), resource_id, before/after snapshots

### Rate Limiting
Configured in `Program.cs` via `AspNetCoreRateLimit`:
- `POST /api/v1/hr/payroll/calculate` → 10 requests/minute
- `PUT /api/v1/hr/payroll/*/finalize` → 10 requests/minute
- `PUT /api/v1/hr/payroll/*/recalculate` → 10 requests/minute

## 4. Salary Masking

`includeSalary` flag controls salary field visibility in list and detail responses:

| User Claim | `includeSalary` | Effect |
|---|---|---|
| Has `HR:PayrollRead` | `true` | Salary amounts returned normally |
| Lacks `HR:PayrollRead` | `false` | All salary fields return `null` (UI renders "***") |

Checked per-request in endpoint handlers:
```csharp
var includeSalary = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
```

## 5. Domain Events

- **Event:** `PayrollProcessed` record — carries RunId, TotalGross, TotalTax, TotalNet, PeriodName, TenantId, ProcessedDate
- **Raised by:** `PayrollService.FinalizePayrollAsync`
- **Consumed by:** Finance module `PayrollProcessedHandler` — creates balanced journal entry (Debit: Salary Expense, Credit: Tax Payable / Bank)
- **Idempotency:** Handler checks if journal entry exists for the same RunId before creating

## 6. Permissions (RBAC)

| Permission | Endpoints |
|---|---|
| `HR:PayrollProcess` | Calculate, finalize, recalculate |
| `HR:PayrollRead` | Full salary visibility in list/detail/payslip |
| Authenticated (no specific permission) | List runs, view run details (salary masked), own payslips |

## 7. Frontend Pages

| Route | Page | Components |
|---|---|---|
| `/hr/payroll` | Payroll Dashboard | `PayrollRunsTable`, `SummaryMetricsCards`, `NewRunDialog` |
| `/hr/payroll/{id}` | Payroll Run Details | `PayrollRecordsTable`, `FinalizeConfirmationModal` |
| `/hr/payroll/{id}/payslip/{employeeId}` | Employee Payslip | `PayslipDocument` |

### State Handling
All pages follow a consistent pattern: **auth guard** → **loading skeleton** → **error state** (with retry) → **empty state** (illustration + CTA) → **data**.

### Salary Masking (Frontend)
Currency values display `"***"` when `null` (masked) — no additional frontend logic needed since the API returns `null` for unauthorized users.

## 8. Performance Considerations
- Synchronous calculation is acceptable for < 1000 employees (estimated < 30s).
- Attendance data fetched from Task App API via HTTP with 30s timeout and catch-block fallback (returns null, calculation proceeds with zero OT/lateness).
- EF Core pagination (`Skip/Take`) on all list endpoints.

## 9. Security Considerations
- Tenant isolation enforced at query level — all queries filter by `TenantId`.
- Salary fields return `null` when user lacks `HR:PayrollRead` — never leaks amounts.
- Payslip endpoint enforces self-only access: employee can only view payslip linked to their `UserId`.
- Finalization idempotency: status guard prevents double-finalize; DB optimistic locking via `DbUpdateConcurrencyException`.
- Rate limiting prevents brute-force on mutation endpoints.

## 10. Error Handling

| Scenario | HTTP Status | Error Code |
|---|---|---|
| Duplicate payroll period | 409 | `DUPLICATE_PERIOD` |
| Finalize already-finalized run | 409 | `ALREADY_FINALIZED` |
| Recalculate already-finalized run | 409 | `ALREADY_FINALIZED` |
| Finance period closed | 400 | `PERIOD_CLOSED` |
| Payroll run not found | 400 / 404 | — |
| No active employees | 400 | — |
| No employee linked to user | 400 | — |
| Rate limit exceeded | 429 | `RATE_LIMIT_EXCEEDED` |
| Unauthorized (missing permission) | 403 | — |

## 11. Test Coverage

- **25 unit tests** in `tests/unit/hr/hr-3-payroll-processing.Test/PayrollServiceTests.cs`
- Framework: xUnit + EF Core InMemory + Moq
- Covers: calculate, finalize, recalculate, salary masking, tenant isolation, validation guards, self-service payslips

## 12. File Map

```
backend/FluxGrid.Api/Modules/HR/
├── API/
│   ├── PayrollDtos.cs              # Request/response records
│   └── PayrollEndpoints.cs          # Minimal API endpoints
├── Application/
│   └── PayrollService.cs            # Business logic
├── Domain/
│   ├── Entities/
│   │   ├── PayrollRun.cs
│   │   └── PayrollRecord.cs
│   └── Events/
│       └── PayrollProcessed.cs      # Domain event

frontend/
├── app/hr/payroll/
│   ├── page.tsx                      # Dashboard
│   ├── [id]/page.tsx                 # Run details
│   └── [id]/payslip/[employeeId]/page.tsx  # Payslip
├── components/hr/
│   ├── PayrollRunsTable.tsx
│   ├── SummaryMetricsCards.tsx
│   ├── NewRunDialog.tsx
│   ├── PayrollRecordsTable.tsx
│   ├── FinalizeConfirmationModal.tsx
│   └── PayslipDocument.tsx
├── hooks/
│   └── usePayroll.ts                 # React Query hooks
└── lib/
    └── hr-types.ts                   # Payroll type definitions
```
