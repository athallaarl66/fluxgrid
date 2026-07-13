"use client";

import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, ExternalLink, Globe, Mail, MapPin, GraduationCap, Briefcase, Award, FileText, Phone } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useCandidate, useDeleteCandidate } from "@/hooks/useRecruitment";
import { CandidateStatusBadge } from "@/components/hr/CandidateStatusBadge";
import { Skeleton } from "@/components/ui/skeleton";

function formatDate(dateStr: string | null) {
  if (!dateStr) return "\u2014";
  return new Date(dateStr).toLocaleDateString("id-ID", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

function formatSalary(min: number | null, max: number | null) {
  const fmt = (v: number) => new Intl.NumberFormat("id-ID").format(v);
  if (max === null && min !== null) return "\u2265 Rp " + fmt(min);
  if (min === null && max !== null) return "\u2264 Rp " + fmt(max);
  if (min !== null && max !== null) return "Rp " + fmt(min) + " \u2013 Rp " + fmt(max);
  return "\u2014";
}

function InfoRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: string | null }) {
  if (!value) return null;
  return (
    <div className="flex items-start gap-2.5">
      <span className="mt-0.5 shrink-0 text-muted-foreground">{icon}</span>
      <div>
        <p className="text-[11px] text-muted-foreground">{label}</p>
        <p className="text-sm text-foreground">{value}</p>
      </div>
    </div>
  );
}

