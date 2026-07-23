"use client";

import { useDraggable } from "@dnd-kit/core";
import { GripVertical, FileText } from "lucide-react";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import type { CandidateListItem } from "@/lib/hr-types";
import { formatDate } from "@/lib/date-utils";

export function KanbanCard({
  candidate,
  onNavigate,
}: {
  candidate: CandidateListItem;
  onNavigate: (id: string) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: candidate.id,
  });

  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)` }
    : undefined;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`rounded-lg border bg-card p-3 cursor-grab active:cursor-grabbing transition-colors group ${
        isDragging ? "opacity-50 border-[#9CAB84] shadow-lg" : "border-border hover:border-[#9CAB84]"
      }`}
      {...attributes}
      {...listeners}
      onClick={(e) => {
        if (!isDragging) onNavigate(candidate.id);
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-1.5 text-muted-foreground">
          <GripVertical className="size-3.5" />
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
