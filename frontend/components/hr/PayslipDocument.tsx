"use client";

import { useRef } from "react";
import { Printer } from "lucide-react";
import type { PayrollRun, PayrollRecord } from "@/lib/hr-types";
import { formatDate } from "@/lib/date-utils";

function formatCurrency(value: number | null) {
  if (value === null) return "—";
  return new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
}

interface PayslipDocumentProps {
  run: PayrollRun;
  record: PayrollRecord;
}

export function PayslipDocument({ run, record }: PayslipDocumentProps) {
  const printRef = useRef<HTMLDivElement>(null);

  function handlePrint() {
    window.print();
  }

  return (
    <div className="space-y-4">
      <div className="hidden md:flex justify-end">
        <button
          type="button"
          onClick={handlePrint}
          className="flex items-center gap-1.5 h-8 rounded-lg border border-border bg-background px-3 text-sm font-medium text-foreground hover:bg-muted transition-colors cursor-pointer"
        >
          <Printer className="size-4" />
          Download PDF
        </button>
      </div>

      <div ref={printRef} className="payslip-document max-w-2xl mx-auto bg-white dark:bg-card rounded-xl border border-border shadow-sm">
        <div className="p-6 space-y-6">
          <div className="text-center border-b border-border pb-4">
            <h2 className="text-xl font-bold text-foreground">Payslip</h2>
            <p className="text-sm text-muted-foreground mt-1">
              {run.periodName} — {formatDate(run.startDate, { day: "numeric", month: "long", year: "numeric" })}
              {" – "}
              {formatDate(run.endDate, { day: "numeric", month: "long", year: "numeric" })}
            </p>
            <p className="text-xs text-muted-foreground mt-1">
              {record.employeeNo} — {record.employeeName}
            </p>
          </div>

          <table className="w-full text-sm">
            <thead>
              <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
                <th className="h-8 px-3 text-left text-[11px] font-semibold text-[#89986D]">Description</th>
                <th className="h-8 px-3 text-right text-[11px] font-semibold text-[#89986D]">Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-b border-border">
                <td className="h-9 px-3 text-xs text-foreground">Base Salary</td>
                <td className="h-9 px-3 text-xs text-foreground tabular-nums text-right">{formatCurrency(record.baseSalary)}</td>
              </tr>
              <tr className="border-b border-border">
                <td className="h-9 px-3 text-xs text-foreground">Overtime Pay</td>
                <td className="h-9 px-3 text-xs text-foreground tabular-nums text-right">{formatCurrency(record.overtimePay)}</td>
              </tr>
              <tr className="border-b border-border">
                <td className="h-9 px-3 text-xs text-destructive">Lateness Deduction</td>
                <td className="h-9 px-3 text-xs text-destructive tabular-nums text-right">{record.latenessDeduction ? `(${formatCurrency(record.latenessDeduction)})` : "—"}</td>
              </tr>
              <tr className="border-b border-border">
                <td className="h-9 px-3 text-xs font-medium text-foreground">Gross Pay</td>
                <td className="h-9 px-3 text-xs font-medium text-foreground tabular-nums text-right">{formatCurrency(record.grossPay)}</td>
              </tr>
              <tr className="border-b border-border">
                <td className="h-9 px-3 text-xs text-muted-foreground">Tax Deduction (PPh 21)</td>
                <td className="h-9 px-3 text-xs text-muted-foreground tabular-nums text-right">({formatCurrency(record.taxDeduction)})</td>
              </tr>
              <tr>
                <td className="h-10 px-3 text-sm font-bold text-foreground border-t-2 border-foreground">Net Take-Home Pay</td>
                <td className="h-10 px-3 text-sm font-bold text-foreground tabular-nums text-right border-t-2 border-foreground">{formatCurrency(record.netPay)}</td>
              </tr>
            </tbody>
          </table>

          <p className="text-[10px] text-muted-foreground text-center pt-2 border-t border-border">
            This is a computer-generated document. No signature is required.
          </p>
        </div>
      </div>
    </div>
  );
}
