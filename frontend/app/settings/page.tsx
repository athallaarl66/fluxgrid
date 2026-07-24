"use client";

import { Settings } from "lucide-react";

export default function SettingsPage() {
  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Settings className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Settings
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Manage your account and application preferences
          </p>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-8 text-center">
        <Settings className="mx-auto size-12 text-muted-foreground/40" />
        <p className="mt-4 text-sm font-medium text-foreground">Coming soon</p>
        <p className="mt-1 text-xs text-muted-foreground">
          Profile settings, security, and theme preferences will be available here.
        </p>
      </div>
    </div>
  );
}
