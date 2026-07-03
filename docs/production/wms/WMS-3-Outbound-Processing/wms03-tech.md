# Technical Specifications: Outbound Processing (WMS-3)

## 1. System Architecture
- **Frontend**: Next.js Client Components (React state for interactive picking UI).
- **Backend**: API Routes managing the state machine for an Order (Pending -> Reserved -> Picked -> Packed -> Shipped).
- **Database**: PostgreSQL (Neon) with Drizzle ORM.
- **Stock Allocation Engine**: A backend service utility that calculates available stock by querying the `stock_ledger` and subtracting already `reserved` quantities.

## 2. Database Schema

### Table: `sales_orders`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `order_no` | VARCHAR(50) | UNIQUE, NOT NULL | Sales Order Number |
| `status` | VARCHAR(50) | NOT NULL | Enum: PENDING, RESERVED, PICKING, PACKED, SHIPPED |
| `customer_id` | UUID | NOT NULL | Reference to external Customer |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |

### Table: `sales_order_lines`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `order_id` | UUID | NOT NULL, FK | Reference to `sales_orders` |
| `item_id` | UUID | NOT NULL, FK | Reference to `inventory_items` |
| `qty_ordered` | DECIMAL | NOT NULL | |
| `qty_reserved`| DECIMAL | NOT NULL | Stock allocated but not shipped |
| `qty_picked` | DECIMAL | NOT NULL | |
| `qty_shipped` | DECIMAL | NOT NULL | |

### Table: `pick_lists`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `order_id` | UUID | NOT NULL, FK | Reference to `sales_orders` |
| `status` | VARCHAR(50) | NOT NULL | Enum: GENERATED, IN_PROGRESS, COMPLETED |
| `assigned_to` | UUID | FK | Reference to `users` |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, decimal, timestamp } from "drizzle-orm/pg-core";
import { inventoryItems } from "./wms01"; 

export const salesOrders = pgTable("sales_orders", {
  id: uuid("id").primaryKey().defaultRandom(),
  orderNo: varchar("order_no", { length: 50 }).notNull().unique(),
  status: varchar("status", { length: 50 }).notNull().default("PENDING"),
  customerId: uuid("customer_id").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});

export const salesOrderLines = pgTable("sales_order_lines", {
  id: uuid("id").primaryKey().defaultRandom(),
  orderId: uuid("order_id").references(() => salesOrders.id).notNull(),
  itemId: uuid("item_id").references(() => inventoryItems.id).notNull(),
  qtyOrdered: decimal("qty_ordered").notNull(),
  qtyReserved: decimal("qty_reserved").notNull().default('0'),
  qtyPicked: decimal("qty_picked").notNull().default('0'),
  qtyShipped: decimal("qty_shipped").notNull().default('0'),
});
```

## 4. API Endpoints

### POST `/api/v1/wms/pick-lists`
- **Description**: Generate a pick list for a given Sales Order.
- **Action**: 
  1. Checks real-time stock availability.
  2. If sufficient, updates `qty_reserved` on the order line.
  3. Creates `pick_lists` record.

### PUT `/api/v1/wms/pick-lists/{id}/pick`
- **Description**: Update picked quantities during execution.

### POST `/api/v1/wms/shipments`
- **Description**: Confirm shipment.
- **Action**:
  1. Validates that `qty_picked` matches `qty_ordered` (unless explicitly adjusted).
  2. Updates order status to SHIPPED.
  3. Writes to `stock_ledger` (Credit Warehouse, Debit Customer/Transit) using the exact quantities.
  4. Removes the `qty_reserved` constraint.
  5. Publishes `ShipmentProcessed` event.

## 5. Domain Events
- **Raised**:
  - `ShipmentProcessed` (OrderID, Total Value, COGS) -> Sent to Finance.
- **Consumed**:
  - `SalesOrderCreated` (External) -> Drops order into the pending queue.

## 6. Permissions (RBAC)
- `wms.outbound.process`: Allows a user to perform pick/pack/ship operations.

## 7. Performance Considerations
- The stock allocation query must calculate available stock: `(Sum of Ledger IN) - (Sum of Ledger OUT) - (Sum of qty_reserved across all active order lines)`. This should be optimized via an indexed materialized view or Redis cache.

## 8. Security Considerations
- Ensure transactions modifying `qty_reserved` and the `stock_ledger` are atomic.

## 9. Error Handling
- Return `422 Unprocessable Entity` if a user attempts to generate a Pick List but `Available Stock < qty_ordered`.

## 10. Seed Data Examples
- Seed a dummy Sales Order `SO-123` to test the outbound queue.
