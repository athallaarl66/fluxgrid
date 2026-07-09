"use client";

import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";

interface MaskedFieldProps {
  label: string;
  value: string;
  mask?: string;
}

export function MaskedField({ label, value, mask = "***" }: MaskedFieldProps) {
  const [revealed, setRevealed] = useState(false);

  return (
    <div className="space-y-1">
      <p className="text-xs text-muted-foreground">{label}</p>
      <div className="flex items-center gap-2">
        <p className="text-sm font-medium text-foreground font-mono tabular-nums">
          {revealed ? value : mask}
        </p>
        <button
          type="button"
          onClick={() => setRevealed(!revealed)}
          className="flex size-6 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
          title={revealed ? "Hide" : "Reveal"}
        >
          {revealed ? <EyeOff className="size-3.5" /> : <Eye className="size-3.5" />}
        </button>
      </div>
    </div>
  );
}
