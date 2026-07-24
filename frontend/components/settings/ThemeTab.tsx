"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Moon, Sun, Monitor } from "lucide-react";

const THEME_KEY = "fluxgrid-theme";

type Theme = "light" | "dark" | "system";

function getStoredTheme(): Theme {
  if (typeof window === "undefined") return "system";
  return (localStorage.getItem(THEME_KEY) as Theme) || "system";
}

function applyTheme(theme: Theme) {
  const html = document.documentElement;
  if (theme === "dark") {
    html.classList.add("dark");
    html.classList.remove("light");
  } else if (theme === "light") {
    html.classList.add("light");
    html.classList.remove("dark");
  } else {
    const prefersDark = window.matchMedia(
      "(prefers-color-scheme: dark)",
    ).matches;
    html.classList.toggle("dark", prefersDark);
    html.classList.toggle("light", !prefersDark);
  }
}

export function ThemeTab() {
  const [theme, setTheme] = useState<Theme>("system");

  useEffect(() => {
    setTheme(getStoredTheme());
  }, []);

  function handleSelect(t: Theme) {
    setTheme(t);
    localStorage.setItem(THEME_KEY, t);
    applyTheme(t);
  }

  const options: { value: Theme; label: string; icon: React.ReactNode }[] = [
    { value: "light", label: "Light", icon: <Sun className="size-4" /> },
    { value: "dark", label: "Dark", icon: <Moon className="size-4" /> },
    { value: "system", label: "System", icon: <Monitor className="size-4" /> },
  ];

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Choose your preferred theme. Your selection is saved locally and persists
        across sessions.
      </p>
      <div className="flex gap-2">
        {options.map((opt) => (
          <Button
            key={opt.value}
            type="button"
            variant={theme === opt.value ? "default" : "outline"}
            size="sm"
            onClick={() => handleSelect(opt.value)}
            className={
              theme === opt.value
                ? "border-accent bg-accent text-accent-foreground font-semibold"
                : "border-border text-muted-foreground"
            }
          >
            {opt.icon}
            <span className="ml-1.5">{opt.label}</span>
          </Button>
        ))}
      </div>
    </div>
  );
}
