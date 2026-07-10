"use client";

import { useState } from "react";
import { X, Loader2, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";
import { useFinalizePayroll } from "@/hooks/usePayroll";
import type { PayrollRun } from "@/lib/hr-types";

interface FinalizeConfirmationModalProps {
  run: PayrollRun;
  employeeCount: number;
  onClose: () => void;
  onFinalized: () => void;
}

function formatCurrency(value: number | null) {
  if (value === null) return "—";
  return new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
}

export function FinalizeConfirmationModal({ run, employeeCount, onClose, onFinalized }: FinalizeConfirmationModalProps) {
  const { toast } = useToast();
  const finalizeMutation = useFinalizePayroll();
  const [acknowledged, setAcknowledged] = useState(false);

  async function handleFinalize() {
    try {
      await finalizeMutation.mutateAsync(run.id);
      toast("Payroll finalized and posted to Finance", "success");
      onFinalized();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to finalize";
      toast(msg, "error");
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="relative w-full max-w-md max-h-[90vh] overflow-y-auto rounded-xl border border-border bg-card p-6 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <TriangleAlert className="size-5 text-destructive" />
            <h2 className="text-lg font-semibold text-foreground">Finalize Payroll</h2>
          </div>
          <button type="button" onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer">
            <X className="size-4" />
          </button>
        </div>

        <div className="space-y-3">
          <div className="rounded-lg border border-border bg-muted/30 p-3 space-y-1.5">
            <div className="flex justify-between text-xs"><span className="text-muted-foreground">Period</span><span className="text-foreground font-medium">{run.periodName}</span></div>
            <div className="flex justify-between text-xs"><span className="text-muted-foreground">Employees</span><span className="text-foreground font-medium">{employeeCount}</span></div>
            <div className="flex justify-between text-xs"><span className="text-muted-foreground">Total Gross</span><span className="text-foreground font-medium tabular-nums">{formatCurrency(run.totalGross)}</span></div>
            <div className="flex justify-between text-xs"><span className="text-muted-foreground">Total Net</span><span className="text-foreground font-medium tabular-nums">{formatCurrency(run.totalNet)}</span></div>
          </div>

          <p className="text-xs text-destructive bg-destructive/10 rounded-lg p-3">
            This will lock the payroll and post journal entries to Finance. This action cannot be undone.
          </p>

          <label className="flex items-start gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={acknowledged}
              onChange={(e) => setAcknowledged(e.target.checked)}
              className="mt-0.5 size-4 accent-primary"
            />
            <span className="text-xs text-muted-foreground">I acknowledge that this will lock the payroll and create journal entries in Finance.</span>
          </label>
        </div>

        <div className="flex items-center justify-end gap-2 pt-4">
          <Button type="button" variant="outline" size="sm" onClick={onClose}>Cancel</Button>
          <Button type="button" variant="destructive" size="sm" disabled={!acknowledged || finalizeMutation.isPending} onClick={handleFinalize}>
            {finalizeMutation.isPending ? <><Loader2 className="size-3.5 animate-spin" /> Finalizing...</> : "Finalize & Post to Ledger"}
          </Button>
        </div>
      </div>
    </div>
  );
}
