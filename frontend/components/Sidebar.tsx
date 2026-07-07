"use client";

import { useState } from "react";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Warehouse,
  Wallet,
  Users,
  ClipboardList,
  Settings,
  HelpCircle,
  Plus,
  ChevronDown,
  ScrollText,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  children?: { label: string; href: string }[];
}

const navItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { label: "WMS", href: "/wms", icon: Warehouse },
  {
    label: "Finance",
    href: "/finance",
    icon: Wallet,
    children: [
      { label: "Chart of Accounts", href: "/finance/chart-of-accounts" },
      { label: "Journal Entries", href: "/finance/journal-entries" },
      { label: "Period Closing", href: "/finance/periods" },
    ],
  },
  { label: "HR", href: "/hr", icon: Users },
  { label: "Projects", href: "/projects", icon: ClipboardList },
];

function isChildActive(pathname: string, children?: NavItem["children"]) {
  return children?.some((c) => pathname.startsWith(c.href)) ?? false;
}

export function Sidebar() {
  const pathname = usePathname();
  const [expanded, setExpanded] = useState<string | null>(
    () =>
      navItems.find((item) => isChildActive(pathname, item.children))?.href ??
      null,
  );

  function toggleExpand(href: string) {
    setExpanded((prev) => (prev === href ? null : href));
  }

  return (
    <aside className="fixed left-0 top-0 z-30 flex h-screen w-[260px] flex-col border-r border-sidebar-border bg-sidebar">
      <div className="flex items-center gap-2 px-5 pt-5 pb-4">
        <div className="flex size-8 items-center justify-center rounded bg-sidebar-primary">
          <span className="text-sm font-bold text-sidebar-primary-foreground">
            F
          </span>
        </div>
        <div>
          <p className="text-sm font-semibold leading-tight text-sidebar-foreground">
            FluxGrid ERP
          </p>
          <p className="text-[11px] font-semibold leading-tight text-muted-foreground uppercase tracking-wider">
            Industrial OS
          </p>
        </div>
      </div>

      <nav className="flex-1 px-3 space-y-0.5">
        {navItems.map((item, idx) => {
          const isActive = pathname === item.href;
          const childActive = isChildActive(pathname, item.children);
          const isExpanded = expanded === item.href;
          const Icon = item.icon;
          const hasChildren = !!item.children && item.children.length > 0;

          return (
            <div key={item.href}>
              <div
                className={cn(
                  "flex items-center rounded transition-colors duration-200",
                  isActive || childActive
                    ? "bg-accent text-accent-foreground"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                )}
              >
                <a
                  href={item.href}
                  className="flex flex-1 items-center gap-3 rounded-l px-3 py-2 text-sm font-medium"
                >
                  <Icon className="size-4 shrink-0" />
                  <span>{item.label}</span>
                </a>
                {hasChildren && (
                  <button
                    type="button"
                    onClick={() => toggleExpand(item.href)}
                    className={cn(
                      "flex size-7 items-center justify-center rounded-r text-muted-foreground transition-colors duration-200 cursor-pointer hover:text-foreground",
                    )}
                  >
                    <ChevronDown
                      className={cn(
                        "size-3.5 transition-transform duration-200",
                        isExpanded && "rotate-180",
                      )}
                    />
                  </button>
                )}
              </div>

              {hasChildren && (
                <div
                  className={cn(
                    "grid transition-all duration-200 ease-in-out",
                    isExpanded ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
                  )}
                >
                  <div className="overflow-hidden">
                    <div className="ml-2 mt-0.5 space-y-0.5 border-l border-border pl-2 pb-0.5">
                      {item.children!.map((child) => {
                        const isChildActive = pathname === child.href || pathname.startsWith(child.href + "/");
                        return (
                          <a
                            key={child.href}
                            href={child.href}
                            className={cn(
                              "flex items-center gap-3 rounded px-3 py-1.5 text-sm font-medium transition-colors duration-200",
                              isChildActive
                                ? "bg-accent text-accent-foreground"
                                : "text-muted-foreground hover:bg-muted hover:text-foreground",
                            )}
                          >
                            {child.label}
                          </a>
                        );
                      })}
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </nav>

      <div className="border-t border-sidebar-border px-3 py-3 space-y-0.5">
        <a
          href="/settings"
          className="flex items-center gap-3 rounded px-3 py-2 text-sm font-medium text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground"
        >
          <Settings className="size-4 shrink-0" />
          Settings
        </a>
        <a
          href="/support"
          className="flex items-center gap-3 rounded px-3 py-2 text-sm font-medium text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground"
        >
          <HelpCircle className="size-4 shrink-0" />
          Support
        </a>
      </div>
    </aside>
  );
}
