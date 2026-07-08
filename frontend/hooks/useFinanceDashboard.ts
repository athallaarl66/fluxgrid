import { useQuery } from "@tanstack/react-query";
import type { DashboardResponse } from "@/lib/dashboard-types";
import { getFinanceDashboard } from "@/lib/dashboard-api";

export function useFinanceDashboard() {
  return useQuery<DashboardResponse>({
    queryKey: ["finance-dashboard"],
    queryFn: getFinanceDashboard,
  });
}
