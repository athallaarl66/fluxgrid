"use client";

import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, Eye, PanelLeftClose, PanelLeft, X } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useCandidateReview, useApproveCandidate, useRejectCandidate, useUpdateCandidate } from "@/hooks/useRecruitment";
import { CandidateReviewTopBar } from "@/components/hr/CandidateReviewTopBar";
import { CandidateReviewForm, type ReviewFormData } from "@/components/hr/CandidateReviewForm";
import { PdfViewerPane } from "@/components/hr/PdfViewerPane";
import { Skeleton } from "@/components/ui/skeleton";

export default function CandidateReviewPage() {
  const params = useParams();
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const id = params.id as string;

  const { data: candidate, isLoading, error } = useCandidateReview(id);
  const approveMutation = useApproveCandidate();
  const rejectMutation = useRejectCandidate();
  const updateMutation = useUpdateCandidate();
  const [pdfCollapsed, setPdfCollapsed] = useState(false);
  const [mobilePdfOpen, setMobilePdfOpen] = useState(false);

  const [formData, setFormData] = useState<ReviewFormData | null>(null);

  useEffect(() => {
    if (candidate && !formData) {
      setFormData({
        name: candidate.name,
        email: candidate.email,
        phone: candidate.phone || "",
        location: candidate.location || "",
        summary: candidate.summary || "",
        education: candidate.education.map((e) => ({ ...e })),
        experience: candidate.experience.map((e) => ({ ...e })),
        skills: candidate.skills.map((s) => s.skillName),
      });
    }
  }, [candidate, formData]);

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/recruitment/" + id + "/review");
  }

  if (authLoading || isLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-12 rounded-xl" />
        <div className="flex gap-4">
          <Skeleton className="h-[70vh] w-1/2 rounded-xl" />
          <Skeleton className="h-[70vh] w-1/2 rounded-xl" />
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

  if (candidate.status !== "PARSED") {
    return (
      <div className="p-5 space-y-4">
        <button type="button" onClick={() => router.push("/hr/recruitment/" + id)} className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer">
          <ArrowLeft className="size-3.5" /> Back to Candidate Detail
        </button>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-muted-foreground">Parsing must complete first before review</p>
          <p className="text-xs text-muted-foreground mt-1">Current status: {candidate.status}</p>
        </div>
      </div>
    );
  }

  async function handleApprove() {
    try {
      if (formData) {
        await updateMutation.mutateAsync({
          id,
          data: {
            name: formData.name,
            email: formData.email,
            phone: formData.phone || undefined,
            location: formData.location || undefined,
            summary: formData.summary || undefined,
            education: formData.education.map((e) => ({
              id: e.id,
              institution: e.institution,
              degree: e.degree,
              fieldOfStudy: e.fieldOfStudy || undefined,
              startDate: e.startDate || undefined,
              endDate: e.endDate || undefined,
              gpa: e.gpa ?? undefined,
            })),
            experience: formData.experience.map((e) => ({
              id: e.id,
              company: e.company,
              role: e.role,
              startDate: e.startDate || undefined,
              endDate: e.endDate || undefined,
              isCurrent: e.isCurrent,
              description: e.description || undefined,
              location: e.location || undefined,
            })),
            skills: formData.skills,
          },
        });
      }
      await approveMutation.mutateAsync({ id });
      router.push("/hr/recruitment/" + id);
    } catch { /* handled by mutation */ }
  }

  async function handleReject() {
    try {
      await rejectMutation.mutateAsync(id);
      router.push("/hr/recruitment/" + id);
    } catch { /* handled by mutation */ }
  }

  return (
    <div className="flex flex-col h-[calc(100vh-var(--header-h,64px))] animate-fade-in">
      <CandidateReviewTopBar
        name={candidate.name}
        status={candidate.status}
        onApprove={handleApprove}
        onReject={handleReject}
        isApproving={approveMutation.isPending || updateMutation.isPending}
        isRejecting={rejectMutation.isPending}
      />

      {/* Mobile: PDF modal trigger */}
      <button
        type="button"
        onClick={() => setMobilePdfOpen(true)}
        className="md:hidden flex items-center justify-center gap-2 py-2 mx-3 my-2 rounded-lg border border-border bg-card text-xs text-muted-foreground hover:text-foreground cursor-pointer"
      >
        <Eye className="size-3.5" /> View Original CV
      </button>

      {/* Desktop: Split layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* PDF pane */}
        <div
          className={`hidden md:flex flex-col border-r border-border bg-card transition-all duration-200 ${
            pdfCollapsed ? "w-0 min-w-0 overflow-hidden border-r-0" : "w-1/2 min-w-[320px]"
          }`}
        >
          <div className="flex items-center justify-between px-3 py-1.5 border-b border-border shrink-0">
            <span className="text-[11px] font-medium text-muted-foreground">Original CV</span>
            <button
              type="button"
              onClick={() => setPdfCollapsed(true)}
              className="size-5 inline-flex items-center justify-center rounded hover:bg-muted cursor-pointer"
              title="Collapse PDF pane"
            >
              <PanelLeftClose className="size-3.5" />
            </button>
          </div>
          <PdfViewerPane fileUrl={candidate.fileUrl} />
        </div>

        {/* Collapsed toggle button */}
        {pdfCollapsed && (
          <button
            type="button"
            onClick={() => setPdfCollapsed(false)}
            className="hidden md:flex items-center justify-center w-6 border-r border-border bg-card hover:bg-muted cursor-pointer shrink-0"
            title="Show PDF pane"
          >
            <PanelLeft className="size-3.5" />
          </button>
        )}

        {/* Form pane */}
        <div className="flex-1 min-w-0">
          {formData && (
            <CandidateReviewForm data={formData} onChange={setFormData} />
          )}
        </div>
      </div>

      {/* Mobile PDF modal */}
      {mobilePdfOpen && (
        <div className="fixed inset-0 z-50 bg-background md:hidden flex flex-col">
          <div className="flex items-center justify-between px-3 py-2 border-b border-border bg-card">
            <span className="text-xs font-medium">Original CV</span>
            <button
              type="button"
              onClick={() => setMobilePdfOpen(false)}
              className="size-6 inline-flex items-center justify-center rounded hover:bg-muted cursor-pointer"
            >
              <X className="size-4" />
            </button>
          </div>
          <div className="flex-1 overflow-hidden">
            <PdfViewerPane fileUrl={candidate.fileUrl} />
          </div>
        </div>
      )}
    </div>
  );
}
