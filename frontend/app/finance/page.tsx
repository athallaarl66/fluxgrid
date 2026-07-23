"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useFinanceDashboard } from "@/hooks/useFinanceDashboard";
import { Skeleton } from "@/components/ui/skeleton";
import { FinanceNav } from "@/components/finance/FinanceNav";
import { DashboardKpiCard } from "@/components/finance/DashboardKpiCard";
import { DashboardChart } from "@/components/finance/DashboardChart";
import {
  Wallet,
  Banknote,
  Scale,
  TrendingUp,
  TrendingDown,
  LineChart,
  FileText,
} from "lucide-react";
import { formatDate } from "@/lib/date-utils";

function formatCurrency(value: number) {
  if (value >= 1_000_000_000)
    return `Rp ${(value / 1_000_000_000).toFixed(1)}B`;
  if (value >= 1_000_000)
    return `Rp ${(value / 1_000_000).toFixed(0)}M`;
  if (value >= 1_000)
    return `Rp ${(value / 1_000).toFixed(0)}K`;
  return `Rp ${Math.round(value).toLocaleString("id-ID")}`;
}

export default function FinanceDashboardPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const currentYear = new Date().getFullYear();
  const [chartYear, setChartYear] = useState(currentYear);
  const { data, isLoading, error } = useFinanceDashboard(chartYear);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/finance");
    }
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  if (error) {
    return (
      <div className="p-5 space-y-6 animate-fade-in">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
            <Wallet className="size-5 text-accent-foreground" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Finance Dashboard</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">Financial overview and key performance indicators</p>
          </div>
        </div>
        <FinanceNav />
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load dashboard data</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Wallet className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Finance Dashboard</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">Financial overview and key performance indicators</p>
        </div>
      </div>

      <FinanceNav />

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : data ? (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            <DashboardKpiCard label="Total Assets" value={formatCurrency(data.totalAssets)} icon={Banknote} />
            <DashboardKpiCard label="Total Liabilities" value={formatCurrency(data.totalLiabilities)} icon={Scale} />
            <DashboardKpiCard label="Total Equity" value={formatCurrency(data.totalEquity)} icon={Scale} />
            <DashboardKpiCard label="Revenue (MTD)" value={formatCurrency(data.revenueMtd)} icon={TrendingUp} />
            <DashboardKpiCard label="Expenses (MTD)" value={formatCurrency(data.expensesMtd)} icon={TrendingDown} />
            <DashboardKpiCard
              label="Net Income (MTD)"
              value={formatCurrency(data.netIncomeMtd)}
              icon={LineChart}
              trend={data.netIncomeMtd >= 0
                ? { value: "Positive", positive: true }
                : { value: "Negative", positive: false }
              }
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="rounded-xl border border-border bg-card p-4">
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-sm font-semibold text-foreground">Monthly Revenue vs Expenses</h2>
                <select
                  value={chartYear}
                  onChange={(e) => setChartYear(Number(e.target.value))}
                  className="h-7 rounded border border-border bg-card px-2 text-[12px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
                >
                  <option value={currentYear}>{currentYear}</option>
                  <option value={currentYear - 1}>{currentYear - 1}</option>
                </select>
              </div>
              <DashboardChart data={data.monthlyTrend} />
            </div>

            <div className="rounded-xl border border-border bg-card p-4">
              <h2 className="text-sm font-semibold text-foreground mb-3">Recent Journal Entries</h2>
              {data.recentEntries.length === 0 ? (
                <p className="text-sm text-muted-foreground py-8 text-center">No recent entries</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Entry</th>
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Date</th>
                        <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Description</th>
                        <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Debit</th>
                        <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Credit</th>
                        <th className="h-8 px-2 text-center text-[11px] font-semibold text-[#89986D]">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.recentEntries.map((entry) => (
                        <tr key={entry.id} className="border-b border-border hover:bg-muted/40">
                          <td className="h-8 px-2 text-xs tabular-nums text-muted-foreground font-mono">{entry.entryNo}</td>
                          <td className="h-8 px-2 text-xs text-muted-foreground">
                            {formatDate(entry.transactionDate, { year: undefined })}
                          </td>
                          <td className="h-8 px-2 text-xs text-foreground max-w-[200px] truncate">{entry.description}</td>
                          <td className="h-8 px-2 text-right text-xs tabular-nums text-foreground">
                            {entry.totalDebit > 0 ? formatCurrency(entry.totalDebit) : "—"}
                          </td>
                          <td className="h-8 px-2 text-right text-xs tabular-nums text-foreground">
                            {entry.totalCredit > 0 ? formatCurrency(entry.totalCredit) : "—"}
                          </td>
                          <td className="h-8 px-2 text-center">
                            <span className="inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold bg-emerald-100 text-emerald-700">
                              {entry.status}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}