export default function CandidateDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const id = params.id as string;

  const { data: candidate, isLoading, error } = useCandidate(id);
  const deleteMutation = useDeleteCandidate();

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/recruitment/" + id);
  }

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-20 rounded-xl" />
        <div className="space-y-3">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-32" />
        </div>
      </div>
    );
  }

  if (!user) return null;

  if (error || !candidate) {
    return (
      <div className="p-5 space-y-4">
        <button type="button" onClick={() => router.back()} className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer">
          <ArrowLeft className="size-3.5" /> Back
        </button>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Candidate not found</p>
          <p className="text-xs text-muted-foreground mt-1">The candidate may have been removed</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-5 animate-fade-in">
      <button type="button" onClick={() => router.push("/hr/recruitment")} className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors">
        <ArrowLeft className="size-3.5" /> Back to Recruitment
      </button>

      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">{candidate.name}</h1>
            <CandidateStatusBadge status={candidate.status} />
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{candidate.email}</p>
        </div>
        <button
          type="button"
          onClick={async () => {
            if (!confirm("Delete this candidate and its CV file?")) return;
            try {
              await deleteMutation.mutateAsync(id);
              router.push("/hr/recruitment");
            } catch {}
          }}
          disabled={deleteMutation.isPending}
          className="h-8 rounded-lg border border-input px-3 text-xs text-muted-foreground hover:text-destructive hover:border-destructive disabled:opacity-50 cursor-pointer transition-colors"
        >
          {deleteMutation.isPending ? "Deleting..." : "Delete"}
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2 space-y-5">
          <div className="rounded-xl border border-border bg-card p-5">
            <h2 className="text-sm font-semibold text-foreground mb-4">Personal Information</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <InfoRow icon={<Mail className="size-4" />} label="Email" value={candidate.email} />
              <InfoRow icon={<Phone className="size-4" />} label="Phone" value={candidate.phone} />
              <InfoRow icon={<MapPin className="size-4" />} label="Location" value={candidate.location} />
              <InfoRow icon={<Globe className="size-4" />} label="LinkedIn" value={candidate.linkedInUrl} />
              <InfoRow icon={<Globe className="size-4" />} label="GitHub" value={candidate.gitHubUrl} />
              <InfoRow icon={<Globe className="size-4" />} label="Portfolio" value={candidate.portfolioUrl} />
            </div>
            {candidate.summary && (
              <div className="mt-4 pt-4 border-t border-border">
                <p className="text-[11px] text-muted-foreground mb-1">Summary</p>
                <p className="text-sm text-foreground">{candidate.summary}</p>
              </div>
            )}
          </div>

          {(candidate.totalExperienceMonths !== null || candidate.expectedSalaryMin !== null || candidate.expectedSalaryMax !== null || candidate.noticePeriodDays !== null) && (
            <div className="rounded-xl border border-border bg-card p-5">
              <h2 className="text-sm font-semibold text-foreground mb-4">Professional Details</h2>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                {candidate.totalExperienceMonths !== null && (
                  <div>
                    <p className="text-[11px] text-muted-foreground">Total Experience</p>
                    <p className="text-sm text-foreground font-medium">{candidate.totalExperienceMonths} months</p>
                  </div>
                )}
                {(candidate.expectedSalaryMin !== null || candidate.expectedSalaryMax !== null) && (
                  <div>
                    <p className="text-[11px] text-muted-foreground">Expected Salary</p>
                    <p className="text-sm text-foreground font-medium">{formatSalary(candidate.expectedSalaryMin, candidate.expectedSalaryMax)}</p>
                  </div>
                )}
                {candidate.noticePeriodDays !== null && (
                  <div>
                    <p className="text-[11px] text-muted-foreground">Notice Period</p>
                    <p className="text-sm text-foreground font-medium">{candidate.noticePeriodDays} days</p>
                  </div>
                )}
              </div>
            </div>
          )}

          {candidate.education.length > 0 && (
            <div className="rounded-xl border border-border bg-card p-5">
              <div className="flex items-center gap-2 mb-4">
                <GraduationCap className="size-4 text-muted-foreground" />
                <h2 className="text-sm font-semibold text-foreground">Education</h2>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border">
                      <th className="h-7 px-2 text-left text-[11px] font-semibold text-muted-foreground">Institution</th>
                      <th className="h-7 px-2 text-left text-[11px] font-semibold text-muted-foreground">Degree</th>
                      <th className="h-7 px-2 text-left text-[11px] font-semibold text-muted-foreground">Field</th>
                      <th className="h-7 px-2 text-left text-[11px] font-semibold text-muted-foreground">Period</th>
                      <th className="h-7 px-2 text-left text-[11px] font-semibold text-muted-foreground">GPA</th>
                    </tr>
                  </thead>
                  <tbody>
                    {candidate.education.map((edu) => (
                      <tr key={edu.id} className="border-b border-border last:border-0">
                        <td className="h-8 px-2 text-xs text-foreground">{edu.institution}</td>
                        <td className="h-8 px-2 text-xs text-foreground">{edu.degree}</td>
                        <td className="h-8 px-2 text-xs text-muted-foreground">{edu.fieldOfStudy || "\u2014"}</td>
                        <td className="h-8 px-2 text-xs text-muted-foreground tabular-nums">{formatDate(edu.startDate)} \u2013 {formatDate(edu.endDate)}</td>
                        <td className="h-8 px-2 text-xs text-muted-foreground tabular-nums">{edu.gpa ?? "\u2014"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {candidate.experience.length > 0 && (
            <div className="rounded-xl border border-border bg-card p-5">
              <div className="flex items-center gap-2 mb-4">
                <Briefcase className="size-4 text-muted-foreground" />
                <h2 className="text-sm font-semibold text-foreground">Experience</h2>
              </div>
              <div className="space-y-4">
                {candidate.experience.map((exp) => (
                  <div key={exp.id} className="relative pl-4 border-l-2 border-[#C5D89D]">
                    <div className="absolute left-[-5px] top-1 size-2 rounded-full bg-[#C5D89D]" />
                    <div>
                      <p className="text-sm font-medium text-foreground">{exp.role}</p>
                      <p className="text-xs text-muted-foreground">{exp.company}{exp.location ? " \u2014 " + exp.location : ""}</p>
                      <p className="text-[11px] text-muted-foreground tabular-nums mt-0.5">{formatDate(exp.startDate)} \u2013 {exp.isCurrent ? "Present" : formatDate(exp.endDate)}</p>
                      {exp.description && <p className="text-xs text-muted-foreground mt-1">{exp.description}</p>}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {candidate.skills.length > 0 && (
            <div className="rounded-xl border border-border bg-card p-5">
              <div className="flex items-center gap-2 mb-4">
                <Award className="size-4 text-muted-foreground" />
                <h2 className="text-sm font-semibold text-foreground">Skills</h2>
              </div>
              <div className="flex flex-wrap gap-2">
                {candidate.skills.map((skill) => (
                  <span key={skill.id} className="inline-flex items-center rounded-full border border-[#C5D89D] bg-[#F6F0D7] px-2.5 py-0.5 text-[11px] text-foreground">
                    {skill.skillName}
                    {skill.proficiencyLevel && <span className="ml-1 text-[10px] text-muted-foreground">({skill.proficiencyLevel})</span>}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="space-y-5">
          {candidate.documents.length > 0 && (
            <div className="rounded-xl border border-border bg-card p-5">
              <div className="flex items-center gap-2 mb-4">
                <FileText className="size-4 text-muted-foreground" />
                <h2 className="text-sm font-semibold text-foreground">Documents</h2>
              </div>
              <div className="space-y-2">
                {candidate.documents.map((doc) => (
                  <div key={doc.id} className="flex items-center justify-between rounded-lg border border-border p-2.5">
                    <div className="min-w-0">
                      <p className="text-xs text-foreground font-medium truncate">{doc.fileName}</p>
                      <p className="text-[11px] text-muted-foreground tabular-nums">{doc.fileType?.toUpperCase() || "\u2014"} {doc.fileSizeBytes ? " \u2022 " + (doc.fileSizeBytes / 1024).toFixed(0) + " KB" : ""}</p>
                    </div>
                    <button type="button" onClick={() => window.open("/api/v1/hr/recruitment/candidates/" + candidate.id + "/documents/" + doc.id + "/download", "_blank")} className="flex items-center gap-1 text-xs text-[#9CAB84] hover:text-[#7A8D6A] cursor-pointer">
                      <ExternalLink className="size-3" /> View
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {candidate.fileUrl && (
            <div className="rounded-xl border border-border bg-card p-5">
              <h2 className="text-sm font-semibold text-foreground mb-3">Uploaded CV</h2>
              <div className="flex items-center justify-between rounded-lg border border-border p-2.5">
                <div className="min-w-0">
                  <p className="text-xs text-foreground font-medium truncate">{candidate.originalFilename || "CV"}</p>
                  <p className="text-[11px] text-muted-foreground tabular-nums">{candidate.fileType?.toUpperCase() || ""}{candidate.fileSizeBytes ? " \u2022 " + (candidate.fileSizeBytes / 1024).toFixed(0) + " KB" : ""}</p>
                </div>
                <button type="button" onClick={() => window.open(candidate.fileUrl!, "_blank")} className="flex items-center gap-1 text-xs text-[#9CAB84] hover:text-[#7A8D6A] cursor-pointer">
                  <ExternalLink className="size-3" /> View CV
                </button>
              </div>
            </div>
          )}

          {candidate.status === "PARSED" && (
            <div className="rounded-xl border border-[#C5D89D] bg-[#F6F0D7] p-4">
              <p className="text-xs font-medium text-[#5A7A3A]">AI Extraction Ready</p>
              <p className="text-[11px] text-[#7A8D6A] mt-1">Parsed data is ready for review.</p>
              <button
                type="button"
                onClick={() => router.push(`/hr/recruitment/${candidate.id}/review`)}
                className="mt-3 inline-flex items-center gap-1 h-7 rounded-lg bg-[#C5D89D] px-3 text-[11px] font-medium text-[#2D4A1E] hover:bg-[#A8C47A] cursor-pointer transition-colors"
              >
                Review & Approve
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
