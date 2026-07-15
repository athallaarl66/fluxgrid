"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Briefcase } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useJobList } from "@/hooks/useRecruitment";
import { JobTable } from "@/components/hr/JobTable";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Search } from "lucide-react";

const STATUS_OPTIONS = [
  { label: "All Status", value: "" },
  { label: "Draft", value: "DRAFT" },
  { label: "Published", value: "PUBLISHED" },
  { label: "Closed", value: "CLOSED" },
];

export default function JobsPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading, error } = useJobList({
    search: search || undefined,
    status: statusFilter || undefined,
    page,
    pageSize,
  });

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment/jobs");
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
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
            <Briefcase className="size-5 text-accent-foreground" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Job Postings</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">
              {data ? `${data.total} jobs` : "Manage job postings"}
            </p>
          </div>
        </div>
        <Button size="sm" onClick={() => router.push("/hr/recruitment/jobs/new")}>
          + New Job
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search by title..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="h-8 w-full rounded border-border bg-card pl-8 text-sm"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="h-8 rounded border border-border bg-card px-2 text-xs text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
        >
          {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>

      {isLoading ? (
        <div className="space-y-2">{Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)}</div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load jobs</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : data && data.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Briefcase className="size-12 text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-foreground">No job postings yet</p>
          <p className="text-xs text-muted-foreground mt-1">Create your first job posting to get started</p>
        </div>
      ) : data ? (
        <>
          <JobTable jobs={data.items} />
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
    </div>
  );
}
