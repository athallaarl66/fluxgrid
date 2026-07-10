"use client";

import type { CandidateListItem } from "@/lib/hr-types";
import { useRouter } from "next/navigation";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

export function CandidateTable({ candidates }: { candidates: CandidateListItem[] }) {
  const router = useRouter();

  if (candidates.length === 0) return null;

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Name</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Email</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Status</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Upload Date</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">File Type</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {candidates.map((c) => (
            <tr
              key={c.id}
              className="border-b border-border hover:bg-muted/40 cursor-pointer"
              onClick={() => router.push(`/hr/recruitment/${c.id}`)}
              onKeyDown={(e) => e.key === "Enter" && router.push(`/hr/recruitment/${c.id}`)}
              tabIndex={0}
            >
              <td className="h-9 px-2 text-xs text-foreground font-medium">{c.name}</td>
              <td className="h-9 px-2 text-xs text-muted-foreground">{c.email}</td>
              <td className="h-9 px-2">
                <CandidateStatusBadge status={c.status} />
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">
                {formatDate(c.createdAt)}
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground uppercase">
                {c.fileType || "—"}
              </td>
              <td className="h-9 px-2">
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    router.push(`/hr/recruitment/${c.id}`);
                  }}
                  className="text-xs text-[#9CAB84] hover:text-[#7A8D6A] underline underline-offset-2 cursor-pointer"
                >
                  View
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
