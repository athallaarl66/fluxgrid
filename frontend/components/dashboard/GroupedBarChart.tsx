"use client";

interface GroupedBarChartProps {
  data: { label: string; inbound: number; outbound: number }[];
  height?: number;
}

export function GroupedBarChart({ data, height = 140 }: GroupedBarProps) {
  const max = Math.max(...data.flatMap((d) => [d.inbound, d.outbound]), 1);

  return (
    <div className="w-full">
      <div className="flex items-end gap-1.5" style={{ height }}>
        {data.map((d, i) => {
          const inPct = (d.inbound / max) * 100;
          const outPct = (d.outbound / max) * 100;
          return (
            <div key={i} className="flex-1 flex items-end justify-center gap-0.5 h-full">
              <div
                className="flex-1 rounded-t bg-green-500/70 dark:bg-green-400/60 transition-all duration-300"
                style={{ height: `${Math.max(inPct, 2)}%` }}
                title={`Inbound: ${d.inbound}`}
              />
              <div
                className="flex-1 rounded-t bg-amber-500/70 dark:bg-amber-400/60 transition-all duration-300"
                style={{ height: `${Math.max(outPct, 2)}%` }}
                title={`Outbound: ${d.outbound}`}
              />
            </div>
          );
        })}
      </div>
      <div className="flex gap-1.5 mt-2">
        {data.map((d, i) => (
          <div key={i} className="flex-1 text-center">
            <span className="text-[10px] font-medium text-muted-foreground">{d.label}</span>
          </div>
        ))}
      </div>
      <div className="flex items-center gap-4 mt-2 text-[10px] text-muted-foreground">
        <span className="flex items-center gap-1">
          <span className="size-2 rounded-sm bg-green-500/70" /> Inbound
        </span>
        <span className="flex items-center gap-1">
          <span className="size-2 rounded-sm bg-amber-500/70" /> Outbound
        </span>
      </div>
    </div>
  );
}

interface GroupedBarProps {
  data: { label: string; inbound: number; outbound: number }[];
  height?: number;
}
