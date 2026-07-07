import { apiClient } from "@/lib/api-client";
import type { ReportResponse, LedgerResponse } from "@/lib/report-types";

export function getTrialBalance(params: {
  startDate: string;
  endDate: string;
  includeDrafts?: boolean;
}): Promise<ReportResponse> {
  const qs = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
    includeDrafts: String(params.includeDrafts ?? false),
  });
  return apiClient(`/api/v1/finance/reports/trial-balance?${qs}`);
}

export function getProfitLoss(params: {
  startDate: string;
  endDate: string;
  includeDrafts?: boolean;
}): Promise<ReportResponse> {
  const qs = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
    includeDrafts: String(params.includeDrafts ?? false),
  });
  return apiClient(`/api/v1/finance/reports/pl?${qs}`);
}

export function getBalanceSheet(params: {
  asOfDate: string;
  includeDrafts?: boolean;
  netIncome?: number | null;
}): Promise<ReportResponse> {
  const qs = new URLSearchParams({
    asOfDate: params.asOfDate,
    includeDrafts: String(params.includeDrafts ?? false),
  });
  if (params.netIncome != null) qs.set("netIncome", String(params.netIncome));
  return apiClient(`/api/v1/finance/reports/balance-sheet?${qs}`);
}

export function getAccountLedger(params: {
  accountId: string;
  startDate: string;
  endDate: string;
  includeDrafts?: boolean;
  page?: number;
  pageSize?: number;
}): Promise<LedgerResponse> {
  const qs = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
    includeDrafts: String(params.includeDrafts ?? false),
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 20),
  });
  return apiClient(`/api/v1/finance/reports/${params.accountId}/ledger?${qs}`);
}
