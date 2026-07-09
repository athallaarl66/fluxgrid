"use client";

import type { Employee } from "@/lib/hr-types";
import { useRouter } from "next/navigation";
import { Badge } from "@/components/ui/badge";

const STATUS_VARIANTS: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  ACTIVE: "default",
  ON_LEAVE: "secondary",
  TERMINATED: "destructive",
};

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

export function EmployeeTable({ employees }: { employees: Employee[] }) {
  const router = useRouter();

  if (employees.length === 0) {
    return null;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Employee</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">No</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Job Title</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Department</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Status</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Hire Date</th>
          </tr>
        </thead>
        <tbody>
          {employees.map((emp) => (
            <tr
              key={emp.id}
              className="border-b border-border hover:bg-muted/40 cursor-pointer"
              onClick={() => router.push(`/hr/employees/${emp.id}`)}
              onKeyDown={(e) => e.key === "Enter" && router.push(`/hr/employees/${emp.id}`)}
              tabIndex={0}
            >
              <td className="h-9 px-2 text-xs text-foreground font-medium">
                {emp.firstName} {emp.lastName}
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground font-mono tabular-nums">
                {emp.employeeNo}
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground">{emp.jobTitle}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground">
                {emp.departmentName || "—"}
              </td>
              <td className="h-9 px-2">
                <Badge variant={STATUS_VARIANTS[emp.status] || "outline"}>
                  {emp.status}
                </Badge>
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">
                {formatDate(emp.hireDate)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
