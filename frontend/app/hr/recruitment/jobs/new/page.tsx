"use client";

import { useRouter } from "next/navigation";
import { Briefcase } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useCreateJob } from "@/hooks/useRecruitment";
import { JobForm } from "@/components/hr/JobForm";
import { Skeleton } from "@/components/ui/skeleton";
import type { CreateJobRequest, UpdateJobRequest } from "@/lib/hr-types";

export default function NewJobPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const createJob = useCreateJob();

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment/jobs/new");
  if (authLoading) return <div className="p-5"><Skeleton className="h-8 w-48" /></div>;
  if (!user) return null;

  async function handleSubmit(data: CreateJobRequest | UpdateJobRequest) {
    try {
      const job = await createJob.mutateAsync(data as CreateJobRequest);
      router.push(`/hr/recruitment/jobs/${job.id}`);
    } catch {
      /* error handled by form */
    }
  }

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Briefcase className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">New Job Posting</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">Create a new job posting</p>
        </div>
      </div>

      <JobForm onSubmit={handleSubmit} isSubmitting={createJob.isPending} mode="create" />
    </div>
  );
}
