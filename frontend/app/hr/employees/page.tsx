"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Users } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useEmployeeList } from "@/hooks/useEmployees";
import { useDepartmentList } from "@/hooks/useDepartments";
import { EmployeeTable } from "@/components/hr/EmployeeTable";
import { EmployeeGrid } from "@/components/hr/EmployeeGrid";
import { EmployeeToolbar } from "@/components/hr/EmployeeToolbar";
import { EmployeeFormModal } from "@/components/hr/EmployeeFormModal";
import { Skeleton } from "@/components/ui/skeleton";

export default function EmployeeDirectoryPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [departmentFilter, setDepartmentFilter] = useState("");
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<"table" | "grid">("table");
  const [showFormModal, setShowFormModal] = useState(false);
  const pageSize = 20;

  const { data: deptData } = useDepartmentList();
  const { data, isLoading, error } = useEmployeeList({
    search: search || undefined,
    departmentId: departmentFilter || undefined,
    status: statusFilter || undefined,
    page,
    pageSize,
  });

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/employees");
  }

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-full max-w-sm" />
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <Users className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Employee Directory</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {data ? `${data.total} employees` : "Manage employee records"}
          </p>
        </div>
      </div>

      <EmployeeToolbar
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        statusFilter={statusFilter}
        onStatusFilterChange={(v) => { setStatusFilter(v); setPage(1); }}
        departmentFilter={departmentFilter}
        onDepartmentFilterChange={(v) => { setDepartmentFilter(v); setPage(1); }}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        departments={deptData || []}
        onAddEmployee={() => setShowFormModal(true)}
      />

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load employees</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : data && data.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <Users className="size-12 text-muted-foreground/40 mb-3" />
          <p className="text-sm font-medium text-foreground">No employees match your search</p>
          <p className="text-xs text-muted-foreground mt-1">Try clearing your filters</p>
        </div>
      ) : data ? (
        <>
          {viewMode === "table" ? (
            <EmployeeTable employees={data.items} />
          ) : (
            <EmployeeGrid employees={data.items} />
          )}

          {data.total > data.pageSize && (
            <div className="flex items-center justify-between pt-2">
              <p className="text-xs text-muted-foreground">
                Page {data.page} of {Math.ceil(data.total / data.pageSize)} ({data.total} total)
              </p>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors"
                >
                  Previous
                </button>
                <button
                  type="button"
                  disabled={page >= Math.ceil(data.total / data.pageSize)}
                  onClick={() => setPage((p) => p + 1)}
                  className="h-7 rounded border border-border px-2 text-xs text-foreground disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed hover:bg-muted transition-colors"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      ) : null}

      {showFormModal && (
        <EmployeeFormModal
          onClose={() => setShowFormModal(false)}
        />
      )}
    </div>
  );
}
