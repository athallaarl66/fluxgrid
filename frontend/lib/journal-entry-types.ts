export type JournalEntryStatus = "DRAFT" | "PENDING_APPROVAL" | "POSTED" | "VOID";

export interface JournalEntryLine {
  id?: string;
  accountId: string;
  description?: string;
  debit: number;
  credit: number;
}

export interface JournalEntry {
  id: string;
  entryNo: string;
  transactionDate: string;
  description: string;
  status: JournalEntryStatus;
  totalAmount: number;
  createdBy: string;
  approvedBy?: string;
  tenantId: string;
  createdAt: string;
  lines: JournalEntryLine[];
}

export const STATUS_CONFIG: Record<JournalEntryStatus, { label: string; className: string }> = {
  DRAFT: { label: "Draft", className: "bg-[#E6E2DF] text-[#6B6560]" },
  PENDING_APPROVAL: { label: "Pending Approval", className: "bg-amber-100 text-amber-700" },
  POSTED: { label: "Posted", className: "bg-[#D4E7AB] text-[#4A6B2A]" },
  VOID: { label: "Void", className: "bg-red-100 text-red-700" },
};

export function formatIDR(amount: number): string {
  return new Intl.NumberFormat("id-ID", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}
