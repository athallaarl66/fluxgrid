"use client";

import { useMemo } from "react";
import { ChevronRight } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { flattenTree, type AccountResponse } from "@/lib/coa-types";

interface CoaMobileListProps {
  accounts: AccountResponse[];
  onEdit: (account: AccountResponse) => void;
  onDeactivate: (account: AccountResponse) => void;
  searchQuery: string;
}

function matchesQuery(account: AccountResponse, query: string): boolean {
  const q = query.toLowerCase();
  return (
    account.code.toLowerCase().includes(q) ||
    account.name.toLowerCase().includes(q)
  );
}

export function CoaMobileList({
  accounts,
  onEdit,
  onDeactivate,
  searchQuery,
}: CoaMobileListProps) {
  const items = useMemo(() => {
    const flat = flattenTree(accounts);
    return searchQuery
      ? flat.filter((a) => matchesQuery(a, searchQuery))
      : flat;
  }, [accounts, searchQuery]);

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <p className="text-sm font-medium text-muted-foreground">
          {searchQuery
            ? "No accounts match your search"
            : "No accounts yet"}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-1">
      {items.map((item) => (
        <div
          key={item.id}
          className="flex items-center gap-3 rounded-lg border border-border bg-card px-4 py-3"
        >
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-muted-foreground tabular-nums">
                {item.code}
              </span>
              {!item.isActive && (
                <Badge variant="secondary" className="text-[10px] h-4 px-1.5">
                  Inactive
                </Badge>
              )}
            </div>
            <p className="truncate text-sm font-medium text-foreground">
              {item.name}
            </p>
            <p className="truncate text-[11px] text-muted-foreground">
              {item.path}
            </p>
          </div>

          <div className="flex shrink-0 items-center gap-1">
            <button
              type="button"
              onClick={() => onEdit(item)}
              className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            >
              <ChevronRight className="size-4" />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
