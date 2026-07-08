"use client";

import { cn } from "@/lib/utils";
import { type LucideIcon } from "lucide-react";

interface DashboardKpiCardProps {
  label: string;
  value: string;
  icon: LucideIcon;
  trend?: { value: string; positive: boolean } | null;
}

export function DashboardKpiCard({ label, value, icon: Icon, trend }: DashboardKpiCardProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground">{label}</p>
          <p className="text-xl font-semibold text-foreground tabular-nums">{value}</p>
          {trend && (
            <p className={cn(
              "text-xs font-medium",
              trend.positive ? "text-emerald-600" : "text-red-600",
            )}>
              {trend.value}
            </p>
          )}
        </div>
        <div className="flex size-9 items-center justify-center rounded-lg bg-accent shrink-0">
          <Icon className="size-4 text-accent-foreground" />
        </div>
      </div>
    </div>
  );
}
