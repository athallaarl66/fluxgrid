"use client";

import { useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { formatBalance, type ReportRow, type ReportResponse } from "@/lib/report-types";

interface StatementTreeProps {
  report: ReportResponse;
  showType?: "tb" | "pl" | "bs";
  onDrillDown: (row: ReportRow) => void;
}

function flatten(rows: ReportRow[]): ReportRow[] {
  const result: ReportRow[] = [];
  for (const r of rows) {
    result.push(r);
    if (r.children.length > 0) result.push(...flatten(r.children));
  }
  return result;
}

const PAGE_SIZES = [10, 20, 50, 100];

export function StatementTree({ report, showType = "tb", onDrillDown }: StatementTreeProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  if (report.rows.length === 0) {
    return (
      <div className="flex items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <p className="text-sm text-muted-foreground">No data for this period</p>
      </div>
    );
  }

  const flat = flatten(report.rows);
  const totalPages = Math.ceil(flat.length / pageSize);
  const start = (page - 1) * pageSize;
  const paged = flat.slice(start, start + pageSize);

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[70px]">Code</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Account Name</th>
              {showType === "tb" && (
                <>
                  <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[150px]">Debit</th>
                  <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[150px]">Credit</th>
                </>
              )}
              <th className={cn("h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D]", showType === "tb" ? "w-[150px]" : "w-[200px]")}>Balance</th>
            </tr>
          </thead>
          <tbody>
            {paged.map((row) => {
              const isParent = row.children.length > 0;
              const isGroupHead = isParent && row.depth === 0;
              return (
                <tr key={row.accountId} className={cn(
                  "border-b border-[#E5DEBF] transition-colors hover:bg-[#F7F3F0]",
                  isGroupHead && "bg-[#F6F0D7]",
                )}>
                  <td className={cn("px-3 text-[13px] tabular-nums font-mono", isParent ? "font-semibold text-foreground" : "text-muted-foreground")}>{row.code}</td>
                  <td className={cn("px-3 text-[13px]", isParent ? "font-semibold text-foreground" : "text-foreground")}>{row.name}</td>
                  {showType === "tb" && (
                    <>
                      <td className="px-3 text-right text-[13px] tabular-nums text-foreground">{row.debit !== 0 ? formatBalance(row.debit) : ""}</td>
                      <td className="px-3 text-right text-[13px] tabular-nums text-foreground">{row.credit !== 0 ? formatBalance(row.credit) : ""}</td>
                    </>
                  )}
                  <td className="px-3 text-right text-[13px] tabular-nums text-foreground">
                    <button type="button" onClick={() => onDrillDown(row)} className="hover:underline cursor-pointer">
                      {row.balance !== 0 ? formatBalance(row.balance) : "0"}
                    </button>
                  </td>
                </tr>
              );
            })}
            {/* Totals */}
            <tr className="border-t-2 border-[#9CAB84] bg-[#F6F0D7] font-semibold">
              <td className="px-3 py-2 text-[13px] text-foreground" colSpan={2}>
                {showType === "pl" ? "Total Revenue" : showType === "bs" ? "Total Assets" : "Grand Total"}
              </td>
              {showType === "tb" && (
                <>
                  <td className="px-3 py-2 text-right text-[13px] tabular-nums text-foreground">{formatBalance(report.totalDebit)}</td>
                  <td className="px-3 py-2 text-right text-[13px] tabular-nums text-foreground">{formatBalance(report.totalCredit)}</td>
                </>
              )}
              <td className="px-3 py-2 text-right text-[13px] tabular-nums text-foreground">
                {showType === "tb" ? formatBalance(report.totalDebit)
                  : report.rows.length > 0 ? formatBalance(report.rows[0].balance) : "0"}
              </td>
            </tr>
            {showType === "pl" && report.netIncome != null && report.rows.length >= 2 && (
              <tr className="bg-[#F6F0D7] font-semibold">
                <td className="px-3 py-2 text-[13px] text-foreground" colSpan={2}>Total Expenses</td>
                <td className="px-3 py-2 text-right text-[13px] tabular-nums text-foreground">{formatBalance(report.rows[1].balance)}</td>
              </tr>
            )}
            {showType === "pl" && report.netIncome != null && (
              <tr className="bg-[#F6F0D7] font-bold border-t-2 border-[#9CAB84]">
                <td className="px-3 py-2 text-[13px] text-foreground" colSpan={2}>Net Income</td>
                <td className="px-3 py-2 text-right text-[13px] tabular-nums text-foreground">{formatBalance(report.netIncome)}</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between text-[13px] text-muted-foreground">
        <div className="flex items-center gap-2">
          <span>Show</span>
          <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
            className="h-7 rounded border border-border bg-card px-1 text-[12px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer">
            {PAGE_SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <span>of {flat.length} accounts</span>
        </div>
        <div className="flex items-center gap-1">
          <button type="button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-30 cursor-pointer">
            <ChevronLeft className="size-4" />
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button key={p} type="button" onClick={() => setPage(p)}
              className={cn("flex size-7 items-center justify-center rounded text-[12px] cursor-pointer",
                p === page ? "bg-[#625f4b] text-white" : "text-muted-foreground hover:bg-muted hover:text-foreground")}>
              {p}
            </button>
          ))}
          <button type="button" onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-30 cursor-pointer">
            <ChevronRight className="size-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
