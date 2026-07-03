"use client";

import { useState, useEffect } from "react";
import { Search, Bell, LayoutGrid, User, Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const navTabs = [
  { label: "WMS", href: "/wms" },
  { label: "Finance", href: "/finance" },
  { label: "HR", href: "/hr" },
  { label: "Projects", href: "/projects" },
];

export function Header() {
  const [dark, setDark] = useState(false);

  useEffect(() => {
    const isDark = document.documentElement.classList.contains("dark");
    setDark(isDark);
  }, []);

  const toggleDark = () => {
    const html = document.documentElement;
    html.classList.toggle("dark");
    setDark(html.classList.contains("dark"));
  };

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
        <button className="flex size-8 items-center justify-center rounded text-muted-foreground transition-colors duration-200 hover:bg-muted hover:text-foreground cursor-pointer">
          <User className="size-4" />
        </button>
      </div>
    </header>
  );
}
