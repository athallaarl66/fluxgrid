"use client";

import { useState } from "react";
import { X, Loader2 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useCalculatePayroll } from "@/hooks/usePayroll";
import { useToast } from "@/components/ui/toast";

interface NewRunDialogProps {
  onClose: () => void;
  onCreated: (runId: string) => void;
}

export function NewRunDialog({ onClose, onCreated }: NewRunDialogProps) {
  const { toast } = useToast();
  const calculateMutation = useCalculatePayroll();
  const today = new Date();
  const defaultStart = new Date(today.getFullYear(), today.getMonth(), 1);
  const defaultEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0);

  const [periodName, setPeriodName] = useState(
    today.toLocaleDateString("en-US", { month: "long", year: "numeric" })
  );
  const [startDate, setStartDate] = useState(defaultStart.toISOString().slice(0, 10));
  const [endDate, setEndDate] = useState(defaultEnd.toISOString().slice(0, 10));

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!periodName.trim() || !startDate || !endDate) return;

    try {
      const result = await calculateMutation.mutateAsync({
        periodName: periodName.trim(),
        startDate,
        endDate,
      });
      toast("Payroll calculated successfully", "success");
      onCreated(result.id);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to calculate payroll";
      toast(msg, "error");
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="relative w-full max-w-lg max-h-[90vh] overflow-y-auto rounded-xl border border-border bg-card p-6 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">New Payroll Run</h2>
          <button type="button" onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer">
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Period Name</label>
            <Input value={periodName} onChange={(e) => setPeriodName(e.target.value)} placeholder="e.g. May 2026" required />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground">Start Date</label>
              <Input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
            </div>
            <div className="space-y-1.5">
              <label className="text-sm font-medium text-foreground">End Date</label>
              <Input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} required />
            </div>
          </div>
          {startDate && endDate && new Date(endDate) < new Date(startDate) && (
            <p className="text-xs text-destructive">End date must be after start date</p>
          )}

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>Cancel</Button>
            <Button type="submit" size="sm" disabled={calculateMutation.isPending}>
              {calculateMutation.isPending ? (
                <><Loader2 className="size-3.5 animate-spin" /> Calculating...</>
              ) : (
                "Calculate"
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
