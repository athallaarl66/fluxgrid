"use client";

import { useState } from "react";
import { ArrowRightFromLine } from "lucide-react";
import { useTransfers } from "@/hooks/useTransfers";
import { TransferTable } from "@/components/wms/TransferTable";
import { TransferFilters } from "@/components/wms/TransferFilters";

export default function TransfersPage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<{
    dateFrom?: string;
    dateTo?: string;
    itemId?: string;
  }>({});

  const { data, isLoading } = useTransfers({
    ...filters,
    page,
    pageSize: 20,
  });

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <ArrowRightFromLine className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Transfer Log
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Warehouse inventory transfers between locations
          </p>
        </div>
      </div>

      <TransferFilters onFilter={(f) => { setFilters(f); setPage(1); }} />

      <TransferTable
        transfers={data?.transfers ?? []}
        total={data?.total ?? 0}
        page={page}
        pageSize={20}
        isLoading={isLoading}
        onPageChange={setPage}
      />
    </div>
  );
}
