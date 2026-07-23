"use client";

import { useDroppable } from "@dnd-kit/core";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import { KanbanCard } from "@/components/hr/KanbanCard";
import type { CandidateListItem, CandidateStatus } from "@/lib/hr-types";

const STATUS_LABELS: Record<CandidateStatus, string> = {
  DRAFT: "Draft",
  PARSED: "Parsed",
  PARSE_FAILED: "Parse Failed",
  ACTIVE: "Active",
  INTERVIEW: "Interview",
  HIRED: "Hired",
  REJECTED: "Rejected",
  ARCHIVED: "Archived",
};

export function KanbanColumn({
  status,
  candidates,
  onNavigate,
}: {
  status: CandidateStatus;
  candidates: CandidateListItem[];
  onNavigate: (id: string) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: status });

  return (
    <div
      ref={setNodeRef}
      className={`flex flex-col min-w-[260px] max-w-[260px] rounded-xl border transition-colors ${
        isOver ? "border-[#9CAB84] bg-[#f5f9f0]" : "border-border bg-muted/30"
      }`}
    >
      <div className="flex items-center justify-between px-3 py-2 border-b border-border">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">{STATUS_LABELS[status]}</span>
          <span className="inline-flex size-4 items-center justify-center rounded-full bg-muted text-[10px] font-medium text-muted-foreground">
            {candidates.length}
          </span>
        </div>
        <CandidateStatusBadge status={status} />
      </div>
      <div className="flex-1 overflow-y-auto p-2 space-y-2 min-h-[100px]">
        {candidates.map((c) => (
          <KanbanCard key={c.id} candidate={c} onNavigate={onNavigate} />
        ))}
        {candidates.length === 0 && (
          <p className="text-[10px] text-muted-foreground text-center py-4 italic">No candidates</p>
        )}
      </div>
    </div>
  );
}
