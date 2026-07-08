"use client";

import { useState, useEffect } from "react";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from "@/lib/budget-types";

interface BudgetFormModalProps {
  open: boolean;
  onClose: () => void;
  onSave: (data: CreateBudgetRequest | UpdateBudgetRequest) => void;
  editingBudget?: BudgetResponse | null;
  saving?: boolean;
  accounts?: { id: string; code: string; name: string }[];
  periods?: { id: string; name: string }[];
}

export function BudgetFormModal({
  open,
  onClose,
  onSave,
  editingBudget,
  saving,
  accounts = [],
  periods = [],
}: BudgetFormModalProps) {
  const [accountId, setAccountId] = useState("");
  const [periodId, setPeriodId] = useState("");
  const [plannedAmount, setPlannedAmount] = useState("");
  const [notes, setNotes] = useState("");

  const isEditing = !!editingBudget;

  useEffect(() => {
    if (editingBudget) {
      setAccountId(editingBudget.accountId);
      setPeriodId(editingBudget.periodId);
      setPlannedAmount(String(editingBudget.plannedAmount));
      setNotes(editingBudget.notes ?? "");
    } else {
      setAccountId(periods.length === 1 ? periods[0].id : "");
      setPeriodId(periods.length === 1 ? periods[0].id : "");
      setPlannedAmount("");
      setNotes("");
    }
  }, [editingBudget, open, periods]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!accountId || !periodId || !plannedAmount) return;

    const amount = parseFloat(plannedAmount.replace(/[^0-9.-]/g, ""));
    if (isNaN(amount)) return;

    if (isEditing) {
      onSave({ plannedAmount: amount, notes: notes || null } as UpdateBudgetRequest);
    } else {
      onSave({ accountId, periodId, plannedAmount: amount, notes: notes || undefined } as CreateBudgetRequest);
    }
  }

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-foreground">
            {isEditing ? "Edit Budget" : "New Budget"}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {!isEditing && (
            <>
              <div className="space-y-1.5">
                <label className="text-xs font-medium text-muted-foreground">Account *</label>
                <select
                  required
                  value={accountId}
                  onChange={(e) => setAccountId(e.target.value)}
                  disabled={saving}
                  className="h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-50"
                >
                  <option value="">Select account...</option>
                  {accounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-medium text-muted-foreground">Period *</label>
                <select
                  required
                  value={periodId}
                  onChange={(e) => setPeriodId(e.target.value)}
                  disabled={saving}
                  className="h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-50"
                >
                  <option value="">Select period...</option>
                  {periods.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
            </>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">Planned Amount *</label>
            <Input
              required
              type="text"
              inputMode="decimal"
              placeholder="e.g. 100000000"
              value={plannedAmount}
              onChange={(e) => setPlannedAmount(e.target.value)}
              disabled={saving}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">Notes</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={saving}
              rows={3}
              className="h-20 w-full rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-sm transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-50 resize-none"
              placeholder="Optional notes..."
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose} disabled={saving}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={saving || !plannedAmount}>
              {saving ? "Saving..." : isEditing ? "Update" : "Create"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
