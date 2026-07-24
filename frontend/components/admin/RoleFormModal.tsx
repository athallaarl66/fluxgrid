"use client";

import { useState } from "react";
import { useCreateRole, useUpdateRole, type AdminRole } from "@/hooks/useAdmin";
import { PermissionPicker } from "./PermissionPicker";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { X, Save, ShieldPlus } from "lucide-react";

interface RoleFormModalProps {
  role?: AdminRole | null;
  onClose: () => void;
}

export function RoleFormModal({ role, onClose }: RoleFormModalProps) {
  const isEdit = !!role;
  const createRole = useCreateRole();
  const updateRole = useUpdateRole();

  const [name, setName] = useState(role?.name ?? "");
  const [description, setDescription] = useState(role?.description ?? "");
  const [permissions, setPermissions] = useState<string[]>(role?.permissions ?? []);
  const [error, setError] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");

    if (isEdit) {
      updateRole.mutate(
        { id: role!.id, name, description: description || undefined, permissions },
        {
          onSuccess: () => onClose(),
          onError: (err) => setError((err as Error).message),
        },
      );
    } else {
      createRole.mutate(
        { name, description: description || undefined, permissions },
        {
          onSuccess: () => onClose(),
          onError: (err) => setError((err as Error).message),
        },
      );
    }
  }

  const isPending = createRole.isPending || updateRole.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="w-full max-w-lg rounded-xl border border-border bg-background p-5 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-foreground">
            {isEdit ? "Edit Role" : "Create Role"}
          </h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground cursor-pointer">
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Name</label>
            <Input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Warehouse Manager"
              className="h-8 border-border bg-card text-sm"
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Description</label>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Optional description"
              className="h-8 border-border bg-card text-sm"
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Permissions</label>
            <PermissionPicker selected={permissions} onChange={setPermissions} />
          </div>

          {error && <p className="text-xs text-destructive">{error}</p>}

          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="outline" size="sm" onClick={onClose} className="border-border text-muted-foreground">
              Cancel
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isPending}
              className="border-accent bg-accent text-accent-foreground font-semibold hover:brightness-[0.95]"
            >
              {isEdit ? <Save className="mr-1 size-3.5" /> : <ShieldPlus className="mr-1 size-3.5" />}
              {isPending ? "Saving..." : isEdit ? "Save" : "Create"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
