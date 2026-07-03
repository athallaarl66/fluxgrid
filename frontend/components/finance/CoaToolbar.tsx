"use client";

import { useState, useEffect, useRef } from "react";
import { Search, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

interface CoaToolbarProps {
  onSearch: (query: string) => void;
  onNewAccount: () => void;
}

export function CoaToolbar({ onSearch, onNewAccount }: CoaToolbarProps) {
  const [value, setValue] = useState("");
  const timeoutRef = useRef<ReturnType<typeof setTimeout>>(undefined as any);

  useEffect(() => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    timeoutRef.current = setTimeout(() => onSearch(value), 300);
    return () => { if (timeoutRef.current) clearTimeout(timeoutRef.current); };
  }, [value, onSearch]);

  return (
    <div className="flex items-center gap-3">
      <div className="relative flex-1">
        <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by code or name..."
          value={value}
          onChange={(e) => setValue(e.target.value)}
          className="h-9 w-full rounded-lg border-border bg-card pl-9 text-sm"
        />
      </div>
      <Button onClick={onNewAccount} size="sm">
        <Plus className="size-4" />
        New Account
      </Button>
    </div>
  );
}
