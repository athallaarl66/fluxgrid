"use client";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import type { MonthlyTrendRow } from "@/lib/dashboard-types";

interface DashboardChartProps {
  data: MonthlyTrendRow[];
}

const MONTH_LABELS = [
  "Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
];

export function DashboardChart({ data }: DashboardChartProps) {
  if (data.length === 0) {
    return (
      <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">
        No trend data available
      </div>
    );
  }

  const chartData = data.map((d) => ({
    month: MONTH_LABELS[d.month - 1] || String(d.month),
    Revenue: d.revenue,
    Expenses: d.expenses,
  }));

  return (
    <div className="h-64">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} barCategoryGap="20%">
          <CartesianGrid strokeDasharray="3 3" stroke="#e5debf" />
          <XAxis dataKey="month" tick={{ fontSize: 11, fill: "#49473e" }} axisLine={{ stroke: "#e5debf" }} />
          <YAxis tick={{ fontSize: 11, fill: "#49473e" }} axisLine={{ stroke: "#e5debf" }} tickFormatter={(v: number) => v >= 1_000_000 ? `${(v / 1_000_000).toFixed(0)}M` : v >= 1_000 ? `${(v / 1_000).toFixed(0)}K` : String(v)} />
          <Tooltip
            contentStyle={{
              background: "#fff",
              border: "1px solid #e5debf",
              borderRadius: "0.5rem",
              fontSize: "12px",
            }}
          />
          <Legend wrapperStyle={{ fontSize: "11px" }} />
          <Bar dataKey="Revenue" fill="#546434" radius={[3, 3, 0, 0]} maxBarSize={32} />
          <Bar dataKey="Expenses" fill="#ba1a1a" radius={[3, 3, 0, 0]} maxBarSize={32} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
