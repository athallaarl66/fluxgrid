import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  BudgetVsActualRow,
  PaginatedResult,
} from "@/lib/budget-types";
import {
  getBudgets,
  createBudget,
  updateBudget,
  deleteBudget,
  getBudgetReport,
} from "@/lib/budget-api";

export function useBudgets(params?: {
  periodId?: string;
  accountId?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery<PaginatedResult<BudgetResponse>>({
    queryKey: ["budgets", params],
    queryFn: () => getBudgets(params),
  });
}

export function useBudgetReport(periodId: string | undefined) {
  return useQuery<BudgetVsActualRow[]>({
    queryKey: ["budgets", "report", periodId],
    queryFn: () => getBudgetReport(periodId!),
    enabled: !!periodId,
  });
}

export function useCreateBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBudgetRequest) =>
      apiClient<BudgetResponse>("/api/v1/finance/budgets", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["budgets"] }),
  });
}

export function useUpdateBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBudgetRequest }) =>
      apiClient<BudgetResponse>(`/api/v1/finance/budgets/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["budgets"] }),
  });
}

export function useDeleteBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<void>(`/api/v1/finance/budgets/${id}`, {
        method: "DELETE",
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["budgets"] }),
  });
}
