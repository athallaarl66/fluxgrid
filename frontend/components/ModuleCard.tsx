"use client";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { Package, Wallet, Users, ClipboardList } from "lucide-react";
import type { ModuleInfo } from "@/hooks/useDashboard";

const iconMap: Record<string, typeof Package> = {
  package: Package,
  wallet: Wallet,
  users: Users,
  clipboard: ClipboardList,
};

interface ModuleCardProps {
  module: ModuleInfo;
  hasPermission: boolean;
}

export function ModuleCard({ module, hasPermission }: ModuleCardProps) {
  const Icon = iconMap[module.icon] || Package;

  return (
    <a
      href={module.path}
      className={cn(
        "block transition-opacity duration-200",
        !hasPermission && "cursor-not-allowed",
      )}
      onClick={(e) => {
        if (!hasPermission) e.preventDefault();
      }}
      title={!hasPermission ? "You lack permission" : undefined}
    >
      <div
        className={cn(
          "rounded-lg border border-border bg-card transition-colors duration-200",
          hasPermission && "hover:bg-muted",
          !hasPermission && "opacity-40",
        )}
      >
        <div className="p-4">
          <div className="flex items-start gap-3">
            <div className="flex size-10 shrink-0 items-center justify-center rounded bg-primary-container dark:bg-muted">
              <Icon className="size-5 text-accent-foreground" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-2">
                <h3 className="text-sm font-semibold text-foreground">
                  {module.name}
                </h3>
                <Badge className="shrink-0 bg-secondary-container text-on-secondary-container text-[11px] font-semibold rounded-full px-2 py-0.5 border-0">
                  {module.metric}
                </Badge>
              </div>
              <p className="mt-0.5 text-[13px] leading-[1.4] text-muted-foreground truncate">
                {module.description}
              </p>
            </div>
          </div>
        </div>
      </div>
    </a>
  );
}
