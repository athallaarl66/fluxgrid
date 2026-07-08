"use client";

import { cn } from "@/lib/utils";
import { AlertTriangle } from "lucide-react";
import type { BudgetVsActualRow } from "@/lib/budget-types";

interface BudgetVarianceReportProps {
  data: BudgetVsActualRow[] | undefined;
  isLoading: boolean;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("id-ID", { style: "decimal", minimumFractionDigits: 0 }).format(value);
}

export function BudgetVarianceReport({ data, isLoading }: BudgetVarianceReportProps) {
  if (isLoading) {
    return (
      <div className="space-y-2 animate-pulse">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-9 rounded bg-muted" />
        ))}
      </div>
    );
  }

  const rows = data ?? [];
  const empty = rows.length === 0;

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Account</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D]">Planned</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D]">Actual</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D]">Variance</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D]">Var %</th>
              <th className="h-9 px-3 text-center text-[11px] font-semibold text-[#89986D] w-[60px]"></th>
            </tr>
          </thead>
          <tbody>
            {empty ? (
              <tr>
                <td colSpan={6} className="h-24 text-center text-sm text-muted-foreground">
                  No budgets for this period
                </td>
              </tr>
            ) : (
              rows.map((row, i) => (
                <tr key={i} className={cn(
                  "border-b border-border transition-colors hover:bg-muted/40",
                  row.isFlagged && "bg-red-50/50",
                )}>
                  <td className="h-9 px-3">
                    <span className="text-sm text-foreground">{row.accountCode}</span>
                    <span className="ml-1.5 text-xs text-muted-foreground">— {row.accountName}</span>
                  </td>
                  <td className="h-9 px-3 text-right text-sm tabular-nums text-foreground">{formatCurrency(row.plannedAmount)}</td>
                  <td className="h-9 px-3 text-right text-sm tabular-nums text-foreground">{formatCurrency(row.actualAmount)}</td>
                  <td className={cn(
                    "h-9 px-3 text-right text-sm tabular-nums font-medium",
                    row.variance >= 0 ? "text-emerald-600" : "text-red-600",
                  )}>
                    {row.variance >= 0 ? "+" : ""}{formatCurrency(row.variance)}
                  </td>
                  <td className={cn(
                    "h-9 px-3 text-right text-sm tabular-nums font-medium",
                    row.variancePercentage >= 0 ? "text-emerald-600" : "text-red-600",
                  )}>
                    {row.variancePercentage >= 0 ? "+" : ""}{row.variancePercentage}%
                  </td>
                  <td className="h-9 px-3 text-center">
                    {row.isFlagged && (
                      <AlertTriangle className="size-4 text-amber-500 inline-block" />
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
