import { apiClient } from "./api-client";
import type { LedgerEntryResponse, BalanceResponse, PurchaseOrder, PurchaseReceipt } from "./wms-types";

export interface StockLedgerParams {
  sku?: string;
  locationId?: string;
  locationCode?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

export function getStockLedger(params: StockLedgerParams = {}) {
  const searchParams = new URLSearchParams();
  if (params.sku) searchParams.set("sku", params.sku);
  if (params.locationId) searchParams.set("locationId", params.locationId);
  if (params.locationCode) searchParams.set("locationCode", params.locationCode);
  if (params.startDate) searchParams.set("startDate", params.startDate);
  if (params.endDate) searchParams.set("endDate", params.endDate);
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  const qs = searchParams.toString();
  return apiClient<LedgerEntryResponse>(
    `/api/v1/wms/stock-ledger${qs ? `?${qs}` : ""}`,
  );
}

export function getInventoryBalance(itemId: string, locationId: string) {
  return apiClient<BalanceResponse>(
    `/api/v1/wms/inventory/balance?itemId=${itemId}&locationId=${locationId}`,
  );
}

export interface PoListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

export function getPurchaseOrders(params: PoListParams = {}) {
  const sp = new URLSearchParams();
  if (params.search) sp.set("search", params.search);
  if (params.page) sp.set("page", String(params.page));
  if (params.pageSize) sp.set("pageSize", String(params.pageSize));
  const qs = sp.toString();
  return apiClient<{ items: PurchaseOrder[]; total: number; page: number; pageSize: number }>(
    `/api/v1/wms/purchase-orders${qs ? `?${qs}` : ""}`,
  );
}

export function getPurchaseOrder(id: string) {
  return apiClient<PurchaseOrder>(`/api/v1/wms/purchase-orders/${id}`);
}

export interface ReceiptListParams {
  status?: string;
  poReference?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

export function getReceipts(params: ReceiptListParams = {}) {
  const sp = new URLSearchParams();
  if (params.status) sp.set("status", params.status);
  if (params.poReference) sp.set("poReference", params.poReference);
  if (params.startDate) sp.set("startDate", params.startDate);
  if (params.endDate) sp.set("endDate", params.endDate);
  if (params.page) sp.set("page", String(params.page));
  if (params.pageSize) sp.set("pageSize", String(params.pageSize));
  const qs = sp.toString();
  return apiClient<{ items: PurchaseReceipt[]; total: number; page: number; pageSize: number }>(
    `/api/v1/wms/receipts${qs ? `?${qs}` : ""}`,
  );
}

export function getReceipt(id: string) {
  return apiClient<PurchaseReceipt>(`/api/v1/wms/receipts/${id}`);
}

export function createReceipt(data: {
  poReference: string;
  receivedBy: string;
  lines: { itemId: string; qtyReceived: number; qtyPassed: number; qtyFailed: number }[];
}) {
  return apiClient<{ success: boolean; receiptId: string; error: string | null }>("/api/v1/wms/receipts", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function confirmReceipt(id: string) {
  return apiClient<{ success: boolean; error: string | null }>(`/api/v1/wms/receipts/${id}/confirm`, {
    method: "POST",
  });
}

export function submitPutaway(id: string, data: { lines: { lineId: string; locationId: string }[] }) {
  return apiClient<{ success: boolean; error: string | null }>(`/api/v1/wms/receipts/${id}/putaway`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}
