"use client";

import { cn } from "@/lib/utils";

const TABS = ["Top AI Matches", "Job Description", "All Applicants"] as const;
export type JobTab = (typeof TABS)[number];

interface JobTabsProps {
  active: JobTab;
  onTabChange: (tab: JobTab) => void;
}

export function JobTabs({ active, onTabChange }: JobTabsProps) {
  return (
    <div className="flex border-b border-border">
      {TABS.map((tab) => (
        <button
          key={tab}
          type="button"
          onClick={() => onTabChange(tab)}
          className={cn(
            "h-8 px-4 text-xs font-medium transition-colors cursor-pointer",
            active === tab
              ? "border-b-2 border-[#9CAB84] text-[#586838]"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          {tab}
        </button>
      ))}
    </div>
  );
}
