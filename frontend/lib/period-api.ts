import { Period } from "../lib/period-types";

const API_BASE = "/api/v1/finance/periods";

/** Fetch all periods */
export const fetchPeriods = async (): Promise<Period[]> => {
  const res = await fetch(API_BASE, { method: "GET" });
  if (!res.ok) {
    throw new Error("Failed to fetch periods");
  }
  return res.json();
};

/** Validate that a period can be closed */
export const validateClose = async (periodId: string): Promise<boolean> => {
  const res = await fetch(`${API_BASE}/${periodId}/validate`, { method: "GET" });
  if (!res.ok) {
    throw new Error(`Validate close failed for period ${periodId}`);
  }
  const data = await res.json();
  // API returns { canClose: boolean }
  return data.canClose;
};

/** Close a period */
export const closePeriod = async (periodId: string): Promise<void> => {
  const res = await fetch(`${API_BASE}/${periodId}/close`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  if (!res.ok) {
    throw new Error(`Close period failed for ${periodId}`);
  }
};

/** Reopen a closed period */
export const reopenPeriod = async (periodId: string, reason: string): Promise<void> => {
  const res = await fetch(`${API_BASE}/${periodId}/reopen`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason }),
  });
  if (!res.ok) {
    throw new Error(`Reopen period failed for ${periodId}`);
  }
};

/** Generate missing periods (admin utility) */
export const generatePeriods = async (): Promise<void> => {
  const res = await fetch(`${API_BASE}/generate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  if (!res.ok) {
    throw new Error("Generate periods failed");
  }
};
