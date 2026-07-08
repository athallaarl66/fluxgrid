"use client";

import type { StockLedgerEntry } from "@/lib/wms-types";
import { cn } from "@/lib/utils";

interface StockLedgerMobileListProps {
  entries: StockLedgerEntry[];
  valuationMethod: "fifo" | "average";
  onEntryClick: (transactionId: string) => void;
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function getTypeBadge(refType: string) {
  const upper = refType.toUpperCase();
  if (upper === "PURCHASE_RECEIPT")
    return { label: "Inbound", className: "bg-green-100 text-green-700" };
  if (upper === "SHIPMENT")
    return { label: "Outbound", className: "bg-red-100 text-red-700" };
  if (upper === "TRANSFER")
    return { label: "Transfer", className: "bg-blue-100 text-blue-700" };
  return { label: refType, className: "bg-gray-100 text-gray-700" };
}

export function StockLedgerMobileList({
  entries,
  onEntryClick,
}: StockLedgerMobileListProps) {
  return (
    <div className="space-y-2">
      {entries.map((entry) => {
        const badge = getTypeBadge(entry.referenceType);
        return (
          <div
            key={entry.id}
            onClick={() => onEntryClick(entry.transactionId)}
            className="rounded-lg border border-border bg-card p-3 cursor-pointer active:bg-muted"
          >
            <div className="flex items-center justify-between mb-1">
              <span className="text-xs font-medium">{formatDate(entry.createdAt)}</span>
              <span
                className={cn(
                  "rounded-full px-2 py-0.5 text-[11px] font-medium",
                  badge.className,
                )}
              >
                {badge.label}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-xs text-muted-foreground">
                Ref: {entry.transactionId.slice(0, 8)}...
              </span>
              <span
                className={cn(
                  "text-sm font-semibold",
                  entry.quantity > 0 ? "text-green-600" : "text-red-600",
                )}
              >
                {entry.quantity > 0 ? "+" : ""}
                {entry.quantity}
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
