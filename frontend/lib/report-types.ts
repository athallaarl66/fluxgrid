export interface ReportRow {
  accountId: string;
  code: string;
  name: string;
  type: string;
  depth: number;
  debit: number;
  credit: number;
  balance: number;
  children: ReportRow[];
}

export interface ReportResponse {
  rows: ReportRow[];
  totalDebit: number;
  totalCredit: number;
  netIncome: number | null;
}

export interface LedgerDetailRow {
  entryId: string;
  entryNo: string;
  transactionDate: string;
  description: string;
  debit: number;
  credit: number;
  createdAt: string;
}

export interface LedgerResponse {
  rows: LedgerDetailRow[];
  total: number;
  page: number;
  pageSize: number;
}

export type ReportType = "trial-balance" | "pl" | "balance-sheet";

export function formatBalance(amount: number): string {
  const abs = Math.abs(amount);
  const formatted = new Intl.NumberFormat("id-ID", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(abs);
  return amount < 0 ? `(${formatted})` : formatted;
}
