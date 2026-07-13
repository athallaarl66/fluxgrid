import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { CandidateListItem, CandidateDetail, UploadUrlResponse, CreateCandidateRequest, PaginatedResponse, ApproveCandidateResponse, RejectCandidateResponse } from "@/lib/hr-types";

export function useCandidateList(params: {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams();
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));

  return useQuery<PaginatedResponse<CandidateListItem>>({
    queryKey: ["candidates", params],
    queryFn: () => apiClient<PaginatedResponse<CandidateListItem>>(`/api/v1/hr/recruitment/candidates?${searchParams.toString()}`),
  });
}

export function useCandidate(id: string) {
  return useQuery<CandidateDetail>({
    queryKey: ["candidate", id],
    queryFn: () => apiClient<CandidateDetail>(`/api/v1/hr/recruitment/candidates/${id}`),
    enabled: !!id,
  });
}

export function useUploadCv() {
  return useMutation({
    mutationFn: (data: { fileName: string; fileType: string; fileSize: number; fileHash: string }) =>
      apiClient<UploadUrlResponse>("/api/v1/hr/recruitment/upload-url", {
        method: "POST",
        body: JSON.stringify(data),
      }),
  });
}

export function useCreateCandidate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCandidateRequest) =>
      apiClient<{ id: string }>("/api/v1/hr/recruitment/candidates", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["candidates"] });
    },
  });
}

export function useApproveCandidate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<ApproveCandidateResponse>(`/api/v1/hr/recruitment/candidates/${id}/approve`, {
        method: "PUT",
      }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["candidate", id] });
      queryClient.invalidateQueries({ queryKey: ["candidates"] });
    },
  });
}

export function useRejectCandidate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<RejectCandidateResponse>(`/api/v1/hr/recruitment/candidates/${id}/reject`, {
        method: "PUT",
      }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["candidate", id] });
      queryClient.invalidateQueries({ queryKey: ["candidates"] });
    },
  });
}

export function useDeleteCandidate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<void>(`/api/v1/hr/recruitment/candidates/${id}`, {
        method: "DELETE",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["candidates"] });
    },
  });
}

export function useCandidateReview(id: string) {
  return useQuery<CandidateDetail>({
    queryKey: ["candidate-review", id],
    queryFn: () => apiClient<CandidateDetail>(`/api/v1/hr/recruitment/candidates/${id}`),
    enabled: !!id,
  });
}
