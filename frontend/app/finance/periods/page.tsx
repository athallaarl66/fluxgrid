"use client";

import React, { useState } from "react";
import { usePeriods, useValidateClose, useClosePeriod, useReopenPeriod, useGeneratePeriods } from "../../../hooks/usePeriods";
import { Period } from "../../../lib/period-types";
import PeriodsTable from "../../../components/finance/PeriodsTable";
import ClosePeriodDialog from "../../../components/finance/ClosePeriodDialog";
import ReopenPeriodDialog from "../../../components/finance/ReopenPeriodDialog";

export default function PeriodsPage() {
  const { data: periods, isLoading } = usePeriods();
  const generateMutation = useGeneratePeriods();
  
  const [selectedPeriod, setSelectedPeriod] = useState<Period | null>(null);
  const [showCloseDialog, setShowCloseDialog] = useState(false);
  const [showReopenDialog, setShowReopenDialog] = useState(false);

  const handleActionMenu = (period: Period) => {
    setSelectedPeriod(period);
    if (period.status === "OPEN") {
      setShowCloseDialog(true);
    } else {
      setShowReopenDialog(true);
    }
  };

  if (isLoading) {
    return <div className="p-5 text-sm text-[#49473e]">Loading periods…</div>;
  }

  return (
    <div className="p-5 bg-[#fdf8f5] min-h-screen">
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3 mb-4">
        <h1 className="text-2xl font-semibold text-[#1c1b1a] tracking-tight leading-tight">Accounting Periods</h1>
        <button
          onClick={() => generateMutation.mutate()}
          className="bg-[#625f4b] hover:bg-[#706d59] text-white px-4 py-2 rounded text-sm font-medium transition-colors w-full sm:w-auto"
          disabled={generateMutation.isPending}
        >
          {generateMutation.isPending ? "Generating…" : "Generate Missing Periods"}
        </button>
      </div>
      <PeriodsTable periods={periods || []} onActionMenu={handleActionMenu} />
      
      {selectedPeriod && showCloseDialog && (
        <ClosePeriodDialog
          period={selectedPeriod}
          open={showCloseDialog}
          onClose={() => setShowCloseDialog(false)}
        />
      )}
      
      {selectedPeriod && showReopenDialog && (
        <ReopenPeriodDialog
          period={selectedPeriod}
          open={showReopenDialog}
          onClose={() => setShowReopenDialog(false)}
        />
      )}
    </div>
  );
}
