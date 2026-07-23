"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight, Check, X, Loader2 } from "lucide-react";
import { useChangeCandidateStatus } from "@/hooks/useRecruitment";
import { MatchScoreBadge } from "@/components/hr/MatchScoreBadge";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import type { JobMatchItem } from "@/lib/hr-types";
import { toast } from "sonner";

export function AllApplicantsTable({
  matches,
  jobId,
}: {
  matches: JobMatchItem[];
  jobId: string;
}) {
  const changeStatus = useChangeCandidateStatus();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<"score" | "name" | "date">("score");
  const [filterType, setFilterType] = useState<"all" | "ai" | "manual">("all");

  const filtered = matches
    .filter((m) => filterType === "all" || m.matchType.toLowerCase() === filterType)
    .sort((a, b) => {
      if (sortBy === "name") return a.candidateName.localeCompare(b.candidateName);
      if (sortBy === "date") return new Date(b.calculatedAt).getTime() - new Date(a.calculatedAt).getTime();
      return b.matchScore - a.matchScore;
    });

  async function handleStatusChange(candidateId: string, status: string) {
    try {
      await changeStatus.mutateAsync({ id: candidateId, status });
      toast.success(`Candidate moved to ${status}`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to update status";
      toast.error(msg);
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3">
        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value as typeof sortBy)}
          className="h-7 rounded-md border border-input bg-transparent px-2 text-[11px] outline-none"
        >
          <option value="score">Sort by Score</option>
          <option value="name">Sort by Name</option>
          <option value="date">Sort by Date</option>
        </select>
        <select
          value={filterType}
          onChange={(e) => setFilterType(e.target.value as typeof filterType)}
          className="h-7 rounded-md border border-input bg-transparent px-2 text-[11px] outline-none"
        >
          <option value="all">All Types</option>
          <option value="ai">AI Matched</option>
          <option value="manual">Manual</option>
        </select>
        <span className="text-[10px] text-muted-foreground">{filtered.length} applicant(s)</span>
      </div>

      <div className="rounded-xl border border-border overflow-hidden">
        <table className="w-full text-xs">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th className="text-left px-3 py-2 font-medium text-muted-foreground w-8"></th>
              <th className="text-left px-3 py-2 font-medium text-muted-foreground">Name</th>
              <th className="text-left px-3 py-2 font-medium text-muted-foreground">Score</th>
              <th className="text-left px-3 py-2 font-medium text-muted-foreground">Type</th>
              <th className="text-left px-3 py-2 font-medium text-muted-foreground">Status</th>
              <th className="text-right px-3 py-2 font-medium text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((m) => (
              <>
                <tr
                  key={m.candidateId}
                  className="border-b border-border hover:bg-muted/20 cursor-pointer"
                  onClick={() => setExpandedId(expandedId === m.candidateId ? null : m.candidateId)}
                >
                  <td className="px-3 py-2">
                    {expandedId === m.candidateId ? <ChevronDown className="size-3" /> : <ChevronRight className="size-3" />}
                  </td>
                  <td className="px-3 py-2">
                    <p className="font-medium text-foreground">{m.candidateName}</p>
                    <p className="text-[10px] text-muted-foreground">{m.candidateEmail}</p>
                  </td>
                  <td className="px-3 py-2">
                    <MatchScoreBadge score={m.matchScore} />
                  </td>
                  <td className="px-3 py-2">
                    <span className={`inline-flex h-5 items-center rounded px-1.5 text-[10px] font-medium ${
                      m.matchType === "MANUAL" ? "bg-blue-50 text-blue-700" : "bg-[#e6f6ca] text-[#63714f]"
                    }`}>
                      {m.matchType}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <CandidateStatusBadge status={"ACTIVE" as any} />
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
                      <button
                        type="button"
                        onClick={() => handleStatusChange(m.candidateId, "INTERVIEW")}
                        disabled={changeStatus.isPending}
                        className="h-6 px-2 rounded border border-[#C5D89D] bg-[#F6F0D7] text-[10px] font-medium text-[#5A7A3A] hover:bg-[#C5D89D] cursor-pointer disabled:opacity-50"
                      >
                        Shortlist
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          if (confirm(`Reject ${m.candidateName}?`)) handleStatusChange(m.candidateId, "REJECTED");
                        }}
                        disabled={changeStatus.isPending}
                        className="h-6 px-2 rounded border border-input text-[10px] font-medium text-muted-foreground hover:bg-destructive hover:text-destructive-foreground cursor-pointer disabled:opacity-50"
                      >
                        Reject
                      </button>
                    </div>
                  </td>
                </tr>
                {expandedId === m.candidateId && (
                  <tr key={`${m.candidateId}-detail`}>
                    <td colSpan={6} className="px-6 py-3 bg-muted/10">
                      <p className="text-[11px] text-muted-foreground italic">AI reasoning loaded on demand from match details modal.</p>
                    </td>
                  </tr>
                )}
              </>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-8 text-center text-xs text-muted-foreground">No applicants found</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
