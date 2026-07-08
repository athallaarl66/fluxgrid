"use client";

import type { StockLedgerEntry } from "@/lib/wms-types";
import { cn } from "@/lib/utils";

interface StockLedgerTableProps {
  entries: StockLedgerEntry[];
  valuationMethod: "fifo" | "average";
  onRowClick: (transactionId: string) => void;
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function formatCurrency(value: number) {
  return `Rp ${Math.round(value).toLocaleString("id-ID")}`;
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

export function StockLedgerTable({
  entries,
  valuationMethod,
  onRowClick,
}: StockLedgerTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border bg-card">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] sticky top-0 bg-card">
            <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Date
            </th>
            <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Ref No.
            </th>
            <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              SKU
            </th>
            <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Location
            </th>
            <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Type
            </th>
            <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Qty In
            </th>
            <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Qty Out
            </th>
            <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Balance
            </th>
            <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Valuation
            </th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry) => {
            const badge = getTypeBadge(entry.referenceType);
            return (
              <tr
                key={entry.id}
                onClick={() => onRowClick(entry.transactionId)}
                className="border-b border-border hover:bg-muted/50 cursor-pointer h-9"
              >
                <td className="px-3 py-1 text-xs">{formatDate(entry.createdAt)}</td>
                <td className="px-3 py-1 text-xs font-mono">
                  {entry.transactionId.slice(0, 8)}...
                </td>
                <td className="px-3 py-1 text-xs">{entry.itemId.slice(0, 8)}</td>
                <td className="px-3 py-1 text-xs">{entry.locationId.slice(0, 8)}</td>
                <td className="px-3 py-1">
                  <span
                    className={cn(
                      "inline-block rounded-full px-2 py-0.5 text-[11px] font-medium",
                      badge.className,
                    )}
                  >
                    {badge.label}
                  </span>
                </td>
                <td
                  className={cn(
                    "px-3 py-1 text-xs text-right font-medium",
                    entry.quantity > 0 && "text-green-600",
                  )}
                >
                  {entry.quantity > 0 ? `+${entry.quantity}` : ""}
                </td>
                <td
                  className={cn(
                    "px-3 py-1 text-xs text-right font-medium",
                    entry.quantity < 0 && "text-red-600",
                  )}
                >
                  {entry.quantity < 0 ? Math.abs(entry.quantity) : ""}
                </td>
                <td className="px-3 py-1 text-xs text-right">{entry.quantity}</td>
                <td className="px-3 py-1 text-xs text-right">
                  {formatCurrency(entry.quantity * entry.unitCost)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
