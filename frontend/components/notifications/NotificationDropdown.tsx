"use client";

import { useNotifications, useMarkAllRead } from "@/hooks/useNotifications";
import { NotificationItemRow } from "./NotificationItem";
import { Button } from "@/components/ui/button";
import { CheckCheck } from "lucide-react";

export function NotificationDropdown() {
  const { data, isLoading } = useNotifications();
  const markAllRead = useMarkAllRead();

  const notifs = data?.notifications ?? [];
  const count = data?.count ?? 0;

  return (
    <div className="w-80 rounded-xl border border-border bg-popover shadow-lg overflow-hidden">
      <div className="flex items-center justify-between px-3 py-2.5 border-b border-border">
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-semibold text-foreground">Notifications</h3>
          {count > 0 && (
            <span className="rounded-full bg-accent px-1.5 py-0.5 text-[10px] font-semibold text-accent-foreground">
              {count}
            </span>
          )}
        </div>
        {count > 0 && (
          <Button
            variant="ghost"
            size="sm"
            className="h-6 text-xs text-muted-foreground hover:text-foreground"
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
          >
            <CheckCheck className="mr-1 size-3" />
            Mark all read
          </Button>
        )}
      </div>

      <div className="max-h-80 overflow-y-auto">
        {isLoading ? (
          <div className="px-3 py-6 text-center text-xs text-muted-foreground">
            Loading...
          </div>
        ) : notifs.length === 0 ? (
          <div className="px-3 py-6 text-center text-xs text-muted-foreground">
            No unread notifications
          </div>
        ) : (
          notifs.map((n) => (
            <NotificationItemRow key={n.id} notification={n} />
          ))
        )}
      </div>
    </div>
  );
}
