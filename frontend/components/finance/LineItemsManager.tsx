"use client";

import { useEffect } from "react";
import { Plus, Trash2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import type { JournalEntryLine } from "@/lib/journal-entry-types";
import { formatIDR } from "@/lib/journal-entry-types";
import type { AccountResponse } from "@/lib/coa-types";

interface LineItem extends Omit<JournalEntryLine, "id"> {
  _key: string;
}

interface Props {
  lines: LineItem[];
  accounts: AccountResponse[];
  onChange: (lines: LineItem[]) => void;
}

function parseAmount(raw: string): number {
  return parseFloat(raw.replace(/[^0-9.]/g, "")) || 0;
}

let _keyCounter = 1;
export function makeLineKey() {
  return `line-${_keyCounter++}`;
}

export function LineItemsManager({ lines, accounts, onChange }: Props) {
  const totalDebit = lines.reduce((s, l) => s + l.debit, 0);
  const totalCredit = lines.reduce((s, l) => s + l.credit, 0);
  const isBalanced = totalDebit > 0 && totalDebit === totalCredit;

  const addLine = () => {
    const lastDebit = lines[lines.length - 1]?.debit ?? 0;
    const lastCredit = lines[lines.length - 1]?.credit ?? 0;
    const prevUnbalanced = totalDebit > totalCredit;
    onChange([
      ...lines,
      {
        _key: makeLineKey(),
        accountId: "",
        description: "",
        debit: prevUnbalanced ? 0 : 0,
        credit: prevUnbalanced ? totalDebit - totalCredit : 0,
      },
    ]);
  };

  const removeLine = (key: string) => {
    if (lines.length <= 2) return;
    onChange(lines.filter((l) => l._key !== key));
  };

  const updateLine = (key: string, patch: Partial<LineItem>) => {
    onChange(lines.map((l) => (l._key === key ? { ...l, ...patch } : l)));
  };

  return (
    <div className="space-y-2">
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-8 px-3 text-left text-[11px] font-semibold text-[#89986D]">Account</th>
              <th className="h-8 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[200px]">Description</th>
              <th className="h-8 px-3 text-right text-[11px] font-semibold text-[#89986D] w-[160px]">Debit</th>
              <th className="h-8 px-3 text-right text-[11px] font-semibold text-[#89986D] w-[160px]">Credit</th>
              <th className="h-8 w-10" />
            </tr>
          </thead>
          <tbody>
            {lines.map((line, idx) => (
              <tr key={line._key} className="border-b border-border">
                <td className="px-2 py-1.5">
                  <select
                    value={line.accountId}
                    onChange={(e) => updateLine(line._key, { accountId: e.target.value })}
                    className="w-full h-7 rounded border border-border bg-card px-2 text-[12px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
                  >
                    <option value="">Select account…</option>
                    {accounts.map((a) => (
                      <option key={a.id} value={a.id}>
                        {a.code} — {a.name}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="px-2 py-1.5">
                  <input
                    type="text"
                    value={line.description ?? ""}
                    onChange={(e) => updateLine(line._key, { description: e.target.value })}
                    placeholder="Line description"
                    className="w-full h-7 rounded border border-border bg-card px-2 text-[12px] text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring"
                  />
                </td>
                <td className="px-2 py-1.5">
                  <input
                    type="number"
                    min={0}
                    value={line.debit || ""}
                    onChange={(e) => updateLine(line._key, { debit: parseAmount(e.target.value) })}
                    placeholder="0"
                    className="w-full h-7 rounded border border-border bg-card px-2 text-[12px] text-right tabular-nums text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
                  />
                </td>
                <td className="px-2 py-1.5">
                  <input
                    type="number"
                    min={0}
                    value={line.credit || ""}
                    onChange={(e) => updateLine(line._key, { credit: parseAmount(e.target.value) })}
                    placeholder="0"
                    className="w-full h-7 rounded border border-border bg-card px-2 text-[12px] text-right tabular-nums text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
                  />
                </td>
                <td className="px-2 py-1.5 text-center">
                  <button
                    type="button"
                    disabled={lines.length <= 2}
                    onClick={() => removeLine(line._key)}
                    className="flex size-6 items-center justify-center rounded text-muted-foreground hover:bg-destructive/10 hover:text-destructive disabled:opacity-30 disabled:pointer-events-none cursor-pointer"
                  >
                    <Trash2 className="size-3" />
                  </button>
                </td>
              </tr>
            ))}

            {/* Totals row */}
            <tr className="border-t-2 border-[#9CAB84] bg-[#F6F0D7]">
              <td colSpan={2} className="h-8 px-3 text-[11px] font-semibold text-[#89986D] text-right">
                Total
              </td>
              <td className="h-8 px-3 text-right tabular-nums text-[12px] font-semibold text-foreground">
                {formatIDR(totalDebit)}
              </td>
              <td className="h-8 px-3 text-right tabular-nums text-[12px] font-semibold text-foreground">
                {formatIDR(totalCredit)}
              </td>
              <td />
            </tr>
          </tbody>
        </table>
      </div>

      {/* Balance warning */}
      {totalDebit > 0 && !isBalanced && (
        <p className="text-[11px] font-semibold text-destructive">
          ⚠ Unbalanced: Debit {formatIDR(totalDebit)} ≠ Credit {formatIDR(totalCredit)} (diff: {formatIDR(Math.abs(totalDebit - totalCredit))})
        </p>
      )}
      {isBalanced && (
        <p className="text-[11px] font-semibold text-emerald-600">✓ Balanced</p>
      )}

      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={addLine}
        className="h-7 text-[11px] cursor-pointer"
      >
        <Plus className="size-3 mr-1" /> Add Line
      </Button>
    </div>
  );
}
