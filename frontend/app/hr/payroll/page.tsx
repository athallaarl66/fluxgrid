"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { DollarSign } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { usePayrollRunList } from "@/hooks/usePayroll";
import { PayrollRunsTable } from "@/components/hr/PayrollRunsTable";
import { SummaryMetricsCards } from "@/components/hr/SummaryMetricsCards";
import { NewRunDialog } from "@/components/hr/NewRunDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";

export default function PayrollDashboardPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1);
  const [showNewRun, setShowNewRun] = useState(false);
  const pageSize = 20;

  const { data, isLoading, error } = usePayrollRunList({
    status: statusFilter || undefined,
    page,
    pageSize,
  });

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/payroll");
  }

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-4 gap-3"><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /></div>
        <Skeleton className="h-8 w-full max-w-sm" />
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  const runs = data?.items || [];
  const totalRuns = data?.total || 0;
  const totalGross = runs.reduce((s, r) => s + (r.totalGross ?? 0), 0);
  const totalNet = runs.reduce((s, r) => s + (r.totalNet ?? 0), 0);

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <DollarSign className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Payroll Dashboard</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {data ? `${totalRuns} payroll runs` : "Manage payroll processing"}
          </p>
        </div>
      </div>

      <SummaryMetricsCards cards={[
        { label: "Total Runs", value: String(totalRuns) },
        { label: "Current Period Gross", value: runs.length > 0 ? new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0 }).format(totalGross) : "—" },
        { label: "Current Period Net", value: runs.length > 0 ? new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0 }).format(totalNet) : "—" },
        { label: "DRAFT Runs", value: String(runs.filter((r) => r.status === "DRAFT").length) },
      ]} />

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {["", "DRAFT", "FINALIZED"].map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => { setStatusFilter(s); setPage(1); }}
              className={`h-7 rounded border px-2.5 text-xs font-medium cursor-pointer transition-colors ${
                statusFilter === s
                  ? "border-primary bg-primary/10 text-primary"
                  : "border-border text-muted-foreground hover:bg-muted hover:text-foreground"
              }`}
            >
              {s || "All"}
            </button>
          ))}
        </div>
        <Button size="sm" onClick={() => setShowNewRun(true)}>+ Run Payroll</Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load payroll runs</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : runs.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <DollarSign className="size-12 text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-foreground">No payroll runs yet</p>
          <p className="text-xs text-muted-foreground mt-1">Create your first payroll run to get started</p>
        </div>
      ) : (
        <>
          <PayrollRunsTable runs={runs} />

          {data && data.total > data.pageSize && (
            <div className="flex items-center justify-between pt-2">
              <p className="text-xs text-muted-foreground">
                Page {data.page} of {Math.ceil(data.total / data.pageSize)} ({data.total} total)
              </p>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors"
                >
                  Previous
                </button>
                <button
                  type="button"
                  disabled={page >= Math.ceil(data.total / data.pageSize)}
                  onClick={() => setPage((p) => p + 1)}
                  className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}

      {showNewRun && (
        <NewRunDialog
          onClose={() => setShowNewRun(false)}
          onCreated={(runId) => {
            setShowNewRun(false);
            router.push(`/hr/payroll/${runId}`);
          }}
        />
      )}
    </div>
  );
}
