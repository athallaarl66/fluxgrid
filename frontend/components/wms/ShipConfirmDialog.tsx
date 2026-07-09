"use client";

import { useToast } from "@/components/ui/toast";
import type { PickList } from "@/lib/wms-types";

interface ShipConfirmDialogProps {
  open: boolean;
  pickList: PickList | null;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
}

export function ShipConfirmDialog({ open, pickList, onConfirm, onCancel }: ShipConfirmDialogProps) {
  const { toast } = useToast();
  if (!open || !pickList) return null;

  const totalExpected = pickList.items.reduce((s, i) => s + i.qtyExpected, 0);
  const totalPicked = pickList.items.reduce((s, i) => s + i.qtyPicked, 0);
  const itemCount = pickList.items.length;

  const handleConfirm = async () => {
    try {
      await onConfirm();
    } catch {
      toast("Failed to confirm shipment", "error");
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-sm rounded-xl border border-border bg-card p-5 shadow-xl">
        <h3 className="text-sm font-semibold text-foreground mb-1">Confirm Shipment</h3>
        <p className="text-xs text-muted-foreground mb-4">
          Order {pickList.orderNo} — {itemCount} item{itemCount !== 1 ? "s" : ""}
        </p>

        <div className="rounded-lg border border-border bg-muted/30 p-3 space-y-1.5 mb-4">
          <div className="flex justify-between text-xs">
            <span className="text-muted-foreground">Items shipped</span>
            <span className="font-medium">{itemCount}</span>
          </div>
          <div className="flex justify-between text-xs">
            <span className="text-muted-foreground">Total qty picked</span>
            <span className="font-medium">{totalPicked}</span>
          </div>
          <div className="flex justify-between text-xs">
            <span className="text-muted-foreground">Total qty expected</span>
            <span className="font-medium">{totalExpected}</span>
          </div>
        </div>

        <p className="text-xs text-muted-foreground mb-4">
          This will record the shipment in the stock ledger and update inventory balances.
        </p>

        <div className="flex gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="flex-1 h-8 rounded-lg border border-border text-sm font-medium cursor-pointer hover:bg-muted"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            className="flex-1 h-8 rounded-lg bg-primary text-primary-foreground text-sm font-medium cursor-pointer hover:bg-primary/80"
          >
            Confirm Shipment
          </button>
        </div>
      </div>
    </div>
  );
}
