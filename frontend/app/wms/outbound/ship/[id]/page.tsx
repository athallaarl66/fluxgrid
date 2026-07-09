"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useSalesOrder, useConfirmShipment } from "@/hooks/useOutbound";
import type { PickList } from "@/lib/wms-types";
import { PackingTable } from "@/components/wms/PackingTable";
import { ShipConfirmDialog } from "@/components/wms/ShipConfirmDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { useToast } from "@/components/ui/toast";

export default function ShipPage() {
  const router = useRouter();
  const params = useParams();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();
  const orderId = params.id as string;

  const { data: order, isLoading: orderLoading, error: orderError } = useSalesOrder(orderId);
  const confirmShipment = useConfirmShipment();
  const [showConfirm, setShowConfirm] = useState(false);
  const [verified, setVerified] = useState(false);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push(`/login?redirect=/wms/outbound/ship/${orderId}`);
    }
  }, [user, authLoading, router, orderId]);

  const handleVerify = useCallback(async (lines: { itemId: string; verifiedQty: number }[]) => {
    toast("Packing verification passed. Ready to ship.", "success");
    setVerified(true);
  }, [toast]);

  const handleShipConfirm = useCallback(async () => {
    await confirmShipment.mutateAsync(orderId, {
      onSuccess: (result) => {
        if (result.success) {
          toast(`Shipment ${result.shipmentId} confirmed`, "success");
          router.push("/wms/outbound");
        } else {
          toast(result.error || "Failed to confirm shipment", "error");
        }
      },
      onError: (err) => {
        toast((err as Error).message || "Failed to confirm shipment", "error");
      },
    });
  }, [confirmShipment, orderId, router, toast]);

  if (authLoading || orderLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  }

  if (!user) return null;

  if (orderError || !order) {
    return (
      <div className="p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <p className="text-sm font-medium">Order not found</p>
          <p className="text-xs mt-1">{(orderError as Error)?.message}</p>
        </div>
      </div>
    );
  }

  const pickList: PickList = {
    id: "",
    orderId: order.id,
    orderNo: order.orderNo,
    status: "COMPLETED",
    assignedTo: null,
    createdAt: order.createdAt,
    items: order.lines.map((l) => ({
      id: l.id,
      orderLineId: l.id,
      itemId: l.itemId,
      itemSku: l.itemSku,
      itemName: l.itemName,
      locationId: null,
      locationCode: null,
      qtyExpected: l.qtyOrdered,
      qtyPicked: l.qtyPicked,
      shortPickReason: null,
    })),
    tenantId: order.tenantId,
  };

  return (
    <div className="p-5 space-y-4">
      <WmsNav />

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold tracking-tight">Packing & Shipment</h1>
          <p className="text-sm text-muted-foreground">{order.orderNo} — {order.customerName}</p>
        </div>
        {order.status === "PACKED" && verified && (
          <button
            type="button"
            onClick={() => setShowConfirm(true)}
            className="h-8 px-3 rounded-lg bg-primary text-primary-foreground text-xs font-medium cursor-pointer hover:bg-primary/80"
          >
            Confirm Shipment
          </button>
        )}
      </div>

      <PackingTable pickList={pickList} onVerify={handleVerify} />

      <ShipConfirmDialog
        open={showConfirm}
        pickList={pickList}
        onConfirm={handleShipConfirm}
        onCancel={() => setShowConfirm(false)}
      />
    </div>
  );
}
