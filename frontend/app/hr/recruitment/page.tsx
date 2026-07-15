"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Users, Briefcase } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useCandidateList } from "@/hooks/useRecruitment";
import { CandidateTable } from "@/components/hr/CandidateTable";
import { CandidateToolbar } from "@/components/hr/CandidateToolbar";
import { UploadCvDialog } from "@/components/hr/UploadCvDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

const SECTIONS = ["Candidates", "Jobs"] as const;

export default function RecruitmentPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [section, setSection] = useState<string>("Candidates");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1);
  const [showUploadDialog, setShowUploadDialog] = useState(false);
  const pageSize = 20;

  const { data, isLoading, error } = useCandidateList({
    search: search || undefined,
    status: statusFilter || undefined,
    page,
    pageSize,
  });

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment");
  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-full max-w-sm" />
        <div className="space-y-2">{Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)}</div>
      </div>
    );
  }
  if (!user) return null;

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Users className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Recruitment</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {section === "Candidates"
              ? data ? `${data.total} candidates` : "Manage candidate CVs"
              : "Manage job postings"}
          </p>
        </div>
      </div>

      <div className="flex gap-0 border-b border-border">
        {SECTIONS.map((s) => (
          <button
            key={s}
            type="button"
            onClick={() => setSection(s)}
            className={cn(
              "h-8 px-4 text-xs font-medium transition-colors cursor-pointer flex items-center gap-1.5",
              section === s
                ? "border-b-2 border-[#9CAB84] text-[#586838]"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            {s === "Candidates" ? <Users className="size-3.5" /> : <Briefcase className="size-3.5" />}
            {s}
          </button>
        ))}
      </div>

      {section === "Candidates" ? (
        <>
          <CandidateToolbar
            search={search}
            onSearchChange={(v) => { setSearch(v); setPage(1); }}
            statusFilter={statusFilter}
            onStatusFilterChange={(v) => { setStatusFilter(v); setPage(1); }}
            onUploadCv={() => setShowUploadDialog(true)}
          />

          {isLoading ? (
            <div className="space-y-2">{Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)}</div>
          ) : error ? (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <p className="text-sm text-destructive font-medium">Failed to load candidates</p>
              <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
            </div>
          ) : data && data.items.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <Users className="size-12 text-muted-foreground/40 mb-3" />
              <p className="text-sm font-medium text-foreground">No candidates yet</p>
              <p className="text-xs text-muted-foreground mt-1">Upload your first CV to get started</p>
            </div>
          ) : data ? (
            <>
              <CandidateTable candidates={data.items} />
              {data.total > data.pageSize && (
                <div className="flex items-center justify-between pt-2">
                  <p className="text-xs text-muted-foreground">
                    Page {data.page} of {Math.ceil(data.total / data.pageSize)} ({data.total} total)
                  </p>
                  <div className="flex items-center gap-1">
                    <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}
                      className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors">Previous</button>
                    <button type="button" disabled={page >= Math.ceil(data.total / data.pageSize)} onClick={() => setPage((p) => p + 1)}
                      className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors">Next</button>
                  </div>
                </div>
              )}
            </>
          ) : null}

          {showUploadDialog && <UploadCvDialog onClose={() => setShowUploadDialog(false)} />}
        </>
      ) : (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Briefcase className="size-12 text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-foreground">Job Postings</p>
          <p className="text-xs text-muted-foreground mt-1 mb-4">Manage and publish job openings</p>
          <button
            type="button"
            onClick={() => router.push("/hr/recruitment/jobs")}
            className="h-8 rounded bg-[#9CAB84] px-4 text-xs font-semibold text-white hover:bg-[#7A8D6A] cursor-pointer transition-colors"
          >
            Go to Job Postings
          </button>
        </div>
      )}
    </div>
  );
}
