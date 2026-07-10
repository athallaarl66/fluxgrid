"use client";

import { useCallback, useState } from "react";
import { useDropzone } from "react-dropzone";
import { Upload } from "lucide-react";
import { cn } from "@/lib/utils";

interface DropzoneAreaProps {
  onFilesSelected: (files: File[]) => void;
  disabled?: boolean;
}

export function DropzoneArea({ onFilesSelected, disabled }: DropzoneAreaProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      setIsDragOver(false);
      if (acceptedFiles.length > 0) onFilesSelected(acceptedFiles);
    },
    [onFilesSelected]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      "application/pdf": [".pdf"],
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document": [".docx"],
    },
    maxSize: 5 * 1024 * 1024,
    disabled,
    onDragEnter: () => setIsDragOver(true),
    onDragLeave: () => setIsDragOver(false),
  });

  return (
    <div
      {...getRootProps()}
      className={cn(
        "flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-[#E5DEBF] bg-[#FDF8F5] px-6 py-8 text-center transition-all duration-200 cursor-pointer",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
        isDragActive && "border-solid border-2 border-[#C5D89D] bg-[#F6F0D7]",
        disabled && "pointer-events-none opacity-50"
      )}
      role="button"
      aria-label="Upload candidate CVs. Drop zone. Browse files."
    >
      <input {...getInputProps()} aria-hidden="true" />
      <div
        className={cn(
          "flex size-12 items-center justify-center rounded-full bg-[#F6F0D7] mb-3 transition-transform duration-200",
          isDragActive && "scale-105"
        )}
      >
        <Upload className="size-5 text-[#9CAB84]" />
      </div>
      <p className="text-sm font-medium text-foreground">
        Drag & drop CV files here
      </p>
      <p className="mt-1 text-xs text-muted-foreground">
        or <span className="text-[#9CAB84] underline underline-offset-2">browse files</span>
      </p>
      <p className="mt-2 text-[11px] text-muted-foreground">
        PDF, DOCX — Max 5MB per file — Up to 20 files
      </p>
    </div>
  );
}
