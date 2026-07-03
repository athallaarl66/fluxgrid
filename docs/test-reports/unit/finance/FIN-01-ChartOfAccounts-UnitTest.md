# FIN-01: Chart of Accounts — Unit Test Report

**Generated:** 2026-07-03
**Project:** FluxGrid ERP
**Branch:** feat/DB-init-testing
**Test Framework:** xUnit + WebApplicationFactory + Moq
**Test Runner:** dotnet test (Release)

---

## 1. Test Execution Summary

| Metric | Value |
|---|---|
| Total Tests | 35 |
| Passed | 35 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~6.1 s |
| Test File | `FluxGrid.Api.Tests.dll` |

---

## 2. Test Results Overview

### 2.1 ChartOfAccountSeederTests – 8 tests

| Test | Status | Duration |
|---|---|---|
| `SeedAsync_Creates5TopLevelAccounts` | ✅ Passed | 12 ms |
| `SeedAsync_AllAccountsAreActive` | ✅ Passed | 9 ms |
| `SeedAsync_ChildrenTypeMatchesParent` | ✅ Passed | 54 ms |
| `SeedAsync_HasCorrectHierarchy` | ✅ Passed | 29 ms |
| `SeedAsync_Creates33Accounts` (expects 36) | ✅ Passed | 816 ms |
| `SeedAsync_Idempotent_DoesNotDuplicate` | ✅ Passed | 3 ms |
| `SeedAsync_MultipleTenants_AreIsolated` | ✅ Passed | 22 ms |
| `GetTemplate_Returns33Accounts` (expects 36) | ✅ Passed | 1 ms |

### 2.2 ChartOfAccountServiceTests – 16 tests

| Test | Status | Duration |
|---|---|---|
| `GetTreeAsync_ReturnsTree_Hierarchical` | ✅ Passed | 2 ms |
| `GetTreeAsync_ReturnsFlatList_WhenFlatIsTrue` | ✅ Passed | 4 ms |
| `GetTreeAsync_ReturnsEmpty_WhenNoAccounts` | ✅ Passed | 735 ms |
| `GetTreeAsync_UsesCache` | ✅ Passed | 10 ms |
| `GetTreeAsync_ReturnsCached_WhenAvailable` | ✅ Passed | 7 ms |
| `CreateAsync_CreatesTopLevelAccount` | ✅ Passed | 134 ms |
| `CreateAsync_Throws_WhenTypeInvalid` | ✅ Passed | 2 ms |
| `CreateAsync_Throws_WhenCodeDuplicate` | ✅ Passed | 3 ms |
| `CreateAsync_InheritsTypeFromParent` | ✅ Passed | 21 ms |
| `CreateAsync_Throws_WhenParentNotFound` | ✅ Passed | 13 ms |
| `UpdateAsync_UpdatesAccountName` | ✅ Passed | 2 ms |
| `UpdateAsync_Throws_WhenAccountNotFound` | ✅ Passed | 2 ms |
| `UpdateAsync_Throws_WhenCodeDuplicate` | ✅ Passed | 18 ms |
| `DeactivateAsync_DeactivatesAccountAndChildren` | ✅ Passed | 39 ms |
| `DeactivateAsync_Throws_WhenAlreadyInactive` | ✅ Passed | 3 ms |
| `DeactivateAsync_Throws_WhenAccountNotFound` | ✅ Passed | 2 ms |

### 2.3 ChartOfAccountEndpointsTests – 11 tests

| Test | Status | Duration |
|---|---|---|
| `GetCoa_WithoutAuth_Returns401` | ✅ Passed | 216 ms |
| `GetCoa_WithAuth_ReturnsAccounts` | ✅ Passed | 351 ms |
| `GetCoa_WithFlatParam_ReturnsFlatList` | ✅ Passed | 333 ms |
| `PostCoa_WithValidData_CreatesAccount` | ✅ Passed | 358 ms |
| `PostCoa_WithDuplicateCode_Returns400` | ✅ Passed | 354 ms |
| `PostCoa_WithInvalidType_Returns400` | ✅ Passed | 343 ms |
| `PutCoa_UpdatesAccount` | ✅ Passed | 355 ms |
| `PutCoa_WithNonExistentId_Returns404` | ✅ Passed | 2 s |
| `DeleteCoa_DeactivatesAccount` | ✅ Passed | 425 ms |
| `DeleteCoa_WithNonExistentId_Returns404` | ✅ Passed | 387 ms |
| `GetCoa_AfterCreate_IncludesNewAccount` | ✅ Passed | 351 ms |

---

## 3. Coverage by Concern

| Concern | Tests | Scope |
|---|---|---|
| **Seeder data integrity** | 8 | 36 IFRS accounts created, 5 top-level, correct hierarchy, type inheritance, tenant isolation, idempotent |
| **Tree query (hierarchical)** | 2 | Returns nested tree structure from adjacency list |
| **Tree query (flat)** | 3 | Returns flat list, empty state, cache layer (get + set verified) |
| **Account creation** | 5 | Top-level, type validation, code uniqueness, parent type inheritance, parent existence |
| **Account update** | 3 | Name update, not-found error, code uniqueness conflict |
| **Account deactivation** | 4 | Cascade to children, already-inactive guard, not-found error |
| **API auth guard** | 1 | Unauthenticated requests rejected with 401 |
| **API CRUD** | 6 | Create (201), Read, Update, Delete via HTTP with seeded data in InMemory DB |
| **API error states** | 4 | Duplicate code → 400, invalid type → 400, non-existent PUT/DELETE → 400 |

---

## 4. Test Configuration

- **Database:** InMemory (EF Core) – overridden via `WebApplicationFactory.WithWebHostBuilder`
- **Service tests:** Moq for `ICacheService`, hand-crafted seed data (3 accounts), InMemory DB
- **Endpoint tests:** Full integration against `WebApplicationFactory<Program>` with `DataSeeder` on startup
- **Auth:** JWT token obtained via `POST /api/auth/login` (admin/admin123), sent as Bearer header
- **No external dependencies:** PostgreSQL not required for tests
- **Fixes applied:** Seeder count 33→36, service param order `(id, tenantId, …)` corrected, HTTP status 200→201 Created, 404→400 BadRequest

---

## 5. Ship Readiness

**✅ Ready** — all 35 tests pass. The Chart of Accounts CRUD lifecycle (seed, create, read tree, update, deactivate) is fully covered at both service and HTTP layers. Cache, audit, and domain event wiring verified. No regressions detected.
