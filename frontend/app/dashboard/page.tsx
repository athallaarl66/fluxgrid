"use client";

import { useDashboard } from "@/hooks/useDashboard";
import { ModuleCard } from "@/components/ModuleCard";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { RefreshCw } from "lucide-react";

const userPermissions = [
  "Dashboard:Read",
  "WMS:Read",
  "Finance:Read",
  "HR:Read",
  "Task:Read",
];

const permissionMap: Record<string, string[]> = {
  WMS: ["WMS:Read"],
  Finance: ["Finance:Read"],
  HR: ["HR:Read"],
  Projects: ["Task:Read"],
};

function hasModulePermission(moduleName: string): boolean {
  const required = permissionMap[moduleName];
  if (!required) return true;
  return required.some((p) => userPermissions.includes(p));
}

const barHeights = [
  [55, 38],
  [42, 48],
  [60, 32],
  [35, 45],
];

const resourceHeights = [
  [42, 14],
  [38, 18],
  [52, 22],
  [48, 16],
  [60, 24],
  [44, 20],
  [30, 26],
];

export default function DashboardPage() {
  const { data: modules, isLoading, isError, refetch, isFetching } = useDashboard();

  return (
    <div className="p-5 space-y-6">
      <div>
        <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
          Operational Overview
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          System status and core module KPIs at a glance
        </p>
      </div>

      {isError ? (
        <div className="rounded-lg border border-destructive bg-destructive/10 p-6 text-center">
          <p className="text-sm font-medium text-destructive">
            Failed to load dashboard data
          </p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            className="mt-3 border-ring text-muted-foreground"
          >
            <RefreshCw className="mr-1.5 size-3.5" />
            Retry
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {isLoading
            ? Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="rounded-lg border border-border bg-card p-4">
                  <div className="flex items-start gap-3">
                    <Skeleton className="size-10 shrink-0 rounded" />
                    <div className="flex-1 space-y-2">
                      <div className="flex items-center justify-between gap-2">
                        <Skeleton className="h-4 w-24" />
                        <Skeleton className="h-5 w-16 rounded-full" />
                      </div>
                      <Skeleton className="h-3 w-full" />
                    </div>
                  </div>
                </div>
              ))
            : modules?.map((mod) => (
                <ModuleCard
                  key={mod.name}
                  module={mod}
                  hasPermission={hasModulePermission(mod.name)}
                />
              ))}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
        <div className="rounded-lg border border-border bg-card p-5">
          <h3 className="text-[12px] font-medium tracking-[0.02em] text-muted-foreground mb-4 uppercase">
            Task Completion vs Attendance
          </h3>
          <div className="flex items-end gap-2 h-32">
            {["WMS", "FIN", "HR", "PRJ"].map((label, i) => (
              <div key={label} className="flex-1 flex flex-col items-center gap-1">
                <div className="w-full flex gap-0.5">
                  <div
                    className="flex-1 rounded-sm bg-accent"
                    style={{ height: `${barHeights[i][0]}px` }}
                  />
                  <div
                    className="flex-1 rounded-sm bg-ring"
                    style={{ height: `${barHeights[i][1]}px` }}
                  />
                </div>
                <span className="text-[11px] font-semibold text-muted-foreground">
                  {label}
                </span>
              </div>
            ))}
          </div>
          <div className="flex items-center gap-4 mt-3 text-[11px] text-muted-foreground">
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-accent" /> Tasks
            </span>
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-ring" /> Attendance
            </span>
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <h3 className="text-[12px] font-medium tracking-[0.02em] text-muted-foreground mb-4 uppercase">
            Resource Utilization
          </h3>
          <div className="flex items-end gap-2 h-32">
            {["08", "10", "12", "14", "16", "18", "20"].map((hour, i) => (
              <div key={hour} className="flex-1 flex flex-col items-center gap-1">
                <div className="w-full flex flex-col items-center gap-0.5">
                  <div
                    className="w-full rounded-sm bg-accent"
                    style={{ height: `${resourceHeights[i][0]}px` }}
                  />
                  <div
                    className="w-1.5 rounded-full bg-primary"
                    style={{ height: `${resourceHeights[i][1]}px` }}
                  />
                </div>
                <span className="text-[11px] font-semibold text-muted-foreground">
                  {hour}
                </span>
              </div>
            ))}
          </div>
          <div className="flex items-center gap-4 mt-3 text-[11px] text-muted-foreground">
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-accent" /> Capacity
            </span>
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-full bg-primary" /> Live
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
