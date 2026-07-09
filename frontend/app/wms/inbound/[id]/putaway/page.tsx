"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useReceipt, useSubmitPutaway } from "@/hooks/useInbound";
import { BinSelectionTable } from "@/components/wms/BinSelectionTable";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { useToast } from "@/components/ui/toast";

const STATIC_LOCATIONS: { id: string; code: string; type: string }[] = [
  { id: "00000000-0000-0000-0000-000000000002", code: "WH-MAIN", type: "WAREHOUSE" },
  { id: "00000000-0000-0000-0000-000000000003", code: "QUARANTINE", type: "QUARANTINE" },
];

export default function PutawayPage({ params }: { params: { id: string } }) {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();

  const { data: receipt, isLoading, error } = useReceipt(params.id);
  const putawayMutation = useSubmitPutaway();

  const [binAssignments, setBinAssignments] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push(`/login?redirect=/wms/inbound/${params.id}/putaway`);
    }
  }, [user, authLoading, router, params.id]);

  const handleBinChange = useCallback((key: string, locationId: string) => {
    setBinAssignments((prev) => ({ ...prev, [key]: locationId }));
  }, []);

  const handleSubmit = async () => {
    if (!receipt) return;
    setSubmitError(null);

    const hasGoodQty = receipt.lines.some((l) => l.qtyPassed > 0);
    if (hasGoodQty) {
      const allAssigned = receipt.lines
        .filter((l) => l.qtyPassed > 0)
        .every((l) => binAssignments[l.id + "-good"]);
      if (!allAssigned) {
        setSubmitError("Please assign a bin for all lines with passed quantities");
        return;
      }
    }

    const lines = receipt.lines
      .filter((l) => l.qtyPassed > 0)
      .map((l) => ({
        lineId: l.id,
        locationId: binAssignments[l.id + "-good"] || "",
      }));

    try {
      const result = await putawayMutation.mutateAsync({ id: receipt.id, lines });
      if (result.success) {
        toast("Putaway completed successfully", "success");
        router.push("/wms/inbound");
      } else {
        setSubmitError(result.error || "Putaway failed");
      }
    } catch (err) {
      setSubmitError((err as Error).message || "An unexpected error occurred");
    }
  };

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!user) return null;

  if (error) {
    return (
      <div className="p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <p className="text-sm font-medium">Failed to load receipt</p>
          <p className="text-xs mt-1">{(error as Error).message}</p>
        </div>
      </div>
    );
  }

  if (!receipt) {
    return (
      <div className="p-5">
        <p className="text-sm text-muted-foreground">Receipt not found</p>
      </div>
    );
  }

  if (receipt.status !== "PENDING_PUTAWAY") {
    return (
      <div className="p-5 space-y-4">
        <WmsNav />
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-amber-800">
          <p className="text-sm font-medium">Receipt is not in PENDING_PUTAWAY status</p>
          <p className="text-xs mt-1">Current status: {receipt.status}</p>
        </div>
      </div>
    );
  }

  const canSubmit = receipt.lines
    .filter((l) => l.qtyPassed > 0)
    .every((l) => binAssignments[l.id + "-good"])
    && !putawayMutation.isPending;

  return (
    <div className="p-5 space-y-4">
      <WmsNav />

      <div>
        <h1 className="text-xl font-semibold tracking-tight">Putaway Assignment</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Receipt: {receipt.receiptNo} — PO: {receipt.poReference}
        </p>
      </div>

      <div className="rounded-lg border border-border bg-card p-3 flex items-center gap-4 text-xs">
        <div><span className="text-muted-foreground">Status: </span><span className="font-medium text-yellow-700">PENDING_PUTAWAY</span></div>
        <div><span className="text-muted-foreground">Received by: </span><span className="font-medium">{receipt.receivedBy}</span></div>
        <div><span className="text-muted-foreground">Date: </span><span className="font-medium">{new Date(receipt.createdAt).toLocaleDateString("id-ID")}</span></div>
      </div>

      <div className="rounded-lg border border-border bg-card p-4 space-y-4">
        <h2 className="text-sm font-semibold">Assign Storage Bins</h2>
        <BinSelectionTable
          lines={receipt.lines}
          binAssignments={binAssignments}
          locations={STATIC_LOCATIONS}
          onBinChange={handleBinChange}
        />
      </div>

      {submitError && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-800">
          {submitError}
        </div>
      )}

      <div className="flex gap-2">
        <button
          onClick={handleSubmit}
          disabled={!canSubmit}
          className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/80 disabled:opacity-50 cursor-pointer"
        >
          {putawayMutation.isPending ? (
            <>
              <span className="inline-block size-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              Processing...
            </>
          ) : (
            "Confirm Putaway"
          )}
        </button>
        <button
          onClick={() => router.push("/wms/inbound")}
          className="h-8 px-3 rounded-lg border border-border text-sm font-medium hover:bg-muted cursor-pointer"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
