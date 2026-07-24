import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface TransferEntry {
  transactionId: string;
  itemId: string;
  fromLocationId: string | null;
  toLocationId: string | null;
  quantity: number;
  unitCost: number;
  totalValue: number;
  createdAt: string;
}

export interface TransferListResponse {
  transfers: TransferEntry[];
  total: number;
  page: number;
  pageSize: number;
}

export function useTransfers(params: {
  fromLocationId?: string;
  toLocationId?: string;
  itemId?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery<TransferListResponse>({
    queryKey: ["transfers", params],
    queryFn: () => {
      const q = new URLSearchParams();
      if (params.fromLocationId) q.set("fromLocationId", params.fromLocationId);
      if (params.toLocationId) q.set("toLocationId", params.toLocationId);
      if (params.itemId) q.set("itemId", params.itemId);
      if (params.dateFrom) q.set("dateFrom", params.dateFrom);
      if (params.dateTo) q.set("dateTo", params.dateTo);
      if (params.page) q.set("page", String(params.page));
      if (params.pageSize) q.set("pageSize", String(params.pageSize));
      return apiClient<TransferListResponse>(
        `/api/v1/wms/stock-ledger/transfers?${q.toString()}`,
      );
    },
  });
}
