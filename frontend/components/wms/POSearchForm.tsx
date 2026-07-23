"use client";

import type { PurchaseOrder } from "@/lib/wms-types";
import { formatDate } from "@/lib/date-utils";

interface POSearchFormProps {
  poNumber: string;
  onPoNumberChange: (value: string) => void;
  onSearch: () => void;
  loading: boolean;
  po: PurchaseOrder | null;
  error: string | null;
}

export function POSearchForm({ poNumber, onPoNumberChange, onSearch, loading, po, error }: POSearchFormProps) {
  return (
    <div className="space-y-3">
      <div className="flex gap-2">
        <input
          type="text"
          placeholder="Enter PO number (e.g. PO-9999)"
          value={poNumber}
          onChange={(e) => onPoNumberChange(e.target.value)}
          onKeyDown={(e) => { if (e.key === "Enter") onSearch(); }}
          className="flex-1 h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
        />
        <button
          onClick={onSearch}
          disabled={loading || !poNumber.trim()}
          className="inline-flex items-center gap-1 h-8 px-2.5 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/80 disabled:opacity-50 cursor-pointer"
        >
          {loading ? "Searching..." : "Search"}
        </button>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-800">
          {error}
        </div>
      )}

      {po && (
        <div className="rounded-lg border border-border bg-card p-3 space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">{po.poNumber}</span>
            <span className="text-xs text-muted-foreground">{po.supplierName}</span>
          </div>
          <p className="text-xs text-muted-foreground">
            {formatDate(po.poDate, { day: "2-digit", month: "short", year: "numeric" })}
            {" — "}
            {po.lines.length} line(s)
          </p>
        </div>
      )}
    </div>
  );
}
