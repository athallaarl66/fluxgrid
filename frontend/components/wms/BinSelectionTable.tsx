"use client";

import { cn } from "@/lib/utils";
import { BinComboBox } from "./BinComboBox";
import type { PurchaseReceiptLine } from "@/lib/wms-types";

interface LocationOption {
  id: string;
  code: string;
  type: string;
}

interface BinSelectionTableProps {
  lines: PurchaseReceiptLine[];
  binAssignments: Record<string, string>;
  locations: LocationOption[];
  onBinChange: (lineId: string, locationId: string) => void;
}

export function BinSelectionTable({ lines, binAssignments, locations, onBinChange }: BinSelectionTableProps) {
  const quarantineLocs = locations.filter((l) => l.type === "QUARANTINE");
  const quarantineLoc = quarantineLocs.length > 0 ? quarantineLocs[0] : null;

  return (
    <>
      <div className="hidden md:block overflow-x-auto rounded-lg border border-border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] sticky top-0 bg-card">
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Item</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Ordered</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Received</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Passed</th>
              <th className="text-right px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Failed</th>
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Bin</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line) => {
              const hasFailedQty = line.qtyFailed > 0;
              return (
                <tr key={line.id} className="border-b border-border h-10">
                  <td className="px-3 py-1 text-xs">
                    <span className="font-medium">{line.itemSku || line.itemName || line.itemId.slice(0, 8)}</span>
                    {line.itemName && <span className="text-muted-foreground ml-1">— {line.itemName}</span>}
                  </td>
                  <td className="px-3 py-1 text-xs text-right">{line.orderedQty}</td>
                  <td className="px-3 py-1 text-xs text-right">{line.qtyReceived}</td>
                  <td className={cn("px-3 py-1 text-xs text-right", hasFailedQty ? "" : "font-medium")}>{line.qtyPassed}</td>
                  <td className={cn("px-3 py-1 text-xs text-right", hasFailedQty ? "text-red-600 font-medium" : "")}>{line.qtyFailed}</td>
                  <td className="px-3 py-1 min-w-[160px]">
                    {hasFailedQty ? (
                      <div className="space-y-1">
                        <div className="flex items-center gap-1">
                          <span className="text-[10px] text-muted-foreground w-10">Passed:</span>
                          <BinComboBox
                            locations={locations.filter((l) => l.type !== "QUARANTINE")}
                            value={binAssignments[line.id + "-good"] || ""}
                            onChange={(locId) => onBinChange(line.id + "-good", locId)}
                          />
                        </div>
                        <div className="flex items-center gap-1">
                          <span className="text-[10px] text-muted-foreground w-10">Failed:</span>
                          {quarantineLoc ? (
                            <BinComboBox
                              locations={[]}
                              value=""
                              onChange={() => {}}
                              readOnlyValue={`${quarantineLoc.code} (Quarantine)`}
                            />
                          ) : (
                            <span className="text-[10px] text-red-500">No quarantine bin</span>
                          )}
                        </div>
                      </div>
                    ) : (
                      <BinComboBox
                        locations={locations.filter((l) => l.type !== "QUARANTINE")}
                        value={binAssignments[line.id + "-good"] || ""}
                        onChange={(locId) => onBinChange(line.id + "-good", locId)}
                      />
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="md:hidden space-y-2">
        {lines.map((line) => {
          const hasFailedQty = line.qtyFailed > 0;
          return (
            <div key={line.id} className="rounded-lg border border-border bg-card p-3 space-y-2">
              <div className="text-xs font-medium">
                {line.itemSku || line.itemName || line.itemId.slice(0, 8)}
                {line.itemName && <span className="text-muted-foreground ml-1">— {line.itemName}</span>}
              </div>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <div><span className="text-muted-foreground">Ordered: </span><span className="font-medium">{line.orderedQty}</span></div>
                <div><span className="text-muted-foreground">Received: </span><span className="font-medium">{line.qtyReceived}</span></div>
                <div><span className="text-muted-foreground">Passed: </span><span className="font-medium">{line.qtyPassed}</span></div>
                <div><span className={cn("text-muted-foreground", hasFailedQty && "text-red-600")}>Failed: </span><span className={cn("font-medium", hasFailedQty && "text-red-600")}>{line.qtyFailed}</span></div>
              </div>
              <div>
                <label className="text-[10px] text-muted-foreground block mb-1">
                  {hasFailedQty ? "Good items bin" : "Bin"}
                </label>
                <BinComboBox
                  locations={locations.filter((l) => l.type !== "QUARANTINE")}
                  value={binAssignments[line.id + "-good"] || ""}
                  onChange={(locId) => onBinChange(line.id + "-good", locId)}
                />
              </div>
              {hasFailedQty && quarantineLoc && (
                <div>
                  <label className="text-[10px] text-red-500 block mb-1">Failed items → Quarantine</label>
                  <BinComboBox
                    locations={[]}
                    value=""
                    onChange={() => {}}
                    readOnlyValue={`${quarantineLoc.code} (Quarantine)`}
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>
    </>
  );
}
