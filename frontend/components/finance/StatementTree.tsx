"use client";

import { cn } from "@/lib/utils";
import { StatementRow } from "@/components/finance/StatementRow";
import { formatBalance, type ReportRow, type ReportResponse } from "@/lib/report-types";

interface StatementTreeProps {
  report: ReportResponse;
  showType?: "tb" | "pl" | "bs";
  onDrillDown: (row: ReportRow) => void;
}

export function StatementTree({ report, showType = "tb", onDrillDown }: StatementTreeProps) {
  if (report.rows.length === 0) {
    return (
      <div className="flex items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <p className="text-sm text-muted-foreground">No data for this period</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">
              Account
            </th>
            {showType === "tb" && (
              <>
                <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[160px]">
                  Debit
                </th>
                <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[160px]">
                  Credit
                </th>
              </>
            )}
            <th
              className={cn(
                "h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D]",
                showType === "tb" ? "w-[160px]" : "w-[200px]",
              )}
            >
              Balance
            </th>
          </tr>
        </thead>
        <tbody>
          {report.rows.map((row) => (
            <StatementRow
              key={row.accountId}
              row={row}
              showType={showType}
              onDrillDown={onDrillDown}
            />
          ))}
          {/* Totals row */}
          <tr className="border-t-2 border-[#9CAB84] bg-[#F6F0D7]">
            <td className="h-9 px-3 text-[13px] font-semibold text-foreground">
              Total
            </td>
            {showType === "tb" && (
              <>
                <td className="h-9 px-3 text-right text-[13px] font-semibold tabular-nums text-foreground">
                  {formatBalance(report.totalDebit)}
                </td>
                <td className="h-9 px-3 text-right text-[13px] font-semibold tabular-nums text-foreground">
                  {formatBalance(report.totalCredit)}
                </td>
              </>
            )}
            <td className="h-9 px-3 text-right text-[13px] font-semibold tabular-nums text-foreground border-t-2 border-[#9CAB84]">
              {formatBalance(report.rows.reduce((s, r) => s + r.balance, 0))}
            </td>
          </tr>
          {showType === "pl" && report.netIncome != null && (
            <tr className="bg-[#F6F0D7]">
              <td className="h-9 px-3 text-[13px] font-bold text-foreground">
                Net Income
              </td>
              <td className="h-9 px-3 text-right text-[13px] font-bold tabular-nums text-foreground">
                {formatBalance(report.netIncome)}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
