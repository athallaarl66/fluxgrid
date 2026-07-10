import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { PayrollRun, PayrollRunDetail, PayrollRecord, CreatePayrollRequest, PaginatedResponse } from "@/lib/hr-types";

export function usePayrollRunList(params: {
  status?: string;
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams();
  if (params.status) searchParams.set("status", params.status);
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));

  return useQuery<PaginatedResponse<PayrollRun>>({
    queryKey: ["payroll-runs", params],
    queryFn: () => apiClient<PaginatedResponse<PayrollRun>>(`/api/v1/hr/payroll/runs?${searchParams.toString()}`),
  });
}

export function usePayrollRun(id: string) {
  return useQuery<PayrollRunDetail>({
    queryKey: ["payroll-run", id],
    queryFn: () => apiClient<PayrollRunDetail>(`/api/v1/hr/payroll/${id}`),
    enabled: !!id,
  });
}

export function useCalculatePayroll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreatePayrollRequest) =>
      apiClient<PayrollRun>("/api/v1/hr/payroll/calculate", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["payroll-runs"] }); },
  });
}

export function useFinalizePayroll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<PayrollRun>(`/api/v1/hr/payroll/${id}/finalize`, { method: "PUT" }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["payroll-runs"] });
      queryClient.invalidateQueries({ queryKey: ["payroll-run", id] });
    },
  });
}

export function useRecalculatePayroll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<PayrollRun>(`/api/v1/hr/payroll/${id}/recalculate`, { method: "PUT" }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["payroll-runs"] });
      queryClient.invalidateQueries({ queryKey: ["payroll-run", id] });
    },
  });
}

export function useMyPayslips() {
  return useQuery<PayrollRecord[]>({
    queryKey: ["my-payslips"],
    queryFn: () => apiClient<PayrollRecord[]>("/api/v1/hr/payroll/my-payslips"),
  });
}
