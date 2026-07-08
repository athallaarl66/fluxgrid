import { apiClient } from "./api-client";
import type { Period } from "./period-types";

const BASE = "/api/v1/finance/periods";

export const fetchPeriods = async (): Promise<Period[]> =>
  apiClient<Period[]>(BASE);

export const validateClose = async (periodId: string): Promise<boolean> => {
  const data = await apiClient<{ canClose: boolean }>(`${BASE}/${periodId}/validate`);
  return data.canClose;
};

export const closePeriod = async (periodId: string): Promise<void> =>
  apiClient<void>(`${BASE}/${periodId}/close`, { method: "POST", body: JSON.stringify({}) });

export const reopenPeriod = async (periodId: string, reason: string): Promise<void> =>
  apiClient<void>(`${BASE}/${periodId}/reopen`, { method: "POST", body: JSON.stringify({ reason }) });

export const generatePeriods = async (): Promise<void> =>
  apiClient<void>(`${BASE}/generate`, { method: "POST", body: JSON.stringify({}) });
