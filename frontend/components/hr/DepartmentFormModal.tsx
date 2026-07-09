"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { useDepartmentList, useCreateDepartment, useUpdateDepartment } from "@/hooks/useDepartments";
import { useToast } from "@/components/ui/toast";
import { Button } from "@/components/ui/button";
import type { Department } from "@/lib/hr-types";

interface DepartmentFormModalProps {
  onClose: () => void;
  department?: Department;
}

export function DepartmentFormModal({ onClose, department }: DepartmentFormModalProps) {
  const { toast } = useToast();
  const { data: deptData } = useDepartmentList();
  const createMutation = useCreateDepartment();
  const updateMutation = useUpdateDepartment(department?.id || "");
  const isEditing = !!department;

  const [name, setName] = useState(department?.name || "");
  const [parentId, setParentId] = useState(department?.parentId || "");
  const [submitting, setSubmitting] = useState(false);

  const departments = deptData || [];
  const availableParents = departments.filter((d) => d.id !== department?.id);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    setSubmitting(true);

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          name: name.trim(),
          parentId: parentId || null,
        });
        toast("Department updated", "success");
      } else {
        await createMutation.mutateAsync({
          name: name.trim(),
          parentId: parentId || undefined,
        });
        toast("Department created", "success");
      }
      onClose();
    } catch {
      toast(isEditing ? "Failed to update department" : "Failed to create department", "error");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="relative w-full max-w-md rounded-xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">
            {isEditing ? "Edit Department" : "Add Department"}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-xs text-muted-foreground">Department Name</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              placeholder="e.g. Engineering"
              className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            />
          </div>

          <div>
            <label className="text-xs text-muted-foreground">Parent Department (optional)</label>
            <select
              value={parentId}
              onChange={(e) => setParentId(e.target.value)}
              className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 cursor-pointer"
            >
              <option value="">No Parent (Top Level)</option>
              {availableParents.map((dept) => (
                <option key={dept.id} value={dept.id}>{dept.name}</option>
              ))}
            </select>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={submitting || !name.trim()}>
              {submitting ? "Saving..." : isEditing ? "Update" : "Create"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
