"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, Loader2, Lock } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { usePayrollRun, useRecalculatePayroll } from "@/hooks/usePayroll";
import { PayrollRecordsTable } from "@/components/hr/PayrollRecordsTable";
import { SummaryMetricsCards } from "@/components/hr/SummaryMetricsCards";
import { FinalizeConfirmationModal } from "@/components/hr/FinalizeConfirmationModal";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";
import { formatDate } from "@/lib/date-utils";

function formatCurrency(value: number | null) {
  if (value === null) return "***";
  return new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
}

export default function PayrollRunDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();
  const id = params.id as string;
  const [showFinalize, setShowFinalize] = useState(false);

  const { data: detail, isLoading, error, refetch } = usePayrollRun(id);
  const recalculateMutation = useRecalculatePayroll();

  if (!authLoading && !user) {
    router.push(`/login?redirect=/hr/payroll/${id}`);
  }

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-4 gap-3"><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /><Skeleton className="h-24 rounded-xl" /></div>
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (!user) return null;

  if (error || !detail) {
    return (
      <div className="p-5 space-y-4">
        <button type="button" onClick={() => router.back()}
          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer">
          <ArrowLeft className="size-3.5" /> Back
        </button>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Payroll run not found</p>
          <p className="text-xs text-muted-foreground mt-1">It may have been removed</p>
        </div>
      </div>
    );
  }

  const { run, records, totalRecords } = detail;
  const isDraft = run.status === "DRAFT";
  const employeeCount = records.length;
  const totalGross = records.reduce((s, r) => s + (r.grossPay ?? 0), 0);
  const totalDeductions = records.reduce((s, r) => s + (r.taxDeduction ?? 0) + (r.latenessDeduction ?? 0), 0);
  const totalNet = records.reduce((s, r) => s + (r.netPay ?? 0), 0);

  async function handleRecalculate() {
    try {
      await recalculateMutation.mutateAsync(id);
      toast("Payroll recalculated successfully", "success");
      refetch();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to recalculate";
      toast(msg, "error");
    }
  }

  return (
    <div className="p-5 space-y-5 animate-fade-in">
      <button type="button" onClick={() => router.push("/hr/payroll")}
        className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors">
        <ArrowLeft className="size-3.5" /> Back to Payroll Dashboard
      </button>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div>
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">{run.periodName}</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">
              {formatDate(run.startDate)}
              {" — "}
              {formatDate(run.endDate)}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant={isDraft ? "outline" : "default"}>{run.status}</Badge>
            {!isDraft && <Lock className="size-4 text-muted-foreground" />}
          </div>
        </div>

        {isDraft && (
          <div className="flex items-center gap-2">
            <Button variant="secondary" size="sm" disabled={recalculateMutation.isPending} onClick={handleRecalculate}>
              {recalculateMutation.isPending ? <><Loader2 className="size-3.5 animate-spin" /> Recalculating...</> : "Recalculate"}
            </Button>
            <Button variant="destructive" size="sm" onClick={() => setShowFinalize(true)}>
              Finalize & Post to Ledger
            </Button>
          </div>
        )}
      </div>

      <SummaryMetricsCards cards={[
        { label: "Total Gross", value: formatCurrency(totalGross) },
        { label: "Total Deductions", value: formatCurrency(totalDeductions) },
        { label: "Total Net", value: formatCurrency(totalNet) },
        { label: "Employees", value: String(employeeCount) },
      ]} />

      <PayrollRecordsTable records={records} />

      {showFinalize && (
        <FinalizeConfirmationModal
          run={run}
          employeeCount={employeeCount}
          onClose={() => setShowFinalize(false)}
          onFinalized={() => { setShowFinalize(false); refetch(); }}
        />
      )}
    </div>
  );
}
