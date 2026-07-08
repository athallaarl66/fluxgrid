"use client";

import { usePathname } from "next/navigation";
import Link from "next/link";
import { cn } from "@/lib/utils";

const navItems = [
  { label: "Dashboard", href: "/wms" },
  { label: "Stock Ledger", href: "/wms/stock-ledger" },
  { label: "Inbound", href: "/wms/inbound" },
  { label: "Outbound", href: "/wms/outbound" },
];

export function WmsNav() {
  const pathname = usePathname();

  return (
    <nav className="flex items-center gap-1 border-b border-border pb-0 mb-5 overflow-x-auto">
      {navItems.map((item) => {
        const isActive = pathname === item.href || pathname.startsWith(item.href + "/");
        return (
          <Link
            key={item.href}
            href={item.href}
            className={cn(
              "shrink-0 rounded-t-md px-3 py-1.5 text-xs font-medium transition-colors",
              isActive
                ? "bg-card text-foreground border border-border border-b-white"
                : "text-muted-foreground hover:text-foreground hover:bg-muted",
            )}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
