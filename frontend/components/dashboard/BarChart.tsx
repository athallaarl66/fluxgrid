"use client";

interface BarChartProps {
  data: { label: string; value: number }[];
  height?: number;
  color?: string;
  showValues?: boolean;
}

export function BarChart({ data, height = 140, color = "bg-accent", showValues = true }: BarChartProps) {
  const max = Math.max(...data.map((d) => d.value), 1);

  return (
    <div className="w-full">
      <div className="flex items-end gap-1.5" style={{ height }}>
        {data.map((d, i) => {
          const pct = (d.value / max) * 100;
          return (
            <div key={i} className="flex-1 flex flex-col items-center justify-end h-full">
              {showValues && d.value > 0 && (
                <span className="text-[10px] font-medium text-muted-foreground mb-1 tabular-nums">
                  {d.value.toLocaleString()}
                </span>
              )}
              <div
                className={`w-full rounded-t ${color} transition-all duration-300`}
                style={{ height: `${Math.max(pct, 2)}%` }}
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
    </div>
  );
}
