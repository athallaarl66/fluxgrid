"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getSalesOrders,
  getSalesOrder,
  getPickList,
  getShipments,
  createSalesOrder,
  cancelSalesOrder,
  generatePickList,
  executePickItems,
  verifyPacking,
  confirmShipment,
} from "@/lib/wms-api";
import type { SoListParams, ShipListParams } from "@/lib/wms-api";

export const SO_KEY = "sales-orders";
export const PL_KEY = "pick-lists";
export const SHP_KEY = "shipments";

export function useSalesOrders(params: SoListParams = {}) {
  return useQuery({
    queryKey: [SO_KEY, params],
    queryFn: () => getSalesOrders(params),
  });
}

export function useSalesOrder(id: string) {
  return useQuery({
    queryKey: [SO_KEY, id],
    queryFn: () => getSalesOrder(id),
    enabled: !!id,
  });
}

export function usePickList(id: string) {
  return useQuery({
    queryKey: [PL_KEY, id],
    queryFn: () => getPickList(id),
    enabled: !!id,
  });
}

export function useShipments(params: ShipListParams = {}) {
  return useQuery({
    queryKey: [SHP_KEY, params],
    queryFn: () => getShipments(params),
  });
}

export function useCreateSalesOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: createSalesOrder,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SO_KEY] });
    },
  });
}

export function useCancelSalesOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cancelSalesOrder,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SO_KEY] });
    },
  });
}

export function useGeneratePickList() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: generatePickList,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SO_KEY] });
      qc.invalidateQueries({ queryKey: [PL_KEY] });
    },
  });
}

export function useExecutePick() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: { items: { itemId: string; qtyPicked: number; shortPickReason?: string | null }[] } }) =>
      executePickItems(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PL_KEY] });
      qc.invalidateQueries({ queryKey: [SO_KEY] });
    },
  });
}

export function useVerifyPacking() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: verifyPacking,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SO_KEY] });
    },
  });
}

export function useConfirmShipment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: confirmShipment,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SHP_KEY] });
      qc.invalidateQueries({ queryKey: [SO_KEY] });
    },
  });
}
