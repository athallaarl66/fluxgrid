"use client";

import type { EmployeeDetail } from "@/lib/hr-types";
import { EmploymentTimeline } from "@/components/hr/EmploymentTimeline";
import { formatDate } from "@/lib/date-utils";

const MOCK_TIMELINE_EVENTS = [
  {
    date: "2026-01-15",
    type: "Hire",
    description: "Joined the company",
  },
];

export function EmploymentTab({ employee }: { employee: EmployeeDetail }) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <p className="text-xs text-muted-foreground">Department</p>
          <p className="text-sm text-foreground mt-0.5">
            {employee.departmentName || "—"}
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Manager</p>
          <p className="text-sm text-foreground mt-0.5">
            {employee.managerName ? (
              <a
                href={`/hr/employees/${employee.managerId}`}
                className="text-primary hover:underline"
              >
                {employee.managerName}
              </a>
            ) : "—"}
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Job Title</p>
          <p className="text-sm text-foreground mt-0.5">{employee.jobTitle}</p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Hire Date</p>
          <p className="text-sm text-foreground mt-0.5">
            {formatDate(employee.hireDate, {
              day: "numeric",
              month: "long",
              year: "numeric",
            })}
          </p>
        </div>
      </div>

      <div>
        <h3 className="text-sm font-semibold text-foreground mb-3">Employment Timeline</h3>
        <EmploymentTimeline events={MOCK_TIMELINE_EVENTS} />
      </div>
    </div>
  );
}
