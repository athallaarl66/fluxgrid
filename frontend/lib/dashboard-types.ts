export interface DashboardResponse {
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  revenueMtd: number;
  expensesMtd: number;
  netIncomeMtd: number;
  journalEntryCount: number;
  periodId: string;
  recentEntries: RecentEntryRow[];
  monthlyTrend: MonthlyTrendRow[];
}

export interface RecentEntryRow {
  id: string;
  entryNo: string;
  description: string;
  transactionDate: string;
  totalDebit: number;
  totalCredit: number;
  status: string;
}

export interface MonthlyTrendRow {
  month: number;
  revenue: number;
  expenses: number;
}
