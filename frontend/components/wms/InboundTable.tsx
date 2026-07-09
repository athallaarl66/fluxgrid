"use client";

import type { PurchaseReceipt } from "@/lib/wms-types";
import { cn } from "@/lib/utils";

interface InboundTableProps {
  receipts: PurchaseReceipt[];
  onProcessPutaway: (id: string) => void;
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function statusBadge(status: string) {
  switch (status) {
    case "DRAFT":
      return { label: "Draft", className: "bg-gray-100 text-gray-700" };
    case "PENDING_PUTAWAY":
      return { label: "Pending Putaway", className: "bg-yellow-100 text-yellow-700" };
    case "COMPLETED":
      return { label: "Completed", className: "bg-green-100 text-green-700" };
    default:
      return { label: status, className: "bg-gray-100 text-gray-700" };
  }
}

export function InboundTable({ receipts, onProcessPutaway }: InboundTableProps) {
  return (
    <>
      <div className="hidden md:block overflow-x-auto rounded-lg border border-border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] sticky top-0 bg-card">
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Receipt No</th>
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">PO Ref</th>
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Date</th>
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Status</th>
              <th className="text-left px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody>
            {receipts.map((r) => {
              const badge = statusBadge(r.status);
              return (
                <tr key={r.id} className="border-b border-border hover:bg-muted/50 h-9">
                  <td className="px-3 py-1 text-xs font-mono">{r.receiptNo}</td>
                  <td className="px-3 py-1 text-xs">{r.poReference}</td>
                  <td className="px-3 py-1 text-xs">{formatDate(r.createdAt)}</td>
                  <td className="px-3 py-1">
                    <span className={cn("inline-block rounded-full px-2 py-0.5 text-[11px] font-medium", badge.className)}>
                      {badge.label}
                    </span>
                  </td>
                  <td className="px-3 py-1">
                    {r.status === "PENDING_PUTAWAY" ? (
                      <button
                        onClick={() => onProcessPutaway(r.id)}
                        className="text-xs font-medium text-blue-600 hover:text-blue-800 underline cursor-pointer"
                      >
                        Process Putaway
                      </button>
                    ) : (
                      <span className="text-xs text-muted-foreground">—</span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="md:hidden space-y-2">
        {receipts.map((r) => {
          const badge = statusBadge(r.status);
          return (
            <div key={r.id} className="rounded-lg border border-border bg-card p-3 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-mono font-medium">{r.receiptNo}</span>
                <span className={cn("inline-block rounded-full px-2 py-0.5 text-[11px] font-medium", badge.className)}>
                  {badge.label}
                </span>
              </div>
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>PO: {r.poReference}</span>
                <span>{formatDate(r.createdAt)}</span>
              </div>
              {r.status === "PENDING_PUTAWAY" && (
                <button
                  onClick={() => onProcessPutaway(r.id)}
                  className="w-full text-xs font-medium text-blue-600 hover:text-blue-800 underline cursor-pointer text-center"
                >
                  Process Putaway
                </button>
              )}
            </div>
          );
        })}
      </div>
    </>
  );
}
