"use client";

import { useEffect, useState } from "react";
import type { JobMatchItem, CandidateDetail, MatchReasoningResponse } from "@/lib/hr-types";
import { useCandidate } from "@/hooks/useRecruitment";
import { useMatchReasoning } from "@/hooks/useRecruitment";
import { MatchScoreBadge } from "@/components/hr/MatchScoreBadge";
import { X } from "lucide-react";

interface CandidateMatchDetailsModalProps {
  jobId: string;
  match: JobMatchItem;
  onClose: () => void;
  onShortlist?: (candidateId: string) => void;
  onReject?: (candidateId: string) => void;
}

export function CandidateMatchDetailsModal({
  jobId,
  match,
  onClose,
  onShortlist,
  onReject,
}: CandidateMatchDetailsModalProps) {
  const { data: candidate, isLoading: loadingCandidate } = useCandidate(match.candidateId);
  const reasoningMutation = useMatchReasoning();
  const [reasoning, setReasoning] = useState<string | null>(null);
  const [loadingReasoning, setLoadingReasoning] = useState(false);

  useEffect(() => {
    if (!reasoning && !loadingReasoning) {
      setLoadingReasoning(true);
      reasoningMutation.mutateAsync({ jobId, candidateId: match.candidateId })
        .then((res) => setReasoning(res.reasoning))
        .catch(() => setReasoning("Unable to generate reasoning at this time."))
        .finally(() => setLoadingReasoning(false));
    }
  }, []);

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="relative w-full max-w-lg rounded border border-border bg-card p-5 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          onClick={onClose}
          className="absolute right-3 top-3 flex size-6 items-center justify-center rounded text-muted-foreground hover:bg-muted cursor-pointer"
        >
          <X className="size-4" />
        </button>

        <div className="flex items-center gap-3 mb-4">
          <MatchScoreBadge score={match.matchScore} />
          <div>
            <h3 className="text-sm font-semibold text-foreground">{match.candidateName}</h3>
            <p className="text-xs text-muted-foreground">{match.candidateEmail}</p>
          </div>
        </div>

        {loadingCandidate ? (
          <div className="space-y-2 py-4">
            <div className="h-3 w-2/3 rounded bg-muted animate-pulse" />
            <div className="h-3 w-1/2 rounded bg-muted animate-pulse" />
          </div>
        ) : candidate ? (
          <div className="space-y-3 mb-4">
            <div>
              <p className="text-xs font-medium text-[#89986D]">Skills</p>
              <div className="flex flex-wrap gap-1 mt-1">
                {candidate.skills.map((s) => (
                  <span key={s.id} className="inline-flex h-5 items-center rounded bg-[#e6f6ca] px-2 text-[10px] font-medium text-[#63714f]">
                    {s.skillName}
                  </span>
                ))}
              </div>
            </div>
            <div>
              <p className="text-xs font-medium text-[#89986D]">Experience</p>
              <p className="text-xs text-foreground mt-0.5">
                {candidate.totalExperienceMonths
                  ? `${Math.floor(candidate.totalExperienceMonths / 12)}y ${candidate.totalExperienceMonths % 12}m`
                  : "—"}
              </p>
            </div>
          </div>
        ) : null}

        <div className="rounded border border-[#C5D89D] bg-gradient-to-r from-[#F6F0D7] to-[#C5D89D] p-3 mb-4">
          <div className="flex items-center gap-1.5 mb-1">
            <svg className="size-3.5 text-[#586838]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
            <span className="text-[11px] font-semibold text-[#586838] uppercase tracking-wider">AI Insight</span>
          </div>
          {loadingReasoning ? (
            <div className="space-y-1">
              <div className="h-2 w-full rounded bg-[#C5D89D]/60 animate-pulse" />
              <div className="h-2 w-3/4 rounded bg-[#C5D89D]/60 animate-pulse" />
            </div>
          ) : (
            <p className="text-xs text-[#586838] leading-relaxed">{reasoning || "No AI reasoning available."}</p>
          )}
        </div>

        <div className="flex items-center gap-2">
          {onShortlist && (
            <button
              type="button"
              onClick={() => { onShortlist(match.candidateId); onClose(); }}
              className="h-8 flex-1 rounded bg-[#9CAB84] text-xs font-semibold text-white hover:bg-[#7A8D6A] cursor-pointer transition-colors"
            >
              Shortlist
            </button>
          )}
          {onReject && (
            <button
              type="button"
              onClick={() => { onReject(match.candidateId); onClose(); }}
              className="h-8 flex-1 rounded border border-[#ffdad6] bg-[#ffdad6]/20 text-xs font-semibold text-[#93000a] hover:bg-[#ffdad6]/40 cursor-pointer transition-colors"
            >
              Reject
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
