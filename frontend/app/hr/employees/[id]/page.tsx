"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEmployee } from "@/hooks/useEmployees";
import { ProfileHeader } from "@/components/hr/ProfileHeader";
import { PersonalInfoTab } from "@/components/hr/PersonalInfoTab";
import { EmploymentTab } from "@/components/hr/EmploymentTab";
import { PayrollTab } from "@/components/hr/PayrollTab";
import { Skeleton } from "@/components/ui/skeleton";

const TABS = [
  { id: "personal", label: "Personal Info" },
  { id: "employment", label: "Employment" },
  { id: "payroll", label: "Payroll Details" },
];

export default function EmployeeProfilePage() {
  const params = useParams();
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [activeTab, setActiveTab] = useState("personal");
  const id = params.id as string;

  const { data: employee, isLoading, error } = useEmployee(id);

  if (!authLoading && !user) {
    router.push(`/login?redirect=/hr/employees/${id}`);
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

  if (error || !employee) {
    return (
      <div className="p-5 space-y-4">
        <button
          type="button"
          onClick={() => router.back()}
          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer"
        >
          <ArrowLeft className="size-3.5" />
          Back
        </button>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Employee not found</p>
          <p className="text-xs text-muted-foreground mt-1">The employee may have been removed</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-5 space-y-5 animate-fade-in">
      <button
        type="button"
        onClick={() => router.push("/hr/employees")}
        className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
      >
        <ArrowLeft className="size-3.5" />
        Back to Directory
      </button>

      <ProfileHeader employee={employee} />

      <div className="border-b border-border">
        <div className="flex gap-0">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors cursor-pointer ${
                activeTab === tab.id
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-5">
        {activeTab === "personal" && <PersonalInfoTab employee={employee} />}
        {activeTab === "employment" && <EmploymentTab employee={employee} />}
        {activeTab === "payroll" && <PayrollTab employee={employee} />}
      </div>
    </div>
  );
}
