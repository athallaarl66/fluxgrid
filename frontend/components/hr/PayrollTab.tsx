"use client";

import { useAuth } from "@/lib/auth-context";
import type { EmployeeDetail } from "@/lib/hr-types";
import { MaskedField } from "@/components/hr/MaskedField";

function formatSalary(value: number): string {
  return `Rp ${value.toLocaleString("id-ID")}`;
}

export function PayrollTab({ employee }: { employee: EmployeeDetail }) {
  const { user } = useAuth();
  const hasPayrollAccess = user?.roles?.includes("HR:PayrollRead");

  if (!hasPayrollAccess) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <p className="text-sm text-muted-foreground font-medium">
          You do not have permission to view payroll details
        </p>
        <p className="text-xs text-muted-foreground mt-1">
          Contact your administrator for access
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <MaskedField
        label="Base Salary"
        value={employee.baseSalary ? formatSalary(employee.baseSalary) : "—"}
        mask="Rp ***.***.***"
      />
      <MaskedField
        label="Bank Name"
        value={employee.bankName || "—"}
      />
      <MaskedField
        label="Bank Account"
        value={employee.bankAccount || "—"}
      />
      <MaskedField
        label="Tax ID (NPWP)"
        value={employee.taxId || "—"}
      />
    </div>
  );
}
