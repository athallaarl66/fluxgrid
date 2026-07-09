"use client";

import type { Employee } from "@/lib/hr-types";
import { EmployeeCard } from "@/components/hr/EmployeeCard";

export function EmployeeGrid({ employees }: { employees: Employee[] }) {
  if (employees.length === 0) {
    return null;
  }

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
      {employees.map((emp) => (
        <EmployeeCard key={emp.id} employee={emp} />
      ))}
    </div>
  );
}
