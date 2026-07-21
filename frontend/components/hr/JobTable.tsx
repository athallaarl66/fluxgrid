"use client";

import type { JobPosting, JobPostingStatus } from "@/lib/hr-types";
import { useRouter } from "next/navigation";

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

const STATUS_META: Record<JobPostingStatus, { label: string; bg: string; text: string }> = {
  DRAFT: { label: "Draft", bg: "bg-muted", text: "text-muted-foreground" },
  PUBLISHED: { label: "Published", bg: "bg-[#d4e7ab]", text: "text-[#586838]" },
  CLOSED: { label: "Closed", bg: "bg-[#ffdad6]", text: "text-[#93000a]" },
};

export function JobTable({ jobs }: { jobs: JobPosting[] }) {
  const router = useRouter();

  if (jobs.length === 0) return null;

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Title</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Location</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Status</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Created</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {jobs.map((j) => {
            const meta = STATUS_META[j.status];
            return (
              <tr
                key={j.id}
                className="border-b border-border hover:bg-muted/40 cursor-pointer"
                onClick={() => router.push(`/hr/recruitment/jobs/${j.id}`)}
                onKeyDown={(e) => e.key === "Enter" && router.push(`/hr/recruitment/jobs/${j.id}`)}
                tabIndex={0}
              >
                <td className="h-9 px-2 text-xs text-foreground font-medium">{j.title}</td>
                <td className="h-9 px-2 text-xs text-muted-foreground">{j.location || "—"}</td>
                <td className="h-9 px-2">
                  <span className={`inline-flex h-5 items-center rounded-[10px] px-2 text-[11px] font-semibold ${meta.bg} ${meta.text}`}>
                    {meta.label}
                  </span>
                </td>
                <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">
                  {formatDate(j.createdAt)}
                </td>
                <td className="h-9 px-2 space-x-2">
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      router.push(`/hr/recruitment/jobs/${j.id}`);
                    }}
                    className="text-xs text-[#9CAB84] hover:text-[#7A8D6A] underline underline-offset-2 cursor-pointer"
                  >
                    View
                  </button>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      router.push(`/hr/recruitment/jobs/${j.id}/edit`);
                    }}
                    className="text-xs text-[#5A7A3A] hover:text-[#2D4A1E] underline underline-offset-2 cursor-pointer font-medium"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
