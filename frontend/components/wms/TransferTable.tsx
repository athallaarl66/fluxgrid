"use client";

import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { ChevronLeft, ChevronRight, ArrowRight } from "lucide-react";
import type { TransferEntry } from "@/hooks/useTransfers";

function fmtDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function fmtNum(n: number) {
  return n.toLocaleString("id-ID", { maximumFractionDigits: 2 });
}

interface TransferTableProps {
  transfers: TransferEntry[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  onPageChange: (page: number) => void;
}

export function TransferTable({
  transfers,
  total,
  page,
  pageSize,
  isLoading,
  onPageChange,
}: TransferTableProps) {
  const totalPages = Math.ceil(total / pageSize);

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Date</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Item</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">From</th>
              <th className="px-3 py-2 text-center text-xs font-medium text-muted-foreground"></th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">To</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Qty</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Unit Cost</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Total</th>
            </tr>
          </thead>
          <tbody>
            {transfers.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-sm text-muted-foreground">
                  No transfers found.
                </td>
              </tr>
            ) : (
              transfers.map((t) => (
                <tr key={t.transactionId} className="border-b border-border last:border-0 hover:bg-muted/30">
                  <td className="px-3 py-2 text-muted-foreground text-xs whitespace-nowrap">
                    {fmtDate(t.createdAt)}
                  </td>
                  <td className="px-3 py-2 font-medium text-foreground text-xs">
                    {t.itemId.slice(0, 8)}...
                  </td>
                  <td className="px-3 py-2 text-xs text-muted-foreground">
                    {t.fromLocationId ? t.fromLocationId.slice(0, 8) + "..." : "—"}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <ArrowRight className="size-3.5 text-muted-foreground mx-auto" />
                  </td>
                  <td className="px-3 py-2 text-xs text-muted-foreground">
                    {t.toLocationId ? t.toLocationId.slice(0, 8) + "..." : "—"}
                  </td>
                  <td className="px-3 py-2 text-right text-foreground font-medium tabular-nums">
                    {fmtNum(t.quantity)}
                  </td>
                  <td className="px-3 py-2 text-right text-muted-foreground tabular-nums text-xs">
                    {fmtNum(t.unitCost)}
                  </td>
                  <td className="px-3 py-2 text-right text-foreground tabular-nums">
                    {fmtNum(t.totalValue)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>Page {page} of {totalPages} ({total} transfers)</span>
          <div className="flex gap-1">
            <Button
              variant="outline"
              size="sm"
              className="h-7 border-border text-muted-foreground"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
            >
              <ChevronLeft className="size-3.5" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-7 border-border text-muted-foreground"
              disabled={page >= totalPages}
              onClick={() => onPageChange(page + 1)}
            >
              <ChevronRight className="size-3.5" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
