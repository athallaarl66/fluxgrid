"use client";

import { useState } from "react";
import { useAdminRoles, useDeleteRole, type AdminRole } from "@/hooks/useAdmin";
import { RoleFormModal } from "./RoleFormModal";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ShieldPlus, Pencil, Trash2, Users } from "lucide-react";

export function RoleTable() {
  const [editing, setEditing] = useState<AdminRole | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const { data: roles, isLoading } = useAdminRoles();
  const deleteRole = useDeleteRole();

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-16 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-end">
        <Button
          size="sm"
          onClick={() => setShowCreate(true)}
          className="border-accent bg-accent text-accent-foreground font-semibold text-xs hover:brightness-[0.95]"
        >
          <ShieldPlus className="mr-1 size-3.5" />
          Add Role
        </Button>
      </div>

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Role</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Description</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Permissions</th>
              <th className="px-3 py-2 text-center text-xs font-medium text-muted-foreground">Users</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody>
            {(roles ?? []).length === 0 ? (
              <tr>
                <td colSpan={5} className="px-3 py-8 text-center text-sm text-muted-foreground">
                  No roles found.
                </td>
              </tr>
            ) : (
              (roles ?? []).map((r) => (
                <tr key={r.id} className="border-b border-border last:border-0 hover:bg-muted/30">
                  <td className="px-3 py-2 font-medium text-foreground">{r.name}</td>
                  <td className="px-3 py-2 text-muted-foreground text-xs">{r.description || "—"}</td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap gap-1">
                      {r.permissions.slice(0, 3).map((p) => (
                        <span key={p} className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">
                          {p}
                        </span>
                      ))}
                      {r.permissions.length > 3 && (
                        <span className="text-[10px] text-muted-foreground/60">
                          +{r.permissions.length - 3}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-3 py-2 text-center">
                    <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                      <Users className="size-3" />
                      {r.userCount}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => setEditing(r)}
                        className="p-1 rounded text-muted-foreground hover:text-foreground hover:bg-muted cursor-pointer"
                      >
                        <Pencil className="size-3.5" />
                      </button>
                      <button
                        onClick={() => {
                          if (r.userCount > 0) {
                            alert("Cannot delete role with assigned users.");
                            return;
                          }
                          if (confirm(`Delete role "${r.name}"?`)) deleteRole.mutate(r.id);
                        }}
                        className="p-1 rounded text-muted-foreground hover:text-destructive hover:bg-muted cursor-pointer"
                      >
                        <Trash2 className="size-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {(showCreate || editing) && (
        <RoleFormModal
          role={editing}
          onClose={() => { setEditing(null); setShowCreate(false); }}
        />
      )}
    </div>
  );
}
