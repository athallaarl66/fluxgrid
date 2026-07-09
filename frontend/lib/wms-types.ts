export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  uom: string;
  tenantId: string;
}

export interface Location {
  id: string;
  code: string;
  type: "WAREHOUSE" | "TRANSIT" | "SUPPLIER" | "CUSTOMER" | "QUARANTINE";
  tenantId: string;
}

export interface PurchaseOrder {
  id: string;
  poNumber: string;
  supplierName: string;
  poDate: string;
  lines: PurchaseOrderLine[];
  tenantId: string;
}

export interface PurchaseOrderLine {
  id: string;
  itemId: string;
  itemSku: string | null;
  itemName: string | null;
  orderedQty: number;
  receivedQty: number;
  openQty: number;
}

export interface PurchaseReceipt {
  id: string;
  receiptNo: string;
  poReference: string;
  status: string;
  receivedBy: string;
  createdAt: string;
  lines: PurchaseReceiptLine[];
  tenantId: string;
}

export interface PurchaseReceiptLine {
  id: string;
  itemId: string;
  itemSku: string | null;
  itemName: string | null;
  orderedQty: number;
  qtyReceived: number;
  qtyPassed: number;
  qtyFailed: number;
  putawayLocId: string | null;
  putawayLocCode: string | null;
}

export interface StockLedgerEntry {
  id: string;
  transactionId: string;
  itemId: string;
  locationId: string;
  quantity: number;
  unitCost: number;
  referenceType: string;
  referenceId: string;
  tenantId: string;
  createdAt: string;
}

export interface InventoryBalance {
  id: string;
  itemId: string;
  locationId: string;
  balanceQty: number;
  balanceValue: number;
  tenantId: string;
  updatedAt: string;
}

export interface CreateMovementRequest {
  entries: {
    itemId: string;
    locationId: string;
    quantity: number;
    unitCost: number;
    referenceType: string;
    referenceId: string;
  }[];
}

export interface LedgerEntryResponse {
  items: StockLedgerEntry[];
  total: number;
  page: number;
  pageSize: number;
}

export interface BalanceResponse {
  itemId: string;
  locationId: string;
  balanceQty: number;
  balanceValue: number;
}

export interface CostLayerDto {
  entryId: string;
  quantity: number;
  unitCost: number;
  createdAt: string;
}

export interface ValuationResponse {
  method: string;
  averageCost: number;
  totalQuantity: number;
  totalValue: number;
  layers: CostLayerDto[] | null;
}

export interface SalesOrder {
  id: string;
  orderNo: string;
  status: string;
  customerId: string;
  customerName: string;
  notes: string | null;
  createdAt: string;
  lines: SalesOrderLine[];
  tenantId: string;
}

export interface SalesOrderLine {
  id: string;
  itemId: string;
  itemSku: string | null;
  itemName: string | null;
  qtyOrdered: number;
  qtyReserved: number;
  qtyPicked: number;
  qtyShipped: number;
}

export interface SoListResult {
  items: SalesOrder[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PickList {
  id: string;
  orderId: string;
  orderNo: string | null;
  status: string;
  assignedTo: string | null;
  createdAt: string;
  items: PickListItem[];
  tenantId: string;
}

export interface PickListItem {
  id: string;
  orderLineId: string;
  itemId: string;
  itemSku: string | null;
  itemName: string | null;
  locationId: string | null;
  locationCode: string | null;
  qtyExpected: number;
  qtyPicked: number;
  shortPickReason: string | null;
}

export interface Shipment {
  id: string;
  shipmentNo: string;
  orderId: string;
  orderNo: string | null;
  status: string;
  shippedAt: string | null;
  tenantId: string;
}

export interface ShipListResult {
  items: Shipment[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SoCreateRequest {
  orderNo: string;
  customerId: string;
  customerName: string;
  notes?: string;
  lines: { itemId: string; qtyOrdered: number }[];
}

export interface PickExecuteRequest {
  items: { itemId: string; qtyPicked: number; shortPickReason?: string | null }[];
}

export interface VerifyRequest {
  orderId: string;
  lines: { itemId: string; verifiedQty: number }[];
}
