"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Building2 } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useDepartmentList, useDeleteDepartment } from "@/hooks/useDepartments";
import { DepartmentTable } from "@/components/hr/DepartmentTable";
import { DepartmentFormModal } from "@/components/hr/DepartmentFormModal";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { Department } from "@/lib/hr-types";

export default function DepartmentsPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const { data: departments, isLoading, error } = useDepartmentList();
  const deleteMutation = useDeleteDepartment();
  const [showFormModal, setShowFormModal] = useState(false);
  const [editingDept, setEditingDept] = useState<Department | undefined>(undefined);

  if (!authLoading && !user) {
    router.push("/login?redirect=/hr/departments");
  }

  if (authLoading) {
    return (
      <div className="p-5 space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-8 w-full max-w-sm" />
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) return null;

  function handleEdit(dept: Department) {
    setEditingDept(dept);
    setShowFormModal(true);
  }

  function handleDelete(dept: Department) {
    if (window.confirm(`Delete "${dept.name}"? This action cannot be undone.`)) {
      deleteMutation.mutate(dept.id);
    }
  }

  function handleCloseForm() {
    setShowFormModal(false);
    setEditingDept(undefined);
  }

  return (
    <div className="p-5 space-y-4 animate-fade-in">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
            <Building2 className="size-5 text-accent-foreground" />
          </div>
          <div>
            <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Departments</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">
              {departments ? `${departments.length} departments` : "Manage organizational units"}
            </p>
          </div>
        </div>
        <Button size="sm" onClick={() => setShowFormModal(true)}>
          Add Department
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-9 w-full" />
          ))}
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm text-destructive font-medium">Failed to load departments</p>
          <p className="text-xs text-muted-foreground mt-1">Please try again later</p>
        </div>
      ) : (
        <DepartmentTable
          departments={departments || []}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />
      )}

      {showFormModal && (
        <DepartmentFormModal
          department={editingDept}
          onClose={handleCloseForm}
        />
      )}
    </div>
  );
}
