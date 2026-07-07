"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Skeleton } from "@/components/ui/skeleton";
import { Wallet, BookOpen, ScrollText, CalendarCheck, BarChart3, ArrowRight } from "lucide-react";

const modules = [
  {
    name: "Chart of Accounts",
    href: "/finance/chart-of-accounts",
    icon: BookOpen,
    description: "Manage account codes, hierarchy, and types",
  },
  {
    name: "Journal Entries",
    href: "/finance/journal-entries",
    icon: ScrollText,
    description: "Record and approve double-entry journal entries",
  },
  {
    name: "Period Closing",
    href: "/finance/periods",
    icon: CalendarCheck,
    description: "Close and reopen accounting periods, validate entries",
  },
  {
    name: "Reports",
    href: "/finance/reports",
    icon: BarChart3,
    description: "Trial Balance, Profit & Loss, and Balance Sheet",
  },
];

export default function FinancePage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/finance");
    }
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Wallet className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Finance
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            General ledger, chart of accounts, and financial reporting
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {modules.map((mod) => {
          const Icon = mod.icon;
          return (
            <button
              key={mod.href}
              type="button"
              onClick={() => router.push(mod.href)}
              className="group flex flex-col items-start gap-3 rounded-lg border border-border bg-card p-5 text-left transition-all hover:border-ring hover:shadow-sm cursor-pointer"
            >
              <div className="flex size-9 items-center justify-center rounded-lg bg-accent">
                <Icon className="size-4 text-accent-foreground" />
              </div>
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-semibold text-foreground">
                    {mod.name}
                  </h3>
                  <ArrowRight className="size-3.5 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {mod.description}
                </p>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
