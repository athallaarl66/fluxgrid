"use client";

import { useState } from "react";
import {
  ChevronRight,
  ChevronDown,
  MoreHorizontal,
  Pencil,
  Trash2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import type { AccountResponse } from "@/lib/coa-types";

interface CoaTreeItemProps {
  account: AccountResponse;
  depth: number;
  onEdit: (account: AccountResponse) => void;
  onDeactivate: (account: AccountResponse) => void;
}

export function CoaTreeItem({
  account,
  depth,
  onEdit,
  onDeactivate,
}: CoaTreeItemProps) {
  const [expanded, setExpanded] = useState(depth === 0);
  const [menuOpen, setMenuOpen] = useState(false);
  const hasChildren = account.children && account.children.length > 0;

  return (
    <div>
      <div
        className={cn(
          "group flex items-center gap-2 rounded-lg px-3 py-2 transition-colors hover:bg-muted/50",
          !account.isActive && "opacity-60",
        )}
        style={{ paddingLeft: `${12 + depth * 24}px` }}
      >
        <button
          type="button"
          onClick={() => setExpanded(!expanded)}
          className={cn(
            "flex size-5 shrink-0 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted",
            !hasChildren && "invisible",
          )}
        >
          {expanded ? (
            <ChevronDown className="size-3.5" />
          ) : (
            <ChevronRight className="size-3.5" />
          )}
        </button>

        <span className="min-w-[5rem] text-sm font-medium text-muted-foreground tabular-nums">
          {account.code}
        </span>

        <span className="flex-1 truncate text-sm font-medium text-foreground">
          {account.name}
        </span>

        {!account.isActive && (
          <Badge variant="secondary" className="shrink-0">
            Inactive
          </Badge>
        )}

        <div className="relative shrink-0">
          <button
            type="button"
            onClick={() => setMenuOpen(!menuOpen)}
            className="flex size-7 items-center justify-center rounded text-muted-foreground opacity-0 transition-all hover:bg-muted hover:text-foreground group-hover:opacity-100"
          >
            <MoreHorizontal className="size-4" />
          </button>

          {menuOpen && (
            <>
              <div
                className="fixed inset-0 z-10"
                onClick={() => setMenuOpen(false)}
              />
              <div className="absolute right-0 z-20 mt-1 w-36 rounded-lg border border-border bg-popover py-1 shadow-lg">
                <button
                  type="button"
                  onClick={() => {
                    setMenuOpen(false);
                    onEdit(account);
                  }}
                  className="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors hover:bg-muted"
                >
                  <Pencil className="size-3.5 text-muted-foreground" />
                  Edit
                </button>
                {account.isActive && (
                  <button
                    type="button"
                    onClick={() => {
                      setMenuOpen(false);
                      onDeactivate(account);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm text-destructive transition-colors hover:bg-destructive/10"
                  >
                    <Trash2 className="size-3.5" />
                    Deactivate
                  </button>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {expanded && hasChildren && (
        <div>
          {account.children.map((child) => (
            <CoaTreeItem
              key={child.id}
              account={child}
              depth={depth + 1}
              onEdit={onEdit}
              onDeactivate={onDeactivate}
            />
          ))}
        </div>
      )}
    </div>
  );
}
