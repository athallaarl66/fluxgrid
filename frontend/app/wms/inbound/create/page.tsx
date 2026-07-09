"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { usePurchaseOrder, useCreateReceipt } from "@/hooks/useInbound";
import { POSearchForm } from "@/components/wms/POSearchForm";
import { ReceiptLineItems, type LineItem } from "@/components/wms/ReceiptLineItems";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { useToast } from "@/components/ui/toast";

export default function CreateReceiptPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();

  const [poNumber, setPoNumber] = useState("");
  const [searchedPo, setSearchedPo] = useState<string | null>(null);
  const [receivedBy, setReceivedBy] = useState("");
  const [lines, setLines] = useState<LineItem[]>([]);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const { data: po, isFetching: poLoading, error: poError } = usePurchaseOrder(searchedPo ?? "");
  const createMutation = useCreateReceipt();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/wms/inbound/create");
    }
  }, [user, authLoading, router]);

  useEffect(() => {
    if (po) {
      setLines(po.lines.map((l) => ({
        itemId: l.itemId,
        itemSku: l.itemSku,
        itemName: l.itemName,
        orderedQty: l.orderedQty,
        qtyReceived: 0,
        qtyPassed: 0,
        qtyFailed: 0,
      })));
      setSearchError(null);
    }
  }, [po]);

  useEffect(() => {
    if (poError) {
      setSearchError("PO not found. Please check the PO number.");
      setLines([]);
    }
  }, [poError]);

  const handleSearch = useCallback(() => {
    if (!poNumber.trim()) return;
    setSearchedPo(null);
    setSearchError(null);
    setLines([]);
    setTimeout(() => setSearchedPo(poNumber.trim()), 0);
  }, [poNumber]);

  const handleLineChange = useCallback((index: number, field: "qtyReceived" | "qtyPassed" | "qtyFailed", value: number) => {
    setLines((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], [field]: value };
      return next;
    });
  }, []);

  const validateLines = useCallback((): string | null => {
    for (let i = 0; i < lines.length; i++) {
      const l = lines[i];
      if (l.qtyReceived > l.orderedQty) {
        return `Over-receiving: ${l.itemSku || l.itemName} — received ${l.qtyReceived} exceeds ordered ${l.orderedQty}`;
      }
      if (l.qtyPassed + l.qtyFailed !== l.qtyReceived) {
        return `Qty mismatch: ${l.itemSku || l.itemName} — passed (${l.qtyPassed}) + failed (${l.qtyFailed}) ≠ received (${l.qtyReceived})`;
      }
    }
    return null;
  }, [lines]);

  const handleSubmit = async () => {
    setSubmitError(null);
    const validationError = validateLines();
    if (validationError) {
      setSubmitError(validationError);
      return;
    }

    try {
      const result = await createMutation.mutateAsync({
        poReference: poNumber.trim(),
        receivedBy: receivedBy || user?.name || "Unknown",
        lines: lines.map((l) => ({
          itemId: l.itemId,
          qtyReceived: l.qtyReceived,
          qtyPassed: l.qtyPassed,
          qtyFailed: l.qtyFailed,
        })),
      });

      if (result.success) {
        toast("Receipt created successfully", "success");
        router.push("/wms/inbound");
      } else {
        setSubmitError(result.error || "Failed to create receipt");
      }
    } catch (err) {
      setSubmitError((err as Error).message || "An unexpected error occurred");
    }
  };

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!user) return null;

  const canSubmit = lines.length > 0 && lines.some((l) => l.qtyReceived > 0) && !createMutation.isPending;

  return (
    <div className="p-5 space-y-4">
      <WmsNav />

      <div>
        <h1 className="text-xl font-semibold tracking-tight">New Receipt</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Search for a purchase order and record received quantities
        </p>
      </div>

      <div className="rounded-lg border border-border bg-card p-4 space-y-4">
        <h2 className="text-sm font-semibold">1. Search Purchase Order</h2>
        <POSearchForm
          poNumber={poNumber}
          onPoNumberChange={(v) => { setPoNumber(v); setSearchError(null); }}
          onSearch={handleSearch}
          loading={poLoading}
          po={po ?? null}
          error={searchError}
        />
      </div>

      {lines.length > 0 && (
        <div className="rounded-lg border border-border bg-card p-4 space-y-4">
          <h2 className="text-sm font-semibold">2. Line Items & Quality Check</h2>
          <ReceiptLineItems lines={lines} onChange={handleLineChange} />
        </div>
      )}

      {lines.length > 0 && (
        <div className="rounded-lg border border-border bg-card p-4 space-y-3">
          <h2 className="text-sm font-semibold">3. Confirm Receipt</h2>
          <div>
            <label className="text-xs text-muted-foreground block mb-1">Received By</label>
            <input
              type="text"
              value={receivedBy}
              onChange={(e) => setReceivedBy(e.target.value)}
              placeholder={user?.name || "Username"}
              className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 w-64"
            />
          </div>

          {submitError && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-800">
              {submitError}
            </div>
          )}

          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/80 disabled:opacity-50 cursor-pointer"
          >
            {createMutation.isPending ? (
              <>
                <span className="inline-block size-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                Submitting...
              </>
            ) : (
              "Confirm Receipt"
            )}
          </button>
        </div>
      )}
    </div>
  );
}
