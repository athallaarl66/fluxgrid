"use client";

import type { CandidateStatus } from "@/lib/hr-types";

const STYLES: Record<CandidateStatus, string> = {
  DRAFT: "border border-[#7A776D] text-[#7A776D]",
  PARSED: "bg-[#C5D89D]/20 text-[#5A7A3A] border border-[#C5D89D]",
  PARSE_FAILED: "bg-orange-50 text-orange-700 border border-orange-200",
  ACTIVE: "bg-[#C5D89D] text-[#2D4A1E] border border-[#A8C47A]",
  INTERVIEW: "bg-blue-50 text-blue-700 border border-blue-200",
  HIRED: "bg-green-700 text-white border border-green-800",
  REJECTED: "bg-[#FFDAD6] text-[#C00100] border border-[#FFDAD6]",
  ARCHIVED: "bg-gray-100 text-gray-500 border border-gray-200",
};

const LABELS: Record<CandidateStatus, string> = {
  DRAFT: "Draft",
  PARSED: "Parsed",
  PARSE_FAILED: "Parse Failed",
  ACTIVE: "Active",
  INTERVIEW: "Interview",
  HIRED: "Hired",
  REJECTED: "Rejected",
  ARCHIVED: "Archived",
};

export function CandidateStatusBadge({ status }: { status: CandidateStatus }) {
  return (
    <span
      className={`inline-flex h-5 items-center rounded-full px-2.5 py-0.5 text-[11px] font-medium leading-none ${STYLES[status] || STYLES.DRAFT}`}
    >
      {LABELS[status] || status}
    </span>
  );
}
