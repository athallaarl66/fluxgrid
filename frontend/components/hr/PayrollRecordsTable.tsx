"use client";

import type { PayrollRecord } from "@/lib/hr-types";

function formatCurrency(value: number | null) {
  if (value === null) return "***";
  return new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
}

export function PayrollRecordsTable({ records }: { records: PayrollRecord[] }) {
  if (records.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <p className="text-sm text-muted-foreground">No employee records in this run</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-border">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7] sticky top-0">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Employee</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">No</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Base Salary</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Overtime</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Lateness</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Gross</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Tax</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Net</th>
          </tr>
        </thead>
        <tbody>
          {records.map((rec, i) => (
            <tr
              key={rec.id}
              className={`border-b border-border hover:bg-muted/40 ${i % 2 === 0 ? "bg-white dark:bg-transparent" : "bg-[#FAF8F0] dark:bg-muted/10"}`}
            >
              <td className="h-9 px-2 text-xs text-foreground font-medium">{rec.employeeName}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground font-mono tabular-nums">{rec.employeeNo}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums text-right">{formatCurrency(rec.baseSalary)}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums text-right">{formatCurrency(rec.overtimePay)}</td>
              <td className="h-9 px-2 text-xs text-destructive tabular-nums text-right">{rec.latenessDeduction ? `(${formatCurrency(rec.latenessDeduction)})` : "—"}</td>
              <td className="h-9 px-2 text-xs text-foreground tabular-nums text-right font-medium">{formatCurrency(rec.grossPay)}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums text-right">({formatCurrency(rec.taxDeduction)})</td>
              <td className="h-9 px-2 text-xs text-foreground tabular-nums text-right font-semibold">{formatCurrency(rec.netPay)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
