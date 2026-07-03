"use client";

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

  return (
    <aside className="fixed left-0 top-0 z-30 flex h-screen w-[260px] flex-col border-r border-sidebar-border bg-sidebar">
      <div className="flex items-center gap-2 px-5 pt-5 pb-4">
        <div className="flex size-8 items-center justify-center rounded bg-sidebar-primary">
          <span className="text-sm font-bold text-sidebar-primary-foreground">F</span>
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

      <div className="px-3 pb-3">
        <button className="flex w-full items-center gap-2 rounded border border-accent bg-accent px-3 py-1.5 text-sm font-semibold text-accent-foreground transition-colors duration-200 hover:brightness-[0.95] active:shadow-[inset_0_1px_2px_rgba(0,0,0,0.15)] cursor-pointer">
          <Plus className="size-4" />
          New Task
        </button>
      </div>

      <nav className="flex-1 px-3 space-y-0.5">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          const childActive = isChildActive(pathname, item.children);
          const Icon = item.icon;
          return (
            <div key={item.href}>
              <a
                href={item.href}
                className={cn(
                  "flex items-center gap-3 rounded px-3 py-2 text-sm font-medium transition-colors duration-200",
                  isActive || childActive
                    ? "bg-accent text-accent-foreground"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                )}
              >
                <Icon className="size-4 shrink-0" />
                <span className="flex-1">{item.label}</span>
                {item.children && (
                  <ChevronDown
                    className={cn(
                      "size-3.5 text-muted-foreground transition-transform",
                      childActive && "rotate-180",
                    )}
                  />
                )}
              </a>
              {(isActive || childActive) && item.children && (
                <div className="ml-2 mt-0.5 space-y-0.5 border-l border-border pl-2">
                  {item.children.map((child) => {
                    const isChildActive = pathname === child.href;
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
