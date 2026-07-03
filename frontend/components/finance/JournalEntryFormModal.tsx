"use client";

import { useState, useEffect } from "react";
import { X } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { LineItemsManager, makeLineKey } from "./LineItemsManager";
import { createJournalEntry, updateJournalEntry } from "@/lib/journal-entry-api";
import { STATUS_CONFIG, type JournalEntry } from "@/lib/journal-entry-types";
import { flattenTree, type AccountResponse } from "@/lib/coa-types";
import { apiClient } from "@/lib/api-client";

interface LineItem {
  _key: string;
  accountId: string;
  description?: string;
  debit: number;
  credit: number;
}

interface Props {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  editEntry?: JournalEntry | null;
}

function makeDefaultLines(): LineItem[] {
  return [
    { _key: makeLineKey(), accountId: "", description: "", debit: 0, credit: 0 },
    { _key: makeLineKey(), accountId: "", description: "", debit: 0, credit: 0 },
  ];
}

export function JournalEntryFormModal({ open, onClose, onSuccess, editEntry }: Props) {
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [description, setDescription] = useState("");
  const [lines, setLines] = useState<LineItem[]>(makeDefaultLines);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load COA for account selection
  useEffect(() => {
    apiClient<AccountResponse[]>("/api/v1/finance/chart-of-accounts?flat=true")
      .then(setAccounts)
      .catch(() => {});
  }, []);

  // Populate from editEntry
  useEffect(() => {
    if (editEntry) {
      setDate(editEntry.transactionDate.slice(0, 10));
      setDescription(editEntry.description);
      setLines(
        editEntry.lines.map((l) => ({
          _key: makeLineKey(),
          accountId: l.accountId,
          description: l.description ?? "",
          debit: l.debit,
          credit: l.credit,
        }))
      );
    } else {
      setDate(new Date().toISOString().slice(0, 10));
      setDescription("");
      setLines(makeDefaultLines());
    }
    setError(null);
  }, [editEntry, open]);

  const totalDebit = lines.reduce((s, l) => s + l.debit, 0);
  const totalCredit = lines.reduce((s, l) => s + l.credit, 0);
  const isBalanced = totalDebit > 0 && totalDebit === totalCredit;
  const hasLines = lines.some((l) => l.accountId);

  const submit = async (status: "DRAFT" | "SUBMIT") => {
    if (!description.trim()) { setError("Description is required."); return; }
    if (status !== "DRAFT" && !isBalanced) { setError("Debit and Credit must be equal to submit."); return; }
    if (status !== "DRAFT" && !hasLines) { setError("At least one line with an account is required."); return; }

    setSaving(true);
    setError(null);
    try {
      const payload = {
        transactionDate: date,
        description: description.trim(),
        status,
        lines: lines.filter((l) => l.accountId).map((l) => ({
          accountId: l.accountId,
          description: l.description || undefined,
          debit: l.debit,
          credit: l.credit,
        })),
      };
      if (editEntry) {
        await updateJournalEntry(editEntry.id, payload);
      } else {
        await createJournalEntry(payload);
      }
      onSuccess();
      onClose();
    } catch (e: unknown) {
      const err = e as { message?: string };
      setError(err?.message || "An error occurred");
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative z-10 w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-xl border border-border bg-card shadow-xl mx-4">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <div className="flex items-center gap-3">
            <h2 className="text-base font-semibold text-foreground">
              {editEntry ? "Edit Journal Entry" : "New Journal Entry"}
            </h2>
            {editEntry && (
              <span
                className={cn(
                  "inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-semibold",
                  STATUS_CONFIG[editEntry.status].className
                )}
              >
                {STATUS_CONFIG[editEntry.status].label}
              </span>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-5">
          {/* Metadata */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
                Transaction Date <span className="text-destructive">*</span>
              </label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full h-8 rounded border border-border bg-background px-3 text-sm text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
              />
            </div>
            <div className="space-y-1.5">
              <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
                Description <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="e.g., Monthly rent payment"
                className="w-full h-8 rounded border border-border bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring"
              />
            </div>
          </div>

          {/* Line Items */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
              Journal Lines
            </label>
            <LineItemsManager lines={lines} accounts={accounts} onChange={setLines} />
          </div>

          {/* Error */}
          {error && (
            <p className="text-[12px] font-semibold text-destructive">{error}</p>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-2 border-t border-border px-6 py-4">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            className="h-8 cursor-pointer"
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => submit("DRAFT")}
            disabled={saving}
            className="h-8 cursor-pointer"
          >
            Save as Draft
          </Button>
          <Button
            type="button"
            size="sm"
            onClick={() => submit("SUBMIT")}
            disabled={saving || !isBalanced}
            className="h-8 cursor-pointer"
          >
            {saving ? "Submitting…" : "Submit"}
          </Button>
        </div>
      </div>
    </div>
  );
}
