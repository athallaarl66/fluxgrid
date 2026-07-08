"use client";

import { useState, useCallback, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { BarChart3 } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { ReportControls } from "@/components/finance/ReportControls";
import { StatementTree } from "@/components/finance/StatementTree";
import { LedgerDrilldownModal } from "@/components/finance/LedgerDrilldownModal";
import {
  useTrialBalance,
  useProfitLoss,
  useBalanceSheet,
  useAccountLedger,
} from "@/hooks/useReports";
import {
  formatBalance,
  type ReportRow,
  type ReportType,
} from "@/lib/report-types";
import { fetchPeriods } from "@/lib/period-api";

function today() {
  return new Date().toISOString().slice(0, 10);
}

function flattenRows(rows: ReportRow[]): { code: string; name: string; balance: string }[] {
  const result: { code: string; name: string; balance: string }[] = [];
  function walk(list: ReportRow[]) {
    for (const r of list) {
      result.push({ code: r.code, name: r.name, balance: formatBalance(r.balance) });
      if (r.children.length > 0) walk(r.children);
    }
  }
  walk(rows);
  return result;
}

function exportCsv(rows: ReportRow[], reportType: string) {
  const flat = flattenRows(rows);
  const header = "Code,Name,Balance\n";
  const body = flat.map((r) => `"${r.code}","${r.name}","${r.balance}"`).join("\n");
  const csv = header + body;
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `${reportType}-${today()}.csv`;
  link.click();
  URL.revokeObjectURL(link.href);
}

const TABS: { key: ReportType; label: string }[] = [
  { key: "trial-balance", label: "Trial Balance" },
  { key: "pl", label: "Profit & Loss" },
  { key: "balance-sheet", label: "Balance Sheet" },
];

export default function FinancialReportsPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();

  const [activeTab, setActiveTab] = useState<ReportType>("trial-balance");
  const [startDate, setStartDate] = useState("2025-01-01");
  const [endDate, setEndDate] = useState("2026-12-31");
  const [asOfDate, setAsOfDate] = useState("2026-12-31");
  const [includeDrafts, setIncludeDrafts] = useState(false);
  const [showZeroBalances, setShowZeroBalances] = useState(false);

  // Drill-down state
  const [drillDownRow, setDrillDownRow] = useState<ReportRow | null>(null);
  const [drillDownPage, setDrillDownPage] = useState(1);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/finance/reports");
    }
  }, [user, authLoading, router]);

  const tbQuery = useTrialBalance(startDate, endDate, includeDrafts);
  const plQuery = useProfitLoss(startDate, endDate, includeDrafts);
  const bsQuery = useBalanceSheet(asOfDate, includeDrafts, plQuery.data?.netIncome ?? null);

  const ledgerQuery = useAccountLedger(
    drillDownRow?.accountId ?? null,
    startDate,
    endDate,
    includeDrafts,
    drillDownPage,
  );

  const currentQuery = {
    "trial-balance": tbQuery,
    pl: plQuery,
    "balance-sheet": bsQuery,
  }[activeTab];

  const handleRefresh = useCallback(() => {
    tbQuery.refetch();
    plQuery.refetch();
    bsQuery.refetch();
  }, [tbQuery, plQuery, bsQuery]);

  const handleDrillDown = useCallback((row: ReportRow) => {
    setDrillDownRow(row);
    setDrillDownPage(1);
  }, []);

  const handleExportCsv = useCallback(() => {
    if (currentQuery.data) {
      exportCsv(currentQuery.data.rows, activeTab);
    }
  }, [currentQuery.data, activeTab]);

  const handleDrillDownPageChange = useCallback((page: number) => {
    setDrillDownPage(page);
  }, []);

  const isBalanceSheet = activeTab === "balance-sheet";

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 rounded-lg" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      {/* Page header */}
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <BarChart3 className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Financial Reports
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Trial Balance, Profit & Loss, and Balance Sheet
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 text-[13px] font-medium transition-colors cursor-pointer border-b-2 -mb-px ${
              activeTab === tab.key
                ? "border-[#625f4b] text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Controls */}
      <ReportControls
        startDate={startDate}
        endDate={endDate}
        asOfDate={asOfDate}
        includeDrafts={includeDrafts}
        showZeroBalances={showZeroBalances}
        reportType={activeTab}
        isFetching={currentQuery.isFetching}
        onStartDateChange={setStartDate}
        onEndDateChange={setEndDate}
        onAsOfDateChange={setAsOfDate}
        onIncludeDraftsChange={setIncludeDrafts}
        onShowZeroBalancesChange={setShowZeroBalances}
        onRefresh={handleRefresh}
        onExportCsv={handleExportCsv}
      />

      {/* Report content */}
      {currentQuery.isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full rounded" />
          ))}
        </div>
      ) : currentQuery.isError ? (
        <div className="rounded-lg border border-destructive/20 bg-destructive/5 p-6 text-center">
          <p className="text-sm font-medium text-destructive">
            Failed to load report
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            {currentQuery.error instanceof Error
              ? currentQuery.error.message
              : "An unexpected error occurred"}
          </p>
          <button
            type="button"
            onClick={() => currentQuery.refetch()}
            className="mt-3 text-[12px] font-medium text-[#625f4b] hover:underline cursor-pointer"
          >
            Try again
          </button>
        </div>
      ) : currentQuery.data ? (
        <StatementTree
          report={currentQuery.data}
          showType={activeTab === "trial-balance" ? "tb" : isBalanceSheet ? "bs" : "pl"}
          onDrillDown={handleDrillDown}
        />
      ) : null}

      {/* Drill-down modal */}
      <LedgerDrilldownModal
        open={drillDownRow !== null}
        row={drillDownRow}
        data={ledgerQuery.data?.rows ?? []}
        total={ledgerQuery.data?.total ?? 0}
        page={drillDownPage}
        pageSize={20}
        loading={ledgerQuery.isLoading}
        onClose={() => setDrillDownRow(null)}
        onPageChange={handleDrillDownPageChange}
      />
    </div>
  );
}
