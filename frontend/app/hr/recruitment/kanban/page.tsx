"use client";

import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { KanbanBoard } from "@/components/hr/KanbanBoard";

export default function KanbanPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();

  if (!authLoading && !user) router.push("/login?redirect=/hr/recruitment/kanban");
  if (authLoading || !user) return null;

  return (
    <div className="animate-fade-in">
      <div className="flex items-center gap-3 px-5 pt-5 pb-2">
        <button
          type="button"
          onClick={() => router.push("/hr/recruitment")}
          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground cursor-pointer"
        >
          <ArrowLeft className="size-3.5" /> Back
        </button>
        <h1 className="text-lg font-semibold text-foreground">Candidate Pipeline</h1>
      </div>
      <KanbanBoard />
    </div>
  );
}
