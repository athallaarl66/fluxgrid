"use client";

import type { SalesOrder } from "@/lib/wms-types";
import { OrderCard } from "./OrderCard";
import { EmptyKanban } from "./EmptyKanban";

const columns = [
  { key: "to-pick", label: "To Pick", statuses: ["PENDING", "RESERVED"], color: "border-t-[#d4a373]" },
  { key: "picking", label: "Picking", statuses: ["PICKING"], color: "border-t-[#5a8fbf]" },
  { key: "to-pack", label: "To Pack", statuses: ["PACKED"], color: "border-t-[#9b72cf]" },
  { key: "shipped", label: "Shipped", statuses: ["SHIPPED"], color: "border-t-[#6ba368]" },
];

interface OutboundKanbanProps {
  orders: SalesOrder[];
  onAction: (action: string, order: SalesOrder) => void;
}

export function OutboundKanban({ orders, onAction }: OutboundKanbanProps) {
  if (orders.length === 0) return <EmptyKanban />;

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
      {columns.map((col) => {
        const colOrders = orders.filter((o) => col.statuses.includes(o.status));
        return (
          <div key={col.key} className="space-y-2">
            <div className="flex items-center gap-2 px-1">
              <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">{col.label}</h3>
              <span className="inline-flex items-center justify-center size-4 rounded-full bg-muted text-[10px] font-medium text-muted-foreground">
                {colOrders.length}
              </span>
            </div>
            <div className="space-y-2 min-h-[200px]">
              {colOrders.length === 0 ? (
                <div className="rounded-lg border border-dashed border-border p-4 text-center">
                  <p className="text-[11px] text-muted-foreground">No orders</p>
                </div>
              ) : (
                colOrders.map((order) => (
                  <OrderCard key={order.id} order={order} onAction={onAction} />
                ))
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
