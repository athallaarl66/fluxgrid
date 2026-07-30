"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";

interface TransferFiltersProps {
  onFilter: (filters: {
    dateFrom?: string;
    dateTo?: string;
    itemId?: string;
  }) => void;
}

export function TransferFilters({ onFilter }: TransferFiltersProps) {
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [itemId, setItemId] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    onFilter({
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
      itemId: itemId || undefined,
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex items-end gap-2 flex-wrap">
      <div className="space-y-1">
        <label className="text-[10px] font-medium text-muted-foreground">From Date</label>
        <Input
          type="date"
          value={dateFrom}
          onChange={(e) => setDateFrom(e.target.value)}
          className="h-8 w-36 border-border bg-card text-sm"
        />
      </div>
      <div className="space-y-1">
        <label className="text-[10px] font-medium text-muted-foreground">To Date</label>
        <Input
          type="date"
          value={dateTo}
          onChange={(e) => setDateTo(e.target.value)}
          className="h-8 w-36 border-border bg-card text-sm"
        />
      </div>
      <div className="space-y-1">
        <label className="text-[10px] font-medium text-muted-foreground">Item ID</label>
        <Input
          value={itemId}
          onChange={(e) => setItemId(e.target.value)}
          placeholder="UUID"
          className="h-8 w-48 border-border bg-card text-sm"
        />
      </div>
      <button
        type="submit"
        className="h-8 px-3 rounded border border-accent bg-accent text-accent-foreground text-xs font-semibold hover:brightness-[0.95]"
      >
        Filter
      </button>
    </form>
  );
}
