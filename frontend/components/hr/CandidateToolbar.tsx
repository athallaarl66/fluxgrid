"use client";

import { Search, Upload } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import type { CandidateStatus } from "@/lib/hr-types";

const STATUS_OPTIONS: { label: string; value: CandidateStatus | "" }[] = [
  { label: "All Status", value: "" },
  { label: "Draft", value: "DRAFT" },
  { label: "Parsed", value: "PARSED" },
  { label: "Parse Failed", value: "PARSE_FAILED" },
  { label: "Active", value: "ACTIVE" },
  { label: "Interview", value: "INTERVIEW" },
  { label: "Hired", value: "HIRED" },
  { label: "Rejected", value: "REJECTED" },
  { label: "Archived", value: "ARCHIVED" },
];

interface CandidateToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  statusFilter: string;
  onStatusFilterChange: (value: string) => void;
  onUploadCv: () => void;
}

export function CandidateToolbar({
  search,
  onSearchChange,
  statusFilter,
  onStatusFilterChange,
  onUploadCv,
}: CandidateToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative flex-1 min-w-[200px] max-w-sm">
        <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by name or email..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className="h-8 w-full rounded border-border bg-card pl-8 text-sm"
        />
      </div>

      <select
        value={statusFilter}
        onChange={(e) => onStatusFilterChange(e.target.value)}
        className="h-8 rounded border border-border bg-card px-2 text-xs text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
      >
        {STATUS_OPTIONS.map((opt) => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>

      <Button size="sm" onClick={onUploadCv}>
        <Upload className="size-3.5" />
        Upload CV
      </Button>
    </div>
  );
}
