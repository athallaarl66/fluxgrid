# Test Report: wms-01-stock-management.Test

## Summary

| Metric | Value |
|--------|-------|
| **Test Project** | `wms-01-stock-management.Test` |
| **Test Class** | `StockLedgerServiceTests` |
| **Total Tests** | 30 |
| **Passed** | 29 |
| **Failed** | 0 |
| **Skipped** | 1 |
| **Execution Time** | 377ms |

## Test Results

### All Tests Passed ✓ (1 skipped — requires PostgreSQL for transactions)

---

## Test Coverage Breakdown

### 1. GetLedgerAsync Tests (6 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `GetLedgerAsync_ReturnsPaginatedResults` | ✓ |
| 2 | `GetLedgerAsync_FiltersBySku` | ✓ |
| 3 | `GetLedgerAsync_FiltersByDateRange` | ✓ |
| 4 | `GetLedgerAsync_FiltersByLocationId` | ✓ |
| 5 | `GetLedgerAsync_ExcludesOtherTenants` | ✓ |
| 6 | `GetLedgerAsync_ReturnsEmptyForNoMatches` | ✓ |
| 7 | `GetLedgerAsync_OrdersByCreatedAtDescending` | ✓ |

**Coverage:**
- Pagination (page, pageSize, total)
- SKU search via InventoryItem lookup
- Date range filtering (startDate, endDate)
- Location filtering
- Tenant isolation (cross-tenant exclusion)
- Empty result handling
- Sort order (created_at descending)

---

### 2. GetBalanceAsync Tests (3 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `GetBalanceAsync_ReturnsBalance` | ✓ |
| 2 | `GetBalanceAsync_ReturnsNullForNonExistent` | ✓ |
| 3 | `GetBalanceAsync_ExcludesOtherTenants` | ✓ |

**Coverage:**
- Returns balance with correct qty and value
- Null for non-existent item/location
- Tenant isolation

---

### 3. CalculateFifoValuationAsync Tests (5 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `CalculateFifoValuationAsync_SingleLayer_ReturnsCorrectAverage` | ✓ |
| 2 | `CalculateFifoValuationAsync_MultipleLayers_ReturnsAllLayers` | ✓ |
| 3 | `CalculateFifoValuationAsync_ExcludesNegativeEntries` | ✓ |
| 4 | `CalculateFifoValuationAsync_ReturnsZeroForNoEntries` | ✓ |
| 5 | `CalculateFifoValuationAsync_OrdersByCreatedAtAscending` | ✓ |

**Coverage:**
- Single cost layer calculation
- Multiple cost layers with correct aggregate
- Filters out negative (outbound) entries
- Empty ledger handling
- FIFO ordering (earliest first)

---

### 4. CalculateAverageCostValuationAsync Tests (5 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `CalculateAverageCostValuationAsync_ReturnsWeightedAverage` | ✓ |
| 2 | `CalculateAverageCostValuationAsync_SingleEntry` | ✓ |
| 3 | `CalculateAverageCostValuationAsync_ReturnsZeroForNoEntries` | ✓ |
| 4 | `CalculateAverageCostValuationAsync_ExcludesNegativeEntries` | ✓ |
| 5 | `CalculateAverageCostValuationAsync_ExcludesOtherTenants` | ✓ |

**Coverage:**
- Weighted average calculation (10×1000 + 10×1500) / 20 = 1250
- Single entry edge case
- Empty ledger returning zero
- Negative entry exclusion
- Tenant isolation

---

### 5. CreateMovementAsync Validation Tests (3 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `CreateMovementAsync_RejectsSingleEntry` | ✓ |
| 2 | `CreateMovementAsync_RejectsUnbalancedEntries` | ✓ |
| 3 | `CreateMovementAsync_RejectsZeroEntries` | ✓ |

**Coverage:**
- Minimum 2 entries required (debit + credit)
- Double-entry validation: SUM(quantity) must equal zero
- Empty entries list rejection
- Returns proper error messages

*Full transaction + pessimistic locking tests require PostgreSQL (skipped).*

---

### 6. DTO Types Tests (6 tests)

| # | Test Name | Status |
|---|-----------|--------|
| 1 | `CreateMovementEntry_PropertiesMatchConstructor` | ✓ |
| 2 | `LedgerResult_PropertiesMatchConstructor` | ✓ |
| 3 | `MovementResult_Success` | ✓ |
| 4 | `MovementResult_Failure` | ✓ |
| 5 | `FifoValuationResult_Properties` | ✓ |
| 6 | `AverageCostResult_Properties` | ✓ |

**Coverage:**
- Record constructor parameter alignment
- Success/failure result patterns
- Value object immutability

---

## Business Rules Tested

### Double-Entry Validation
- ✓ Minimum 2 entries per transaction
- ✓ SUM(quantity) must equal zero (balanced)
- ✓ Unbalanced entries rejected with error message

### Tenant Isolation
- ✓ GetLedgerAsync excludes other tenants
- ✓ GetBalanceAsync excludes other tenants
- ✓ CalculateAverageCost excludes other tenants
- ✓ All queries filter by TenantId

### Inventory Valuation
- ✓ FIFO: multiple cost layers tracked oldest-first
- ✓ FIFO: total value = sum(layer.qty × layer.cost)
- ✓ Average Cost: weighted avg = total_value / total_qty
- ✓ Negative entries excluded from valuation

### Pagination & Filtering
- ✓ Page/pageSize respected
- ✓ SKU, location, date range filters
- ✓ Empty results handled gracefully

---

## Skipped Tests

| Test | Reason |
|------|--------|
| `CreateMovementAsync_AcceptsBalancedEntries_Integration` | Requires real PostgreSQL for `BeginTransactionAsync` + `FOR UPDATE` support. EF Core InMemory does not support transactions. |

---

## Execution Details

```
Command: dotnet test tests/unit/wms/wms-01-stock-management.Test/FluxGrid.Api.Tests.csproj
Framework: .NET 8.0
Test Runner: xUnit.net 2.5.3
Runtime: Microsoft.NETCore.App 8.0
```

---

## Files

| File | Description |
|------|-------------|
| `tests/unit/wms/wms-01-stock-management.Test/StockLedgerServiceTests.cs` | Test implementation |
| `tests/unit/wms/wms-01-stock-management.Test/FluxGrid.Api.Tests.csproj` | Test project |
| `backend/FluxGrid.Api/Modules/WMS/Application/StockLedgerService.cs` | Production code |

---

*Generated: 2026-07-08*
