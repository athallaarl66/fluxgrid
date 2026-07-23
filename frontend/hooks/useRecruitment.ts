import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { CandidateListItem, CandidateDetail, UploadUrlResponse, CreateCandidateRequest, PaginatedResponse, ApproveCandidateResponse, RejectCandidateResponse, JobPosting, CreateJobRequest, UpdateJobRequest, PublishJobResponse, JobMatchResponse, MatchReasoningResponse } from "@/lib/hr-types";

export interface CandidateUpdateData {
  name: string;
  email: string;
  phone?: string;
  location?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  portfolioUrl?: string;
  summary?: string;
  totalExperienceMonths?: number;
  expectedSalaryMin?: number;
  expectedSalaryMax?: number;
  noticePeriodDays?: number;
  education?: { id?: string; institution: string; degree: string; fieldOfStudy?: string; startDate?: string; endDate?: string; gpa?: number }[];
  experience?: { id?: string; company: string; role: string; startDate?: string; endDate?: string; isCurrent: boolean; description?: string; location?: string }[];
  skills?: string[];
}

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
    mutationFn: ({ id, data }: { id: string; data?: CandidateUpdateData }) =>
      apiClient<ApproveCandidateResponse>(`/api/v1/hr/recruitment/candidates/${id}/approve`, {
        method: "PUT",
        body: data ? JSON.stringify(data) : undefined,
        headers: data ? { "Content-Type": "application/json" } : undefined,
      }),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ["candidate", id] });
      queryClient.invalidateQueries({ queryKey: ["candidate-review", id] });
      queryClient.invalidateQueries({ queryKey: ["candidates"] });
    },
  });
}

export function useUpdateCandidate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CandidateUpdateData }) =>
      apiClient<CandidateDetail>(`/api/v1/hr/recruitment/candidates/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ["candidate", id] });
      queryClient.invalidateQueries({ queryKey: ["candidate-review", id] });
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

// ─── Job Posting Hooks ─────────────────────────────────────────────────────

export function useJobList(params: {
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

  return useQuery<PaginatedResponse<JobPosting>>({
    queryKey: ["jobs", params],
    queryFn: () => apiClient<PaginatedResponse<JobPosting>>(`/api/v1/hr/recruitment/jobs?${searchParams.toString()}`),
  });
}

export function useJob(id: string) {
  return useQuery<JobPosting>({
    queryKey: ["job", id],
    queryFn: () => apiClient<JobPosting>(`/api/v1/hr/recruitment/jobs/${id}`),
    enabled: !!id,
  });
}

export function useCreateJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateJobRequest) =>
      apiClient<JobPosting>("/api/v1/hr/recruitment/jobs", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
    },
  });
}

export function useUpdateJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateJobRequest }) =>
      apiClient<JobPosting>(`/api/v1/hr/recruitment/jobs/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ["job", id] });
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
    },
  });
}

export function useDeleteJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<void>(`/api/v1/hr/recruitment/jobs/${id}`, {
        method: "DELETE",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
    },
  });
}

export function usePublishJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<PublishJobResponse>(`/api/v1/hr/recruitment/jobs/${id}/publish`, {
        method: "POST",
      }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["job", id] });
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
    },
  });
}

export function useCloseJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient<PublishJobResponse>(`/api/v1/hr/recruitment/jobs/${id}/close`, {
        method: "POST",
      }),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["job", id] });
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
    },
  });
}

export function useJobMatches(jobId: string, minScore?: number, limit?: number) {
  const searchParams = new URLSearchParams();
  if (minScore !== undefined) searchParams.set("minScore", String(minScore));
  if (limit !== undefined) searchParams.set("limit", String(limit));

  return useQuery<JobMatchResponse>({
    queryKey: ["job-matches", jobId, minScore, limit],
    queryFn: () => apiClient<JobMatchResponse>(`/api/v1/hr/recruitment/jobs/${jobId}/matches?${searchParams.toString()}`),
    enabled: !!jobId,
  });
}

export function useMatchReasoning() {
  return useMutation({
    mutationFn: ({ jobId, candidateId }: { jobId: string; candidateId: string }) =>
      apiClient<MatchReasoningResponse>(`/api/v1/hr/recruitment/jobs/${jobId}/matches/${candidateId}/reasoning`, {
        method: "POST",
      }),
  });
}
