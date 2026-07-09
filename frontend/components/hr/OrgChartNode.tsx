"use client";

import type { OrgChartNode as OrgChartNodeType } from "@/lib/hr-types";

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

interface OrgChartNodeProps {
  node: OrgChartNodeType;
}

export function OrgChartNode({ node }: OrgChartNodeProps) {
  const color = hashColor(`${node.firstName}${node.lastName}`);
  const initials = getInitials(node.firstName, node.lastName);

  return (
    <div className="flex flex-col items-center" role="treeitem" tabIndex={0}>
      <div className="flex flex-col items-center bg-card border border-border rounded-lg px-4 py-3 shadow-sm min-w-[140px] hover:shadow-md hover:border-ring/30 transition-shadow">
        <div
          className="flex size-9 items-center justify-center rounded-full text-[13px] font-semibold text-white select-none"
          style={{ backgroundColor: color }}
          aria-hidden="true"
        >
          {initials}
        </div>
        <p className="mt-1.5 text-xs font-medium text-foreground text-center leading-tight">
          {node.firstName} {node.lastName}
        </p>
        <p className="text-[10px] text-muted-foreground text-center leading-tight mt-0.5">
          {node.jobTitle}
        </p>
        {node.departmentName && (
          <p className="text-[10px] text-muted-foreground/60 text-center mt-0.5">
            {node.departmentName}
          </p>
        )}
      </div>
      {node.children.length > 0 && (
        <>
          <div className="w-px h-4 bg-border" />
          <div className="flex gap-6">
            {node.children.map((child) => (
              <div key={child.id} className="flex flex-col items-center">
                <div className="w-px h-3 bg-border" />
                <OrgChartNode node={child} />
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
