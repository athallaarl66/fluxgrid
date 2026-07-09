"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import { Check, X } from "lucide-react";
import type { PickList } from "@/lib/wms-types";

interface PackingTableProps {
  pickList: PickList;
  onVerify: (lines: { itemId: string; verifiedQty: number }[]) => Promise<void>;
}

export function PackingTable({ pickList, onVerify }: PackingTableProps) {
  const [verifiedQtys, setVerifiedQtys] = useState<Record<string, number>>(() => {
    const init: Record<string, number> = {};
    pickList.items.forEach((i) => { init[i.itemId] = i.qtyPicked; });
    return init;
  });

  const allMatch = pickList.items.every((i) => verifiedQtys[i.itemId] === i.qtyPicked);
  const anyMismatch = pickList.items.some((i) => verifiedQtys[i.itemId] !== i.qtyPicked);

  const handleVerify = () => {
    const lines = Object.entries(verifiedQtys).map(([itemId, verifiedQty]) => ({
      itemId,
      verifiedQty,
    }));
    onVerify(lines);
  };

  return (
    <div className="space-y-3">
      <>
        <div className="hidden md:block overflow-x-auto rounded-lg border border-border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b-2 border-[#9CAB84] sticky top-0 bg-card">
                <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Item</th>
                <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Expected</th>
                <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Picked</th>
                <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Verified</th>
                <th className="text-center px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Status</th>
              </tr>
            </thead>
            <tbody>
              {pickList.items.map((item) => {
                const verified = verifiedQtys[item.itemId] ?? item.qtyPicked;
                const match = verified === item.qtyPicked;
                return (
                  <tr key={item.id} className="border-b border-border h-10">
                    <td className="px-3 py-1 text-xs">
                      <span className="font-medium">{item.itemSku || item.itemName || item.itemId.slice(0, 8)}</span>
                    </td>
                    <td className="px-3 py-1 text-xs text-right">{item.qtyExpected}</td>
                    <td className="px-3 py-1 text-xs text-right">{item.qtyPicked}</td>
                    <td className="px-3 py-1 text-right">
                      <input
                        type="number"
                        min={0}
                        value={verified}
                        onChange={(e) =>
                          setVerifiedQtys((prev) => ({ ...prev, [item.itemId]: Number(e.target.value) }))
                        }
                        className="w-20 h-7 rounded border border-border bg-background px-2 text-xs text-right tabular-nums"
                      />
                    </td>
                    <td className="px-3 py-1 text-center">
                      {match ? (
                        <Check className="size-4 text-green-600 inline-block" />
                      ) : (
                        <X className="size-4 text-destructive inline-block" />
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="md:hidden space-y-2">
          {pickList.items.map((item) => {
            const verified = verifiedQtys[item.itemId] ?? item.qtyPicked;
            const match = verified === item.qtyPicked;
            return (
              <div key={item.id} className="rounded-lg border border-border bg-card p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-medium">{item.itemSku || item.itemName || item.itemId.slice(0, 8)}</span>
                  {match ? (
                    <Check className="size-4 text-green-600" />
                  ) : (
                    <X className="size-4 text-destructive" />
                  )}
                </div>
                <div className="grid grid-cols-3 gap-2 text-xs">
                  <div><span className="text-muted-foreground">Exp: </span>{item.qtyExpected}</div>
                  <div><span className="text-muted-foreground">Picked: </span>{item.qtyPicked}</div>
                  <div>
                    <input
                      type="number"
                      min={0}
                      value={verified}
                      onChange={(e) =>
                        setVerifiedQtys((prev) => ({ ...prev, [item.itemId]: Number(e.target.value) }))
                      }
                      className="w-16 h-6 rounded border border-border bg-background px-1 text-xs text-right"
                    />
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </>

      <div className="flex items-center justify-between text-xs">
        <div>
          {anyMismatch ? (
            <span className="text-destructive font-medium">Quantity mismatch detected</span>
          ) : (
            <span className="text-green-600 font-medium">All quantities match</span>
          )}
        </div>
        <button
          type="button"
          disabled={!allMatch}
          onClick={handleVerify}
          className="h-8 px-3 rounded-lg bg-primary text-primary-foreground text-xs font-medium cursor-pointer hover:bg-primary/80 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Confirm Packing Verification
        </button>
      </div>
    </div>
  );
}
