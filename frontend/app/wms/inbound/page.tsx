"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/lib/auth-context";
import { useReceipts } from "@/hooks/useInbound";
import { InboundTable } from "@/components/wms/InboundTable";
import { Skeleton } from "@/components/ui/skeleton";
import { WmsNav } from "@/components/wms/WmsNav";

export default function InboundPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useReceipts({ page, pageSize: 20 });

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/wms/inbound");
    }
  }, [user, authLoading, router]);

  if (authLoading) {
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
          <p className="text-sm font-medium">Failed to load receipts</p>
          <p className="text-xs mt-1">{(error as Error).message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-4">
      <WmsNav />

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold tracking-tight">Inbound Receipts</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage purchase receipts and putaway
          </p>
        </div>
        <Link
          href="/wms/inbound/create"
          className="inline-flex items-center gap-1.5 h-8 px-2.5 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/80 cursor-pointer"
        >
          + New Receipt
        </Link>
      </div>

      {isLoading ? (
        <div className="space-y-1">
          {Array.from({ length: 10 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      ) : data && data.items.length > 0 ? (
        <>
          <InboundTable
            receipts={data.items}
            onProcessPutaway={(id) => router.push(`/wms/inbound/${id}/putaway`)}
          />
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>Page {data.page} of {Math.ceil(data.total / data.pageSize)}</span>
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
          <p className="text-sm font-medium">No receipts found</p>
          <Link href="/wms/inbound/create" className="text-xs text-primary hover:underline mt-2">
            Create your first receipt
          </Link>
        </div>
      )}
    </div>
  );
}
