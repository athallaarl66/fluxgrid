import React from "react";
import { cn } from "@/lib/utils";

interface StatusBadgeProps {
  status: "OPEN" | "CLOSED";
}

export default function StatusBadge({ status }: StatusBadgeProps) {
  const isOpen = status === "OPEN";
  const bg = isOpen ? "bg-secondary-100" : "bg-outline-100";
  const txt = isOpen ? "text-secondary-800" : "text-outline-800";
  return (
    <span className={cn("inline-flex items-center rounded px-2 py-0.5 text-[11px] font-semibold", bg, txt)}>
      {status}
    </span>
  );
}
