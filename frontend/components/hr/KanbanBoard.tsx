"use client";

import { useState } from "react";
import { DndContext, DragOverlay, PointerSensor, useSensor, useSensors, closestCenter } from "@dnd-kit/core";
import type { DragStartEvent, DragEndEvent } from "@dnd-kit/core";
import { KanbanColumn } from "@/components/hr/KanbanColumn";
import { KanbanCard } from "@/components/hr/KanbanCard";
import { useKanban, useChangeCandidateStatus } from "@/hooks/useRecruitment";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

const COLUMNS = ["DRAFT", "PARSED", "ACTIVE", "INTERVIEW", "HIRED"] as const;
const COLLAPSED_COLUMNS = ["REJECTED", "ARCHIVED"] as const;

export function KanbanBoard() {
  const router = useRouter();
  const { data: candidates, isLoading } = useKanban();
  const changeStatus = useChangeCandidateStatus();
  const [activeId, setActiveId] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
  );

  const grouped = (candidates ?? []).reduce<Record<string, typeof candidates>>((acc, c) => {
    (acc[c.status] ??= []).push(c);
    return acc;
  }, {});

  const activeCandidate = activeId ? candidates?.find((c) => c.id === activeId) : null;

  function handleDragStart(event: DragStartEvent) {
    setActiveId(String(event.active.id));
  }

  async function handleDragEnd(event: DragEndEvent) {
    setActiveId(null);
    const { active, over } = event;
    if (!over) return;

    const candidateId = String(active.id);
    const newStatus = String(over.id);

    const candidate = candidates?.find((c) => c.id === candidateId);
    if (!candidate || candidate.status === newStatus) return;

    try {
      await changeStatus.mutateAsync({ id: candidateId, status: newStatus });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Invalid transition";
      toast.error(msg);
    }
  }

  if (isLoading) {
    return (
      <div className="flex gap-3 p-5">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex-1 h-[400px] rounded-xl bg-muted/30 animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <div className="flex gap-3 p-5 overflow-x-auto">
        {COLUMNS.map((status) => (
          <KanbanColumn
            key={status}
            status={status}
            candidates={grouped[status] ?? []}
            onNavigate={(id) => router.push(`/hr/recruitment/${id}`)}
          />
        ))}
        {COLLAPSED_COLUMNS.map((status) => (
          <KanbanColumn
            key={status}
            status={status}
            candidates={grouped[status] ?? []}
            onNavigate={(id) => router.push(`/hr/recruitment/${id}`)}
          />
        ))}
      </div>
      <DragOverlay>
        {activeCandidate ? (
          <div className="opacity-80 rotate-2">
            <KanbanCard candidate={activeCandidate} onNavigate={() => {}} />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
