"use client";

import { useState } from "react";
import { useAdminUsers, useDeleteUser, type AdminUser } from "@/hooks/useAdmin";
import { UserFormModal } from "./UserFormModal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { UserPlus, Pencil, Trash2, ChevronLeft, ChevronRight, Shield } from "lucide-react";

export function UserTable() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useAdminUsers({ search: search || undefined, page, pageSize: 10 });
  const deleteUser = useDeleteUser();

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  const users = data?.users ?? [];
  const total = data?.total ?? 0;
  const totalPages = Math.ceil(total / 10);

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <Input
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          placeholder="Search users..."
          className="h-8 w-56 border-border bg-card text-sm"
        />
        <div className="flex-1" />
        <Button
          size="sm"
          onClick={() => setShowCreate(true)}
          className="border-accent bg-accent text-accent-foreground font-semibold text-xs hover:brightness-[0.95]"
        >
          <UserPlus className="mr-1 size-3.5" />
          Add User
        </Button>
      </div>

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Name</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Email</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Role</th>
              <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Status</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-3 py-8 text-center text-sm text-muted-foreground">
                  No users found.
                </td>
              </tr>
            ) : (
              users.map((u) => (
                <tr key={u.id} className="border-b border-border last:border-0 hover:bg-muted/30">
                  <td className="px-3 py-2 font-medium text-foreground">{u.name}</td>
                  <td className="px-3 py-2 text-muted-foreground">{u.email}</td>
                  <td className="px-3 py-2">
                    {u.roles.length > 0 ? (
                      <span className="inline-flex items-center gap-1 rounded bg-accent/50 px-1.5 py-0.5 text-[11px] font-medium text-accent-foreground">
                        <Shield className="size-2.5" />
                        {u.roles[0]}
                      </span>
                    ) : (
                      <span className="text-muted-foreground/60 text-xs">—</span>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <span className={`text-xs font-medium ${u.isActive ? "text-green-600" : "text-muted-foreground"}`}>
                      {u.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => setEditing(u)}
                        className="p-1 rounded text-muted-foreground hover:text-foreground hover:bg-muted cursor-pointer"
                      >
                        <Pencil className="size-3.5" />
                      </button>
                      <button
                        onClick={() => {
                          if (confirm(`Deactivate ${u.name}?`)) deleteUser.mutate(u.id);
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

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>Page {page} of {totalPages} ({total} users)</span>
          <div className="flex gap-1">
            <Button
              variant="outline"
              size="sm"
              className="h-7 border-border text-muted-foreground"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="size-3.5" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-7 border-border text-muted-foreground"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              <ChevronRight className="size-3.5" />
            </Button>
          </div>
        </div>
      )}

      {(showCreate || editing) && (
        <UserFormModal
          user={editing}
          onClose={() => { setEditing(null); setShowCreate(false); }}
        />
      )}
    </div>
  );
}
