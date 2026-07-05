import React from "react";
import { Period } from "../../lib/period-types";
import StatusBadge from "./StatusBadge";
import { cn } from "@/lib/utils";

interface ActionMenuProps {
  period: Period;
  onClose: () => void;
  onReopen: () => void;
}

export default function ActionMenu({ period, onClose, onReopen }: ActionMenuProps) {
  const canClose = period.status === "OPEN";
  const canReopen = period.status === "CLOSED";

  return (
    <div className="flex items-center gap-2">
      <StatusBadge status={period.status as any} />
      {canClose && (
        <button className="text-primary-600 hover:underline" onClick={onClose}>Close</button>
      )}
      {canReopen && (
        <button className="text-destructive-600 hover:underline" onClick={onReopen}>Reopen</button>
      )}
    </div>
  );
}
