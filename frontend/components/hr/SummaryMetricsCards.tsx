"use client";

import { cn } from "@/lib/utils";

interface MetricCard {
  label: string;
  value: string;
  subtext?: string;
}

export function SummaryMetricsCards({ cards }: { cards: MetricCard[] }) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
      {cards.map((card) => (
        <div key={card.label} className="rounded-xl border border-border bg-card p-4">
          <p className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">{card.label}</p>
          <p className="mt-1.5 text-xl font-semibold text-foreground tabular-nums">{card.value}</p>
          {card.subtext && (
            <p className="mt-0.5 text-xs text-muted-foreground">{card.subtext}</p>
          )}
        </div>
      ))}
    </div>
  );
}
