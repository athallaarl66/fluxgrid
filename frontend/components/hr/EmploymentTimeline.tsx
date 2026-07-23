"use client";

import { formatDate } from "@/lib/date-utils";

interface TimelineEvent {
  date: string;
  type: string;
  description: string;
}

const EVENT_ICONS: Record<string, string> = {
  Hire: "●",
  Promotion: "▲",
  Transfer: "→",
  Termination: "■",
};

export function EmploymentTimeline({ events }: { events: TimelineEvent[] }) {
  if (events.length === 0) {
    return (
      <div className="py-8 text-center">
        <p className="text-xs text-muted-foreground">No employment history yet</p>
      </div>
    );
  }

  return (
    <div className="relative pl-6 space-y-4">
      {events.map((event, idx) => (
        <div key={idx} className="relative">
          <div className="absolute -left-6 top-0 flex size-5 items-center justify-center rounded-full border-2 border-border bg-card">
            <span className="text-[10px] text-muted-foreground">
              {EVENT_ICONS[event.type] || "•"}
            </span>
          </div>
          {idx < events.length - 1 && (
            <div className="absolute -left-[11.5px] top-5 bottom-0 w-px bg-border" />
          )}
          <div>
            <p className="text-xs text-muted-foreground">
              {formatDate(event.date, {
                day: "numeric",
                month: "long",
                year: "numeric",
              })}
            </p>
            <p className="text-sm text-foreground mt-0.5">
              <span className="font-medium">{event.type}:</span>{" "}
              {event.description}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}
