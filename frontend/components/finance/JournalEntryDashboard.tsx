"use client";

import { useState, useEffect, useCallback } from "react";
import { Plus, RefreshCw, Filter, ChevronLeft, ChevronRight, Eye, Pencil, Trash2, CheckCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  getJournalEntries,
  approveJournalEntry,
  deleteJournalEntry,
} from "@/lib/journal-entry-api";
import {
  STATUS_CONFIG,
  formatIDR,
  type JournalEntry,
  type JournalEntryStatus,
} from "@/lib/journal-entry-types";

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: "", label: "All" },
  { value: "DRAFT", label: "Draft" },
  { value: "PENDING_APPROVAL", label: "Pending Approval" },
  { value: "POSTED", label: "Posted" },
  { value: "VOID", label: "Void" },
];

const PAGE_SIZES = [5, 10, 20, 50];

interface Props {
  onNew: () => void;
  onEdit: (entry: JournalEntry) => void;
  onView: (entry: JournalEntry) => void;
  refreshKey?: number;
}

export function JournalEntryDashboard({ onNew, onEdit, onView, refreshKey }: Props) {
  const [entries, setEntries] = useState<JournalEntry[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const fetchEntries = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getJournalEntries(
        statusFilter ? (statusFilter as JournalEntryStatus) : undefined,
        page,
        pageSize
      );
      setEntries(data.items);
      setTotal(data.total);
      setTotalPages(data.totalPages);
    } catch {
      setEntries([]);
    } finally {
      setLoading(false);
    }
  }, [statusFilter, page, pageSize]);

  useEffect(() => {
    fetchEntries();
  }, [fetchEntries, refreshKey]);

  const handleApprove = async (id: string) => {
    setActionLoading(id + "-approve");
    try {
      await approveJournalEntry(id);
      await fetchEntries();
    } finally {
      setActionLoading(null);
    }
  };

  const handleVoid = async (entry: JournalEntry) => {
    if (!confirm(`Void journal entry ${entry.entryNo}?`)) return;
    setActionLoading(entry.id + "-void");
    try {
      await deleteJournalEntry(entry.id);
      await fetchEntries();
    } finally {
      setActionLoading(null);
    }
  };

  return (
    <div className="space-y-3">
      {/* Toolbar */}
      <div className="flex items-center gap-2">
        <div className="flex items-center gap-1.5">
          <Filter className="size-3.5 text-muted-foreground shrink-0" />
          <select
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
            className="h-8 rounded border border-border bg-card px-2 text-[11px] font-medium text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
          >
            {STATUS_FILTERS.map((f) => (
              <option key={f.value} value={f.value}>{f.label}</option>
            ))}
          </select>
        </div>
        <Button variant="ghost" size="sm" onClick={fetchEntries} disabled={loading}
          className="h-8 px-2 text-muted-foreground cursor-pointer">
          <RefreshCw className={cn("size-3.5", loading && "animate-spin")} />
        </Button>
        <Button size="sm" onClick={onNew} className="h-8 ml-auto cursor-pointer">
          <Plus className="size-3.5 mr-1" /> New Journal Entry
        </Button>
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[130px]">Entry No</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[110px]">Date</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Description</th>
              <th className="h-9 px-3 text-right text-[11px] font-semibold text-[#89986D] w-[150px]">Amount (IDR)</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[150px]">Status</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[80px]"></th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} className="border-b border-border">
                  {Array.from({ length: 6 }).map((_, j) => (
                    <td key={j} className="h-9 px-3"><div className="h-4 rounded bg-muted animate-pulse" /></td>
                  ))}
                </tr>
              ))
            ) : entries.length === 0 ? (
              <tr><td colSpan={6} className="h-24 text-center text-sm text-muted-foreground">No journal entries found</td></tr>
            ) : (
              entries.map((entry) => {
                const cfg = STATUS_CONFIG[entry.status];
                return (
                  <tr key={entry.id} className="group border-b border-border transition-colors hover:bg-muted/40">
                    <td className="h-9 px-3 text-sm tabular-nums font-mono text-muted-foreground">{entry.entryNo}</td>
                    <td className="h-9 px-3 text-sm tabular-nums text-foreground">
                      {new Date(entry.transactionDate).toLocaleDateString("id-ID")}
                    </td>
                    <td className="h-9 px-3 text-sm text-foreground truncate max-w-[280px]">{entry.description}</td>
                    <td className="h-9 px-3 text-sm text-right tabular-nums text-foreground font-medium">{formatIDR(entry.totalAmount)}</td>
                    <td className="h-9 px-3">
                      <span className={cn("inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-semibold", cfg.className)}>{cfg.label}</span>
                    </td>
                    <td className="h-9 px-3">
                      <div className="flex items-center justify-end gap-0.5">
                        <button type="button" onClick={() => onView(entry)}
                          className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground opacity-0 group-hover:opacity-100 cursor-pointer" title="View">
                          <Eye className="size-3.5" />
                        </button>
                        {entry.status === "DRAFT" && (
                          <button type="button" onClick={() => onEdit(entry)}
                            className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground opacity-0 group-hover:opacity-100 cursor-pointer" title="Edit">
                            <Pencil className="size-3.5" />
                          </button>
                        )}
                        {entry.status === "PENDING_APPROVAL" && (
                          <button type="button" onClick={() => handleApprove(entry.id)} disabled={actionLoading === entry.id + "-approve"}
                            className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-emerald-50 hover:text-emerald-700 opacity-0 group-hover:opacity-100 cursor-pointer disabled:opacity-50" title="Approve">
                            <CheckCircle className="size-3.5" />
                          </button>
                        )}
                        {(entry.status === "DRAFT" || entry.status === "PENDING_APPROVAL") && (
                          <button type="button" onClick={() => handleVoid(entry)} disabled={actionLoading === entry.id + "-void"}
                            className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive opacity-0 group-hover:opacity-100 cursor-pointer disabled:opacity-50" title="Void">
                            <Trash2 className="size-3.5" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between text-[13px] text-muted-foreground">
        <div className="flex items-center gap-2">
          <span>Show</span>
          <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
            className="h-7 rounded border border-border bg-card px-1 text-[12px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer">
            {PAGE_SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <span>of {total} entries</span>
        </div>
        <div className="flex items-center gap-1">
          <button type="button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-30 cursor-pointer">
            <ChevronLeft className="size-4" />
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button key={p} type="button" onClick={() => setPage(p)}
              className={cn("flex size-7 items-center justify-center rounded text-[12px] cursor-pointer",
                p === page ? "bg-[#625f4b] text-white" : "text-muted-foreground hover:bg-muted hover:text-foreground")}>
              {p}
            </button>
          ))}
          <button type="button" onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-30 cursor-pointer">
            <ChevronRight className="size-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
