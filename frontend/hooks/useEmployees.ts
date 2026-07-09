import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { Employee, EmployeeDetail, CreateEmployeeRequest, UpdateEmployeeRequest, PaginatedResponse } from "@/lib/hr-types";

export function useEmployeeList(params: {
  search?: string;
  departmentId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams();
  if (params.search) searchParams.set("search", params.search);
  if (params.departmentId) searchParams.set("department_id", params.departmentId);
  if (params.status) searchParams.set("status", params.status);
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("page_size", String(params.pageSize));

  return useQuery<PaginatedResponse<Employee>>({
    queryKey: ["employees", params],
    queryFn: () => apiClient<PaginatedResponse<Employee>>(`/api/v1/hr/employees?${searchParams.toString()}`),
  });
}

export function useEmployee(id: string) {
  return useQuery<EmployeeDetail>({
    queryKey: ["employee", id],
    queryFn: () => apiClient<EmployeeDetail>(`/api/v1/hr/employees/${id}`),
    enabled: !!id,
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEmployeeRequest) =>
      apiClient<EmployeeDetail>("/api/v1/hr/employees", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["employees"] });
    },
  });
}

export function useUpdateEmployee(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateEmployeeRequest) =>
      apiClient<EmployeeDetail>(`/api/v1/hr/employees/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["employees"] });
      queryClient.invalidateQueries({ queryKey: ["employee", id] });
    },
  });
}

export function useTerminateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) =>
      apiClient<void>(`/api/v1/hr/employees/${id}/terminate`, {
        method: "POST",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["employees"] });
    },
  });
}
