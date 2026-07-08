"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useStockLedger } from "@/hooks/useStockLedger";
import { Skeleton } from "@/components/ui/skeleton";
import { ValuationToggle } from "@/components/wms/ValuationToggle";
import { LedgerFilters } from "@/components/wms/LedgerFilters";
import { StockLedgerTable } from "@/components/wms/StockLedgerTable";
import { StockLedgerMobileList } from "@/components/wms/StockLedgerMobileList";
import { LedgerDetailSheet } from "@/components/wms/LedgerDetailSheet";

export default function StockLedgerPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [valuationMethod, setValuationMethod] = useState<"fifo" | "average">("fifo");
  const [sku, setSku] = useState("");
  const [locationId, setLocationId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [page, setPage] = useState(1);
  const [selectedTransactionId, setSelectedTransactionId] = useState<string | null>(null);

  const { data, isLoading, error } = useStockLedger({
    sku: sku || undefined,
    locationCode: locationId || undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
    page,
    pageSize: 20,
  });

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/wms/stock-ledger");
    }
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!user) return null;

  if (error) {
    return (
      <div className="p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <p className="text-sm font-medium">Failed to load stock ledger</p>
          <p className="text-xs mt-1">{(error as Error).message}</p>
          <button
            onClick={() => window.location.reload()}
            className="mt-2 text-xs font-medium text-red-600 hover:text-red-800 underline cursor-pointer"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Stock Ledger</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Track all inventory movements with double-entry traceability
        </p>
      </div>

      <div className="flex items-center justify-between gap-4 flex-wrap">
        <ValuationToggle value={valuationMethod} onChange={setValuationMethod} />
        <LedgerFilters
          sku={sku}
          onSkuChange={(v) => { setSku(v); setPage(1); }}
          locationId={locationId}
          onLocationChange={(v) => { setLocationId(v); setPage(1); }}
          startDate={startDate}
          onStartDateChange={(v) => { setStartDate(v); setPage(1); }}
          endDate={endDate}
          onEndDateChange={(v) => { setEndDate(v); setPage(1); }}
        />
      </div>

      {isLoading ? (
        <div className="space-y-1">
          {Array.from({ length: 10 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      ) : data && data.items.length > 0 ? (
        <>
          <div className="hidden md:block">
            <StockLedgerTable
              entries={data.items}
              valuationMethod={valuationMethod}
              onRowClick={(id) => setSelectedTransactionId(id)}
            />
          </div>
          <div className="md:hidden">
            <StockLedgerMobileList
              entries={data.items}
              valuationMethod={valuationMethod}
              onEntryClick={(id) => setSelectedTransactionId(id)}
            />
          </div>
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>
              Page {data.page} of {Math.ceil(data.total / data.pageSize)}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1 rounded border border-border disabled:opacity-40 cursor-pointer"
              >
                Previous
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= Math.ceil((data?.total ?? 0) / 20)}
                className="px-3 py-1 rounded border border-border disabled:opacity-40 cursor-pointer"
              >
                Next
              </button>
            </div>
          </div>
        </>
      ) : (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground">
          <div className="size-16 mb-4 rounded-full bg-muted flex items-center justify-center">
            <span className="text-2xl">📦</span>
          </div>
          <p className="text-sm font-medium">No ledger entries found for the selected filters</p>
        </div>
      )}

      {selectedTransactionId && (
        <LedgerDetailSheet
          transactionId={selectedTransactionId}
          open={!!selectedTransactionId}
          onClose={() => setSelectedTransactionId(null)}
        />
      )}
    </div>
  );
}
