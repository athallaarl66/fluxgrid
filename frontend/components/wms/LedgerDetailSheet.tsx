"use client";

import { useEffect, useRef } from "react";
import { X } from "lucide-react";

interface LedgerDetailSheetProps {
  transactionId: string;
  open: boolean;
  onClose: () => void;
}

export function LedgerDetailSheet({
  transactionId,
  open,
  onClose,
}: LedgerDetailSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    if (open) {
      document.addEventListener("keydown", handleKeyDown);
    }
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [open, onClose]);

  useEffect(() => {
    if (open) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [open]);

  if (!open) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-40 bg-black/20"
        onClick={onClose}
      />
      <div
        ref={sheetRef}
        className="fixed right-0 top-0 z-50 h-full w-full max-w-md bg-card border-l border-border shadow-xl"
      >
        <div className="flex items-center justify-between px-5 py-4 border-b border-border">
          <h2 className="text-sm font-semibold">Transaction Details</h2>
          <button
            onClick={onClose}
            className="rounded-md p-1 text-muted-foreground hover:text-foreground cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>
        <div className="p-5 space-y-4">
          <div>
            <p className="text-xs text-muted-foreground">Transaction ID</p>
            <p className="text-sm font-mono mt-0.5">{transactionId}</p>
          </div>
          <div className="rounded-lg border border-border p-3 space-y-2">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Debit Entry
            </p>
            <p className="text-xs text-muted-foreground">
              +Qty at Location A — pending implementation
            </p>
          </div>
          <div className="rounded-lg border border-border p-3 space-y-2">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Credit Entry
            </p>
            <p className="text-xs text-muted-foreground">
              -Qty at Location B — pending implementation
            </p>
          </div>
        </div>
      </div>
    </>
  );
}
