"use client";

import { useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { Briefcase, MapPin, DollarSign, Clock } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useJob, useJobMatches, usePublishJob, useCloseJob } from "@/hooks/useRecruitment";
import { JobTabs, type JobTab } from "@/components/hr/JobTabs";
import { MatchRankingTable } from "@/components/hr/MatchRankingTable";
import { AllApplicantsTable } from "@/components/hr/AllApplicantsTable";
import { CandidateMatchDetailsModal } from "@/components/hr/CandidateMatchDetailsModal";
import { MatchScoreBadge } from "@/components/hr/MatchScoreBadge";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/date-utils";

export default function JobDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const { user, loading: authLoading } = useAuth();
  const { data: job, isLoading, error } = useJob(params.id);
  const { data: matchData, isLoading: matchesLoading } = useJobMatches(params.id, 0.5);
  const publishJob = usePublishJob();
  const closeJob = useCloseJob();

  const [activeTab, setActiveTab] = useState<JobTab>("Top AI Matches");
  const [selectedMatch, setSelectedMatch] = useState<{ candidateId: string } | null>(null);

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment/jobs/" + params.id);
  if (authLoading) return <div className="p-5 space-y-4">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-6 w-full" />)}</div>;
  if (!user) return null;
  if (isLoading) return <div className="p-5 space-y-4">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-6 w-full" />)}</div>;
  if (error || !job) return <div className="p-5 text-sm text-destructive">Job not found</div>;

  const matchItem = selectedMatch
    ? matchData?.matches.find((m) => m.candidateId === selectedMatch.candidateId)
    : undefined;

  async function handlePublish() {
    if (!job) return;
    try { await publishJob.mutateAsync(job.id); } catch { /* alert handled by UI */ }
  }

  async function handleClose() {
    if (!job) return;
    try { await closeJob.mutateAsync(job.id); } catch { /* alert handled by UI */ }
  }

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
            <Briefcase className="size-5 text-accent-foreground" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">{job.title}</h1>
            <p className="mt-0.5 flex items-center gap-3 text-xs text-muted-foreground">
              {job.location && <span className="flex items-center gap-1"><MapPin className="size-3" />{job.location}</span>}
              {job.salaryMin !== null && (
                <span className="flex items-center gap-1">
                  <DollarSign className="size-3" />
                  {job.salaryMin?.toLocaleString()}{job.salaryMax ? ` - ${job.salaryMax?.toLocaleString()}` : ""}
                </span>
              )}
              <span className="flex items-center gap-1"><Clock className="size-3" />Created {formatDate(job.createdAt)}</span>
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {job.status === "DRAFT" && (
            <>
              <Button size="sm" onClick={() => router.push(`/hr/recruitment/jobs/${job.id}/edit`)}>Edit</Button>
              <Button size="sm" onClick={handlePublish} disabled={publishJob.isPending}>
                {publishJob.isPending ? "Publishing..." : "Publish"}
              </Button>
            </>
          )}
          {job.status === "PUBLISHED" && (
            <Button size="sm" variant="destructive" onClick={handleClose} disabled={closeJob.isPending}>
              {closeJob.isPending ? "Closing..." : "Close"}
            </Button>
          )}
        </div>
      </div>

      {/* Status line */}
      <div className="flex items-center gap-2">
        <span className="text-xs font-medium text-[#89986D]">Status:</span>
        {job.status === "DRAFT" && <span className="inline-flex h-5 items-center rounded-[10px] bg-muted px-2 text-[11px] font-semibold text-muted-foreground">Draft</span>}
        {job.status === "PUBLISHED" && <MatchScoreBadge score={1} />}
        {job.status === "CLOSED" && <span className="inline-flex h-5 items-center rounded-[10px] bg-[#ffdad6] px-2 text-[11px] font-semibold text-[#93000a]">Closed</span>}
      </div>

      {/* Tabs */}
      <JobTabs active={activeTab} onTabChange={setActiveTab} />

      {/* Tab content */}
      {activeTab === "Job Description" && (
        <div className="space-y-4">
          <div>
            <h3 className="text-xs font-semibold text-[#89986D] uppercase tracking-wider mb-1">Description</h3>
            <p className="text-sm text-foreground whitespace-pre-wrap">{job.description}</p>
          </div>
          {job.requirements && (
            <div>
              <h3 className="text-xs font-semibold text-[#89986D] uppercase tracking-wider mb-1">Requirements</h3>
              <p className="text-sm text-foreground whitespace-pre-wrap">{job.requirements}</p>
            </div>
          )}
          <div>
            <h3 className="text-xs font-semibold text-[#89986D] uppercase tracking-wider mb-1">Required Skills</h3>
            <div className="flex flex-wrap gap-1">
              {job.requiredSkills.length > 0
                ? job.requiredSkills.map((s) => (
                    <span key={s} className="inline-flex h-5 items-center rounded bg-[#e6f6ca] px-2 text-[10px] font-medium text-[#63714f]">{s}</span>
                  ))
                : <span className="text-xs text-muted-foreground">—</span>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4 text-sm">
            {job.minExperienceYears !== null && (
              <div><span className="text-xs font-medium text-[#89986D]">Experience</span><p className="text-foreground">{job.minExperienceYears}{job.maxExperienceYears ? ` - ${job.maxExperienceYears}` : "+"} years</p></div>
            )}
            {job.salaryMin !== null && (
              <div><span className="text-xs font-medium text-[#89986D]">Salary Range</span><p className="text-foreground">{job.salaryMin?.toLocaleString()}{job.salaryMax ? ` - ${job.salaryMax?.toLocaleString()}` : ""}</p></div>
            )}
          </div>
        </div>
      )}

      {activeTab === "Top AI Matches" && (
        <div>
          {matchesLoading ? (
            <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)}</div>
          ) : matchData ? (
            <MatchRankingTable
              matches={matchData.matches}
              onView={(cid) => setSelectedMatch({ candidateId: cid })}
            />
          ) : (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <p className="text-sm font-medium text-foreground">No matches available</p>
              <p className="text-xs text-muted-foreground mt-1">Publish this job to generate matches</p>
            </div>
          )}
        </div>
      )}

      {activeTab === "All Applicants" && (
        <div>
          {matchData ? (
            <AllApplicantsTable matches={matchData.matches} jobId={job.id} />
          ) : (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <p className="text-sm font-medium text-foreground">No applicants yet</p>
              <p className="text-xs text-muted-foreground mt-1">Publish this job and assign candidates to see them here</p>
            </div>
          )}
        </div>
      )}

      {/* Match detail modal */}
      {selectedMatch && matchItem && (
        <CandidateMatchDetailsModal
          jobId={job.id}
          match={matchItem}
          onClose={() => setSelectedMatch(null)}
        />
      )}
    </div>
  );
}
