"use client";

import { cn } from "@/lib/utils";

interface ValuationToggleProps {
  value: "fifo" | "average";
  onChange: (value: "fifo" | "average") => void;
}

export function ValuationToggle({ value, onChange }: ValuationToggleProps) {
  return (
    <div className="flex items-center gap-1 rounded-lg border border-[#9CAB84] p-0.5">
      <button
        onClick={() => onChange("fifo")}
        className={cn(
          "rounded-md px-3 py-1 text-xs font-medium transition-colors cursor-pointer",
          value === "fifo"
            ? "bg-[#C5D89D] text-[#89986D]"
            : "text-muted-foreground hover:text-foreground",
        )}
      >
        FIFO
      </button>
      <button
        onClick={() => onChange("average")}
        className={cn(
          "rounded-md px-3 py-1 text-xs font-medium transition-colors cursor-pointer",
          value === "average"
            ? "bg-[#C5D89D] text-[#89986D]"
            : "text-muted-foreground hover:text-foreground",
        )}
      >
        Average Cost
      </button>
    </div>
  );
}
