"use client";

import { useEffect, useState } from "react";
import { FileText, X, File } from "lucide-react";
import { Progress } from "@/components/ui/progress";
import type { FileItem } from "@/hooks/useFileUpload";
import { cn } from "@/lib/utils";

interface FileQueueListProps {
  files: FileItem[];
  onRemove: (id: string) => void;
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function FileIcon({ type }: { type: string }) {
  if (type === "application/pdf") return <FileText className="size-4 text-red-500" />;
  return <File className="size-4 text-blue-500" />;
}

function StatusBadge({ status, error }: { status: FileItem["status"]; error?: string }) {
  if (status === "success") return <span className="text-[11px] text-green-600 font-medium">Done</span>;
  if (status === "uploading") return <span className="text-[11px] text-blue-600 font-medium">Uploading…</span>;
  if (status === "validating") return <span className="text-[11px] text-amber-600 font-medium">Validating…</span>;
  if (status === "error") return <span className="text-[11px] text-destructive font-medium" title={error}>Error</span>;
  return <span className="text-[11px] text-muted-foreground font-medium">Ready</span>;
}

export function FileQueueList({ files, onRemove }: FileQueueListProps) {
  const [visibleIds, setVisibleIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (files.length === 0) return;
    const newIds = new Set(visibleIds);
    for (const f of files) newIds.add(f.id);
    setVisibleIds(newIds);
  }, [files]);

  if (files.length === 0) return null;

  return (
    <div className="space-y-1.5">
      <p className="text-xs text-muted-foreground font-medium">{files.length} file{files.length > 1 ? "s" : ""}</p>
      <div className="space-y-1">
        {files.map((item, index) => (
          <div
            key={item.id}
            className={cn(
              "flex items-center gap-3 rounded-lg border border-border bg-card px-3 py-2 transition-all duration-300 ease-out",
              item.status === "error" && "border-destructive/40 bg-destructive/5",
              item.status === "success" && "border-green-300/40 bg-green-50/40"
            )}
            style={{
              animation: `fadeIn 0.3s ease-out`,
              animationFillMode: "both",
              animationDelay: `${index * 50}ms`,
            }}
          >
            <FileIcon type={item.file.type} />

            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-2">
                <p className="text-xs text-foreground font-medium truncate">{item.name}</p>
                <StatusBadge status={item.status} error={item.error} />
              </div>
              <div className="flex items-center gap-2 mt-0.5">
                <span className="text-[11px] text-muted-foreground tabular-nums">{formatSize(item.size)}</span>
                {item.status === "uploading" && (
                  <span className="text-[11px] text-muted-foreground tabular-nums">{item.progress}%</span>
                )}
              </div>
              {(item.status === "uploading" || item.status === "validating") && (
                <Progress value={item.status === "validating" ? 0 : item.progress} className="mt-1 h-1.5" />
              )}
            </div>

            <button
              type="button"
              onClick={() => onRemove(item.id)}
              className="flex size-5 shrink-0 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
              aria-label={`Remove ${item.name}`}
            >
              <X className="size-3" />
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
