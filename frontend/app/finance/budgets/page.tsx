"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { useToast } from "@/components/ui/toast";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Wallet, Plus } from "lucide-react";
import { FinanceNav } from "@/components/finance/FinanceNav";
import { BudgetTable } from "@/components/finance/BudgetTable";
import { BudgetFormModal } from "@/components/finance/BudgetFormModal";
import { BudgetVarianceReport } from "@/components/finance/BudgetVarianceReport";
import {
  useBudgets,
  useBudgetReport,
  useCreateBudget,
  useUpdateBudget,
  useDeleteBudget,
} from "@/hooks/useBudgets";
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from "@/lib/budget-types";
import { apiClient } from "@/lib/api-client";

interface AccountOption {
  id: string;
  code: string;
  name: string;
}

interface PeriodOption {
  id: string;
  name: string;
}

export default function BudgetsPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { toast } = useToast();
  const [showForm, setShowForm] = useState(false);
  const [editingBudget, setEditingBudget] = useState<BudgetResponse | null>(null);
  const [filterPeriod, setFilterPeriod] = useState<string>("");
  const [accounts, setAccounts] = useState<AccountOption[]>([]);
  const [periods, setPeriods] = useState<PeriodOption[]>([]);

  const { data, isLoading, isFetching, refetch } = useBudgets(
    filterPeriod ? { periodId: filterPeriod } : undefined,
  );
  const { data: report, isLoading: reportLoading } = useBudgetReport(filterPeriod || undefined);
  const createBudget = useCreateBudget();
  const updateBudget = useUpdateBudget();
  const deleteBudget = useDeleteBudget();

  useEffect(() => {
    if (!authLoading && !user) {
      router.push("/login?redirect=/finance/budgets");
    }
  }, [user, authLoading, router]);

  useEffect(() => {
    apiClient<AccountOption[]>("/api/v1/finance/chart-of-accounts?flat=true")
      .then((res) => setAccounts(Array.isArray(res) ? res : []))
      .catch(() => {});
    apiClient<PeriodOption[]>("/api/v1/finance/periods")
      .then((res) => {
        const list = Array.isArray(res) ? res : [];
        setPeriods(list);
        if (list.length > 0) setFilterPeriod(list[0].id);
      })
      .catch(() => {});
  }, []);

  function handleSave(data: CreateBudgetRequest | UpdateBudgetRequest) {
    if (editingBudget) {
      updateBudget.mutate(
        { id: editingBudget.id, data: data as UpdateBudgetRequest },
        {
          onSuccess: () => {
            toast("Budget updated", "success");
            setShowForm(false);
            setEditingBudget(null);
          },
          onError: (err: Error) => toast(err.message, "error"),
        },
      );
    } else {
      createBudget.mutate(data as CreateBudgetRequest, {
        onSuccess: () => {
          toast("Budget created", "success");
          setShowForm(false);
        },
        onError: (err: Error) => toast(err.message, "error"),
      });
    }
  }

  function handleEdit(budget: BudgetResponse) {
    setEditingBudget(budget);
    setShowForm(true);
  }

  function handleDelete(budget: BudgetResponse) {
    if (!confirm("Delete this budget?")) return;
    deleteBudget.mutate(budget.id, {
      onSuccess: () => toast("Budget deleted", "success"),
      onError: (err: Error) => toast(err.message, "error"),
    });
  }

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-9 rounded" />)}
        </div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Wallet className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Budget Management</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">Create and manage budgets per account and period</p>
        </div>
      </div>

      <FinanceNav />

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <label className="text-xs font-medium text-muted-foreground">Period</label>
          <select
            value={filterPeriod}
            onChange={(e) => setFilterPeriod(e.target.value)}
            className="h-8 rounded border border-border bg-card px-2 text-xs font-medium text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
          >
            {periods.map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
        </div>
        <Button size="sm" onClick={() => { setEditingBudget(null); setShowForm(true); }} className="h-8 cursor-pointer">
          <Plus className="size-3.5 mr-1" /> New Budget
        </Button>
      </div>

      <BudgetTable
        data={data}
        isLoading={isLoading}
        isFetching={isFetching}
        onRefresh={refetch}
        onEdit={handleEdit}
        onDelete={handleDelete}
      />

      <div>
        <h2 className="text-base font-semibold text-foreground mb-3">Budget vs Actual Report</h2>
        <BudgetVarianceReport data={report} isLoading={reportLoading} />
      </div>

      <BudgetFormModal
        open={showForm}
        onClose={() => { setShowForm(false); setEditingBudget(null); }}
        onSave={handleSave}
        editingBudget={editingBudget}
        saving={createBudget.isPending || updateBudget.isPending}
        accounts={accounts}
        periods={periods}
      />
    </div>
  );
}
