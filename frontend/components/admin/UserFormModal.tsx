"use client";

import { useState } from "react";
import { useCreateUser, useUpdateUser, useAdminRoles, type AdminUser } from "@/hooks/useAdmin";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { X, Save, UserPlus } from "lucide-react";

interface UserFormModalProps {
  user?: AdminUser | null;
  onClose: () => void;
}

export function UserFormModal({ user, onClose }: UserFormModalProps) {
  const isEdit = !!user;
  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const { data: roles } = useAdminRoles();

  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState(user?.roles[0] ?? "");
  const [error, setError] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");

    if (isEdit) {
      updateUser.mutate(
        { id: user!.id, name, email, password: password || undefined, role: role || undefined },
        {
          onSuccess: () => onClose(),
          onError: (err) => setError((err as Error).message),
        },
      );
    } else {
      if (!password) {
        setError("Password is required for new users.");
        return;
      }
      createUser.mutate(
        { name, email, password, role: role || undefined },
        {
          onSuccess: () => onClose(),
          onError: (err) => setError((err as Error).message),
        },
      );
    }
  }

  const isPending = createUser.isPending || updateUser.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="w-full max-w-md rounded-xl border border-border bg-background p-5 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-foreground">
            {isEdit ? "Edit User" : "Create User"}
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
              placeholder="Full name"
              className="h-8 border-border bg-card text-sm"
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Email</label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="user@email.com"
              className="h-8 border-border bg-card text-sm"
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">
              {isEdit ? "New Password (leave blank to keep)" : "Password"}
            </label>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={isEdit ? "••••••••" : "Min 8 characters"}
              className="h-8 border-border bg-card text-sm"
              required={!isEdit}
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Role</label>
            <select
              value={role}
              onChange={(e) => setRole(e.target.value)}
              className="flex h-8 w-full rounded border border-border bg-card px-2 text-sm text-foreground"
            >
              <option value="">No role</option>
              {roles?.map((r) => (
                <option key={r.id} value={r.name}>
                  {r.name}
                </option>
              ))}
            </select>
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
              {isEdit ? <Save className="mr-1 size-3.5" /> : <UserPlus className="mr-1 size-3.5" />}
              {isPending ? "Saving..." : isEdit ? "Save" : "Create"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
