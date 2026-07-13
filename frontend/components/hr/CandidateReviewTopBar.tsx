"use client";

import { Loader2, Check, X } from "lucide-react";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import type { CandidateStatus } from "@/lib/hr-types";

export function CandidateReviewTopBar({
  name,
  status,
  onApprove,
  onReject,
  isApproving,
  isRejecting,
}: {
  name: string;
  status: CandidateStatus;
  onApprove: () => void;
  onReject: () => void;
  isApproving: boolean;
  isRejecting: boolean;
}) {
  return (
    <div className="flex items-center justify-between px-5 py-3 border-b border-border bg-card shrink-0">
      <div className="flex items-center gap-3">
        <h1 className="text-base font-semibold text-foreground">{name}</h1>
        <CandidateStatusBadge status={status} />
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={onReject}
          disabled={isRejecting}
          className="inline-flex items-center gap-1.5 h-8 rounded-lg border border-input px-3 text-xs font-medium text-muted-foreground hover:bg-destructive hover:text-destructive-foreground hover:border-destructive disabled:opacity-50 cursor-pointer transition-colors"
          title="Reject this candidate"
        >
          {isRejecting ? <Loader2 className="size-3.5 animate-spin" /> : <X className="size-3.5" />}
          Reject
        </button>
        <button
          type="button"
          onClick={onApprove}
          disabled={isApproving}
          className="inline-flex items-center gap-1.5 h-8 rounded-lg bg-[#C5D89D] px-3 text-xs font-medium text-[#2D4A1E] hover:bg-[#A8C47A] disabled:opacity-50 cursor-pointer transition-colors"
          title="Data was extracted via AI — review before approving"
        >
          {isApproving ? <Loader2 className="size-3.5 animate-spin" /> : <Check className="size-3.5" />}
          Approve Data
        </button>
      </div>
    </div>
  );
}
