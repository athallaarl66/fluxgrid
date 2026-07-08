import { useQuery } from "@tanstack/react-query";
import type { LedgerEntryResponse, BalanceResponse } from "@/lib/wms-types";
import { getStockLedger, getInventoryBalance } from "@/lib/wms-api";
import type { StockLedgerParams } from "@/lib/wms-api";

export const WMS_LEDGER_KEY = "wms-stock-ledger";
export const WMS_BALANCE_KEY = "wms-inventory-balance";

export function useStockLedger(params: StockLedgerParams) {
  return useQuery<LedgerEntryResponse>({
    queryKey: [WMS_LEDGER_KEY, params],
    queryFn: () => getStockLedger(params),
  });
}

export function useInventoryBalance(itemId: string, locationId: string) {
  return useQuery<BalanceResponse>({
    queryKey: [WMS_BALANCE_KEY, itemId, locationId],
    queryFn: () => getInventoryBalance(itemId, locationId),
    enabled: !!itemId && !!locationId,
  });
}
