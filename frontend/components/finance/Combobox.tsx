"use client";

import { useState, useRef, useEffect } from "react";
import { Search, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import type { AccountOption } from "@/lib/coa-types";

interface ComboboxProps {
  options: AccountOption[];
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function Combobox({
  options,
  value,
  onChange,
  placeholder = "Select account...",
  disabled,
}: ComboboxProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const selected = options.find((o) => o.id === value);
  const filtered = query
    ? options.filter(
        (o) =>
          o.code.toLowerCase().includes(query.toLowerCase()) ||
          o.name.toLowerCase().includes(query.toLowerCase()),
      )
    : options;

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen(!open)}
        className={cn(
          "flex h-8 w-full items-center justify-between rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm transition-colors",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
          "disabled:pointer-events-none disabled:opacity-50",
          open && "border-ring",
        )}
      >
        <span className={cn("truncate", !selected && "text-muted-foreground")}>
          {selected ? `${selected.code} - ${selected.name}` : placeholder}
        </span>
        <Search className="size-3.5 shrink-0 text-muted-foreground" />
      </button>

      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-lg border border-border bg-popover shadow-lg">
          <div className="border-b border-border p-1.5">
            <input
              autoFocus
              placeholder="Type to filter..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="h-7 w-full rounded border border-input bg-transparent px-2 text-xs outline-none focus:border-ring"
            />
          </div>
          <div className="max-h-48 overflow-y-auto p-1">
            <button
              type="button"
              onClick={() => {
                onChange(null);
                setOpen(false);
                setQuery("");
              }}
              className={cn(
                "flex w-full items-center rounded px-2 py-1.5 text-left text-sm transition-colors hover:bg-muted",
                !value && "bg-muted",
              )}
            >
              <span className="text-muted-foreground">None (top-level)</span>
            </button>
            {filtered.map((option) => (
              <button
                key={option.id}
                type="button"
                onClick={() => {
                  onChange(option.id);
                  setOpen(false);
                  setQuery("");
                }}
                className={cn(
                  "flex w-full items-center justify-between rounded px-2 py-1.5 text-left text-sm transition-colors hover:bg-muted",
                  value === option.id && "bg-muted",
                )}
              >
                <span>
                  <span className="font-medium">{option.code}</span> -{" "}
                  {option.name}
                </span>
                {value === option.id && (
                  <Check className="size-3.5 text-primary" />
                )}
              </button>
            ))}
            {filtered.length === 0 && (
              <p className="px-2 py-3 text-center text-xs text-muted-foreground">
                No accounts found
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
