import { apiClient } from "@/lib/api-client";
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  BudgetVsActualRow,
  PaginatedResult,
} from "@/lib/budget-types";

export function getBudgets(params?: {
  periodId?: string;
  accountId?: string;
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams();
  if (params?.periodId) searchParams.set("periodId", params.periodId);
  if (params?.accountId) searchParams.set("accountId", params.accountId);
  if (params?.page) searchParams.set("page", String(params.page));
  if (params?.pageSize) searchParams.set("pageSize", String(params.pageSize));
  const qs = searchParams.toString();
  return apiClient<PaginatedResult<BudgetResponse>>(
    `/api/v1/finance/budgets${qs ? `?${qs}` : ""}`,
  );
}

export function getBudgetById(id: string) {
  return apiClient<BudgetResponse>(`/api/v1/finance/budgets/${id}`);
}

export function createBudget(data: CreateBudgetRequest) {
  return apiClient<BudgetResponse>("/api/v1/finance/budgets", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function updateBudget(id: string, data: UpdateBudgetRequest) {
  return apiClient<BudgetResponse>(`/api/v1/finance/budgets/${id}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function deleteBudget(id: string) {
  return apiClient<void>(`/api/v1/finance/budgets/${id}`, {
    method: "DELETE",
  });
}

export function getBudgetReport(periodId: string) {
  return apiClient<BudgetVsActualRow[]>(
    `/api/v1/finance/budgets/report?periodId=${periodId}`,
  );
}
