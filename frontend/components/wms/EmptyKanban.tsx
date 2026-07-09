"use client";

import { PackageOpen } from "lucide-react";

export function EmptyKanban() {
  return (
    <div className="col-span-full flex flex-col items-center justify-center py-20 text-muted-foreground">
      <PackageOpen className="size-12 mb-3 text-muted-foreground/40" />
      <p className="text-sm font-medium">No outbound orders</p>
      <p className="text-xs mt-1">Sales orders will appear here once created</p>
    </div>
  );
}
