import { useQuery } from "@tanstack/react-query";
import {
  getTrialBalance,
  getProfitLoss,
  getBalanceSheet,
  getAccountLedger,
} from "@/lib/report-api";
import type { ReportResponse, LedgerResponse } from "@/lib/report-types";

export function useTrialBalance(
  startDate: string,
  endDate: string,
  includeDrafts: boolean,
) {
  return useQuery<ReportResponse>({
    queryKey: ["trialBalance", startDate, endDate, includeDrafts],
    queryFn: () => getTrialBalance({ startDate, endDate, includeDrafts }),
    enabled: !!startDate && !!endDate,
  });
}

export function useProfitLoss(
  startDate: string,
  endDate: string,
  includeDrafts: boolean,
) {
  return useQuery<ReportResponse>({
    queryKey: ["profitLoss", startDate, endDate, includeDrafts],
    queryFn: () => getProfitLoss({ startDate, endDate, includeDrafts }),
    enabled: !!startDate && !!endDate,
  });
}

export function useBalanceSheet(
  asOfDate: string,
  includeDrafts: boolean,
  netIncome: number | null | undefined,
) {
  return useQuery<ReportResponse>({
    queryKey: ["balanceSheet", asOfDate, includeDrafts, netIncome],
    queryFn: () => getBalanceSheet({ asOfDate, includeDrafts, netIncome }),
    enabled: !!asOfDate,
  });
}

export function useAccountLedger(
  accountId: string | null,
  startDate: string,
  endDate: string,
  includeDrafts: boolean,
  page: number,
) {
  return useQuery<LedgerResponse>({
    queryKey: ["accountLedger", accountId, startDate, endDate, includeDrafts, page],
    queryFn: () =>
      getAccountLedger({
        accountId: accountId!,
        startDate,
        endDate,
        includeDrafts,
        page,
        pageSize: 20,
      }),
    enabled: !!accountId && !!startDate && !!endDate,
    placeholderData: (previousData) => previousData,
  });
}
