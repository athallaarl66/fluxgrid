"use client";

import { useState } from "react";
import { Settings, User, KeyRound, Palette } from "lucide-react";
import { ProfileTab } from "@/components/settings/ProfileTab";
import { SecurityTab } from "@/components/settings/SecurityTab";
import { ThemeTab } from "@/components/settings/ThemeTab";
import { cn } from "@/lib/utils";

const tabs = [
  { id: "profile", label: "Profile", icon: User },
  { id: "security", label: "Security", icon: KeyRound },
  { id: "theme", label: "Theme", icon: Palette },
] as const;

type TabId = (typeof tabs)[number]["id"];

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState<TabId>("profile");

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

      <div className="flex gap-6">
        <nav className="w-48 shrink-0 space-y-0.5">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={cn(
                  "flex w-full items-center gap-2.5 rounded px-3 py-2 text-sm font-medium transition-colors duration-200",
                  isActive
                    ? "bg-accent text-accent-foreground"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                )}
              >
                <Icon className="size-4 shrink-0" />
                {tab.label}
              </button>
            );
          })}
        </nav>

        <div className="flex-1 rounded-xl border border-border bg-card p-6">
          {activeTab === "profile" && <ProfileTab />}
          {activeTab === "security" && <SecurityTab />}
          {activeTab === "theme" && <ThemeTab />}
        </div>
      </div>
    </div>
  );
}
