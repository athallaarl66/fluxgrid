"use client";

import { useState } from "react";
import {
  useDashboardStats,
  useJournalTrend,
  useInventoryTrend,
  useModuleActivity,
} from "@/hooks/useDashboard";
import { useAuth } from "@/lib/auth-context";
import { Skeleton } from "@/components/ui/skeleton";
import { BarChart } from "@/components/dashboard/BarChart";
import { GroupedBarChart } from "@/components/dashboard/GroupedBarChart";
import { DonutChart } from "@/components/dashboard/DonutChart";
import { PeriodFilter } from "@/components/dashboard/PeriodFilter";
import {
  Warehouse,
  MapPin,
  Users,
  FileText,
  Briefcase,
  UserCheck,
  BookOpen,
  Calendar,
  ArrowRight,
  Wallet,
  ClipboardList,
  TrendingUp,
  Activity,
} from "lucide-react";

const moduleLinks = [
  { label: "WMS", href: "/wms", description: "Warehouse Management", icon: Warehouse, color: "bg-blue-50 dark:bg-blue-950/30 text-blue-600 dark:text-blue-400" },
  { label: "Finance", href: "/finance", description: "General Ledger & Reports", icon: Wallet, color: "bg-green-50 dark:bg-green-950/30 text-green-600 dark:text-green-400" },
  { label: "HR", href: "/hr", description: "Employees & Payroll", icon: Users, color: "bg-amber-50 dark:bg-amber-950/30 text-amber-600 dark:text-amber-400" },
  { label: "Projects", href: "/projects", description: "Task Management", icon: ClipboardList, color: "bg-purple-50 dark:bg-purple-950/30 text-purple-600 dark:text-purple-400" },
];

const quickActions = [
  { label: "Stock Ledger", href: "/wms/stock-ledger", icon: Warehouse },
  { label: "Journal Entries", href: "/finance/journal-entries", icon: FileText },
  { label: "Employees", href: "/hr/employees", icon: UserCheck },
  { label: "Recruitment", href: "/hr/recruitment", icon: Briefcase },
  { label: "Transfer Log", href: "/wms/transfers", icon: ArrowRight },
  { label: "Settings", href: "/settings", icon: Calendar },
];

function StatCard({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
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

function ChartSkeleton() {
  return (
    <div className="space-y-3">
      <Skeleton className="h-4 w-32" />
      <Skeleton className="h-[140px] w-full rounded-lg" />
    </div>
  );
}

export default function DashboardPage() {
  const [period, setPeriod] = useState(6);
  const { data: stats, isLoading, isError } = useDashboardStats();
  const { data: journalTrend, isLoading: journalLoading } = useJournalTrend(period);
  const { data: invTrend, isLoading: invLoading } = useInventoryTrend(period);
  const { data: activity, isLoading: actLoading } = useModuleActivity();
  const { user } = useAuth();

  const greeting = (() => {
    const h = new Date().getHours();
    if (h < 12) return "Good morning";
    if (h < 17) return "Good afternoon";
    return "Good evening";
  })();

  const activitySegments = activity?.map((a, i) => ({
    label: a.module,
    value: a.count,
    color: ["#3b82f6", "#22c55e", "#f59e0b"][i] || "#888",
  })) ?? [];

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
          {greeting}{user?.name ? `, ${user.name}` : ""}
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          FluxGrid ERP — here&apos;s your system at a glance
        </p>
      </div>

      {/* Module Quick Access */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {moduleLinks.map((m) => {
          const Icon = m.icon;
          return (
            <a key={m.label} href={m.href} className="group rounded-lg border border-border bg-card p-4 transition-colors hover:bg-muted/50">
              <div className={`flex size-9 items-center justify-center rounded-lg ${m.color} mb-3`}>
                <Icon className="size-4" />
              </div>
              <p className="text-sm font-semibold text-foreground">{m.label}</p>
              <p className="text-xs text-muted-foreground mt-0.5">{m.description}</p>
            </a>
          );
        })}
      </div>

      {/* Stats Row */}
      <div>
        <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
          System Overview
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {isLoading
            ? Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-[72px] rounded-lg" />)
            : stats && (
                <>
                  <StatCard label="Inventory Items" value={String(stats.inventoryItems)} icon={<Warehouse className="size-4 text-accent-foreground" />} />
                  <StatCard label="Locations" value={String(stats.locations)} icon={<MapPin className="size-4 text-accent-foreground" />} />
                  <StatCard label="Active Employees" value={String(stats.activeEmployees)} icon={<UserCheck className="size-4 text-accent-foreground" />} />
                  <StatCard label="Journal Entries" value={String(stats.journalEntries)} icon={<FileText className="size-4 text-accent-foreground" />} />
                  <StatCard label="Candidates" value={String(stats.candidates)} icon={<Briefcase className="size-4 text-accent-foreground" />} />
                  <StatCard label="Open Positions" value={String(stats.openJobPostings)} icon={<ClipboardList className="size-4 text-accent-foreground" />} />
                  <StatCard label="Chart of Accounts" value={String(stats.chartOfAccounts)} icon={<BookOpen className="size-4 text-accent-foreground" />} />
                  <StatCard label="Periods" value={String(stats.accountingPeriods)} icon={<Calendar className="size-4 text-accent-foreground" />} />
                </>
              )}
        </div>
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        {/* Journal Entries Trend */}
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <TrendingUp className="size-4 text-muted-foreground" />
              <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Journal Entries
              </h3>
            </div>
            <PeriodFilter value={period} onChange={setPeriod} />
          </div>
          {journalLoading ? (
            <ChartSkeleton />
          ) : journalTrend && journalTrend.length > 0 ? (
            <BarChart
              data={journalTrend.map((d) => ({ label: d.label, value: d.value }))}
              color="bg-blue-500/60 dark:bg-blue-400/50"
            />
          ) : (
            <p className="text-xs text-muted-foreground text-center py-10">No data yet</p>
          )}
        </div>

        {/* Inbound vs Outbound */}
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <Activity className="size-4 text-muted-foreground" />
              <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Inventory Movement
              </h3>
            </div>
            <PeriodFilter value={period} onChange={setPeriod} />
          </div>
          {invLoading ? (
            <ChartSkeleton />
          ) : invTrend && invTrend.length > 0 ? (
            <GroupedBarChart data={invTrend} />
          ) : (
            <p className="text-xs text-muted-foreground text-center py-10">No data yet</p>
          )}
        </div>

        {/* Module Activity Donut */}
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-4">
            <ClipboardList className="size-4 text-muted-foreground" />
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              This Month Activity
            </h3>
          </div>
          {actLoading ? (
            <ChartSkeleton />
          ) : activitySegments.length > 0 ? (
            <div className="flex items-center justify-center py-2">
              <DonutChart segments={activitySegments} />
            </div>
          ) : (
            <p className="text-xs text-muted-foreground text-center py-10">No data yet</p>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div>
        <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
          Quick Actions
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          {quickActions.map((a) => {
            const Icon = a.icon;
            return (
              <a
                key={a.href}
                href={a.href}
                className="flex items-center gap-2.5 rounded-lg border border-border bg-card px-3 py-2.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              >
                <Icon className="size-4 shrink-0" />
                {a.label}
              </a>
            );
          })}
        </div>
      </div>
    </div>
  );
}
