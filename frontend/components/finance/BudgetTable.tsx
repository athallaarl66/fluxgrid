"use client";

import { Pencil, Trash2, RefreshCw } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import type { BudgetResponse, PaginatedResult } from "@/lib/budget-types";

interface BudgetTableProps {
  data: PaginatedResult<BudgetResponse> | undefined;
  isLoading: boolean;
  isFetching?: boolean;
  onRefresh?: () => void;
  onEdit: (budget: BudgetResponse) => void;
  onDelete: (budget: BudgetResponse) => void;
}

export function BudgetTable({
  data,
  isLoading,
  isFetching,
  onRefresh,
  onEdit,
  onDelete,
}: BudgetTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2 animate-pulse">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="h-9 rounded bg-muted" />
        ))}
      </div>
    );
  }

  const items = data?.items ?? [];
  const empty = items.length === 0;

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted-foreground">
          {data ? `${data.total} budget${data.total !== 1 ? "s" : ""}` : ""}
        </p>
        {onRefresh && (
          <Button variant="ghost" size="sm" onClick={onRefresh} disabled={isFetching} className="h-7 px-2 text-muted-foreground cursor-pointer">
            <RefreshCw className={cn("size-3.5", isFetching && "animate-spin")} />
          </Button>
        )}
      </div>

      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Account</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Period</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D]">Planned Amount</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Notes</th>
              <th className="h-9 px-3 text-center text-[11px] font-semibold text-[#89986D] w-[70px]"></th>
            </tr>
          </thead>
          <tbody>
            {empty ? (
              <tr>
                <td colSpan={5} className="h-24 text-center text-sm text-muted-foreground">
                  No budgets yet
                </td>
              </tr>
            ) : (
              items.map((budget) => (
                <tr key={budget.id} className="group border-b border-border transition-colors hover:bg-muted/40">
                  <td className="h-9 px-3">
                    <span className="text-sm text-foreground">{budget.accountCode}</span>
                    <span className="ml-1.5 text-xs text-muted-foreground">— {budget.accountName}</span>
                  </td>
                  <td className="h-9 px-3 text-sm text-muted-foreground">{budget.periodName}</td>
                  <td className="h-9 px-3 text-right text-sm tabular-nums text-foreground font-medium">
                    {new Intl.NumberFormat("id-ID", { style: "decimal", minimumFractionDigits: 0 }).format(budget.plannedAmount)}
                  </td>
                  <td className="h-9 px-3 text-sm text-muted-foreground max-w-[200px] truncate">
                    {budget.notes ?? <span className="text-muted-foreground/40">—</span>}
                  </td>
                  <td className="h-9 px-3">
                    <div className="flex items-center justify-center gap-0.5">
                      <button type="button" onClick={() => onEdit(budget)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground opacity-0 group-hover:opacity-100 cursor-pointer" title="Edit">
                        <Pencil className="size-3.5" />
                      </button>
                      <button type="button" onClick={() => onDelete(budget)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive opacity-0 group-hover:opacity-100 cursor-pointer" title="Delete">
                        <Trash2 className="size-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-[11px] text-muted-foreground">
          <span>Page {data.page} of {data.totalPages}</span>
        </div>
      )}
    </div>
  );
}
