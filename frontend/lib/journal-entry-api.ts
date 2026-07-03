import { apiClient } from "./api-client";
import type { JournalEntry, JournalEntryLine, JournalEntryStatus } from "./journal-entry-types";

export interface CreateJournalEntryPayload {
  transactionDate: string;
  description: string;
  lines: Pick<JournalEntryLine, "accountId" | "description" | "debit" | "credit">[];
  status: "DRAFT" | "SUBMIT";
}

export interface UpdateJournalEntryPayload extends CreateJournalEntryPayload {}

export async function getJournalEntries(
  status?: JournalEntryStatus,
  page = 1,
  pageSize = 20
): Promise<JournalEntry[]> {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  return apiClient(`/api/v1/finance/journal-entries?${params.toString()}`);
}

export async function getJournalEntry(id: string): Promise<JournalEntry> {
  return apiClient(`/api/v1/finance/journal-entries/${id}`);
}

export async function createJournalEntry(payload: CreateJournalEntryPayload): Promise<JournalEntry> {
  return apiClient(`/api/v1/finance/journal-entries`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateJournalEntry(
  id: string,
  payload: UpdateJournalEntryPayload
): Promise<JournalEntry> {
  return apiClient(`/api/v1/finance/journal-entries/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function approveJournalEntry(id: string): Promise<JournalEntry> {
  return apiClient(`/api/v1/finance/journal-entries/${id}/approve`, {
    method: "POST",
  });
}

export async function deleteJournalEntry(id: string): Promise<void> {
  return apiClient(`/api/v1/finance/journal-entries/${id}`, { method: "DELETE" });
}
