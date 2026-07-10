"use client";

import { cn } from "@/lib/utils";

interface ProgressProps {
  value: number;
  max?: number;
  className?: string;
}

export function Progress({ value, max = 100, className }: ProgressProps) {
  const pct = Math.min(Math.max(value, 0), max);

  return (
    <div
      role="progressbar"
      aria-valuenow={pct}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-label={`${Math.round((pct / max) * 100)} percent complete`}
      className={cn("h-2 w-full rounded-full bg-muted overflow-hidden", className)}
    >
      <div
        className="h-full rounded-full bg-primary transition-all duration-300 ease-out"
        style={{ width: `${(pct / max) * 100}%` }}
      />
    </div>
  );
}
