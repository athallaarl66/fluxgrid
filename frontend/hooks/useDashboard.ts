import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface DashboardStats {
  inventoryItems: number;
  locations: number;
  activeEmployees: number;
  candidates: number;
  openJobPostings: number;
  journalEntries: number;
  chartOfAccounts: number;
  accountingPeriods: number;
}

export interface MonthlyData {
  label: string;
  value: number;
}

export interface InventoryTrendPoint {
  label: string;
  inbound: number;
  outbound: number;
}

export interface ModuleActivity {
  module: string;
  count: number;
  unit: string;
}

export function useDashboardStats() {
  return useQuery<DashboardStats>({
    queryKey: ["dashboard-stats"],
    queryFn: () => apiClient<DashboardStats>("/api/dashboard/stats"),
  });
}

export function useJournalTrend(months = 6) {
  return useQuery<MonthlyData[]>({
    queryKey: ["journal-trend", months],
    queryFn: () => apiClient<MonthlyData[]>(`/api/dashboard/charts/journal-trend?months=${months}`),
  });
}

export function useInventoryTrend(months = 6) {
  return useQuery<InventoryTrendPoint[]>({
    queryKey: ["inventory-trend", months],
    queryFn: () => apiClient<InventoryTrendPoint[]>(`/api/dashboard/charts/inventory-trend?months=${months}`),
  });
}

export function useModuleActivity() {
  return useQuery<ModuleActivity[]>({
    queryKey: ["module-activity"],
    queryFn: () => apiClient<ModuleActivity[]>("/api/dashboard/activity"),
  });
}
