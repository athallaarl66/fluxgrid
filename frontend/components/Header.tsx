"use client";

import { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import {
  Search,
  Bell,
  LayoutGrid,
  User,
  Moon,
  Sun,
  LogOut,
  Settings,
  Wallet,
  Warehouse,
  Users,
  ClipboardList,
  ArrowRightFromLine,
  PenLine,
  Upload,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { useNotifications } from "@/hooks/useNotifications";
import { NotificationDropdown } from "@/components/notifications/NotificationDropdown";

const navTabs = [
  { label: "WMS", href: "/wms" },
  { label: "Finance", href: "/finance" },
  { label: "HR", href: "/hr" },
  { label: "Projects", href: "/projects" },
];

const quickActions = [
  { label: "Create Journal Entry", href: "/finance/journal-entries", icon: PenLine },
  { label: "New Receipt", href: "/wms/inbound/create", icon: ArrowRightFromLine },
  { label: "Upload CV", href: "/hr/recruitment", icon: Upload },
];

const modules = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutGrid },
  { label: "WMS", href: "/wms", icon: Warehouse },
  { label: "Finance", href: "/finance", icon: Wallet },
  { label: "HR", href: "/hr", icon: Users },
  { label: "Projects", href: "/projects", icon: ClipboardList },
];

export function Header() {
  const router = useRouter();
  const [dark, setDark] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [quickOpen, setQuickOpen] = useState(false);
  const [launcherOpen, setLauncherOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const userMenuRef = useRef<HTMLDivElement>(null);
  const quickRef = useRef<HTMLDivElement>(null);
  const launcherRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);
  const { data: notifData } = useNotifications();

  useEffect(() => {
    const isDark = document.documentElement.classList.contains("dark");
    setDark(isDark);
  }, []);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) setUserMenuOpen(false);
      if (quickRef.current && !quickRef.current.contains(e.target as Node)) setQuickOpen(false);
      if (launcherRef.current && !launcherRef.current.contains(e.target as Node)) setLauncherOpen(false);
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) setNotifOpen(false);
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  const toggleDark = () => {
    const html = document.documentElement;
    html.classList.toggle("dark");
    const isDark = html.classList.contains("dark");
    setDark(isDark);
    localStorage.setItem("fluxgrid-theme", isDark ? "dark" : "light");
  };

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/dashboard?search=${encodeURIComponent(searchQuery.trim())}`);
      setSearchQuery("");
    }
  }

  async function handleLogout() {
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/login");
  }

  return (
    <header className="flex h-14 items-center justify-between border-b border-border bg-background px-5">
      <nav className="hidden md:flex items-center gap-1">
        {navTabs.map((tab) => (
          <a
            key={tab.href}
            href={tab.href}
            className="rounded px-3 py-1.5 text-sm font-medium text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground"
          >
            {tab.label}
          </a>
        ))}
      </nav>

      <div className="flex items-center gap-2 ml-auto">
        <form onSubmit={handleSearch} className="relative hidden sm:block">
          <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="h-8 w-48 rounded border-border bg-card pl-8 text-sm text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring"
          />
        </form>

        {/* Quick Action */}
        <div className="relative" ref={quickRef}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => { setQuickOpen(!quickOpen); setLauncherOpen(false); }}
            className="border-accent text-accent-foreground font-semibold text-xs h-8 hover:brightness-[0.95] active:shadow-[inset_0_1px_2px_rgba(0,0,0,0.15)]"
          >
            Quick Action
          </Button>
          {quickOpen && (
            <div className="absolute right-0 z-20 mt-1 w-52 rounded-lg border border-border bg-popover py-1 shadow-lg">
              {quickActions.map((a) => {
                const Icon = a.icon;
                return (
                  <a
                    key={a.href}
                    href={a.href}
                    onClick={() => setQuickOpen(false)}
                    className="flex items-center gap-2.5 px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
                  >
                    <Icon className="size-3.5" />
                    {a.label}
                  </a>
                );
              })}
            </div>
          )}
        </div>

        <button
          onClick={toggleDark}
          className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer"
          title={dark ? "Light mode" : "Dark mode"}
        >
          {dark ? <Sun className="size-4" /> : <Moon className="size-4" />}
        </button>

        {/* Bell — Notifications */}
        <div className="relative" ref={notifRef}>
          <button
            onClick={() => { setNotifOpen(!notifOpen); setQuickOpen(false); setLauncherOpen(false); }}
            className="relative flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer"
            title="Notifications"
          >
            <Bell className="size-4" />
            {(notifData?.count ?? 0) > 0 && (
              <span className="absolute -top-0.5 -right-0.5 size-4 rounded-full bg-accent text-[9px] font-bold text-accent-foreground flex items-center justify-center">
                {notifData!.count > 9 ? "9+" : notifData!.count}
              </span>
            )}
          </button>
          {notifOpen && (
            <div className="absolute right-0 z-20 mt-1">
              <NotificationDropdown />
            </div>
          )}
        </div>

        {/* LayoutGrid — Module Switcher */}
        <div className="relative" ref={launcherRef}>
          <button
            onClick={() => { setLauncherOpen(!launcherOpen); setQuickOpen(false); }}
            className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer"
            title="Modules"
          >
            <LayoutGrid className="size-4" />
          </button>
          {launcherOpen && (
            <div className="absolute right-0 z-20 mt-1 w-48 rounded-lg border border-border bg-popover py-1 shadow-lg">
              {modules.map((m) => {
                const Icon = m.icon;
                return (
                  <a
                    key={m.href}
                    href={m.href}
                    onClick={() => setLauncherOpen(false)}
                    className="flex items-center gap-2.5 px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
                  >
                    <Icon className="size-3.5" />
                    {m.label}
                  </a>
                );
              })}
            </div>
          )}
        </div>

        <div className="relative" ref={userMenuRef}>
          <button
            onClick={() => setUserMenuOpen(!userMenuOpen)}
            className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer"
          >
            <User className="size-4" />
          </button>
          {userMenuOpen && (
            <>
              <div className="fixed inset-0 z-10" onClick={() => setUserMenuOpen(false)} />
              <div className="absolute right-0 z-20 mt-1 w-36 rounded-lg border border-border bg-popover py-1 shadow-lg">
                <a
                  href="/settings"
                  onClick={() => setUserMenuOpen(false)}
                  className="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors hover:bg-muted"
                >
                  <Settings className="size-3.5 text-muted-foreground" />
                  Settings
                </a>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors hover:bg-muted"
                >
                  <LogOut className="size-3.5 text-muted-foreground" />
                  Logout
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
