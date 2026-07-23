const TIMEZONE_MAP: Record<string, string> = {
  WIB: "Asia/Jakarta",
  WITA: "Asia/Makassar",
  WIT: "Asia/Jayapura",
  UTC: "UTC",
};

export function getTimezone(): string {
  if (typeof window === "undefined") return "Asia/Jakarta";
  return localStorage.getItem("timezone") ?? Intl.DateTimeFormat().resolvedOptions().timeZone;
}

export function setTimezone(tz: string) {
  localStorage.setItem("timezone", tz);
}

export function formatDate(
  dateStr: string | null | undefined,
  options?: Intl.DateTimeFormatOptions & { timezone?: string }
): string {
  if (!dateStr) return "—";
  const tz = options?.timezone ?? getTimezone();
  const { timezone: _, ...fmt } = options ?? {};
  return new Date(dateStr).toLocaleDateString("id-ID", {
    timeZone: tz,
    day: "numeric",
    month: "short",
    year: "numeric",
    ...fmt,
  });
}

export function formatDateTime(
  dateStr: string | null | undefined,
  options?: Intl.DateTimeFormatOptions & { timezone?: string }
): string {
  if (!dateStr) return "—";
  const tz = options?.timezone ?? getTimezone();
  const { timezone: _, ...fmt } = options ?? {};
  return new Date(dateStr).toLocaleString("id-ID", {
    timeZone: tz,
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    ...fmt,
  });
}

export function formatRelative(dateStr: string | null | undefined): string {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}h ago`;
  const diffD = Math.floor(diffH / 24);
  if (diffD < 30) return `${diffD}d ago`;
  return formatDate(dateStr);
}

export const TIMEZONE_OPTIONS = [
  { label: "WIB (Jakarta)", value: "Asia/Jakarta" },
  { label: "WITA (Makassar)", value: "Asia/Makassar" },
  { label: "WIT (Jayapura)", value: "Asia/Jayapura" },
  { label: "UTC", value: "UTC" },
  { label: "Singapore", value: "Asia/Singapore" },
  { label: "Tokyo", value: "Asia/Tokyo" },
  { label: "New York", value: "America/New_York" },
  { label: "London", value: "Europe/London" },
];
