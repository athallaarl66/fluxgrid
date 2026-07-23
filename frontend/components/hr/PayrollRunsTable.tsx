"use client";

import type { PayrollRun } from "@/lib/hr-types";
import { useRouter } from "next/navigation";
import { Badge } from "@/components/ui/badge";

import { formatDate } from "@/lib/date-utils";

const STATUS_VARIANTS: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  DRAFT: "outline",
  FINALIZED: "default",
};

function formatCurrency(value: number | null) {
  if (value === null) return "***";
  return new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
}

export function PayrollRunsTable({ runs }: { runs: PayrollRun[] }) {
  const router = useRouter();

  if (runs.length === 0) return null;

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Period</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Date Range</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Total Gross</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Total Net</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Status</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Processed By</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Created</th>
          </tr>
        </thead>
        <tbody>
          {runs.map((run) => (
            <tr
              key={run.id}
              className="border-b border-border hover:bg-muted/40 cursor-pointer"
              onClick={() => router.push(`/hr/payroll/${run.id}`)}
              onKeyDown={(e) => e.key === "Enter" && router.push(`/hr/payroll/${run.id}`)}
              tabIndex={0}
            >
              <td className="h-9 px-2 text-xs text-foreground font-medium">{run.periodName}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground">
                {formatDate(run.startDate)} – {formatDate(run.endDate)}
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">{formatCurrency(run.totalGross)}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">{formatCurrency(run.totalNet)}</td>
              <td className="h-9 px-2">
                <Badge variant={STATUS_VARIANTS[run.status] || "outline"}>{run.status}</Badge>
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground">{run.processedBy}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">{formatDate(run.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
