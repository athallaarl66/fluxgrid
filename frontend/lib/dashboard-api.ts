import { apiClient } from "@/lib/api-client";
import type { DashboardResponse } from "@/lib/dashboard-types";

export function getFinanceDashboard() {
  return apiClient<DashboardResponse>("/api/v1/finance/dashboard");
}
