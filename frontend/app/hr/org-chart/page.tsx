"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Users } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useOrgChart } from "@/hooks/useOrgChart";
import { OrgChartTree } from "@/components/hr/OrgChartTree";
import { OrgChartMobileList } from "@/components/hr/OrgChartMobileList";
import { Skeleton } from "@/components/ui/skeleton";

export default function OrgChartPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { data, isLoading, error } = useOrgChart();
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const mq = window.matchMedia("(max-width: 767px)");
    setIsMobile(mq.matches);
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    mq.addEventListener("change", handler);
    return () => mq.removeEventListener("change", handler);
  }, []);

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/org-chart");
  }

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-[600px] w-full rounded-lg" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Users className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Organization Chart
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {data ? `${data.length} root ${data.length === 1 ? "unit" : "units"}` : "Company structure"}
          </p>
        </div>
      </div>

      {isLoading ? (
        <Skeleton className="h-[600px] w-full rounded-lg" />
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load org chart</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : data && data.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Users className="size-12 text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-foreground">No employees found</p>
          <p className="text-xs text-muted-foreground mt-1">Add employees to build your organization chart</p>
        </div>
      ) : data ? (
        isMobile ? (
          <OrgChartMobileList nodes={data} />
        ) : (
          <OrgChartTree nodes={data} />
        )
      ) : null}
    </div>
  );
}
