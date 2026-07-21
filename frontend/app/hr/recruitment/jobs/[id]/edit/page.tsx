"use client";

import { useRouter, useParams } from "next/navigation";
import { Briefcase } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useJob, useUpdateJob } from "@/hooks/useRecruitment";
import { JobForm } from "@/components/hr/JobForm";
import { Skeleton } from "@/components/ui/skeleton";
import type { UpdateJobRequest } from "@/lib/hr-types";

export default function EditJobPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const { user, loading: authLoading } = useAuth();
  const { data: job, isLoading } = useJob(params.id);
  const updateJob = useUpdateJob();

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment/jobs/" + params.id + "/edit");
  if (authLoading) return <div className="p-5"><Skeleton className="h-8 w-48" /></div>;
  if (!user) return null;
  if (isLoading) return <div className="p-5 space-y-4">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-6 w-full" />)}</div>;
  if (!job) return <div className="p-5 text-sm text-destructive">Job not found</div>;

  async function handleSubmit(data: UpdateJobRequest) {
    await updateJob.mutateAsync({ id: job.id, data });
    router.push(`/hr/recruitment/jobs/${job.id}`);
  }

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Briefcase className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Edit Job Posting</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">{job.title}</p>
        </div>
      </div>

      <JobForm
        initial={{
          title: job.title,
          description: job.description,
          requirements: job.requirements ?? undefined,
          requiredSkills: job.requiredSkills,
          minExperienceYears: job.minExperienceYears ?? undefined,
          maxExperienceYears: job.maxExperienceYears ?? undefined,
          location: job.location ?? undefined,
          salaryMin: job.salaryMin ?? undefined,
          salaryMax: job.salaryMax ?? undefined,
        }}
        onSubmit={handleSubmit}
        isSubmitting={updateJob.isPending}
        mode="edit"
      />
    </div>
  );
}
