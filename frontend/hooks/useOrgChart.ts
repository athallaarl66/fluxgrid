import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { OrgChartNode } from "@/lib/hr-types";

function buildTree(employees: OrgChartNode[]): OrgChartNode[] {
  const map = new Map<string, OrgChartNode>();
  const roots: OrgChartNode[] = [];

  for (const emp of employees) {
    map.set(emp.id, { ...emp, children: [] });
  }

  for (const emp of employees) {
    const node = map.get(emp.id)!;
    if (emp.managerId && map.has(emp.managerId)) {
      map.get(emp.managerId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }

  return roots;
}

export function useOrgChart() {
  return useQuery<OrgChartNode[]>({
    queryKey: ["org-chart"],
    queryFn: async () => {
      const data = await apiClient<OrgChartNode[]>("/api/v1/hr/org-chart");
      return buildTree(data);
    },
  });
}
