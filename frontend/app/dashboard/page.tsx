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

export default function DashboardPage() {
  const { data: modules, isLoading, isError, refetch, isFetching } = useDashboard();

  return (
    <div className="p-5 space-y-6">
      <div>
        <h1 className="text-2xl font-semibold leading-tight tracking-tight text-[#1c1b1a]">
          Operational Overview
        </h1>
        <p className="mt-1 text-sm text-[#49473e]">
          System status and core module KPIs at a glance
        </p>
      </div>

      {isError ? (
        <div className="rounded-lg border border-[#ba1a1a] bg-[#ffdad6] p-6 text-center">
          <p className="text-sm font-medium text-[#93000a]">
            Failed to load dashboard data
          </p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            className="mt-3 border-[#9cab84] text-[#706d59]"
          >
            <RefreshCw className="mr-1.5 size-3.5" />
            Retry
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {isLoading
            ? Array.from({ length: 4 }).map((_, i) => (
                <div
                  key={i}
                  className="rounded-lg border border-[#e5debf] bg-white p-4"
                >
                  <div className="flex items-start gap-3">
                    <Skeleton className="size-10 shrink-0 rounded-lg" />
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
        <div className="rounded-lg border border-[#e5debf] bg-white p-5">
          <h3 className="text-sm font-semibold text-[#1c1b1a] mb-4">
            Task Completion vs Attendance
          </h3>
          <div className="flex items-end gap-2 h-32">
            {["WMS", "FIN", "HR", "PRJ"].map((label) => (
              <div key={label} className="flex-1 flex flex-col items-center gap-1">
                <div className="w-full flex gap-0.5">
                  <div
                    className="flex-1 rounded-sm bg-[#c5d89d]"
                    style={{ height: `${40 + Math.random() * 40}px` }}
                  />
                  <div
                    className="flex-1 rounded-sm bg-[#9cab84]"
                    style={{ height: `${30 + Math.random() * 30}px` }}
                  />
                </div>
                <span className="text-[11px] font-semibold text-[#49473e]">
                  {label}
                </span>
              </div>
            ))}
          </div>
          <div className="flex items-center gap-4 mt-3 text-[11px] text-[#49473e]">
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-[#c5d89d]" /> Tasks
            </span>
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-[#9cab84]" /> Attendance
            </span>
          </div>
        </div>

        <div className="rounded-lg border border-[#e5debf] bg-white p-5">
          <h3 className="text-sm font-semibold text-[#1c1b1a] mb-4">
            Resource Utilization
          </h3>
          <div className="flex items-end gap-2 h-32">
            {["08", "10", "12", "14", "16", "18", "20"].map((hour) => (
              <div key={hour} className="flex-1 flex flex-col items-center gap-1">
                <div className="w-full flex flex-col items-center gap-0.5">
                  <div
                    className="w-full rounded-sm bg-[#c5d89d]"
                    style={{ height: `${20 + Math.random() * 50}px` }}
                  />
                  <div
                    className="w-1.5 rounded-full bg-[#625f4b]"
                    style={{ height: `${10 + Math.random() * 20}px` }}
                  />
                </div>
                <span className="text-[11px] font-semibold text-[#49473e]">
                  {hour}
                </span>
              </div>
            ))}
          </div>
          <div className="flex items-center gap-4 mt-3 text-[11px] text-[#49473e]">
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-sm bg-[#c5d89d]" /> Capacity
            </span>
            <span className="flex items-center gap-1">
              <span className="size-2.5 rounded-full bg-[#625f4b]" /> Live
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
