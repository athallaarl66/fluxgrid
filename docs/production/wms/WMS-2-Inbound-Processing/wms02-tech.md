# Technical Specifications: Inbound Processing (WMS-2)

## 0. Enums

### `ReceiptStatus`
```csharp
public enum ReceiptStatus { DRAFT, PENDING_PUTAWAY, COMPLETED }
```
- **DRAFT**: Initial state after creation. Lines can be edited.
- **PENDING_PUTAWAY**: Confirmed by approver, ready for bin assignment.
- **COMPLETED**: Putaway processed, stock ledger updated.

### `LocationType`
```csharp
public enum LocationType { WAREHOUSE, TRANSIT, SUPPLIER, CUSTOMER, QUARANTINE }
```
- **QUARANTINE**: Auto-assigned for failed QA items during putaway.

## 1. System Architecture
- **Frontend**: Next.js Server Actions for processing form submissions.
- **Backend**: Minimal API endpoints with service layer enforcing business logic (e.g., PO quantity validation, tenant isolation).
- **Database**: PostgreSQL (Npgsql) with explicit `BeginTransactionAsync` to ensure receipt headers, lines, stock ledger entries, and inventory balances are committed atomically.
- **Auth**: JWT Bearer + claim-based authorization with granular permission policies.

## 2. Database Schema

### Table: `purchase_receipts`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `receipt_no` | VARCHAR(50) | UNIQUE, NOT NULL | Auto-generated standard number (`RCP-{yyyyMMdd}-{N:D4}`) |
| `po_reference`| VARCHAR(50) | NOT NULL | External PO number |
| `status` | VARCHAR(20) | NOT NULL | Enum: DRAFT, PENDING_PUTAWAY, COMPLETED (stored as string) |
| `received_by` | VARCHAR(100) | NOT NULL | User ID of receiver |
| `tenant_id` | UUID | NOT NULL, FK, Indexed | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `purchase_receipt_lines`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `receipt_id` | UUID | NOT NULL, FK (CASCADE) | Reference to `purchase_receipts` |
| `item_id` | UUID | NOT NULL, FK (RESTRICT) | Reference to `inventory_items` |
| `ordered_qty`| DECIMAL(18,4) | NOT NULL | Original PO ordered quantity (snapshot) |
| `qty_received`| DECIMAL(18,4) | NOT NULL | Total quantity received |
| `qty_passed` | DECIMAL(18,4) | NOT NULL | Quantity that passed QA inspection |
| `qty_failed` | DECIMAL(18,4) | NOT NULL | Quantity that failed QA inspection |
| `putaway_loc_id`| UUID | FK (SET NULL) | Reference to `locations` (set during putaway) |

### Additional Tables

#### `purchase_orders`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `po_number` | VARCHAR(50) | NOT NULL | PO number (unique per tenant) |
| `supplier_name` | VARCHAR(200) | NOT NULL | Supplier name |
| `po_date` | DATE | NOT NULL | PO issue date |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |

Unique index on `(tenant_id, po_number)`.

#### `purchase_order_lines`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `po_id` | UUID | NOT NULL, FK (CASCADE) | Reference to `purchase_orders` |
| `item_id` | UUID | NOT NULL, FK (RESTRICT) | Reference to `inventory_items` |
| `ordered_qty`| DECIMAL(18,4) | NOT NULL | Quantity ordered |
| `received_qty`| DECIMAL(18,4) | DEFAULT 0 | Cumulative quantity received |

## 3. EF Core Entity Configuration

Purchase receipts and orders are configured via Fluent API in `AppDbContext.OnModelCreating`:

