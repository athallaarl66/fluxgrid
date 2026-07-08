import { apiClient } from "@/lib/api-client";
import type { DashboardResponse } from "@/lib/dashboard-types";

export function getFinanceDashboard(year?: number) {
  const params = year ? `?year=${year}` : "";
  return apiClient<DashboardResponse>(`/api/v1/finance/dashboard${params}`);
}
