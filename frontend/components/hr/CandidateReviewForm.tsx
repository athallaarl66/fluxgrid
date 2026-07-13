"use client";

import type { CandidateDetail, CandidateEducation, CandidateExperience } from "@/lib/hr-types";
import { SkillsInput } from "@/components/hr/SkillsInput";
import { Plus, Trash2 } from "lucide-react";

interface ReviewFormData {
  name: string;
  email: string;
  phone: string;
  location: string;
  summary: string;
  education: CandidateEducation[];
  experience: CandidateExperience[];
  skills: string[];
}

export function CandidateReviewForm({
  data,
  onChange,
}: {
  data: ReviewFormData;
  onChange: (data: ReviewFormData) => void;
}) {
  function updateField<K extends keyof ReviewFormData>(key: K, value: ReviewFormData[K]) {
    onChange({ ...data, [key]: value });
  }

  function updateEducation(index: number, field: keyof CandidateEducation, value: unknown) {
    const edu = [...data.education];
    edu[index] = { ...edu[index], [field]: value };
    updateField("education", edu);
  }

  function addEducation() {
    updateField("education", [
      ...data.education,
      { id: crypto.randomUUID(), institution: "", degree: "", fieldOfStudy: null, startDate: null, endDate: null, gpa: null },
    ]);
  }

  function removeEducation(index: number) {
    updateField("education", data.education.filter((_, i) => i !== index));
  }

  function updateExperience(index: number, field: keyof CandidateExperience, value: unknown) {
    const exp = [...data.experience];
    exp[index] = { ...exp[index], [field]: value };
    updateField("experience", exp);
  }

  function addExperience() {
    updateField("experience", [
      ...data.experience,
      { id: crypto.randomUUID(), company: "", role: "", startDate: null, endDate: null, isCurrent: false, description: null, location: null },
    ]);
  }

  function removeExperience(index: number) {
    updateField("experience", data.experience.filter((_, i) => i !== index));
  }

  function Input({ label, value, onChange: onValChange, placeholder }: { label: string; value: string; onChange: (v: string) => void; placeholder?: string }) {
    return (
      <div>
        <p className="text-[10px] text-muted-foreground mb-0.5">{label}</p>
        <input
          type="text"
          value={value}
          onChange={(e) => onValChange(e.target.value)}
          placeholder={placeholder}
          className="w-full h-7 rounded-md border border-input bg-transparent px-2 text-xs outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
      </div>
    );
  }

  return (
    <div className="space-y-5 p-5 overflow-y-auto h-full">
      {/* Personal */}
      <section>
        <div className="flex items-center gap-2 mb-3">
          <h2 className="text-sm font-semibold text-foreground">Personal Information</h2>
          <span className="text-[10px] text-muted-foreground italic">[Extracted]</span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Input label="Full Name" value={data.name} onChange={(v) => updateField("name", v)} />
          <Input label="Email" value={data.email} onChange={(v) => updateField("email", v)} />
          <Input label="Phone" value={data.phone} onChange={(v) => updateField("phone", v)} />
          <Input label="Location" value={data.location} onChange={(v) => updateField("location", v)} />
        </div>
        <div className="mt-3">
          <p className="text-[10px] text-muted-foreground mb-0.5">Summary</p>
          <textarea
            value={data.summary}
            onChange={(e) => updateField("summary", e.target.value)}
            rows={3}
            className="w-full rounded-md border border-input bg-transparent px-2 py-1 text-xs outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
          />
        </div>
      </section>

      {/* Education */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-semibold text-foreground">Education</h2>
            <span className="text-[10px] text-muted-foreground italic">[Extracted]</span>
          </div>
          <button type="button" onClick={addEducation} className="flex items-center gap-1 text-[11px] text-[#9CAB84] hover:text-[#7A8D6A] cursor-pointer">
            <Plus className="size-3" /> Add
          </button>
        </div>
        <div className="space-y-3">
          {data.education.map((edu, i) => (
            <div key={edu.id || i} className="rounded-lg border border-border p-3 space-y-2 relative">
              <button type="button" onClick={() => removeEducation(i)} className="absolute top-2 right-2 size-5 inline-flex items-center justify-center rounded hover:bg-muted text-muted-foreground hover:text-destructive cursor-pointer">
                <Trash2 className="size-3" />
              </button>
              <div className="grid grid-cols-2 gap-2 pr-6">
                <Input label="Institution" value={edu.institution} onChange={(v) => updateEducation(i, "institution", v)} />
                <Input label="Degree" value={edu.degree} onChange={(v) => updateEducation(i, "degree", v)} />
                <Input label="Field of Study" value={edu.fieldOfStudy || ""} onChange={(v) => updateEducation(i, "fieldOfStudy", v)} />
                <Input label="GPA" value={edu.gpa?.toString() || ""} onChange={(v) => updateEducation(i, "gpa", v ? parseFloat(v) : null)} />
              </div>
            </div>
          ))}
          {data.education.length === 0 && (
            <p className="text-[11px] text-muted-foreground italic">No education entries</p>
          )}
        </div>
      </section>

      {/* Experience */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-semibold text-foreground">Experience</h2>
            <span className="text-[10px] text-muted-foreground italic">[Extracted]</span>
          </div>
          <button type="button" onClick={addExperience} className="flex items-center gap-1 text-[11px] text-[#9CAB84] hover:text-[#7A8D6A] cursor-pointer">
            <Plus className="size-3" /> Add
          </button>
        </div>
        <div className="space-y-3">
          {data.experience.map((exp, i) => (
            <div key={exp.id || i} className="rounded-lg border border-border p-3 space-y-2 relative">
              <button type="button" onClick={() => removeExperience(i)} className="absolute top-2 right-2 size-5 inline-flex items-center justify-center rounded hover:bg-muted text-muted-foreground hover:text-destructive cursor-pointer">
                <Trash2 className="size-3" />
              </button>
              <div className="grid grid-cols-2 gap-2 pr-6">
                <Input label="Company" value={exp.company} onChange={(v) => updateExperience(i, "company", v)} />
                <Input label="Role" value={exp.role} onChange={(v) => updateExperience(i, "role", v)} />
                <Input label="Location" value={exp.location || ""} onChange={(v) => updateExperience(i, "location", v)} />
                <label className="flex items-center gap-1.5 text-xs text-muted-foreground pt-4">
                  <input type="checkbox" checked={exp.isCurrent} onChange={(e) => updateExperience(i, "isCurrent", e.target.checked)} className="size-3" />
                  Current position
                </label>
              </div>
              <div>
                <p className="text-[10px] text-muted-foreground mb-0.5">Description</p>
                <textarea
                  value={exp.description || ""}
                  onChange={(e) => updateExperience(i, "description", e.target.value)}
                  rows={2}
                  className="w-full rounded-md border border-input bg-transparent px-2 py-1 text-xs outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
                />
              </div>
            </div>
          ))}
          {data.experience.length === 0 && (
            <p className="text-[11px] text-muted-foreground italic">No experience entries</p>
          )}
        </div>
      </section>

      {/* Skills */}
      <section>
        <div className="flex items-center gap-2 mb-3">
          <h2 className="text-sm font-semibold text-foreground">Skills</h2>
          <span className="text-[10px] text-muted-foreground italic">[Extracted]</span>
        </div>
        <SkillsInput skills={data.skills} onChange={(v) => updateField("skills", v)} />
      </section>
    </div>
  );
}

export type { ReviewFormData };
