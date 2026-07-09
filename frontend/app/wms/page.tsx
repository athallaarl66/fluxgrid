"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { apiClient } from "@/lib/api-client";
import { Warehouse, ScrollText, PackageOpen, ArrowRightFromLine, ArrowLeftFromLine } from "lucide-react";

interface WmsStats {
  itemCount: number;
  locationCount: number;
  inboundMtd: number;
  outboundMtd: number;
}

export default function WmsDashboardPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [stats, setStats] = useState<WmsStats | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/wms");
    }
  }, [user, authLoading, router]);

  useEffect(() => {
    if (user) {
      apiClient<WmsStats>("/api/v1/wms/dashboard")
        .then(setStats)
        .catch(() => setStats(null))
        .finally(() => setStatsLoading(false));
    }
  }, [user]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Warehouse className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            WMS Dashboard
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Warehouse management overview and quick access
          </p>
        </div>
      </div>

      <WmsNav />

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <StatCard
          label="Total SKUs"
          value={statsLoading ? "—" : String(stats?.itemCount ?? 0)}
          icon={<PackageOpen className="size-4 text-accent-foreground" />}
        />
        <StatCard
          label="Locations"
          value={statsLoading ? "—" : String(stats?.locationCount ?? 0)}
          icon={<Warehouse className="size-4 text-accent-foreground" />}
        />
        <StatCard
          label="Inbound MTD"
          value={statsLoading ? "—" : String(Math.round(stats?.inboundMtd ?? 0))}
          icon={<ArrowLeftFromLine className="size-4 text-accent-foreground" />}
        />
        <StatCard
          label="Outbound MTD"
          value={statsLoading ? "—" : String(Math.round(stats?.outboundMtd ?? 0))}
          icon={<ArrowRightFromLine className="size-4 text-accent-foreground" />}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="rounded-xl border border-border bg-card p-4">
          <h2 className="text-sm font-semibold text-foreground mb-3">Quick Actions</h2>
          <div className="space-y-2">
            <a
              href="/wms/stock-ledger"
              className="flex items-center gap-3 rounded-lg border border-border p-3 hover:bg-muted/50 transition-colors"
            >
              <ScrollText className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium text-foreground">View Stock Ledger</p>
                <p className="text-xs text-muted-foreground">Track all inventory movements</p>
              </div>
            </a>
            <a
              href="/wms/inbound"
              className="flex items-center gap-3 rounded-lg border border-border p-3 hover:bg-muted/50 transition-colors"
            >
              <ArrowLeftFromLine className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium text-foreground">Inbound Processing</p>
                <p className="text-xs text-muted-foreground">Receive purchase orders and returns</p>
              </div>
            </a>
            <a
              href="/wms/outbound"
              className="flex items-center gap-3 rounded-lg border border-border p-3 hover:bg-muted/50 transition-colors"
            >
              <ArrowRightFromLine className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium text-foreground">Outbound Processing</p>
                <p className="text-xs text-muted-foreground">Process shipments and transfers</p>
              </div>
            </a>
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-4">
          <h2 className="text-sm font-semibold text-foreground mb-3">Recent Activity</h2>
          <p className="text-sm text-muted-foreground py-8 text-center">
            No recent movements — start by recording inventory movements in Stock Ledger
          </p>
        </div>
      </div>
    </div>
  );
}

function StatCard({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground">{label}</p>
          <p className="text-xl font-semibold text-foreground tabular-nums">{value}</p>
        </div>
        <div className="flex size-9 items-center justify-center rounded-lg bg-accent shrink-0">
          {icon}
        </div>
      </div>
    </div>
  );
}
