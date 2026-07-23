"use client";
import React, { useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Period } from "../../lib/period-types";
import { cn } from "@/lib/utils";
import { formatDate } from "@/lib/date-utils";

interface PeriodsTableProps {
  periods: Period[];
  onActionMenu: (period: Period) => void;
}

const PAGE_SIZES = [10, 20, 50, 100];

export default function PeriodsTable({ periods, onActionMenu }: PeriodsTableProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const totalPages = Math.ceil(periods.length / pageSize);
  const start = (page - 1) * pageSize;
  const paged = periods.slice(start, start + pageSize);

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded border border-[#cac6bb] bg-white -mx-5 sm:mx-0">
        <table className="w-full min-w-[640px]">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#f6f0d7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Name</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Start Date</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">End Date</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Status</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Actions</th>
            </tr>
          </thead>
          <tbody>
            {paged.map((p) => (
              <tr key={p.id} className="h-9 border-b border-[#e6e2df] hover:bg-[#f7f3f0] transition-colors">
                <td className="px-3 text-[13px] font-medium text-[#1c1b1a]">{p.name}</td>
                <td className="px-3 text-[13px] text-[#49473e] tabular-nums">{formatDate(p.startDate, { year: 'numeric', month: '2-digit', day: '2-digit' })}</td>
                <td className="px-3 text-[13px] text-[#49473e] tabular-nums">{formatDate(p.endDate, { year: 'numeric', month: '2-digit', day: '2-digit' })}</td>
                <td className="px-3">
                  <span className={cn("inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide",
                    p.status === "OPEN" ? "bg-[#d4e7ab] text-[#586838]" : "bg-[#e6e2df] text-[#49473e]")}>
                    {p.status}
                  </span>
                </td>
                <td className="px-3 text-right">
                  <button onClick={() => onActionMenu(p)}
                    className="text-[13px] font-medium text-[#625f4b] hover:text-[#706d59] hover:underline transition-colors">
                    {p.status === "OPEN" ? "Close" : "Reopen"}
                  </button>
                </td>
              </tr>
            ))}
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
          <span>of {periods.length} periods</span>
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
