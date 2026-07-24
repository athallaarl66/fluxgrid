"use client";

import { useState } from "react";
import { usePermissions, type PermissionInfo } from "@/hooks/useAdmin";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

interface PermissionPickerProps {
  selected: string[];
  onChange: (perms: string[]) => void;
}

export function PermissionPicker({ selected, onChange }: PermissionPickerProps) {
  const { data: permissions, isLoading } = usePermissions();

  if (isLoading) {
    return <Skeleton className="h-40 w-full" />;
  }

  const grouped = (permissions ?? []).reduce<Record<string, PermissionInfo[]>>(
    (acc, p) => {
      (acc[p.module] ??= []).push(p);
      return acc;
    },
    {},
  );

  function toggle(perm: string) {
    onChange(
      selected.includes(perm)
        ? selected.filter((p) => p !== perm)
        : [...selected, perm],
    );
  }

  function toggleModule(module: string, perms: string[]) {
    const allSelected = perms.every((p) => selected.includes(p));
    if (allSelected) {
      onChange(selected.filter((p) => !perms.includes(p)));
    } else {
      onChange([...new Set([...selected, ...perms])]);
    }
  }

  return (
    <div className="space-y-3 max-h-60 overflow-y-auto pr-1">
      {Object.entries(grouped).map(([module, perms]) => {
        const allSelected = perms.every((p) => selected.includes(p.permission));
        const someSelected = perms.some((p) => selected.includes(p.permission)) && !allSelected;
        return (
          <div key={module}>
            <button
              type="button"
              onClick={() => toggleModule(module, perms.map((p) => p.permission))}
              className={cn(
                "flex items-center gap-2 text-xs font-semibold uppercase tracking-wider mb-1 cursor-pointer",
                allSelected ? "text-accent-foreground" : "text-muted-foreground",
              )}
            >
              <span
                className={cn(
                  "size-3.5 rounded border flex items-center justify-center shrink-0",
                  allSelected
                    ? "bg-accent border-accent"
                    : someSelected
                      ? "bg-accent/50 border-accent"
                      : "border-border",
                )}
              >
                {allSelected && (
                  <svg className="size-2.5 text-accent-foreground" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M2 6l3 3 5-5" />
                  </svg>
                )}
              </span>
              {module}
            </button>
            <div className="ml-5 space-y-0.5">
              {perms.map((p) => (
                <label
                  key={p.permission}
                  className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground cursor-pointer py-0.5"
                >
                  <input
                    type="checkbox"
                    checked={selected.includes(p.permission)}
                    onChange={() => toggle(p.permission)}
                    className="size-3.5 rounded border-border accent-accent"
                  />
                  <span className="truncate">{p.description}</span>
                  <span className="text-[10px] text-muted-foreground/60 ml-auto shrink-0">
                    {p.permission}
                  </span>
                </label>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
