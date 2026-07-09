import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getPurchaseOrders,
  getPurchaseOrder,
  getReceipts,
  getReceipt,
  createReceipt,
  confirmReceipt,
  submitPutaway,
} from "@/lib/wms-api";
import type { PoListParams, ReceiptListParams } from "@/lib/wms-api";

export const PO_KEY = "purchase-orders";
export const RCP_KEY = "purchase-receipts";

export function usePurchaseOrders(params: PoListParams = {}) {
  return useQuery({
    queryKey: [PO_KEY, params],
    queryFn: () => getPurchaseOrders(params),
  });
}

export function usePurchaseOrder(id: string) {
  return useQuery({
    queryKey: [PO_KEY, id],
    queryFn: () => getPurchaseOrder(id),
    enabled: !!id,
  });
}

export function useReceipts(params: ReceiptListParams = {}) {
  return useQuery({
    queryKey: [RCP_KEY, params],
    queryFn: () => getReceipts(params),
  });
}

export function useReceipt(id: string) {
  return useQuery({
    queryKey: [RCP_KEY, id],
    queryFn: () => getReceipt(id),
    enabled: !!id,
  });
}

export function useCreateReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: createReceipt,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [RCP_KEY] });
      qc.invalidateQueries({ queryKey: [PO_KEY] });
    },
  });
}

export function useConfirmReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: confirmReceipt,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [RCP_KEY] });
    },
  });
}

export function useSubmitPutaway() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, lines }: { id: string; lines: { lineId: string; locationId: string }[] }) =>
      submitPutaway(id, { lines }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [RCP_KEY] });
    },
  });
}
