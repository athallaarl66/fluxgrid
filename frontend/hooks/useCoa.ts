import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from "@/lib/coa-types";

const COA_KEY = ["coa"] as const;

export function useCoaTree() {
  return useQuery<AccountResponse[]>({
    queryKey: COA_KEY,
    queryFn: () => apiClient<AccountResponse[]>("/api/v1/finance/chart-of-accounts"),
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAccountRequest) =>
      apiClient<AccountResponse>("/api/v1/finance/chart-of-accounts", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: COA_KEY }),
  });
}

export function useUpdateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAccountRequest }) =>
      apiClient<AccountResponse>(`/api/v1/finance/chart-of-accounts/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: COA_KEY }),
  });
}

export function useDeactivateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<AccountResponse>(
        `/api/v1/finance/chart-of-accounts/${id}`,
        { method: "DELETE" },
      ),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: COA_KEY }),
  });
}
