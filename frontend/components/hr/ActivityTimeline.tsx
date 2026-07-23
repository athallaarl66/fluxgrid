"use client";

import { useState } from "react";
import { ArrowRightLeft, UserPlus, UserMinus, MessageSquare, Upload, CheckCircle, Edit3, Send } from "lucide-react";
import { useActivityLog, useAddNote, type ActivityLogEntry } from "@/hooks/useRecruitment";

const ACTION_ICONS: Record<string, typeof ArrowRightLeft> = {
  STATUS_CHANGED: ArrowRightLeft,
  ASSIGNED_TO_JOB: UserPlus,
  REMOVED_FROM_JOB: UserMinus,
  NOTE_ADDED: MessageSquare,
  CV_UPLOADED: Upload,
  PARSE_COMPLETED: CheckCircle,
  DATA_EDITED: Edit3,
};

const ACTION_LABELS: Record<string, string> = {
  STATUS_CHANGED: "Status changed",
  ASSIGNED_TO_JOB: "Assigned to job",
  REMOVED_FROM_JOB: "Removed from job",
  NOTE_ADDED: "Note added",
  CV_UPLOADED: "CV uploaded",
  PARSE_COMPLETED: "Parse completed",
  DATA_EDITED: "Data edited",
};

function formatTime(dateStr: string) {
  const d = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}h ago`;
  const diffD = Math.floor(diffH / 24);
  return `${diffD}d ago`;
}

function getDetailText(entry: ActivityLogEntry): string {
  if (!entry.details) return "";
  try {
    const d = JSON.parse(entry.details);
    if (entry.action === "STATUS_CHANGED") return `${d.from} → ${d.to}`;
    if (entry.action === "ASSIGNED_TO_JOB") return d.jobTitle ?? "";
    if (entry.action === "REMOVED_FROM_JOB") return d.jobTitle ?? "";
    if (entry.action === "NOTE_ADDED") return d.note ?? "";
    if (entry.action === "DATA_EDITED" && d.fields) return `Edited: ${d.fields.join(", ")}`;
    return "";
  } catch {
    return "";
  }
}

export function ActivityTimeline({ candidateId }: { candidateId: string }) {
  const { data, isLoading } = useActivityLog(candidateId);
  const addNote = useAddNote();
  const [note, setNote] = useState("");

  async function handleAddNote() {
    if (!note.trim()) return;
    await addNote.mutateAsync({ candidateId, note: note.trim() });
    setNote("");
  }

  if (isLoading) {
    return <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-12 rounded-lg bg-muted animate-pulse" />)}</div>;
  }

  const entries = data?.items ?? [];

  return (
    <div className="space-y-3">
      <div className="flex gap-2">
        <input
          type="text"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleAddNote()}
          placeholder="Add a note..."
          className="flex-1 h-7 rounded-md border border-input bg-transparent px-2 text-xs outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
        <button
          type="button"
          onClick={handleAddNote}
          disabled={!note.trim() || addNote.isPending}
          className="h-7 px-2 rounded-md bg-[#C5D89D] text-[#2D4A1E] text-xs font-medium hover:bg-[#A8C47A] disabled:opacity-50 cursor-pointer"
        >
          <Send className="size-3" />
        </button>
      </div>

      {entries.length === 0 ? (
        <p className="text-[11px] text-muted-foreground italic text-center py-4">No activity recorded</p>
      ) : (
        <div className="space-y-0">
          {entries.map((entry) => {
            const Icon = ACTION_ICONS[entry.action] ?? ArrowRightLeft;
            return (
              <div key={entry.id} className="flex gap-3 py-2">
                <div className="flex flex-col items-center">
                  <div className="size-6 rounded-full bg-muted flex items-center justify-center shrink-0">
                    <Icon className="size-3 text-muted-foreground" />
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[11px] font-medium text-foreground">{ACTION_LABELS[entry.action] ?? entry.action}</p>
                  {getDetailText(entry) && (
                    <p className="text-[10px] text-muted-foreground truncate">{getDetailText(entry)}</p>
                  )}
                  <p className="text-[10px] text-muted-foreground">{formatTime(entry.createdAt)}</p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
