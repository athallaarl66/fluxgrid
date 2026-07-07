"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Eye, EyeOff } from "lucide-react";

export default function LoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        const data = await response.json();
        setError(data.message || "Invalid credentials");
        return;
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
              <div>
                <h1 className="text-lg font-semibold text-foreground">
                  FluxGrid ERP
                </h1>
                <p className="text-[12px] font-medium text-muted-foreground">
                  Industrial OS
                </p>
              </div>
            </div>
            <h2 className="text-xl font-semibold text-foreground">Welcome back</h2>
            <p className="text-sm text-muted-foreground mt-1">Sign in to your account</p>
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
              <label htmlFor="password" className="text-[11px] font-semibold leading-none text-muted-foreground">
                Password
              </label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Enter your password"
                  required
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 flex size-5 items-center justify-center text-muted-foreground hover:text-foreground transition-colors duration-200 cursor-pointer"
                  tabIndex={-1}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
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
              {loading ? "Signing in..." : "Sign In"}
            </Button>

            <div className="text-center">
              <button
                type="button"
                onClick={() => router.push("/login/change-password")}
                className="text-xs text-muted-foreground hover:text-foreground underline underline-offset-4 transition-colors duration-200 cursor-pointer"
              >
                Change password
              </button>
            </div>
          </form>

        </div>
      </div>
    </div>
  );
}
