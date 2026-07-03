"use client";

import { useMemo } from "react";
import { CoaTreeItem } from "@/components/finance/CoaTreeItem";
import type { AccountResponse } from "@/lib/coa-types";

interface CoaTreeViewProps {
  accounts: AccountResponse[];
  onEdit: (account: AccountResponse) => void;
  onDeactivate: (account: AccountResponse) => void;
  searchQuery: string;
}

function matchesQuery(account: AccountResponse, query: string): boolean {
  const q = query.toLowerCase();
  return (
    account.code.toLowerCase().includes(q) ||
    account.name.toLowerCase().includes(q)
  );
}

function filterTree(
  accounts: AccountResponse[],
  query: string,
): AccountResponse[] {
  if (!query) return accounts;
  return accounts
    .map((acc) => {
      const children = acc.children ? filterTree(acc.children, query) : [];
      if (matchesQuery(acc, query) || children.length > 0) {
        return { ...acc, children };
      }
      return null;
    })
    .filter(Boolean) as AccountResponse[];
}

export function CoaTreeView({
  accounts,
  onEdit,
  onDeactivate,
  searchQuery,
}: CoaTreeViewProps) {
  const filtered = useMemo(
    () => filterTree(accounts, searchQuery),
    [accounts, searchQuery],
  );

  if (filtered.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <p className="text-sm font-medium text-muted-foreground">
          {searchQuery
            ? "No accounts match your search"
            : "No accounts yet"}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">
          {searchQuery
            ? "Try a different search term"
            : "Click 'New Account' to create your first account"}
        </p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-border bg-card">
      {filtered.map((account) => (
        <CoaTreeItem
          key={account.id}
          account={account}
          depth={0}
          onEdit={onEdit}
          onDeactivate={onDeactivate}
        />
      ))}
    </div>
  );
}
