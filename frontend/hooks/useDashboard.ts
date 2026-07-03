import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface ModuleInfo {
  name: string;
  path: string;
  description: string;
  icon: string;
  metric: string;
}

export function useDashboard() {
  return useQuery<ModuleInfo[]>({
    queryKey: ["dashboard"],
    queryFn: () => apiClient<ModuleInfo[]>("/api/dashboard"),
  });
}
