"use client";

import { Search, Bell, LayoutGrid, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const navTabs = [
  { label: "WMS", href: "/wms" },
  { label: "Finance", href: "/finance" },
  { label: "HR", href: "/hr" },
  { label: "Projects", href: "/projects" },
];

export function Header() {
  return (
    <header className="flex h-14 items-center justify-between border-b border-[#e5debf] bg-[#fdf8f5] px-5">
      <nav className="hidden md:flex items-center gap-1">
        {navTabs.map((tab) => (
          <a
            key={tab.href}
            href={tab.href}
            className="rounded-md px-3 py-1.5 text-sm font-medium text-[#49473e] transition-colors duration-200 hover:bg-[#f1edea] hover:text-[#1c1b1a]"
          >
            {tab.label}
          </a>
        ))}
      </nav>

      <div className="flex items-center gap-2 ml-auto">
        <div className="relative hidden sm:block">
          <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-[#7a776d]" />
          <Input
            type="search"
            placeholder="Search..."
            className="h-8 w-48 rounded-md border-[#e5debf] bg-white pl-8 text-sm text-[#1c1b1a] placeholder:text-[#7a776d]"
          />
        </div>

        <Button
          variant="outline"
          size="sm"
          className="border-[#9cab84] text-[#706d59] font-semibold text-xs h-8"
        >
          Quick Action
        </Button>

        <button className="flex size-8 items-center justify-center rounded-md text-[#49473e] transition-colors duration-200 hover:bg-[#f1edea] cursor-pointer">
          <Bell className="size-4" />
        </button>
        <button className="flex size-8 items-center justify-center rounded-md text-[#49473e] transition-colors duration-200 hover:bg-[#f1edea] cursor-pointer">
          <LayoutGrid className="size-4" />
        </button>
        <button className="flex size-8 items-center justify-center rounded-md text-[#49473e] transition-colors duration-200 hover:bg-[#f1edea] cursor-pointer">
          <User className="size-4" />
        </button>
      </div>
    </header>
  );
}
