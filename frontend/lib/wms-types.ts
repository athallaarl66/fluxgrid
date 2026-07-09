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
