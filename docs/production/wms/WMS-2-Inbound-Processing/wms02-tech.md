# Technical Specifications: Inbound Processing (WMS-2)

## 1. System Architecture
- **Frontend**: Next.js Server Actions for processing form submissions.
- **Backend**: API Routes enforcing business logic (e.g., PO quantity validation).
- **Database**: PostgreSQL (Neon) with transaction wrapping to ensure receipt headers, lines, and stock ledger entries are committed atomically.

## 2. Database Schema

### Table: `purchase_receipts`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `receipt_no` | VARCHAR(50) | UNIQUE, NOT NULL | Auto-generated standard number |
| `po_reference`| VARCHAR(50) | NOT NULL | External PO number |
| `status` | VARCHAR(50) | NOT NULL | Enum: DRAFT, PENDING_PUTAWAY, COMPLETED |
| `received_by` | UUID | NOT NULL, FK | Reference to `users` |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `purchase_receipt_lines`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `receipt_id` | UUID | NOT NULL, FK | Reference to `purchase_receipts` |
| `item_id` | UUID | NOT NULL, FK | Reference to `inventory_items` |
| `qty_received`| DECIMAL | NOT NULL | |
| `qty_passed` | DECIMAL | NOT NULL | |
| `qty_failed` | DECIMAL | NOT NULL | |
| `putaway_loc_id`| UUID | FK | Reference to `locations` (set during putaway) |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, decimal, timestamp } from "drizzle-orm/pg-core";
import { inventoryItems, locations } from "./wms01"; // References

export const purchaseReceipts = pgTable("purchase_receipts", {
  id: uuid("id").primaryKey().defaultRandom(),
  receiptNo: varchar("receipt_no", { length: 50 }).notNull().unique(),
  poReference: varchar("po_reference", { length: 50 }).notNull(),
  status: varchar("status", { length: 50 }).notNull().default("PENDING_PUTAWAY"),
  receivedBy: uuid("received_by").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const purchaseReceiptLines = pgTable("purchase_receipt_lines", {
  id: uuid("id").primaryKey().defaultRandom(),
  receiptId: uuid("receipt_id").references(() => purchaseReceipts.id).notNull(),
  itemId: uuid("item_id").references(() => inventoryItems.id).notNull(),
  qtyReceived: decimal("qty_received").notNull(),
  qtyPassed: decimal("qty_passed").notNull(),
  qtyFailed: decimal("qty_failed").notNull(),
  putawayLocId: uuid("putaway_loc_id").references(() => locations.id),
});
```

## 4. API Endpoints

### POST `/api/v1/wms/purchase-receipts`
- **Description**: Create the initial receipt.
- **Request Body**: PO Ref, array of line items (item ID, qty received, passed, failed).
- **Action**: Creates `purchase_receipts` header and lines. Sets status to PENDING_PUTAWAY. 

### POST `/api/v1/wms/purchase-receipts/{id}/putaway`
- **Description**: Confirm putaway locations.
- **Request Body**: Array mapping line item IDs to `putawayLocId`.
- **Action**: 
  1. Updates lines with location IDs.
  2. Updates status to COMPLETED.
  3. Inserts records into `stock_ledger` (Debit assigned location, Credit Supplier).
  4. Dispatches `ReceiptProcessed` domain event.

## 5. Domain Events
- **Raised**:
  - `ReceiptProcessed` (ReceiptID, Total Value) -> Finance module listens to this to create Accounts Payable journal entries.
- **Consumed**:
  - `PurchaseOrderCreated` (External) -> Creates expected receipts (if applicable based on integration scope).

## 6. Permissions (RBAC)
- `wms.inbound.create`: Required to access the receive forms.
- `wms.inbound.approve`: (Optional) Required if quantity exceeds PO limit.

## 7. Performance Considerations
- Use a single database transaction for the Putaway endpoint. Writing to the receipt table and the stock ledger table simultaneously must be atomic to prevent orphaned inventory records.

## 8. Security Considerations
- Validate all incoming item IDs and location IDs to ensure they belong to the user's `tenant_id`.

## 9. Error Handling Strategy
- Check constraint: Ensure `qty_passed + qty_failed == qty_received` at the database level using a CHECK constraint or Drizzle validation.
- Rollback transaction completely if any ledger insertion fails.

## 10. Seed Data Examples
- Seed a dummy PO number `PO-9999` to allow testing of the receipt creation without relying on an external system initially.
