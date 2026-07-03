"use client";

import { Card, CardContent } from "@/components/ui/card";
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
      <Card
        className={cn(
          "group border border-[#e5debf] bg-white transition-colors duration-200 hover:bg-[#f7f3f0]",
          !hasPermission && "opacity-40",
        )}
      >
        <CardContent className="flex items-start gap-3 p-4">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-[#f6f0d7]">
            <Icon className="size-5 text-[#706d59]" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between gap-2">
              <h3 className="text-sm font-semibold text-[#1c1b1a]">
                {module.name}
              </h3>
              <Badge
                variant="secondary"
                className="shrink-0 bg-[#d4e7ab] text-[#586838] text-[11px] font-semibold rounded-full px-2 py-0.5"
              >
                {module.metric}
              </Badge>
            </div>
            <p className="mt-0.5 text-[13px] leading-[1.4] text-[#49473e] truncate">
              {module.description}
            </p>
          </div>
        </CardContent>
      </Card>
    </a>
  );
}
