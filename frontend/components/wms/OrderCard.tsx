"use client";

import { cn } from "@/lib/utils";
import type { SalesOrder } from "@/lib/wms-types";

const statusColors: Record<string, string> = {
  PENDING: "border-t-[#d4a373]",
  RESERVED: "border-t-[#8B9B6F]",
  PICKING: "border-t-[#5a8fbf]",
  PACKED: "border-t-[#9b72cf]",
  SHIPPED: "border-t-[#6ba368]",
  CANCELLED: "border-t-[#a0a0a0]",
};

const statusLabels: Record<string, string> = {
  PENDING: "To Pick",
  RESERVED: "To Pick",
  PICKING: "Picking",
  PACKED: "To Pack",
  SHIPPED: "Shipped",
  CANCELLED: "Cancelled",
};

const statusBadgeClasses: Record<string, string> = {
  PENDING: "bg-yellow-100 text-yellow-700",
  RESERVED: "bg-blue-100 text-blue-700",
  PICKING: "bg-blue-100 text-blue-700",
  PACKED: "bg-purple-100 text-purple-700",
  SHIPPED: "bg-green-100 text-green-700",
  CANCELLED: "bg-gray-100 text-gray-500",
};

interface OrderCardProps {
  order: SalesOrder;
  onAction: (action: string, order: SalesOrder) => void;
}

export function OrderCard({ order, onAction }: OrderCardProps) {
  const totalQty = order.lines.reduce((s, l) => s + l.qtyOrdered, 0);
  const lineCount = order.lines.length;

  const isCancelled = order.status === "CANCELLED";
  const canGeneratePick = order.status === "PENDING" || order.status === "RESERVED";
  const canPick = order.status === "RESERVED" || order.status === "PICKING";
  const canPack = order.status === "PICKING";
  const canShip = order.status === "PACKED";

  return (
    <div
      className={cn(
        "rounded-lg border border-border bg-card p-3 space-y-2 border-t-[2px] transition-shadow hover:shadow-sm",
        statusColors[order.status] || "border-t-border",
        isCancelled && "opacity-60",
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-xs font-mono font-semibold text-foreground truncate">{order.orderNo}</p>
          <p className="text-[11px] text-muted-foreground truncate">{order.customerName}</p>
        </div>
        <span
          className={cn(
            "shrink-0 inline-block rounded-full px-2 py-0.5 text-[10px] font-medium",
            statusBadgeClasses[order.status] || "bg-gray-100 text-gray-700",
          )}
        >
          {statusLabels[order.status] || order.status}
        </span>
      </div>

      <div className="flex gap-3 text-[11px] text-muted-foreground">
        <span>{lineCount} line{lineCount !== 1 ? "s" : ""}</span>
        <span>{totalQty} qty</span>
      </div>

      {!isCancelled && (
        <div className="flex flex-wrap gap-1.5 pt-1">
          {canGeneratePick && (
            <button
              type="button"
              onClick={() => onAction("generate-pick", order)}
              className="px-2 py-1 text-[11px] font-medium rounded-md bg-[#8B9B6F]/10 text-[#8B9B6F] hover:bg-[#8B9B6F]/20 cursor-pointer"
            >
              Generate Pick
            </button>
          )}
          {canPick && (
            <button
              type="button"
              onClick={() => onAction("pick", order)}
              className="px-2 py-1 text-[11px] font-medium rounded-md bg-blue-100 text-blue-700 hover:bg-blue-200 cursor-pointer"
            >
              Pick
            </button>
          )}
          {canPack && (
            <button
              type="button"
              onClick={() => onAction("pack", order)}
              className="px-2 py-1 text-[11px] font-medium rounded-md bg-purple-100 text-purple-700 hover:bg-purple-200 cursor-pointer"
            >
              Pack & Ship
            </button>
          )}
          {canShip && (
            <button
              type="button"
              onClick={() => onAction("ship", order)}
              className="px-2 py-1 text-[11px] font-medium rounded-md bg-green-100 text-green-700 hover:bg-green-200 cursor-pointer"
            >
              Ship
            </button>
          )}
        </div>
      )}
    </div>
  );
}
