"use client";

import { useState } from "react";
import { useProfile, useChangePassword } from "@/hooks/useProfile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { KeyRound } from "lucide-react";

export function SecurityTab() {
  const { data: profile, isLoading } = useProfile();
  const changePassword = useChangePassword();
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState<{
    type: "success" | "error";
    text: string;
  } | null>(null);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-9 w-24" />
      </div>
    );
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setMessage(null);
    if (newPassword !== confirmPassword) {
      setMessage({ type: "error", text: "New passwords do not match." });
      return;
    }
    changePassword.mutate(
      {
        username: profile?.name ?? "",
        oldPassword,
        newPassword,
        confirmNewPassword: confirmPassword,
      },
      {
        onSuccess: () => {
          setMessage({ type: "success", text: "Password changed successfully." });
          setOldPassword("");
          setNewPassword("");
          setConfirmPassword("");
        },
        onError: (err) => {
          setMessage({ type: "error", text: (err as Error).message });
        },
      },
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <label className="text-sm font-medium text-foreground">
          Current Password
        </label>
        <Input
          type="password"
          value={oldPassword}
          onChange={(e) => setOldPassword(e.target.value)}
          placeholder="Enter current password"
          className="border-border bg-card"
          required
        />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium text-foreground">
          New Password
        </label>
        <Input
          type="password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          placeholder="Enter new password"
          className="border-border bg-card"
          required
        />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium text-foreground">
          Confirm New Password
        </label>
        <Input
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          placeholder="Confirm new password"
          className="border-border bg-card"
          required
        />
      </div>
      <div className="flex items-center gap-3">
        <Button
          type="submit"
          size="sm"
          disabled={changePassword.isPending}
          className="border-accent bg-accent text-accent-foreground font-semibold hover:brightness-[0.95]"
        >
          <KeyRound className="mr-1.5 size-3.5" />
          {changePassword.isPending ? "Changing..." : "Change Password"}
        </Button>
        {message && (
          <span
            className={`text-xs ${
              message.type === "success"
                ? "text-muted-foreground"
                : "text-destructive"
            }`}
          >
            {message.text}
          </span>
        )}
      </div>
    </form>
  );
}
