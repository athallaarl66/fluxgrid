"use client";

import { useState } from "react";

interface PeriodFilterProps {
  value: number;
  onChange: (months: number) => void;
}

const periods = [
  { label: "1M", months: 1 },
  { label: "6M", months: 6 },
  { label: "1Y", months: 12 },
];

export function PeriodFilter({ value, onChange }: PeriodFilterProps) {
  return (
    <div className="flex gap-0.5 rounded border border-border p-0.5">
      {periods.map((p) => (
        <button
          key={p.months}
          onClick={() => onChange(p.months)}
          className={`px-2 py-0.5 text-[10px] font-semibold rounded transition-colors cursor-pointer ${
            value === p.months
              ? "bg-accent text-accent-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {p.label}
        </button>
      ))}
    </div>
  );
}
