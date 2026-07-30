"use client";

import { Users, Shield } from "lucide-react";
import { UserTable } from "@/components/admin/UserTable";
import { RoleTable } from "@/components/admin/RoleTable";
import { cn } from "@/lib/utils";
import { useState } from "react";

const tabs = [
  { id: "users" as const, label: "Users", icon: Users },
  { id: "roles" as const, label: "Roles", icon: Shield },
];

export default function AdminPage() {
  const [tab, setTab] = useState<"users" | "roles">("users");

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Shield className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            User & Role Management
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Manage users, roles, and permissions
          </p>
        </div>
      </div>

      <div className="flex gap-1 border-b border-border">
        {tabs.map((t) => {
          const Icon = t.icon;
          return (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors -mb-px cursor-pointer",
                tab === t.id
                  ? "border-accent text-accent-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              <Icon className="size-4" />
              {t.label}
            </button>
          );
        })}
      </div>

      <div>
        {tab === "users" && <UserTable />}
        {tab === "roles" && <RoleTable />}
      </div>
    </div>
  );
}
