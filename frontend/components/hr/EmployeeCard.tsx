"use client";

import type { Employee } from "@/lib/hr-types";
import { useRouter } from "next/navigation";

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

const STATUS_STYLES: Record<string, string> = {
  ACTIVE: "bg-emerald-100 text-emerald-700",
  ON_LEAVE: "bg-amber-100 text-amber-700",
  TERMINATED: "bg-red-100 text-red-700",
};

export function EmployeeCard({ employee }: { employee: Employee }) {
  const router = useRouter();
  const bg = hashColor(`${employee.firstName} ${employee.lastName}`);
  const initials = getInitials(employee.firstName, employee.lastName);

  return (
    <div
      className="flex flex-col items-center rounded-xl border border-border bg-card p-4 text-center cursor-pointer transition-all hover:shadow-md hover:-translate-y-0.5"
      onClick={() => router.push(`/hr/employees/${employee.id}`)}
      onKeyDown={(e) => e.key === "Enter" && router.push(`/hr/employees/${employee.id}`)}
      tabIndex={0}
      role="button"
    >
      <div
        className="flex size-14 items-center justify-center rounded-full text-sm font-bold text-white mb-3"
        style={{ backgroundColor: bg }}
      >
        {initials}
      </div>
      <p className="text-sm font-semibold text-foreground">
        {employee.firstName} {employee.lastName}
      </p>
      <p className="text-xs text-muted-foreground mt-0.5">{employee.jobTitle}</p>
      {employee.departmentName && (
        <p className="text-xs text-muted-foreground mt-0.5">{employee.departmentName}</p>
      )}
      <span
        className={`mt-2 inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[employee.status] || "bg-gray-100 text-gray-700"}`}
      >
        {employee.status}
      </span>
    </div>
  );
}
