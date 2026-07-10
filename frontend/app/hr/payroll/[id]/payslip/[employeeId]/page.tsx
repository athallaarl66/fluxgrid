"use client";

import { useParams, useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { usePayrollRun } from "@/hooks/usePayroll";
import { PayslipDocument } from "@/components/hr/PayslipDocument";
import { Skeleton } from "@/components/ui/skeleton";

export default function PayslipPage() {
  const params = useParams();
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const runId = params.id as string;
  const employeeId = params.employeeId as string;

  const { data: detail, isLoading, error } = usePayrollRun(runId);

  if (!authLoading && !user) {
    router.push(`/login?redirect=/hr/payroll/${runId}/payslip/${employeeId}`);
  }

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-96 rounded-xl" />
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
          <p className="text-sm text-destructive font-medium">Payslip not found</p>
          <p className="text-xs text-muted-foreground mt-1">The payslip may have been removed</p>
        </div>
      </div>
    );
  }

  const record = detail.records.find((r) => r.employeeId === employeeId);

  if (!record) {
    return (
      <div className="p-5 space-y-4">
        <button type="button" onClick={() => router.back()}
          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer">
          <ArrowLeft className="size-3.5" /> Back
        </button>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Employee not found in this payroll run</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-5 animate-fade-in">
      <button type="button" onClick={() => router.push(`/hr/payroll/${runId}`)}
        className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors">
        <ArrowLeft className="size-3.5" /> Back to Payroll Run
      </button>
      <PayslipDocument run={detail.run} record={record} />
    </div>
  );
}
