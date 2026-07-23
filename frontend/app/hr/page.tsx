"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/lib/auth-context";
import { useHrDashboard } from "@/hooks/useHrDashboard";
import { Skeleton } from "@/components/ui/skeleton";
import { Users, GitBranch, DollarSign, FileText, Briefcase, UserCheck, TrendingUp } from "lucide-react";
import { formatDate } from "@/lib/date-utils";

function formatCurrency(value: number) {
  if (value >= 1_000_000)
    return `Rp ${(value / 1_000_000).toFixed(0)}M`;
  if (value >= 1_000)
    return `Rp ${(value / 1_000).toFixed(0)}K`;
  return `Rp ${Math.round(value).toLocaleString("id-ID")}`;
}

function KpiCard({ label, value, icon: Icon }: { label: string; value: string | number; icon: React.ElementType }) {
  return (
    <div className="flex flex-col gap-1.5 rounded-xl border border-border bg-card p-4">
      <div className="flex items-center gap-2">
        <div className="flex size-7 items-center justify-center rounded-md bg-accent">
          <Icon className="size-3.5 text-accent-foreground" />
        </div>
        <span className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider">{label}</span>
      </div>
      <p className="text-xl font-semibold text-foreground tabular-nums">{value}</p>
    </div>
  );
}

export default function HRDashboardPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { data, isLoading, error } = useHrDashboard();

  useEffect(() => {
    if (!authLoading && !user) router.push("/login?redirect=/hr");
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28 rounded-xl" />)}
        </div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Users className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Human Resources</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">HR overview and key metrics</p>
        </div>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28 rounded-xl" />)}
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load dashboard data</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : data ? (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <KpiCard label="Active Employees" value={data.activeEmployees} icon={UserCheck} />
            <KpiCard label="Total Employees" value={data.totalEmployees} icon={Users} />
            <KpiCard label="Payroll (MTD)" value={formatCurrency(data.payrollMtd)} icon={DollarSign} />
            <KpiCard label="Open Positions" value={data.publishedJobs} icon={Briefcase} />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="rounded-xl border border-border bg-card p-4">
              <h2 className="text-sm font-semibold text-foreground mb-3">Candidate Pipeline</h2>
              {data.totalCandidates === 0 ? (
                <p className="text-sm text-muted-foreground py-8 text-center">No candidates yet</p>
              ) : (
                <div className="space-y-2">
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-muted-foreground">Total</span>
                    <span className="text-sm font-semibold tabular-nums">{data.totalCandidates}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-muted-foreground">Active</span>
                    <span className="text-sm tabular-nums text-emerald-600">{data.candidatePipeline.active}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-muted-foreground">Parsed</span>
                    <span className="text-sm tabular-nums text-amber-600">{data.candidatePipeline.parsed}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-xs text-muted-foreground">Rejected</span>
                    <span className="text-sm tabular-nums text-red-600">{data.candidatePipeline.rejected}</span>
                  </div>
                </div>
              )}
            </div>

            <div className="rounded-xl border border-border bg-card p-4">
              <h2 className="text-sm font-semibold text-foreground mb-3">Recent Hires</h2>
              {data.recentHires.length === 0 ? (
                <p className="text-sm text-muted-foreground py-8 text-center">No recent hires</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Name</th>
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Title</th>
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Dept</th>
                        <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Hired</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.recentHires.map((h) => (
                        <tr key={h.id} className="border-b border-border hover:bg-muted/40">
                          <td className="h-8 px-2 text-xs text-foreground font-medium">{h.firstName} {h.lastName}</td>
                          <td className="h-8 px-2 text-xs text-muted-foreground">{h.jobTitle}</td>
                          <td className="h-8 px-2 text-xs text-muted-foreground">{h.department}</td>
                          <td className="h-8 px-2 text-right text-xs text-muted-foreground tabular-nums">
                            {formatDate(h.hireDate)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <ModuleCard href="/hr/employees" icon={Users} label="Employees" desc="Manage employee records" />
            <ModuleCard href="/hr/payroll" icon={DollarSign} label="Payroll" desc="Payroll processing & reports" />
            <ModuleCard href="/hr/recruitment" icon={FileText} label="Recruitment" desc="Candidates & job postings" />
            <ModuleCard href="/hr/org-chart" icon={GitBranch} label="Org Chart" desc="Organizational structure" />
          </div>
        </>
      ) : null}
    </div>
  );
}

function ModuleCard({ href, icon: Icon, label, desc }: { href: string; icon: React.ElementType; label: string; desc: string }) {
  return (
    <Link
      href={href}
      className="flex flex-col gap-2 rounded-xl border border-border bg-card p-4 hover:bg-muted/40 transition-colors"
    >
      <div className="flex size-8 items-center justify-center rounded-lg bg-accent">
        <Icon className="size-4 text-accent-foreground" />
      </div>
      <div>
        <p className="text-sm font-semibold text-foreground">{label}</p>
        <p className="text-xs text-muted-foreground mt-0.5">{desc}</p>
      </div>
    </Link>
  );
}
