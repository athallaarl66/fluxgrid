"use client";

import { GripVertical, FileText } from "lucide-react";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import type { CandidateListItem } from "@/lib/hr-types";

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", { day: "numeric", month: "short" });
}

export function KanbanCard({
  candidate,
  onNavigate,
}: {
  candidate: CandidateListItem;
  onNavigate: (id: string) => void;
}) {
  return (
    <div
      className="rounded-lg border border-border bg-card p-3 cursor-pointer hover:border-[#9CAB84] transition-colors group"
      onClick={() => onNavigate(candidate.id)}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-1.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
          <GripVertical className="size-3.5" data-drag-handle />
        </div>
        <CandidateStatusBadge status={candidate.status} />
      </div>
      <p className="text-xs font-semibold text-foreground mt-1.5 truncate">{candidate.name}</p>
      <p className="text-[10px] text-muted-foreground truncate">{candidate.email}</p>
      <div className="flex items-center gap-2 mt-2 text-[10px] text-muted-foreground">
        <span>{formatDate(candidate.createdAt)}</span>
        {candidate.fileType && (
          <span className="flex items-center gap-0.5">
            <FileText className="size-2.5" />
            {candidate.fileType.toUpperCase()}
          </span>
        )}
      </div>
    </div>
  );
}
