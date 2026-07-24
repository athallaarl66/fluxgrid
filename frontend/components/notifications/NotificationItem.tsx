"use client";

import { useNotifications, type NotificationItem } from "@/hooks/useNotifications";

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return "Just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export function NotificationItemRow({
  notification,
}: {
  notification: NotificationItem;
}) {
  return (
    <div
      className={`px-3 py-2.5 border-b border-border last:border-0 ${
        notification.isRead ? "opacity-60" : ""
      }`}
    >
      <div className="flex items-start gap-2">
        {!notification.isRead && (
          <span className="mt-1.5 size-1.5 rounded-full bg-accent shrink-0" />
        )}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-foreground truncate">
            {notification.title}
          </p>
          <p className="text-xs text-muted-foreground line-clamp-2 mt-0.5">
            {notification.body}
          </p>
          <p className="text-[10px] text-muted-foreground/60 mt-1">
            {timeAgo(notification.createdAt)}
          </p>
        </div>
      </div>
    </div>
  );
}
