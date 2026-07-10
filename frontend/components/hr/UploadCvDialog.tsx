"use client";

import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropzoneArea } from "@/components/hr/DropzoneArea";
import { FileQueueList } from "@/components/hr/FileQueueList";
import { useFileUpload } from "@/hooks/useFileUpload";

interface UploadCvDialogProps {
  onClose: () => void;
}

export function UploadCvDialog({ onClose }: UploadCvDialogProps) {
  const { files, addFiles, removeFile, uploadAll, isUploading, canUpload } = useFileUpload();

  async function handleUploadAll() {
    await uploadAll();
  }

  const hasSuccess = files.some((f) => f.status === "success");
  const hasError = files.some((f) => f.status === "error");

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="relative w-full max-w-lg max-h-[90vh] overflow-y-auto rounded-xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">Upload Candidate CVs</h2>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        <div className="space-y-4">
          <DropzoneArea onFilesSelected={addFiles} disabled={isUploading} />

          <FileQueueList files={files} onRemove={removeFile} />

          {files.length > 0 && (
            <div className="flex items-center justify-end gap-2 pt-2">
              <Button type="button" variant="outline" size="sm" onClick={onClose}>
                Cancel
              </Button>
              <Button
                type="button"
                size="sm"
                disabled={!canUpload}
                onClick={handleUploadAll}
                title={!canUpload && hasError ? "Fix errors before uploading" : undefined}
              >
                {isUploading ? "Uploading..." : "Upload All"}
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
