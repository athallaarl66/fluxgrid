"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Eye, EyeOff } from "lucide-react";

export default function ChangePasswordPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [showOld, setShowOld] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await fetch("/api/auth/change-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username,
          oldPassword,
          newPassword,
          confirmNewPassword,
        }),
      });

      const data = await response.json();

      if (!response.ok) {
        setError(data.message || "Password change failed");
        return;
      }

      if (data.token) {
        document.cookie = `token=${data.token}; path=/; max-age=${60 * 60 * 24}; SameSite=Lax`;
      }

      router.push("/dashboard");
    } catch {
      setError("Connection error. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen bg-background">
      <div className="flex w-full items-center justify-center px-6">
        <div className="w-full max-w-sm">
          <div className="mb-8">
            <div className="flex items-center gap-3 mb-4">
              <div className="flex size-10 items-center justify-center rounded bg-primary">
                <span className="text-lg font-bold text-primary-foreground">F</span>
              </div>
              <div className="text-sm text-muted-foreground">
                <p className="text-xs font-medium">Change your password</p>
              </div>
            </div>
            <h2 className="text-xl font-semibold text-foreground">Set a new password</h2>
            <p className="text-sm text-muted-foreground mt-1">
              Your administrator has required you to change your password before continuing.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label htmlFor="username" className="text-[11px] font-semibold leading-none text-muted-foreground">
                Username
              </label>
              <Input
                id="username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Enter your username"
                required
              />
            </div>
            <div className="space-y-1.5">
              <label htmlFor="oldPassword" className="text-[11px] font-semibold leading-none text-muted-foreground">
                Current Password
              </label>
              <div className="relative">
                <Input
                  id="oldPassword"
                  type={showOld ? "text" : "password"}
                  value={oldPassword}
                  onChange={(e) => setOldPassword(e.target.value)}
                  placeholder="Enter current password"
                  required
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowOld(!showOld)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 flex size-5 items-center justify-center text-muted-foreground hover:text-foreground transition-colors duration-200 cursor-pointer"
                  tabIndex={-1}
                  aria-label={showOld ? "Hide password" : "Show password"}
                >
                  {showOld ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
            </div>
            <div className="space-y-1.5">
              <label htmlFor="newPassword" className="text-[11px] font-semibold leading-none text-muted-foreground">
                New Password
              </label>
              <div className="relative">
                <Input
                  id="newPassword"
                  type={showNew ? "text" : "password"}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="8+ chars, upper, lower, digit, special"
                  required
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowNew(!showNew)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 flex size-5 items-center justify-center text-muted-foreground hover:text-foreground transition-colors duration-200 cursor-pointer"
                  tabIndex={-1}
                  aria-label={showNew ? "Hide password" : "Show password"}
                >
                  {showNew ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
            </div>
            <div className="space-y-1.5">
              <label htmlFor="confirmNewPassword" className="text-[11px] font-semibold leading-none text-muted-foreground">
                Confirm New Password
              </label>
              <Input
                id="confirmNewPassword"
                type="password"
                value={confirmNewPassword}
                onChange={(e) => setConfirmNewPassword(e.target.value)}
                placeholder="Re-enter new password"
                required
              />
            </div>
            {error && (
              <div className="rounded border border-destructive bg-destructive/10 px-3 py-2 text-xs font-medium text-destructive">
                {error}
              </div>
            )}
            <Button
              type="submit"
              className="w-full h-9 font-semibold cursor-pointer"
              disabled={loading}
            >
              {loading ? "Changing password..." : "Change Password"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
