"use client";

import { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { Search, Bell, LayoutGrid, User, Moon, Sun, LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const navTabs = [
  { label: "WMS", href: "/wms" },
  { label: "Finance", href: "/finance" },
  { label: "HR", href: "/hr" },
  { label: "Projects", href: "/projects" },
];

export function Header() {
  const router = useRouter();
  const [dark, setDark] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const isDark = document.documentElement.classList.contains("dark");
    setDark(isDark);
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const toggleDark = () => {
    const html = document.documentElement;
    html.classList.toggle("dark");
    setDark(html.classList.contains("dark"));
  };

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
        <div className="relative hidden sm:block">
          <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search..."
            className="h-8 w-48 rounded border-border bg-card pl-8 text-sm text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring"
          />
        </div>

        <Button
          variant="outline"
          size="sm"
          className="border-accent text-accent-foreground font-semibold text-xs h-8 hover:brightness-[0.95] active:shadow-[inset_0_1px_2px_rgba(0,0,0,0.15)]"
        >
          Quick Action
        </Button>

        <button
          onClick={toggleDark}
          className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer"
          title={dark ? "Light mode" : "Dark mode"}
        >
          {dark ? <Sun className="size-4" /> : <Moon className="size-4" />}
        </button>
        <button className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer">
          <Bell className="size-4" />
        </button>
        <button className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer">
          <LayoutGrid className="size-4" />
        </button>
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
