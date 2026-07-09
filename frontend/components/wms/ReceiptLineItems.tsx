"use client";

import { cn } from "@/lib/utils";

export interface LineItem {
  itemId: string;
  itemSku: string | null;
  itemName: string | null;
  orderedQty: number;
  qtyReceived: number;
  qtyPassed: number;
  qtyFailed: number;
  validationError?: string | null;
  validationWarning?: string | null;
}

interface ReceiptLineItemsProps {
  lines: LineItem[];
  onChange: (index: number, field: "qtyReceived" | "qtyPassed" | "qtyFailed", value: number) => void;
}

export function ReceiptLineItems({ lines, onChange }: ReceiptLineItemsProps) {
  if (lines.length === 0) return null;

  return (
    <div className="space-y-2">
      <div className="hidden md:block overflow-x-auto rounded-lg border border-border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] sticky top-0 bg-card">
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Item</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Ordered</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Received</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Passed</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Failed</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line, i) => (
              <tr key={line.itemId} className={cn("border-b border-border h-10", line.validationError && "bg-red-50")}>
                <td className="px-3 py-1 text-xs">
                  <span className="font-medium">{line.itemSku || line.itemName || line.itemId.slice(0, 8)}</span>
                  {line.itemName && <span className="text-muted-foreground ml-1">— {line.itemName}</span>}
                </td>
                <td className="px-3 py-1 text-xs text-right">{line.orderedQty}</td>
                <td className="px-3 py-1">
                  <input
                    type="number"
                    min={0}
                    value={line.qtyReceived || ""}
                    placeholder="0"
                    onChange={(e) => onChange(i, "qtyReceived", Math.max(0, Number(e.target.value)))}
                    className="w-20 h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring"
                  />
                </td>
                <td className="px-3 py-1">
                  <input
                    type="number"
                    min={0}
                    value={line.qtyPassed || ""}
                    placeholder="0"
                    onChange={(e) => onChange(i, "qtyPassed", Math.max(0, Number(e.target.value)))}
                    className="w-20 h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring"
                  />
                </td>
                <td className="px-3 py-1">
                  <input
                    type="number"
                    min={0}
                    value={line.qtyFailed || ""}
                    placeholder="0"
                    onChange={(e) => onChange(i, "qtyFailed", Math.max(0, Number(e.target.value)))}
                    className="w-20 h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring"
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="md:hidden space-y-2">
        {lines.map((line, i) => (
          <div key={line.itemId} className={cn("rounded-lg border border-border bg-card p-3 space-y-2", line.validationError && "border-red-200 bg-red-50")}>
            <div className="text-xs font-medium">
              {line.itemSku || line.itemName || line.itemId.slice(0, 8)}
              {line.itemName && <span className="text-muted-foreground ml-1">— {line.itemName}</span>}
            </div>
            <div className="grid grid-cols-4 gap-1 text-xs">
              <div>
                <label className="text-muted-foreground block mb-0.5">Ordered</label>
                <span className="font-medium">{line.orderedQty}</span>
              </div>
              <div>
                <label className="text-muted-foreground block mb-0.5">Received</label>
                <input type="number" min={0} value={line.qtyReceived || ""} placeholder="0"
                  onChange={(e) => onChange(i, "qtyReceived", Math.max(0, Number(e.target.value)))}
                  className="w-full h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring" />
              </div>
              <div>
                <label className="text-muted-foreground block mb-0.5">Passed</label>
                <input type="number" min={0} value={line.qtyPassed || ""} placeholder="0"
                  onChange={(e) => onChange(i, "qtyPassed", Math.max(0, Number(e.target.value)))}
                  className="w-full h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring" />
              </div>
              <div>
                <label className="text-muted-foreground block mb-0.5">Failed</label>
                <input type="number" min={0} value={line.qtyFailed || ""} placeholder="0"
                  onChange={(e) => onChange(i, "qtyFailed", Math.max(0, Number(e.target.value)))}
                  className="w-full h-7 rounded border border-input bg-transparent px-2 text-xs text-right outline-none focus-visible:border-ring" />
              </div>
            </div>
          </div>
        ))}
      </div>

      {lines.map((line, i) => (
        <div key={line.itemId} className="space-y-1">
          {line.validationWarning && (
            <p className="text-xs text-amber-600">{line.validationWarning}</p>
          )}
          {line.validationError && (
            <p className="text-xs text-red-600">{line.validationError}</p>
          )}
        </div>
      ))}
    </div>
  );
}
