"use client";

import { X, ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { formatBalance, type ReportRow, type LedgerDetailRow } from "@/lib/report-types";

interface LedgerDrilldownModalProps {
  open: boolean;
  row: ReportRow | null;
  data: LedgerDetailRow[];
  total: number;
  page: number;
  pageSize: number;
  loading: boolean;
  onClose: () => void;
  onPageChange: (page: number) => void;
}

export function LedgerDrilldownModal({
  open,
  row,
  data,
  total,
  page,
  pageSize,
  loading,
  onClose,
  onPageChange,
}: LedgerDrilldownModalProps) {
  if (!open || !row) return null;

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const startItem = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = Math.min(page * pageSize, total);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-3xl max-h-[85vh] overflow-y-auto rounded-xl border border-border bg-card shadow-xl mx-4">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h2 className="text-base font-semibold text-foreground">
              {row.code} — {row.name}
            </h2>
            <p className="mt-0.5 text-[12px] text-muted-foreground">
              Balance: {formatBalance(row.balance)}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-4">
          {loading ? (
            <div className="space-y-2 py-8">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="h-7 rounded bg-muted animate-pulse" />
              ))}
            </div>
          ) : data.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              No journal entry lines found
            </p>
          ) : (
            <div className="overflow-x-auto rounded-lg border border-border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
                    <th className="h-8 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[120px]">Entry No</th>
                    <th className="h-8 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[100px]">Date</th>
                    <th className="h-8 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Description</th>
                    <th className="h-8 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[140px]">Debit</th>
                    <th className="h-8 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D] w-[140px]">Credit</th>
                  </tr>
                </thead>
                <tbody>
                  {data.map((item) => (
                    <tr key={item.entryId} className="border-b border-border hover:bg-muted/40 transition-colors">
                      <td className="h-8 px-3 text-[13px] tabular-nums font-mono text-muted-foreground">
                        {item.entryNo}
                      </td>
                      <td className="h-8 px-3 text-[13px] tabular-nums text-foreground">
                        {new Date(item.transactionDate).toLocaleDateString("id-ID")}
                      </td>
                      <td className="h-8 px-3 text-[13px] text-foreground truncate max-w-[200px]">
                        {item.description}
                      </td>
                      <td className="h-8 px-3 text-right text-[13px] tabular-nums text-foreground">
                        {item.debit !== 0 ? formatBalance(item.debit) : ""}
                      </td>
                      <td className="h-8 px-3 text-right text-[13px] tabular-nums text-foreground">
                        {item.credit !== 0 ? formatBalance(item.credit) : ""}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Footer with pagination */}
        {total > 0 && (
          <div className="flex items-center justify-between border-t border-border px-6 py-3">
            <span className="text-[11px] text-muted-foreground">
              {startItem}–{endItem} of {total} lines
            </span>
            <div className="flex items-center gap-1">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => onPageChange(page - 1)}
                className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer"
              >
                <ChevronLeft className="size-3.5" />
              </button>
              <span className="text-[11px] text-muted-foreground px-2">
                {page} / {totalPages}
              </span>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => onPageChange(page + 1)}
                className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer"
              >
                <ChevronRight className="size-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
