"use client";

import type { OrgChartNode } from "@/lib/hr-types";

function hashColor(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = ((hash % 360) + 360) % 360;
  return `hsl(${hue}, 55%, 50%)`;
}

function getInitials(first: string, last: string) {
  return `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
}

interface OrgChartMobileListProps {
  nodes: OrgChartNode[];
  level?: number;
}

export function OrgChartMobileList({ nodes, level = 0 }: OrgChartMobileListProps) {
  return (
    <ul className="space-y-1" role="tree">
      {nodes.map((node) => (
        <li key={node.id} role="treeitem" tabIndex={0}>
          <div
            className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-muted/60 transition-colors"
            style={{ paddingLeft: `${12 + level * 24}px` }}
          >
            <div
              className="flex size-7 items-center justify-center rounded-full text-[10px] font-semibold text-white shrink-0"
              style={{ backgroundColor: hashColor(`${node.firstName}${node.lastName}`) }}
            >
              {getInitials(node.firstName, node.lastName)}
            </div>
            <div className="min-w-0">
              <p className="text-xs font-medium text-foreground truncate">
                {node.firstName} {node.lastName}
              </p>
              <p className="text-[10px] text-muted-foreground truncate">
                {node.jobTitle}{node.departmentName ? ` — ${node.departmentName}` : ""}
              </p>
            </div>
          </div>
          {node.children.length > 0 && (
            <OrgChartMobileList nodes={node.children} level={level + 1} />
          )}
        </li>
      ))}
    </ul>
  );
}
