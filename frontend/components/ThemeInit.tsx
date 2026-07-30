"use client";

import { useEffect } from "react";

export function ThemeInit() {
  useEffect(() => {
    const stored = localStorage.getItem("fluxgrid-theme");
    const html = document.documentElement;

    if (stored === "dark") {
      html.classList.add("dark");
    } else if (stored === "light") {
      html.classList.remove("dark");
    } else {
      const prefersDark = window.matchMedia(
        "(prefers-color-scheme: dark)",
      ).matches;
      html.classList.toggle("dark", prefersDark);
    }
  }, []);

  return null;
}
