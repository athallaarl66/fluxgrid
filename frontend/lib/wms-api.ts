import { apiClient } from "./api-client";
import type { LedgerEntryResponse, BalanceResponse } from "./wms-types";

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