```csharp
// PurchaseReceipt
modelBuilder.Entity<PurchaseReceipt>(entity =>
{
    entity.ToTable("purchase_receipts");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.ReceiptNo).IsUnique();
    entity.Property(e => e.ReceiptNo).HasMaxLength(50).IsRequired();
    entity.Property(e => e.PoReference).HasMaxLength(50).IsRequired();
    entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
    entity.Property(e => e.ReceivedBy).HasMaxLength(100).IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
    entity.HasMany(e => e.Lines).WithOne(e => e.Receipt)
          .HasForeignKey(e => e.ReceiptId).OnDelete(DeleteBehavior.Cascade);
});

// PurchaseReceiptLine
modelBuilder.Entity<PurchaseReceiptLine>(entity =>
{
    entity.ToTable("purchase_receipt_lines");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.OrderedQty).HasColumnType("decimal(18,4)").IsRequired();
    entity.Property(e => e.QtyReceived).HasColumnType("decimal(18,4)").IsRequired();
    entity.Property(e => e.QtyPassed).HasColumnType("decimal(18,4)").IsRequired();
    entity.Property(e => e.QtyFailed).HasColumnType("decimal(18,4)").IsRequired();
    entity.HasOne(e => e.Item).WithMany()
          .HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.PutawayLoc).WithMany()
          .HasForeignKey(e => e.PutawayLocId).OnDelete(DeleteBehavior.SetNull);
});
```

Entity classes are plain C# POCOs. Enums (`ReceiptStatus`, `LocationType`) are stored as strings via `HasConversion<string>()`.

## 4. API Endpoints

All endpoints are registered via extension methods in `Program.cs` and require JWT Bearer authentication with specific permission claims.

### Purchase Order Endpoints (`PurchaseOrderEndpoints.cs`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/api/v1/wms/purchase-orders` | `wms.inbound.create` | Create a new purchase order with lines |
| GET | `/api/v1/wms/purchase-orders` | `WMS:Read` | List POs with search & pagination |
| GET | `/api/v1/wms/purchase-orders/{id}` | `WMS:Read` | Get PO by ID with line details |

### Purchase Receipt Endpoints (`PurchaseReceiptEndpoints.cs`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/api/v1/wms/receipts` | `wms.inbound.create` | Create receipt (DRAFT). Validates PO exists, over-receiving, qty mismatch, tenant check |
| GET | `/api/v1/wms/receipts` | `WMS:Read` | List receipts with status/PO/date filters & pagination |
| GET | `/api/v1/wms/receipts/{id}` | `WMS:Read` | Get receipt by ID with line details |
| POST | `/api/v1/wms/receipts/{id}/confirm` | `wms.inbound.approve` | Confirm receipt: DRAFT → PENDING_PUTAWAY |
| POST | `/api/v1/wms/receipts/{id}/putaway` | `wms.inbound.approve` | Process putaway: assigns bins, creates stock ledger entries (DEBIT/CREDIT), updates balances, marks COMPLETED |

### Receipt Creation (CreateReceiptAsync)
1. Validates PO exists for tenant
2. For each line: checks `passed + failed == received`, item belongs to PO, and no over-receiving beyond PO ordered qty
3. Generates receipt no: `RCP-{yyyyMMdd}-{N:D4}`
4. Sets status to `DRAFT`
5. Logs audit: `CREATE` action on `purchase_receipt`

### Putaway Processing (ProcessPutawayAsync)
Runs inside `BeginTransactionAsync` for atomicity:
1. Validates receipt status is `PENDING_PUTAWAY`
2. Finds `SUPPLIER-TRANSIT` location (credit side)
3. For each line: validates line & location belong to tenant
4. Double-entry stock ledger: DEBIT destination location, CREDIT transit
5. Updates/creates `InventoryBalance` records
6. Auto-routes failed QA qty to `QUARANTINE` location
7. Sets status to `COMPLETED`
8. Updates PO line `ReceivedQty`
9. Logs audit: `PUTAWAY` action
10. Raises `ReceiptProcessed` domain event

## 5. Domain Events

Domain events are raised via the `DomainEventDispatcher` (scoped in-memory collector pattern).

- **Raised**:
  - `ReceiptProcessed(Guid ReceiptId, decimal TotalValue, Guid TenantId)` — Dispatched after successful putaway. Finance module subscribes to create Accounts Payable journal entries.
  - `StockMovement` — Raised by stock ledger service during inventory movements.
  - `StockOutAlert` — Raised when stock balance drops below threshold.

