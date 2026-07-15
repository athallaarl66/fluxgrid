"use client";

import { useState } from "react";
import type { CreateJobRequest, UpdateJobRequest } from "@/lib/hr-types";

interface JobFormProps {
  initial?: Partial<CreateJobRequest>;
  onSubmit: (data: CreateJobRequest | UpdateJobRequest) => void;
  isSubmitting: boolean;
  mode: "create" | "edit";
}

export function JobForm({ initial, onSubmit, isSubmitting, mode }: JobFormProps) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [requirements, setRequirements] = useState(initial?.requirements ?? "");
  const [skillsText, setSkillsText] = useState(initial?.requiredSkills?.join(", ") ?? "");
  const [minExp, setMinExp] = useState(initial?.minExperienceYears?.toString() ?? "");
  const [maxExp, setMaxExp] = useState(initial?.maxExperienceYears?.toString() ?? "");
  const [location, setLocation] = useState(initial?.location ?? "");
  const [salaryMin, setSalaryMin] = useState(initial?.salaryMin?.toString() ?? "");
  const [salaryMax, setSalaryMax] = useState(initial?.salaryMax?.toString() ?? "");
  const [errors, setErrors] = useState<string[]>([]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs: string[] = [];
    if (!title.trim()) errs.push("Title is required");
    if (title.length > 255) errs.push("Title must be 255 characters or less");
    if (!description.trim()) errs.push("Description is required");
    const min = minExp ? Number(minExp) : undefined;
    const max = maxExp ? Number(maxExp) : undefined;
    if (min !== undefined && max !== undefined && min > max) errs.push("Min experience cannot exceed max experience");
    const smin = salaryMin ? Number(salaryMin) : undefined;
    const smax = salaryMax ? Number(salaryMax) : undefined;
    if (smin !== undefined && smax !== undefined && smin > smax) errs.push("Min salary cannot exceed max salary");
    if (errs.length > 0) { setErrors(errs); return; }
    setErrors([]);

    const skills = skillsText
      .split(",")
      .map((s) => s.trim())
      .filter(Boolean);

    const data: Record<string, unknown> = {
      title: title.trim(),
      description: description.trim(),
      requirements: requirements.trim() || undefined,
      requiredSkills: skills.length > 0 ? skills : undefined,
      minExperienceYears: min,
      maxExperienceYears: max,
      location: location.trim() || undefined,
      salaryMin: smin,
      salaryMax: smax,
    };

    if (mode === "edit") {
      Object.keys(data).forEach((key) => {
        if (data[key] === undefined) delete data[key];
      });
    }

    onSubmit(data as CreateJobRequest | UpdateJobRequest);
  }

  const labelClass = "text-xs font-medium text-[#89986D]";
  const inputClass =
    "h-8 w-full rounded border border-border bg-card px-2 text-sm text-foreground focus:border-ring focus:ring-1 focus:ring-ring";

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-2xl">
      {errors.length > 0 && (
        <div className="rounded border border-[#ffdad6] bg-[#ffdad6]/20 px-3 py-2 text-xs text-[#93000a]">
          {errors.map((e, i) => <p key={i}>{e}</p>)}
        </div>
      )}

      <div className="space-y-1.5">
        <label className={labelClass}>Title *</label>
        <input className={inputClass} value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Senior Frontend Engineer" />
      </div>

      <div className="space-y-1.5">
        <label className={labelClass}>Description *</label>
        <textarea
          className={`${inputClass} h-24 resize-y pt-1.5`}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Job description..."
        />
      </div>

      <div className="space-y-1.5">
        <label className={labelClass}>Requirements</label>
        <textarea
          className={`${inputClass} h-20 resize-y pt-1.5`}
          value={requirements}
          onChange={(e) => setRequirements(e.target.value)}
          placeholder="Requirements and qualifications..."
        />
      </div>

      <div className="space-y-1.5">
        <label className={labelClass}>Required Skills (comma-separated)</label>
        <input className={inputClass} value={skillsText} onChange={(e) => setSkillsText(e.target.value)} placeholder="React, TypeScript, .NET" />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className={labelClass}>Min Experience (years)</label>
          <input type="number" min="0" className={inputClass} value={minExp} onChange={(e) => setMinExp(e.target.value)} />
        </div>
        <div className="space-y-1.5">
          <label className={labelClass}>Max Experience (years)</label>
          <input type="number" min="0" className={inputClass} value={maxExp} onChange={(e) => setMaxExp(e.target.value)} />
        </div>
      </div>

      <div className="space-y-1.5">
        <label className={labelClass}>Location</label>
        <input className={inputClass} value={location} onChange={(e) => setLocation(e.target.value)} placeholder="e.g. Jakarta, Remote" />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className={labelClass}>Salary Min</label>
          <input type="number" min="0" className={inputClass} value={salaryMin} onChange={(e) => setSalaryMin(e.target.value)} />
        </div>
        <div className="space-y-1.5">
          <label className={labelClass}>Salary Max</label>
          <input type="number" min="0" className={inputClass} value={salaryMax} onChange={(e) => setSalaryMax(e.target.value)} />
        </div>
      </div>

      <div className="flex items-center gap-3 pt-2">
        <button
          type="submit"
          disabled={isSubmitting}
          className="h-8 rounded bg-[#9CAB84] px-4 text-xs font-semibold text-white hover:bg-[#7A8D6A] disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed transition-colors"
        >
          {isSubmitting ? "Saving..." : mode === "create" ? "Create Job" : "Update Job"}
        </button>
      </div>
    </form>
  );
}
