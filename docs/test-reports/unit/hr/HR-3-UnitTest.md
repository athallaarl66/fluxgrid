# HR-3 Payroll Processing — Unit Test Report

**Generated:** 2026-07-10
**Project:** FluxGrid ERP
**Test Framework:** xUnit + Moq + EF Core InMemory
**Test Runner:** dotnet test

---

## 1. Test Execution Summary

| Metric | Value |
|---|---|
| Total Tests | 25 |
| Passed | 25 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~540 ms |
| Test File | `FluxGrid.Api.Tests.dll` |
| Test Project | `tests/unit/hr/hr-3-payroll-processing.Test/` |

---

## 2. Test Results Overview

### 2.1 PayrollServiceTests — 25 tests

| Test | Status | Duration |
|---|---|---|
| `CalculatePayrollAsync_CreatesDraftRunWithRecords` | ✅ Passed | < 1 ms |
| `CalculatePayrollAsync_ThrowsOnDuplicatePeriod` | ✅ Passed | < 1 ms |
| `CalculatePayrollAsync_ThrowsWhenNoActiveEmployees` | ✅ Passed | < 1 ms |
| `CalculatePayrollAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `FinalizePayrollAsync_SetsFinalizedAndDispatchesEvent` | ✅ Passed | < 1 ms |
| `FinalizePayrollAsync_ThrowsWhenAlreadyFinalized` | ✅ Passed | < 1 ms |
| `FinalizePayrollAsync_ThrowsWhenPeriodClosed` | ✅ Passed | < 1 ms |
| `FinalizePayrollAsync_ThrowsWhenNotFound` | ✅ Passed | < 1 ms |
| `FinalizePayrollAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `RecalculatePayrollAsync_ClearsAndRecalculates` | ✅ Passed | < 1 ms |
| `RecalculatePayrollAsync_ThrowsWhenFinalized` | ✅ Passed | < 1 ms |
| `RecalculatePayrollAsync_ThrowsWhenNotFound` | ✅ Passed | < 1 ms |
| `GetPayrollRunAsync_ReturnsRunWithRecords` | ✅ Passed | < 1 ms |
| `GetPayrollRunAsync_MasksSalaryWhenNotPermitted` | ✅ Passed | < 1 ms |
| `GetPayrollRunAsync_ShowsSalaryWhenPermitted` | ✅ Passed | < 1 ms |
| `GetPayrollRunAsync_ReturnsNullWhenWrongTenant` | ✅ Passed | < 1 ms |
| `GetPayrollRunAsync_ReturnsNullWhenNotFound` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_ReturnsPaginatedResults` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_FiltersByStatus` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_OrdersByCreatedAtDesc` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_MasksTotalsWhenNotPermitted` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_ShowsTotalsWhenPermitted` | ✅ Passed | < 1 ms |
| `ListPayrollRunsAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `GetMyPayslipsAsync_ReturnsRecordsForLinkedEmployee` | ✅ Passed | < 1 ms |
| `GetMyPayslipsAsync_ThrowsWhenNoLinkedEmployee` | ✅ Passed | < 1 ms |

---

## 3. Coverage by Feature

| Feature Area | Tests | Scope |
|---|---|---|
| **Calculate** | 4 | Draft creation, records, duplicate period guard, tenant isolation |
| **Finalize** | 5 | Status transition, event dispatch, already-finalized guard, closed period guard, tenant isolation |
| **Recalculate** | 3 | Clear + recalculate, finalized guard, not found |
| **Salary Masking** | 4 | `includeSalary=false` masks all amounts (list + detail), `includeSalary=true` shows amounts |
| **Tenant Isolation** | 4 | Cross-tenant data not returned on list, detail, calculate, finalize |
| **Employee Self-Service** | 2 | Returns own payslips, throws when no linked employee |

---

## 4. Test Configuration

- **Database:** EF Core InMemory (isolated per test class via `Guid.NewGuid()` database name)
- **Mocking:** Real `AuditService` and `DomainEventDispatcher` instances; `HttpClient` mocked via `Moq`
- **External deps:** None — PostgreSQL not required; Task App API calls return null (catch block)
- **Backend entry point:** `FluxGrid.Api` project referenced directly

---

## 5. Verification Checklist

| Requirement | Covered By |
|---|---|
| Duplicate period rejected (409) | `CalculatePayrollAsync_ThrowsOnDuplicatePeriod` |
| Already finalized rejected (409) | `FinalizePayrollAsync_ThrowsWhenAlreadyFinalized`, `RecalculatePayrollAsync_ThrowsWhenFinalized` |
| Closed Finance period blocks finalize (400) | `FinalizePayrollAsync_ThrowsWhenPeriodClosed` |
| Salary masked without `HR:PayrollRead` | `GetPayrollRunAsync_MasksSalaryWhenNotPermitted`, `ListPayrollRunsAsync_MasksTotalsWhenNotPermitted` |
| Salary visible with `HR:PayrollRead` | `GetPayrollRunAsync_ShowsSalaryWhenPermitted`, `ListPayrollRunsAsync_ShowsTotalsWhenPermitted` |
| Tenant isolation enforced | 4 cross-tenant tests across calculate, finalize, list, detail |
| Employee sees own payslips only | `GetMyPayslipsAsync_ReturnsRecordsForLinkedEmployee` |
| No employee link throws | `GetMyPayslipsAsync_ThrowsWhenNoLinkedEmployee` |
| Pagination works | `ListPayrollRunsAsync_ReturnsPaginatedResults` |
| Status filter works | `ListPayrollRunsAsync_FiltersByStatus` |
| Not-found returns null/throws | 4 tests across get, finalize, recalculate |

---

## 6. Ship Readiness

**✅ Ready** — all 25 tests pass. Core payroll business logic (calculate, finalize, recalculate, salary masking, tenant isolation, self-service payslips) is covered. No regressions detected.
