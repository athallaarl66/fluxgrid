"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useSalesOrder, useExecutePick } from "@/hooks/useOutbound";
import { getPickListByOrder } from "@/lib/wms-api";
import { PickItemCard } from "@/components/wms/PickItemCard";
import { ShortPickDialog } from "@/components/wms/ShortPickDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { useToast } from "@/components/ui/toast";
import type { PickListItem, PickList } from "@/lib/wms-types";

export default function PickExecutionPage() {
  const router = useRouter();
  const params = useParams();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();
  const orderId = params.id as string;

  const { data: order, isLoading, error } = useSalesOrder(orderId);
  const executePick = useExecutePick();

  const [currentStep, setCurrentStep] = useState(0);
  const [pickResults, setPickResults] = useState<Record<string, { qty: number; shortPickReason?: string }>>({});
  const [shortPickTarget, setShortPickTarget] = useState<PickListItem | null>(null);
  const [showShortDialog, setShowShortDialog] = useState(false);
  const [pickList, setPickList] = useState<PickList | null>(null);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push(`/login?redirect=/wms/outbound/pick/${orderId}`);
    }
  }, [user, authLoading, router, orderId]);

  useEffect(() => {
    if (orderId) {
      getPickListByOrder(orderId).then(setPickList).catch(() => {});
    }
  }, [orderId]);

  const handleConfirm = useCallback((itemId: string, qtyPicked: number) => {
    setPickResults((prev) => ({ ...prev, [itemId]: { qty: qtyPicked } }));
    setCurrentStep((s) => s + 1);
  }, []);

  const handleShortPick = useCallback((item: PickListItem) => {
    setShortPickTarget(item);
    setShowShortDialog(true);
  }, []);

  const handleShortConfirm = useCallback((reason: string, actualQty: number) => {
    if (shortPickTarget) {
      setPickResults((prev) => ({
        ...prev,
        [shortPickTarget.id]: { qty: actualQty, shortPickReason: reason },
      }));
      setCurrentStep((s) => s + 1);
    }
    setShowShortDialog(false);
    setShortPickTarget(null);
  }, [shortPickTarget]);

  const handleFinish = useCallback(() => {
    if (!order || !pickList) return;
    const items = pickList.items.map((i) => ({
      itemId: i.id,
      qtyPicked: pickResults[i.id]?.qty ?? 0,
      shortPickReason: pickResults[i.id]?.shortPickReason ?? null,
    }));

    executePick.mutate(
      { id: pickList.id, data: { items } },
      {
        onSuccess: () => {
          toast("Pick execution completed", "success");
          router.push("/wms/outbound");
        },
        onError: (err) => {
          toast((err as Error).message || "Failed to execute picks", "error");
        },
      },
    );
  }, [order, pickList, pickResults, executePick, router, toast]);

  // Source of truth: pick-list items (real ids/location/expected qty).
  // Fallback: order lines (view-only when no pick list exists yet).
  const items: PickListItem[] = pickList?.items?.length
    ? pickList.items
    : (order?.lines ?? []).map((l) => ({
        id: l.id,
        orderLineId: l.id,
        itemId: l.itemId,
        itemSku: l.itemSku,
        itemName: l.itemName,
        locationId: null,
        locationCode: null,
        qtyExpected: l.qtyOrdered - l.qtyReserved + l.qtyPicked,
        qtyPicked: 0,
        shortPickReason: null,
      }));

  const progress = currentStep > 0 ? Math.min(1, currentStep / items.length) : 0;

  const activeItem = items[currentStep];

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  }

  if (!user) return null;

  if (error || !order) {
    return (
      <div className="p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <p className="text-sm font-medium">Order not found</p>
          <p className="text-xs mt-1">{(error as Error)?.message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-4">
      <WmsNav />

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold tracking-tight">Pick Execution</h1>
          <p className="text-sm text-muted-foreground">{order.orderNo} — {order.customerName}</p>
        </div>
        <span className="text-xs text-muted-foreground">
          Step {Math.min(currentStep + 1, items.length)} of {items.length}
        </span>
      </div>

      <div className="w-full h-1.5 rounded-full bg-muted overflow-hidden">
        <div
          className="h-full rounded-full bg-[#8B9B6F] transition-all duration-300"
          style={{ width: `${items.length > 0 ? (currentStep / items.length) * 100 : 0}%` }}
        />
      </div>

      {currentStep < items.length && activeItem ? (
        <PickItemCard
          item={activeItem}
          onConfirm={handleConfirm}
          onShortPick={handleShortPick}
        />
      ) : (
        <div className="rounded-xl border border-border bg-card p-6 text-center space-y-3">
          <p className="text-sm font-medium text-foreground">All items processed</p>
          <p className="text-xs text-muted-foreground">
            {items.length} item{items.length !== 1 ? "s" : ""} picked. Review and finish to submit.
          </p>
          <div className="space-y-1 text-xs text-left max-w-sm mx-auto">
            {items.map((i) => (
              <div key={i.id} className="flex justify-between">
                <span className="text-muted-foreground">{i.itemSku || i.itemName || i.itemId.slice(0, 8)}</span>
                <span className="font-medium">
                  {pickResults[i.id]?.qty ?? 0} / {i.qtyExpected}
                  {pickResults[i.id]?.shortPickReason ? ` (short: ${pickResults[i.id].shortPickReason})` : ""}
                </span>
              </div>
            ))}
          </div>
          <button
            type="button"
            onClick={handleFinish}
            disabled={executePick.isPending || !pickList}
            className="h-10 px-5 rounded-lg bg-[#8B9B6F] text-white text-sm font-medium cursor-pointer hover:bg-[#7A8B5F] disabled:opacity-40"
          >
            {executePick.isPending ? "Submitting..." : "Finish & Submit"}
          </button>
        </div>
      )}

      <ShortPickDialog
        open={showShortDialog}
        itemLabel={shortPickTarget?.itemSku || shortPickTarget?.itemName || ""}
        qtyExpected={shortPickTarget?.qtyExpected ?? 0}
        onConfirm={(reason, actualQty) => handleShortConfirm(reason, actualQty)}
        onCancel={() => setShowShortDialog(false)}
      />
    </div>
  );
}
