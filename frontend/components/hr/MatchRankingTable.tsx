"use client";

import type { JobMatchItem } from "@/lib/hr-types";
import { MatchScoreBadge } from "@/components/hr/MatchScoreBadge";

interface MatchRankingTableProps {
  matches: JobMatchItem[];
  onView: (candidateId: string) => void;
  onShortlist?: (candidateId: string) => void;
}

export function MatchRankingTable({ matches, onView, onShortlist }: MatchRankingTableProps) {
  if (matches.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <p className="text-sm font-medium text-foreground">No strong matches found in the current pool.</p>
        <p className="text-xs text-muted-foreground mt-1">Try lowering the threshold or uploading more CVs.</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Score</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Candidate</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Skills</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {matches.map((m) => (
            <tr
              key={m.candidateId}
              className="border-b border-border hover:bg-muted/40"
            >
              <td className="h-9 px-2">
                <MatchScoreBadge score={m.matchScore} />
              </td>
              <td className="h-9 px-2 text-xs text-foreground font-medium">{m.candidateName}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground max-w-[200px] truncate">
                {m.skills || "—"}
              </td>
              <td className="h-9 px-2 space-x-2">
                <button
                  type="button"
                  onClick={() => onView(m.candidateId)}
                  className="text-xs text-[#9CAB84] hover:text-[#7A8D6A] underline underline-offset-2 cursor-pointer"
                >
                  View
                </button>
                {onShortlist && (
                  <button
                    type="button"
                    onClick={() => onShortlist(m.candidateId)}
                    className="text-xs text-[#5A7A3A] hover:text-[#2D4A1E] underline underline-offset-2 cursor-pointer font-medium"
                  >
                    Shortlist
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
