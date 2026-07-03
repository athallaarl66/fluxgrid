"use client";

import { useState, useCallback, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";
import {
  useCoaTree,
  useCreateAccount,
  useUpdateAccount,
  useDeactivateAccount,
} from "@/hooks/useCoa";
import { CoaTreeView } from "@/components/finance/CoaTreeView";
import { CoaMobileList } from "@/components/finance/CoaMobileList";
import { CoaToolbar } from "@/components/finance/CoaToolbar";
import { AccountFormModal } from "@/components/finance/AccountFormModal";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { RefreshCw, AlertTriangle } from "lucide-react";
import { cn } from "@/lib/utils";
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from "@/lib/coa-types";

export default function ChartOfAccountsPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();

  const [searchQuery, setSearchQuery] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<AccountResponse | null>(
    null,
  );
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const {
    data: accounts,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
  } = useCoaTree();

  const createMutation = useCreateAccount();
  const updateMutation = useUpdateAccount();
  const deactivateMutation = useDeactivateAccount();

  const authChecked = !authLoading;

  useEffect(() => {
    if (authChecked && !user) {
      router.push("/login?redirect=/finance/chart-of-accounts");
    }
  }, [user, authChecked, router]);

  const handleSearch = useCallback((query: string) => {
    setSearchQuery(query);
  }, []);

  function handleNewAccount() {
    setEditingAccount(null);
    setModalOpen(true);
  }

  function handleEdit(account: AccountResponse) {
    setEditingAccount(account);
    setModalOpen(true);
  }

  function handleDeactivate(account: AccountResponse) {
    setDeletingId(account.id);
  }

  function handleSave(data: CreateAccountRequest | UpdateAccountRequest) {
    if (editingAccount) {
      updateMutation.mutate(
        { id: editingAccount.id, data: data as UpdateAccountRequest },
        {
          onSuccess: () => {
            setModalOpen(false);
            toast("Account updated successfully", "success");
          },
          onError: (err) => {
            toast(
              err instanceof ApiError ? err.message : "Failed to update account",
              "error",
            );
          },
        },
      );
    } else {
      createMutation.mutate(data as CreateAccountRequest, {
        onSuccess: () => {
          setModalOpen(false);
          toast("Account created successfully", "success");
        },
        onError: (err) => {
          toast(
            err instanceof ApiError ? err.message : "Failed to create account",
            "error",
          );
        },
      });
    }
  }

  function confirmDeactivate() {
    if (deletingId) {
      deactivateMutation.mutate(deletingId, {
        onSuccess: () => {
          setDeletingId(null);
          toast("Account deactivated successfully", "success");
        },
        onError: (err) => {
          toast(
            err instanceof ApiError ? err.message : "Failed to deactivate account",
            "error",
          );
        },
      });
    }
  }

  if (!authChecked) {
    return (
      <div className="flex items-center justify-center h-full">
        <Skeleton className="h-8 w-48" />
      </div>
    );
  }

  if (!user) return null;

  if (isError) {
    const isForbidden =
      error instanceof ApiError && error.status === 403;
    return (
      <div className="flex flex-col items-center justify-center gap-4 p-12">
        <AlertTriangle
          className={cn(
            "size-12",
            isForbidden ? "text-destructive" : "text-muted-foreground",
          )}
        />
        <h2 className="text-lg font-semibold text-foreground">
          {isForbidden
            ? "Access Denied"
            : "Failed to Load Chart of Accounts"}
        </h2>
        <p className="text-sm text-muted-foreground text-center max-w-md">
          {isForbidden
            ? "You do not have the required permission (finance.coa.read) to view this page."
            : error instanceof ApiError
              ? error.message
              : "An unexpected error occurred. Please try again."}
        </p>
        {isForbidden && (
          <p className="text-xs text-muted-foreground">
            Contact your administrator to request access.
          </p>
        )}
        {!isForbidden && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
          >
            <RefreshCw className="mr-1.5 size-3.5" />
            Retry
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="p-5 space-y-5">
      <div>
        <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
          Chart of Accounts
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage your financial account structure
        </p>
      </div>

      <CoaToolbar onSearch={handleSearch} onNewAccount={handleNewAccount} />

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton
              key={i}
              className="h-10 w-full rounded-lg"
              style={{ marginLeft: `${(i % 4) * 24}px`, width: `${100 - (i % 4) * 5}%` }}
            />
          ))}
        </div>
      ) : (
        <>
          <div className="hidden md:block">
            <CoaTreeView
              accounts={accounts ?? []}
              onEdit={handleEdit}
              onDeactivate={handleDeactivate}
              searchQuery={searchQuery}
            />
          </div>
          <div className="md:hidden">
            <CoaMobileList
              accounts={accounts ?? []}
              onEdit={handleEdit}
              onDeactivate={handleDeactivate}
              searchQuery={searchQuery}
            />
          </div>
        </>
      )}

      <AccountFormModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSave={handleSave}
        accounts={accounts ?? []}
        editingAccount={editingAccount}
        saving={createMutation.isPending || updateMutation.isPending}
      />

      {deletingId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-sm rounded-xl border border-border bg-card p-6 shadow-lg">
            <h3 className="text-lg font-semibold text-foreground">
              Deactivate Account
            </h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Are you sure you want to deactivate this account and all its
              children? This action can be reversed later.
            </p>
            <div className="flex items-center justify-end gap-2 mt-5">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setDeletingId(null)}
                disabled={deactivateMutation.isPending}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={confirmDeactivate}
                disabled={deactivateMutation.isPending}
              >
                {deactivateMutation.isPending
                  ? "Deactivating..."
                  : "Deactivate"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
