"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Skeleton } from "@/components/ui/skeleton";
import { ScrollText } from "lucide-react";
import { JournalEntryDashboard } from "@/components/finance/JournalEntryDashboard";
import { JournalEntryFormModal } from "@/components/finance/JournalEntryFormModal";
import type { JournalEntry } from "@/lib/journal-entry-types";

export default function JournalEntriesPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [formOpen, setFormOpen] = useState(false);
  const [editEntry, setEditEntry] = useState<JournalEntry | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/finance/journal-entries");
    }
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 rounded-lg" />
      </div>
    );
  }

  if (!user) return null;

  const handleNew = () => {
    setEditEntry(null);
    setFormOpen(true);
  };

  const handleEdit = (entry: JournalEntry) => {
    setEditEntry(entry);
    setFormOpen(true);
  };

  const handleView = (entry: JournalEntry) => {
    setEditEntry(entry);
    setFormOpen(true);
  };

  const handleSuccess = () => {
    setRefreshKey((k) => k + 1);
  };

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      {/* Page header */}
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <ScrollText className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Journal Entries
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            General ledger journal entries with double-entry bookkeeping
          </p>
        </div>
      </div>

      {/* Dashboard */}
      <JournalEntryDashboard
        onNew={handleNew}
        onEdit={handleEdit}
        onView={handleView}
        refreshKey={refreshKey}
      />

      {/* Form Modal */}
      <JournalEntryFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSuccess={handleSuccess}
        editEntry={editEntry}
      />
    </div>
  );
}