- **Consumed**:
  - `PurchaseOrderCreated` (external integration) — Creates expected receipts (future scope).

## 6. Permissions (RBAC)

Defined as string constants in `Permissions.cs` and auto-registered as authorization policies in `Program.cs`. Each policy checks for the claim `"permissions"` containing the required value, or the `"Admin"` role.

| Permission | Endpoints | Description |
|------------|-----------|-------------|
| `wms.inbound.create` | POST create PO, POST create receipt | Create inbound documents |
| `wms.inbound.approve` | POST confirm receipt, POST putaway | Approve/inspect inbound operations |
| `WMS:Read` | GET all WMS endpoints | View WMS data |
| `WMS:Write` | POST stock-ledger/inventory | Write stock movements |
| `WMS:Admin` | — | Full WMS administrative access |

## 7. Performance Considerations
- Use a single database transaction (`BeginTransactionAsync`) for the Putaway endpoint. Writing to receipt lines, stock ledger entries, and inventory balances must be atomic to prevent orphaned inventory records.
- Pagination with `Skip`/`Take` on list endpoints to prevent large result sets.
- EF Core includes are limited to immediate navigation properties; no deep eager loading beyond 2 levels.
- Composite indexes on `(TenantId, PoNumber)` and `(ItemId, LocationId, TenantId)` for common query patterns.

## 8. Security Considerations
- All service methods filter by `tenant_id` — every query includes `TenantId == tenantId` to enforce multi-tenant data isolation.
- Permission-based endpoint authorization via `[Permissions]` attribute — no unauthenticated access to WMS endpoints.
- Input validation: Item IDs and Location IDs are validated against the user's tenant before any write operation.
- Audit logging with `userId`, `ipAddress`, and `userAgent` for all CREATE, CONFIRM, and PUTAWAY actions.

## 9. Error Handling Strategy
- Application-level validation: `qty_passed + qty_failed == qty_received` enforced in `CreateReceiptAsync` before any DB write.
- Over-receiving prevention: Validates receipt qty against `OrderedQty - ReceivedQty` remaining balance.
- Transaction rollback: `catch` block in `ProcessPutawayAsync` calls `tx.RollbackAsync()` if any ledger insertion or balance update fails.
- Business errors returned as result objects (`ReceiptCreateResult`, `ReceiptActionResult`) with string error codes — no exceptions for expected validation failures.
- Tenant isolation: all queries include tenant filter — cross-tenant access returns "not found" rather than authorization error to avoid leaking existence info.

## 10. Seed Data (`WmsDataSeeder.cs`)

Seeded during application startup via `DataSeeder.SeedAsync()`:

| Entity | Data |
|--------|------|
| **Locations** | `SUPPLIER-TRANSIT` (TRANSIT), `WH-MAIN` (WAREHOUSE), `QUARANTINE` (QUARANTINE) |
| **Inventory Items** | `SKU-001` (Safety Helmet), `SKU-002` (Work Gloves) |
| **Purchase Order** | `PO-9999` — 100x SKU-001, Supplier: "Seed Supplier" |

## 11. Service Layer

### PurchaseOrderService
| Method | Description |
|--------|-------------|
| `CreatePoAsync` | Creates PO with lines. Checks for duplicate PO number per tenant. Logs audit. |
| `GetPoByIdAsync` | Returns PO with line details including item SKU/Name and open qty. |
| `GetPoListAsync` | Paginated list with search by PO number or supplier name. |

### PurchaseReceiptService
| Method | Description |
|--------|-------------|
| `CreateReceiptAsync` | Validates PO, qty constraints, item ownership. Creates receipt in DRAFT. Generates receipt no. |
| `GetReceiptAsync` | Single receipt with lines, item details, and putaway location. |
| `GetReceiptListAsync` | Paginated list with status/PO/date filters. |
| `ConfirmReceiptAsync` | DRAFT → PENDING_PUTAWAY. Validates receipt exists and is in DRAFT status. |
| `ProcessPutawayAsync` | Atomic putaway: double-entry stock ledger, inventory balances, PO received qty update, ReceiptProcessed event. |
