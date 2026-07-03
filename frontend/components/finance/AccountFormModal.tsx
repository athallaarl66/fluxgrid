"use client";

import { useState, useEffect } from "react";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Combobox } from "@/components/finance/Combobox";
import { cn } from "@/lib/utils";
import {
  ACCOUNT_TYPES,
  type AccountResponse,
  type AccountOption,
  type AccountType,
  type CreateAccountRequest,
  type UpdateAccountRequest,
} from "@/lib/coa-types";

interface AccountFormModalProps {
  open: boolean;
  onClose: () => void;
  onSave: (data: CreateAccountRequest | UpdateAccountRequest) => void;
  accounts: AccountResponse[];
  editingAccount?: AccountResponse | null;
  saving?: boolean;
}

function toOptions(accounts: AccountResponse[]): AccountOption[] {
  return accounts.flatMap((a) => [
    { id: a.id, code: a.code, name: a.name, type: a.type },
    ...(a.children ? toOptions(a.children) : []),
  ]);
}

export function AccountFormModal({
  open,
  onClose,
  onSave,
  accounts,
  editingAccount,
  saving,
}: AccountFormModalProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [parentId, setParentId] = useState<string | null>(null);
  const [type, setType] = useState<AccountType | "">("");
  const [isActive, setIsActive] = useState(true);

  const isEditing = !!editingAccount;

  useEffect(() => {
    if (editingAccount) {
      setCode(editingAccount.code);
      setName(editingAccount.name);
      setParentId(editingAccount.parentId);
      setType(editingAccount.type);
      setIsActive(editingAccount.isActive);
    } else {
      setCode("");
      setName("");
      setParentId(null);
      setType("");
      setIsActive(true);
    }
  }, [editingAccount, open]);

  function handleParentChange(id: string | null) {
    setParentId(id);
    if (id) {
      const parent = findAccount(accounts, id);
      if (parent) setType(parent.type);
    }
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!code.trim() || !name.trim()) return;

    if (isEditing) {
      onSave({
        code: code.trim(),
        name: name.trim(),
        parentId,
        type: type || undefined,
        isActive,
      } as UpdateAccountRequest);
    } else {
      onSave({
        code: code.trim(),
        name: name.trim(),
        parentId,
        type: type || undefined,
        isActive,
      } as CreateAccountRequest);
    }
  }

  if (!open) return null;

  const options = toOptions(accounts).filter(
    (o) => o.id !== editingAccount?.id,
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-foreground">
            {isEditing ? "Edit Account" : "New Account"}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-muted hover:text-foreground cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">
                Code *
              </label>
              <Input
                required
                placeholder="e.g. 1110"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                disabled={saving}
              />
            </div>
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">
                Type
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as AccountType)}
                disabled={!!parentId || saving}
                className={cn(
                  "h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm transition-colors",
                  "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
                  "disabled:opacity-50",
                )}
              >
                <option value="">Auto from parent</option>
                {ACCOUNT_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </select>
              {parentId && (
                <p className="text-[11px] text-muted-foreground">
                  Inherited from parent
                </p>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              Name *
            </label>
            <Input
              required
              placeholder="e.g. Cash in Bank"
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={saving}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              Parent Account
            </label>
            <Combobox
              options={options}
              value={parentId}
              onChange={handleParentChange}
              placeholder="None (top-level account)"
              disabled={saving}
            />
          </div>

          <div className="flex items-center gap-3">
            <label className="text-xs font-medium text-muted-foreground">
              Status
            </label>
            <button
              type="button"
              onClick={() => setIsActive(!isActive)}
              disabled={saving}
              className="cursor-pointer"
            >
              <Badge variant={isActive ? "default" : "secondary"}>
                {isActive ? "Active" : "Inactive"}
              </Badge>
            </button>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onClose}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={saving || !code.trim() || !name.trim()}>
              {saving ? "Saving..." : isEditing ? "Update" : "Create"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

function findAccount(accounts: AccountResponse[], id: string): AccountResponse | null {
  for (const a of accounts) {
    if (a.id === id) return a;
    if (a.children) {
      const found = findAccount(a.children, id);
      if (found) return found;
    }
  }
  return null;
}
