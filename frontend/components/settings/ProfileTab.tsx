"use client";

import { useState } from "react";
import { useProfile, useUpdateProfile } from "@/hooks/useProfile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Save } from "lucide-react";

export function ProfileTab() {
  const { data: profile, isLoading } = useProfile();
  const updateProfile = useUpdateProfile();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [saved, setSaved] = useState(false);
  const initialized = useState(false);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-9 w-24" />
      </div>
    );
  }

  if (profile && !initialized[0]) {
    initialized[0] = true;
    setName(profile.name);
    setEmail(profile.email);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaved(false);
    updateProfile.mutate(
      { name, email },
      {
        onSuccess: () => setSaved(true),
      },
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <label className="text-sm font-medium text-foreground">Name</label>
        <Input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Your name"
          className="border-border bg-card"
        />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium text-foreground">Email</label>
        <Input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="your@email.com"
          className="border-border bg-card"
        />
      </div>
      <div className="flex items-center gap-3">
        <Button
          type="submit"
          size="sm"
          disabled={updateProfile.isPending}
          className="border-accent bg-accent text-accent-foreground font-semibold hover:brightness-[0.95]"
        >
          <Save className="mr-1.5 size-3.5" />
          {updateProfile.isPending ? "Saving..." : "Save Changes"}
        </Button>
        {saved && (
          <span className="text-xs text-muted-foreground">Profile updated.</span>
        )}
        {updateProfile.isError && (
          <span className="text-xs text-destructive">
            {(updateProfile.error as Error).message}
          </span>
        )}
      </div>
    </form>
  );
}
