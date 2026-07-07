# Test Report: finance-journal02.Test

## Summary

| Metric | Value |
|--------|-------|
| **Test Project** | `finance-journal02.Test` |
| **Test Class** | `JournalEntryServiceTests` |
| **Total Tests** | 29 |
| **Passed** | 29 |
| **Failed** | 0 |
| **Skipped** | 0 |
| **Execution Time** | 2.18s |

## Test Results

### All Tests Passed ✓

---

## Test Coverage Breakdown

### 1. CreateAsync Tests (8 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `CreateAsync_AsDraft_AllowsUnbalancedEntries` | ✓ |
| 2 | `CreateAsync_AsDraft_AcceptsBalancedEntries` | ✓ |
| 3 | `CreateAsync_AsSubmit_RejectsUnbalancedEntries` | ✓ |
| 4 | `CreateAsync_AsSubmit_BelowThreshold_SetsStatusPosted` | ✓ |
| 5 | `CreateAsync_AsSubmit_AtThreshold_SetsStatusPosted` | ✓ |
| 6 | `CreateAsync_AsSubmit_AboveThreshold_SetsStatusPendingApproval` | ✓ |
| 7 | `CreateAsync_SetsCorrectEntryNo` | ✓ |
| 8 | `CreateAsync_SetsCorrectTenantAndUser` | ✓ |

**Coverage:**
- Draft mode allows unbalanced entries
- Submit mode rejects unbalanced entries (422)
- Threshold logic (50,000,000 IDR)
- Below/at threshold → POSTED
- Above threshold → PENDING_APPROVAL

---

### 2. GetListAsync Tests (6 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `GetListAsync_ReturnsAllEntriesForTenant` | ✓ |
| 2 | `GetListAsync_FiltersByStatus` | ✓ |
| 3 | `GetListAsync_ReturnsEmptyForNonExistentStatus` | ✓ |
| 4 | `GetListAsync_RespectsPagination` | ✓ |
| 5 | `GetListAsync_OrdersByTransactionDateDescending` | ✓ |
| 6 | `GetListAsync_ExcludesOtherTenants` | ✓ |

**Coverage:**
- Tenant isolation
- Status filtering (DRAFT, PENDING_APPROVAL, POSTED)
- Pagination support
- Ordering by transaction date (desc)

---

### 3. GetByIdAsync Tests (3 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `GetByIdAsync_ReturnsEntryWithLines` | ✓ |
| 2 | `GetByIdAsync_ReturnsNullForWrongTenant` | ✓ |
| 3 | `GetByIdAsync_ReturnsNullForNonExistentId` | ✓ |

**Coverage:**
- Returns entry with lines included
- Tenant isolation
- Non-existent ID handling

---

### 4. UpdateDraftAsync Tests (5 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `UpdateDraftAsync_UpdatesEntryFields` | ✓ |
| 2 | `UpdateDraftAsync_ThrowsForPostedEntry` | ✓ |
| 3 | `UpdateDraftAsync_ThrowsForNonExistentEntry` | ✓ |
| 4 | `UpdateDraftAsync_AsSubmit_ValidatesBalance` | ✓ |

**Coverage:**
- Update entry fields (date, description, lines)
- Cannot update POSTED entries
- Balance validation on submit

---

### 5. ApproveAsync Tests (5 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `ApproveAsync_ApprovesEntryAndSetsApprovedBy` | ✓ |
| 2 | `ApproveAsync_ThrowsForSelfApproval` | ✓ |
| 3 | `ApproveAsync_ThrowsForNonPendingEntry` | ✓ |
| 4 | `ApproveAsync_ThrowsForNonExistentEntry` | ✓ |
| 5 | `ApproveAsync_ThrowsForWrongTenant` | ✓ |

**Coverage:**
- Approve pending entries (POSTED status)
- Self-approval rejection (SELF_APPROVAL_DENIED)
- Only PENDING_APPROVAL can be approved
- Tenant isolation

---

### 6. DeleteDraftAsync Tests (3 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `DeleteDraftAsync_VoidsDraftEntry` | ✓ |
| 2 | `DeleteDraftAsync_VoidsPendingEntry` | ✓ |
| 3 | `DeleteDraftAsync_ThrowsForPostedEntry` | ✓ |

**Coverage:**
- Void DRAFT entries
- Void PENDING_APPROVAL entries
- Cannot void POSTED entries (CANNOT_VOID_POSTED)

---

## Business Rules Tested

### Debit/Credit Validation
- ✓ Draft entries can be unbalanced
- ✓ Submit entries must be balanced (debit == credit)

### Threshold Logic (50,000,000 IDR)
- ✓ Amount ≤ 50M → POSTED
- ✓ Amount > 50M → PENDING_APPROVAL

### Maker-Checker / Segregation of Duties
- ✓ Creator cannot approve their own entry
- ✓ Non-creator can approve pending entries

### Tenant Isolation
- ✓ Entries are filtered by TenantId
- ✓ Users cannot access other tenants' data

---

## Execution Details

```
Command: dotnet test tests/unit/Finance/finance-journal02.Test/FluxGrid.Api.Tests.csproj
Framework: .NET 8.0
Test Runner: xUnit.net 2.5.3
Runtime: Microsoft.NETCore.App 8.0
```

---

## Files

| File | Description |
|------|-------------|
| `tests/unit/Finance/finance-journal02.Test/JournalEntryServiceTests.cs` | Test implementation |
| `tests/unit/Finance/finance-journal02.Test/FluxGrid.Api.Tests.csproj` | Test project |
| `backend/FluxGrid.Api/Modules/Finance/Application/JournalEntryService.cs` | Production code |

---

*Generated: 2026-07-03*
