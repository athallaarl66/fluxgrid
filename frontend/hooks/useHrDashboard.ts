import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { HrDashboardResponse } from "@/lib/hr-types";

export function useHrDashboard() {
  return useQuery<HrDashboardResponse>({
    queryKey: ["hr-dashboard"],
    queryFn: () => apiClient<HrDashboardResponse>("/api/v1/hr/dashboard"),
  });
}
