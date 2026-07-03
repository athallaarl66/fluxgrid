"use client";

import { useMemo, useState, useEffect } from "react";
import { Pencil, Trash2, Search, RefreshCw, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Filter } from "lucide-react";
import { cn } from "@/lib/utils";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { ACCOUNT_TYPES, flattenTree, type AccountResponse, type FlatAccount, type AccountType } from "@/lib/coa-types";

interface CoaTableProps {
  accounts: AccountResponse[];
  onEdit: (account: AccountResponse) => void;
  onDeactivate: (account: AccountResponse) => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  onNewAccount: () => void;
  isFetching?: boolean;
  onRefresh?: () => void;
}

const PAGE_SIZES = [5, 10, 20, 50] as const;

function getPageNumbers(current: number, total: number): number[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i);
  const pages: number[] = [];
  pages.push(0);
  if (current > 2) pages.push(-1);
  const start = Math.max(1, current - 1);
  const end = Math.min(total - 2, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 3) pages.push(-1);
  pages.push(total - 1);
  return pages;
}

export function CoaTable({
  accounts,
  onEdit,
  onDeactivate,
  searchQuery,
  onSearchChange,
  onNewAccount,
  isFetching,
  onRefresh,
}: CoaTableProps) {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<number>(10);
  const [typeFilter, setTypeFilter] = useState<string>("");

  useEffect(() => { setPage(0); }, [searchQuery]);

  const flatList = useMemo(() => flattenTree(accounts), [accounts]);

  const filtered = useMemo(() => {
    let result = flatList;
    if (typeFilter) result = result.filter((a) => a.type === typeFilter);
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      result = result.filter((a) => a.code.toLowerCase().includes(q) || a.name.toLowerCase().includes(q));
    }
    return result;
  }, [flatList, searchQuery, typeFilter]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const safePage = Math.min(page, totalPages - 1);
  const paginated = filtered.slice(safePage * pageSize, (safePage + 1) * pageSize);

  const parentMap = useMemo(() => {
    const map = new Map<string, string>();
    flatList.forEach((a) => {
      if (a.path.includes(" > ")) {
        const parts = a.path.split(" > ");
        map.set(a.id, parts[parts.length - 2]);
      }
    });
    return map;
  }, [flatList]);

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search by code or name..."
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="h-8 w-full rounded border-border bg-card pl-8 text-sm text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring"
          />
        </div>
        <div className="flex items-center gap-1.5">
          <Filter className="size-3.5 text-muted-foreground shrink-0" />
          <select
            value={typeFilter}
            onChange={(e) => { setTypeFilter(e.target.value); setPage(0); }}
            className="h-8 rounded border border-border bg-card px-2 text-[11px] font-medium text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
          >
            <option value="">All Types</option>
            {ACCOUNT_TYPES.map((t) => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </div>
        {onRefresh && (
          <Button variant="ghost" size="sm" onClick={onRefresh} disabled={isFetching} className="h-8 px-2 text-muted-foreground cursor-pointer">
            <RefreshCw className={cn("size-3.5", isFetching && "animate-spin")} />
          </Button>
        )}
        <Button size="sm" onClick={onNewAccount} className="h-8 ml-auto cursor-pointer">+ New Account</Button>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[90px]">Code</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D]">Name</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[140px]">Parent</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[90px]">Type</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[80px]">Status</th>
              <th className="h-9 px-3 text-left text-[11px] font-semibold text-[#89986D] w-[70px]"></th>
            </tr>
          </thead>
          <tbody>
            {paginated.length === 0 ? (
              <tr>
                <td colSpan={6} className="h-24 text-center text-sm text-muted-foreground">
                  {searchQuery ? "No accounts match your search" : "No accounts yet"}
                </td>
              </tr>
            ) : (
              paginated.map((account) => {
                const parentName = parentMap.get(account.id);
                return (
                  <tr key={account.id} className="group border-b border-border transition-colors hover:bg-muted/40">
                    <td className="h-9 px-3 text-sm tabular-nums text-muted-foreground font-mono">{account.code}</td>
                    <td className={cn("h-9 px-3 text-sm", account.level === 0 ? "font-semibold text-foreground" : "text-foreground")}>{account.name}</td>
                    <td className="h-9 px-3 text-sm text-muted-foreground">{parentName ?? <span className="text-muted-foreground/40">—</span>}</td>
                    <td className="h-9 px-3">
                      <span className={cn(
                        "inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-semibold",
                        account.type === "ASSET" && "bg-blue-100 text-blue-700",
                        account.type === "LIABILITY" && "bg-amber-100 text-amber-700",
                        account.type === "EQUITY" && "bg-purple-100 text-purple-700",
                        account.type === "REVENUE" && "bg-emerald-100 text-emerald-700",
                        account.type === "EXPENSE" && "bg-rose-100 text-rose-700",
                      )}>{account.type}</span>
                    </td>
                    <td className="h-9 px-3">
                      <span className="inline-flex items-center gap-1.5">
                        <span className={cn("inline-block size-1.5 rounded-full", account.isActive ? "bg-emerald-500" : "bg-red-500")} />
                        <span className="text-[11px] font-medium text-muted-foreground">{account.isActive ? "Active" : "Inactive"}</span>
                      </span>
                    </td>
                    <td className="h-9 px-3">
                      <div className="flex items-center justify-end gap-0.5">
                        <button type="button" onClick={() => onEdit(account)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground opacity-0 group-hover:opacity-100 cursor-pointer" title="Edit">
                          <Pencil className="size-3.5" />
                        </button>
                        {account.isActive && (
                          <button type="button" onClick={() => onDeactivate(account)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive opacity-0 group-hover:opacity-100 cursor-pointer" title="Deactivate">
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

      {filtered.length > 0 && (
        <div className="flex items-center justify-between gap-2 text-[11px] text-muted-foreground">
          <div className="flex items-center gap-2">
            <span>Rows per page:</span>
            <select
              value={pageSize}
              onChange={(e) => { setPageSize(Number(e.target.value)); setPage(0); }}
              className="h-7 rounded border border-border bg-card px-1.5 text-[11px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
            >
              {PAGE_SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
            <span>{safePage * pageSize + 1}–{Math.min((safePage + 1) * pageSize, filtered.length)} of {filtered.length}</span>
          </div>
          <div className="flex items-center gap-1">
            <button type="button" disabled={safePage === 0} onClick={() => setPage(0)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer" title="First page">
              <ChevronsLeft className="size-3.5" />
            </button>
            <button type="button" disabled={safePage === 0} onClick={() => setPage(safePage - 1)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer">
              <ChevronLeft className="size-3.5" />
            </button>
            {getPageNumbers(safePage, totalPages).map((p, i) =>
              p === -1 ? (
                <span key={`ellipsis-${i}`} className="px-1 text-muted-foreground/50">…</span>
              ) : (
                <button
                  key={p}
                  type="button"
                  onClick={() => setPage(p)}
                  className={cn(
                    "flex size-7 items-center justify-center rounded text-[11px] font-medium transition-colors cursor-pointer",
                    p === safePage
                      ? "bg-[#9CAB84] text-white"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground",
                  )}
                >
                  {p + 1}
                </button>
              ),
            )}
            <button type="button" disabled={safePage >= totalPages - 1} onClick={() => setPage(safePage + 1)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer">
              <ChevronRight className="size-3.5" />
            </button>
            <button type="button" disabled={safePage >= totalPages - 1} onClick={() => setPage(totalPages - 1)} className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 disabled:pointer-events-none cursor-pointer" title="Last page">
              <ChevronsRight className="size-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
