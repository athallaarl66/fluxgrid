"use client";

import { useState, useRef, useEffect } from "react";

interface LocationOption {
  id: string;
  code: string;
  type: string;
}

interface BinComboBoxProps {
  locations: LocationOption[];
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  readOnlyValue?: string;
}

export function BinComboBox({ locations, value, onChange, disabled, readOnlyValue }: BinComboBoxProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const ref = useRef<HTMLDivElement>(null);

  const selected = locations.find((l) => l.id === value);

  const filtered = locations.filter(
    (l) => l.code.toLowerCase().includes(search.toLowerCase()) || l.type.toLowerCase().includes(search.toLowerCase()),
  );

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (readOnlyValue) {
    return (
      <div className="h-7 flex items-center px-2 rounded bg-muted text-xs text-muted-foreground">
        {readOnlyValue}
      </div>
    );
  }

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => !disabled && setOpen(!open)}
        disabled={disabled}
        className="w-full h-7 flex items-center justify-between px-2 rounded border border-input bg-transparent text-xs text-left outline-none focus-visible:border-ring cursor-pointer disabled:opacity-50"
      >
        <span className={selected ? "" : "text-muted-foreground"}>
          {selected ? selected.code : "Select bin..."}
        </span>
        <span className="text-muted-foreground text-[10px]">▼</span>
      </button>

      {open && (
        <div className="absolute z-50 top-full mt-1 w-full min-w-[200px] rounded-lg border border-border bg-card shadow-lg">
          <div className="p-1">
            <input
              type="text"
              placeholder="Search bin..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              autoFocus
              className="w-full h-7 rounded border border-input bg-transparent px-2 text-xs outline-none focus-visible:border-ring"
            />
          </div>
          <div className="max-h-40 overflow-y-auto">
            {filtered.length === 0 ? (
              <div className="px-2 py-3 text-xs text-muted-foreground text-center">No locations found</div>
            ) : (
              filtered.map((loc) => (
                <button
                  key={loc.id}
                  type="button"
                  onClick={() => {
                    onChange(loc.id);
                    setOpen(false);
                    setSearch("");
                  }}
                  className="w-full flex items-center justify-between px-2 py-1.5 text-xs hover:bg-muted text-left cursor-pointer"
                >
                  <span>{loc.code}</span>
                  <span className="text-[10px] text-muted-foreground">{loc.type}</span>
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
