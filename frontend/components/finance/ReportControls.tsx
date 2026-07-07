"use client";

import { Download, RefreshCw } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface ReportControlsProps {
  startDate: string;
  endDate: string;
  asOfDate: string;
  includeDrafts: boolean;
  showZeroBalances: boolean;
  reportType: string;
  isFetching: boolean;
  onStartDateChange: (val: string) => void;
  onEndDateChange: (val: string) => void;
  onAsOfDateChange: (val: string) => void;
  onIncludeDraftsChange: (val: boolean) => void;
  onShowZeroBalancesChange: (val: boolean) => void;
  onRefresh: () => void;
  onExportCsv: () => void;
}

export function ReportControls({
  startDate,
  endDate,
  asOfDate,
  includeDrafts,
  showZeroBalances,
  reportType,
  isFetching,
  onStartDateChange,
  onEndDateChange,
  onAsOfDateChange,
  onIncludeDraftsChange,
  onShowZeroBalancesChange,
  onRefresh,
  onExportCsv,
}: ReportControlsProps) {
  const isBalanceSheet = reportType === "balance-sheet";

  return (
    <div className="flex flex-wrap items-end gap-3">
      {isBalanceSheet ? (
        <div className="space-y-1">
          <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
            As of Date
          </label>
          <input
            type="date"
            value={asOfDate}
            onChange={(e) => onAsOfDateChange(e.target.value)}
            className="h-8 rounded border border-border bg-card px-3 text-[13px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
          />
        </div>
      ) : (
        <>
          <div className="space-y-1">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
              Start Date
            </label>
            <input
              type="date"
              value={startDate}
              onChange={(e) => onStartDateChange(e.target.value)}
              className="h-8 rounded border border-border bg-card px-3 text-[13px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
            />
          </div>
          <div className="space-y-1">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-[#89986D]">
              End Date
            </label>
            <input
              type="date"
              value={endDate}
              onChange={(e) => onEndDateChange(e.target.value)}
              className="h-8 rounded border border-border bg-card px-3 text-[13px] text-foreground focus:border-ring focus:ring-1 focus:ring-ring"
            />
          </div>
        </>
      )}

      <label className="flex items-center gap-2 h-8">
        <input
          type="checkbox"
          checked={includeDrafts}
          onChange={(e) => onIncludeDraftsChange(e.target.checked)}
          className="size-4 rounded border-border text-[#625f4b] focus:ring-[#625f4b] cursor-pointer"
        />
        <span className="text-[12px] text-foreground">Include Drafts</span>
      </label>

      <label className="flex items-center gap-2 h-8">
        <input
          type="checkbox"
          checked={showZeroBalances}
          onChange={(e) => onShowZeroBalancesChange(e.target.checked)}
          className="size-4 rounded border-border text-[#625f4b] focus:ring-[#625f4b] cursor-pointer"
        />
        <span className="text-[12px] text-foreground">Show Zero Balances</span>
      </label>

      <Button
        variant="ghost"
        size="sm"
        onClick={onRefresh}
        disabled={isFetching}
        className="h-8 px-2 text-muted-foreground cursor-pointer"
      >
        <RefreshCw className={cn("size-3.5", isFetching && "animate-spin")} />
      </Button>

      <Button
        variant="outline"
        size="sm"
        onClick={onExportCsv}
        className="h-8 cursor-pointer"
      >
        <Download className="size-3.5 mr-1" />
        Export CSV
      </Button>
    </div>
  );
}
