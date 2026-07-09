"use client";

import { useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useSalesOrders } from "@/hooks/useOutbound";
import { OutboundKanban } from "@/components/wms/OutboundKanban";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";
import { useToast } from "@/components/ui/toast";
import { useGeneratePickList } from "@/hooks/useOutbound";
import type { SalesOrder } from "@/lib/wms-types";

export default function OutboundPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();
  const { data, isLoading, error } = useSalesOrders({ page: 1, pageSize: 50 });
  const generatePick = useGeneratePickList();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/wms/outbound");
    }
  }, [user, authLoading, router]);

  const handleAction = useCallback((action: string, order: SalesOrder) => {
    switch (action) {
      case "generate-pick":
        generatePick.mutate(order.id, {
          onSuccess: (result) => {
            if (result.success && result.pickListId) {
              toast(`Pick list generated for ${order.orderNo}`, "success");
            } else {
              toast(result.error || "Failed to generate pick list", "error");
            }
          },
          onError: (err) => {
            toast((err as Error).message || "Failed to generate pick list", "error");
          },
        });
        break;
      case "pick":
        router.push(`/wms/outbound/pick/${order.id}`);
        break;
      case "pack":
      case "ship":
        router.push(`/wms/outbound/ship/${order.id}`);
        break;
    }
  }, [generatePick, router, toast]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  if (error) {
    return (
      <div className="p-5">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <p className="text-sm font-medium">Failed to load sales orders</p>
          <p className="text-xs mt-1">{(error as Error).message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <WmsNav />

      <div>
        <h1 className="text-xl font-semibold tracking-tight">Outbound Processing</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Pick, pack, and ship customer orders
        </p>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))}
        </div>
      ) : (
        <OutboundKanban
          orders={data?.items ?? []}
          onAction={handleAction}
        />
      )}
    </div>
  );
}
