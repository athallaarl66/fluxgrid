"use client";

import type { EmployeeDetail } from "@/lib/hr-types";
import { Badge } from "@/components/ui/badge";

const STATUS_VARIANTS: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  ACTIVE: "default",
  ON_LEAVE: "secondary",
  TERMINATED: "destructive",
};

const COLORS = [
  "#E8D5B7", "#C9A96E", "#A8C5A0", "#B8C5D6", "#D4B8C5",
  "#C5B8D4", "#B8D4C5", "#D4C5B8", "#A0B8C5", "#C5A0B8",
];

function hashColor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return COLORS[Math.abs(hash) % COLORS.length];
}

function getInitials(first: string, last: string): string {
  return `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
}

export function ProfileHeader({ employee }: { employee: EmployeeDetail }) {
  const bg = hashColor(`${employee.firstName} ${employee.lastName}`);
  const initials = getInitials(employee.firstName, employee.lastName);

  return (
    <div className="flex items-start gap-5 rounded-xl border border-border bg-card p-5">
      <div
        className="flex size-16 shrink-0 items-center justify-center rounded-full text-lg font-bold text-white"
        style={{ backgroundColor: bg }}
      >
        {initials}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-3 flex-wrap">
          <h1 className="text-xl font-semibold text-foreground">
            {employee.firstName} {employee.lastName}
          </h1>
          <Badge variant={STATUS_VARIANTS[employee.status] || "outline"}>
            {employee.status}
          </Badge>
        </div>
        <p className="text-sm text-muted-foreground mt-0.5">{employee.jobTitle}</p>
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-xs text-muted-foreground">
          <span>{employee.employeeNo}</span>
          {employee.departmentName && <span>{employee.departmentName}</span>}
          <span>{employee.email}</span>
        </div>
      </div>
    </div>
  );
}
