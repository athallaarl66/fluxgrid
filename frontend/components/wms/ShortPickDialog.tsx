"use client";

import { useState } from "react";

type ShortPickReason = "Damaged" | "Missing" | "Other";

interface ShortPickDialogProps {
  open: boolean;
  itemLabel: string;
  qtyExpected: number;
  onConfirm: (reason: ShortPickReason, actualQty: number) => void;
  onCancel: () => void;
}

export function ShortPickDialog({ open, itemLabel, qtyExpected, onConfirm, onCancel }: ShortPickDialogProps) {
  const [reason, setReason] = useState<ShortPickReason>("Damaged");
  const [actualQty, setActualQty] = useState(Math.max(0, qtyExpected - 1));
  const [error, setError] = useState("");

  if (!open) return null;

  const handleConfirm = () => {
    if (actualQty < 0) {
      setError("Quantity cannot be negative");
      return;
    }
    if (actualQty >= qtyExpected) {
      setError("Short pick must be less than expected quantity");
      return;
    }
    onConfirm(reason, actualQty);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-sm rounded-xl border border-border bg-card p-5 shadow-xl">
        <h3 className="text-sm font-semibold text-foreground mb-1">Short Pick</h3>
        <p className="text-xs text-muted-foreground mb-4">
          {itemLabel} — expected {qtyExpected}
        </p>

        <div className="space-y-3">
          <label className="text-xs font-medium text-muted-foreground block">Actual quantity picked</label>
          <input
            type="number"
            min={0}
            max={qtyExpected - 1}
            value={actualQty}
            onChange={(e) => { setActualQty(Number(e.target.value)); setError(""); }}
            className="w-full h-8 rounded-lg border border-border bg-background px-2.5 text-sm"
          />
          {error && <p className="text-[11px] text-destructive">{error}</p>}

          <label className="text-xs font-medium text-muted-foreground block">Reason</label>
          <div className="space-y-1.5">
            {(["Damaged", "Missing", "Other"] as ShortPickReason[]).map((r) => (
              <label key={r} className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="radio"
                  name="short-reason"
                  checked={reason === r}
                  onChange={() => setReason(r)}
                  className="size-3.5 accent-[#8B9B6F]"
                />
                {r}
              </label>
            ))}
          </div>

          <div className="flex gap-2 pt-2">
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
              className="flex-1 h-8 rounded-lg bg-destructive/10 text-destructive text-sm font-medium cursor-pointer hover:bg-destructive/20"
            >
              Confirm Short Pick
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
