# HR-4 CV Upload — Unit Test Report

**Generated:** 2026-07-10
**Project:** FluxGrid ERP
**Test Framework:** xUnit + Moq + EF Core InMemory
**Test Runner:** dotnet test

---

## 1. Test Execution Summary

| Metric | Value |
|---|---|
| Total Tests | 17 |
| Passed | 17 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~344 ms |
| Test File | `FluxGrid.Api.Tests.dll` |
| Test Project | `tests/unit/hr/hr-4-cv-upload.Test/` |

---

## 2. Test Results Overview

### 2.1 RecruitmentServiceTests — 17 tests

| Test | Status | Duration |
|---|---|---|
| `RequestUploadUrlAsync_ReturnsPresignedUrl` | ✅ Passed | < 1 ms |
| `RequestUploadUrlAsync_ThrowsOnInvalidFileType` | ✅ Passed | < 1 ms |
| `RequestUploadUrlAsync_ThrowsOnOversizedFile` | ✅ Passed | < 1 ms |
| `RequestUploadUrlAsync_ThrowsOnDuplicateHash` | ✅ Passed | < 1 ms |
| `RequestUploadUrlAsync_AllowsSameHashForDifferentTenant` | ✅ Passed | < 1 ms |
| `CreateCandidateAsync_CreatesDraftCandidate` | ✅ Passed | < 1 ms |
| `CreateCandidateAsync_ThrowsOnDuplicateEmail` | ✅ Passed | < 1 ms |
| `CreateCandidateAsync_RaisesCandidateUploadedEvent` | ✅ Passed | < 1 ms |
| `CreateCandidateAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `GetCandidatesAsync_ReturnsPaginatedResults` | ✅ Passed | < 1 ms |
| `GetCandidatesAsync_FiltersBySearch` | ✅ Passed | < 1 ms |
| `GetCandidatesAsync_FiltersByStatus` | ✅ Passed | < 1 ms |
| `GetCandidatesAsync_OrdersByCreatedAtDesc` | ✅ Passed | < 1 ms |
| `GetCandidatesAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |
| `GetCandidateDetailAsync_ReturnsCandidateWithSubEntities` | ✅ Passed | < 1 ms |
| `GetCandidateDetailAsync_ReturnsNullWhenNotFound` | ✅ Passed | < 1 ms |
| `GetCandidateDetailAsync_RespectsTenantIsolation` | ✅ Passed | < 1 ms |

---

## 3. Coverage by Feature

| Feature Area | Tests | Scope |
|---|---|---|
| **Upload URL** | 5 | Valid URL generation, invalid file type (.exe), oversized file (6MB), duplicate hash detection, cross-tenant hash allowed |
| **Create Candidate** | 4 | Draft record creation, duplicate email guard, domain event dispatch, tenant isolation |
| **Candidate List** | 5 | Pagination, search by name/email, status filter, sort by created_at desc, tenant isolation |
| **Candidate Detail** | 3 | Returns detail with education/skills sub-entities, null when not found, tenant isolation |

---

## 4. Test Configuration

- **Database:** EF Core InMemory (isolated per test class via `Guid.NewGuid()` database name)
- **Mocking:** `IFileStorageService` mocked via Moq; real `AuditService` and `DomainEventDispatcher` instances
- **External deps:** None — MinIO/S3 not required; `IConfiguration` mocked for bucket name
- **Backend entry point:** `FluxGrid.Api` project referenced directly

---

## 5. Verification Checklist

| Requirement | Covered By |
|---|---|
| Invalid file type (.exe) rejected (400) | `RequestUploadUrlAsync_ThrowsOnInvalidFileType` |
| File exceeds 5MB rejected (400) | `RequestUploadUrlAsync_ThrowsOnOversizedFile` |
| Duplicate SHA-256 hash detected (409) | `RequestUploadUrlAsync_ThrowsOnDuplicateHash` |
| Same hash allowed for different tenant | `RequestUploadUrlAsync_AllowsSameHashForDifferentTenant` |
| Candidate created as DRAFT | `CreateCandidateAsync_CreatesDraftCandidate` |
| Duplicate email rejected (400) | `CreateCandidateAsync_ThrowsOnDuplicateEmail` |
| `CandidateUploaded` event raised | `CreateCandidateAsync_RaisesCandidateUploadedEvent` |
| Tenant isolation enforced | 4 cross-tenant tests across upload, create, list, detail |
| Pagination works | `GetCandidatesAsync_ReturnsPaginatedResults` |
| Search by name/email works | `GetCandidatesAsync_FiltersBySearch` |
| Status filter works | `GetCandidatesAsync_FiltersByStatus` |
| Sorted by created_at desc | `GetCandidatesAsync_OrdersByCreatedAtDesc` |
| Not-found returns null | `GetCandidateDetailAsync_ReturnsNullWhenNotFound` |
| Sub-entities returned in detail | `GetCandidateDetailAsync_ReturnsCandidateWithSubEntities` |

> **Note:** Orphaned S3 file cleanup on DB failure requires a real PostgreSQL unique constraint to trigger `DbUpdateException` — EF Core InMemory does not enforce DB constraints. This is verified via integration/E2E tests.

---

## 6. Ship Readiness

**✅ Ready** — all 17 tests pass. Core recruitment business logic (presigned URL validation, candidate creation, list/detail with filtering, tenant isolation, domain events) is covered. No regressions detected.
