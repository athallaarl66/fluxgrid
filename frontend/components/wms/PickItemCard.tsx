"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import type { PickListItem } from "@/lib/wms-types";

interface PickItemCardProps {
  item: PickListItem;
  onConfirm: (itemId: string, qtyPicked: number) => void;
  onShortPick: (item: PickListItem) => void;
}

export function PickItemCard({ item, onConfirm, onShortPick }: PickItemCardProps) {
  const [qty, setQty] = useState(item.qtyExpected);

  const handleShortPick = () => {
    if (qty < item.qtyExpected) {
      onShortPick(item);
    }
  };

  return (
    <div className="rounded-xl border border-border bg-card p-4 space-y-3">
      <div className="flex items-center justify-between">
        <div className="space-y-0.5">
          <p className="text-xs font-medium text-muted-foreground">
            {item.locationCode || "—"}
          </p>
          <p className="text-sm font-semibold text-foreground">
            {item.itemSku || item.itemName || item.itemId.slice(0, 8)}
          </p>
          {item.itemName && item.itemSku && (
            <p className="text-xs text-muted-foreground">{item.itemName}</p>
          )}
        </div>
        <div className="text-right">
          <p className="text-xs text-muted-foreground">Expected</p>
          <p className="text-lg font-bold tabular-nums text-foreground">{item.qtyExpected}</p>
        </div>
      </div>

      <div className="space-y-1.5">
        <label className="text-xs font-medium text-muted-foreground block">Quantity picked</label>
        <input
          type="number"
          min={0}
          max={item.qtyExpected}
          value={qty}
          onChange={(e) => setQty(Number(e.target.value))}
          className={cn(
            "w-full h-10 rounded-lg border border-border bg-background px-3 text-base font-medium tabular-nums",
            "focus:outline-none focus:ring-2 focus:ring-[#8B9B6F]/40 focus:border-[#8B9B6F]",
          )}
        />
      </div>

      <div className="grid grid-cols-2 gap-2">
        <button
          type="button"
          onClick={() => onConfirm(item.id, qty)}
          disabled={qty <= 0}
          className="h-10 rounded-lg bg-[#8B9B6F] text-white text-sm font-medium cursor-pointer hover:bg-[#7A8B5F] disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Confirm Pick ({qty})
        </button>
        <button
          type="button"
          onClick={handleShortPick}
          disabled={qty >= item.qtyExpected}
          className="h-10 rounded-lg border-2 border-destructive/30 text-destructive text-sm font-medium cursor-pointer hover:bg-destructive/5 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Short Pick
        </button>
      </div>
    </div>
  );
}
