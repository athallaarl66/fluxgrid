"use client";

import { useState, useCallback, useRef } from "react";
import { useUploadCv, useCreateCandidate } from "@/hooks/useRecruitment";

const ALLOWED_TYPES = ["application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"];
const MAX_FILE_SIZE = 5 * 1024 * 1024;
const MAX_FILES = 20;

export interface FileItem {
  id: string;
  file: File;
  name: string;
  size: number;
  type: string;
  status: "pending" | "validating" | "ready" | "uploading" | "success" | "error";
  progress: number;
  error?: string;
  fileHash?: string;
  presignedUrl?: string;
  objectKey?: string;
}

interface UseFileUploadReturn {
  files: FileItem[];
  addFiles: (newFiles: File[]) => void;
  removeFile: (id: string) => void;
  uploadAll: () => Promise<void>;
  isUploading: boolean;
  canUpload: boolean;
}

export function useFileUpload(): UseFileUploadReturn {
  const [files, setFiles] = useState<FileItem[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const getUploadUrl = useUploadCv();
  const createCandidate = useCreateCandidate();
  const abortRefs = useRef<Map<string, AbortController>>(new Map());

  const canUpload = files.length > 0 && files.every((f) => f.status === "ready" || f.status === "error") && !isUploading;

  const addFiles = useCallback((newFiles: File[]) => {
    setFiles((prev) => {
      const existing = [...prev];
      for (const file of newFiles) {
        if (existing.length >= MAX_FILES) break;

        let error: string | undefined;
        if (!ALLOWED_TYPES.includes(file.type)) {
          error = "Invalid file type. Only PDF and DOCX are allowed.";
        } else if (file.size > MAX_FILE_SIZE) {
          error = "File too large. Maximum size is 5MB.";
        }

        existing.push({
          id: crypto.randomUUID(),
          file,
          name: file.name,
          size: file.size,
          type: file.type,
          status: error ? "error" : "ready",
          progress: 0,
          error,
        });
      }
      return existing;
    });
  }, []);

  const removeFile = useCallback((id: string) => {
    const controller = abortRefs.current.get(id);
    if (controller) {
      controller.abort();
      abortRefs.current.delete(id);
    }
    setFiles((prev) => prev.filter((f) => f.id !== id));
  }, []);

  const uploadAll = useCallback(async () => {
    setIsUploading(true);

    const readyFiles = files.filter((f) => f.status === "ready");
    const uploads = readyFiles.map(async (item) => {
      setFiles((prev) => prev.map((f) => (f.id === item.id ? { ...f, status: "validating" as const } : f)));

      try {
        const fileBuffer = await item.file.arrayBuffer();
        const hashBuffer = await crypto.subtle.digest("SHA-256", fileBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const fileHash = hashArray.map((b) => b.toString(16).padStart(2, "0")).join("");

        const ext = item.file.type === "application/pdf" ? "pdf" : "docx";

        setFiles((prev) => prev.map((f) => (f.id === item.id ? { ...f, fileHash, status: "uploading" as const, progress: 0 } : f)));

        const urlResp = await getUploadUrl.mutateAsync({
          fileName: item.name,
          fileType: ext,
          fileSize: item.file.size,
          fileHash,
        });

        setFiles((prev) => prev.map((f) => (f.id === item.id ? { ...f, presignedUrl: urlResp.presignedUrl, objectKey: urlResp.objectKey } : f)));

        await new Promise<void>((resolve, reject) => {
          const xhr = new XMLHttpRequest();
          xhr.open("PUT", urlResp.presignedUrl);
          xhr.setRequestHeader("Content-Type", item.file.type);

          xhr.upload.onprogress = (e) => {
            if (e.lengthComputable) {
              const progress = Math.round((e.loaded / e.total) * 100);
              setFiles((prev) => prev.map((f) => (f.id === item.id ? { ...f, progress } : f)));
            }
          };

          xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) resolve();
            else reject(new Error(`Upload failed with status ${xhr.status}`));
          };

          xhr.onerror = () => reject(new Error("Upload failed"));

          xhr.send(item.file);
        });

        const contentType = item.file.type === "application/pdf" ? "pdf" : "docx";
        const baseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5020";
        await createCandidate.mutateAsync({
          name: item.name.replace(/\.[^/.]+$/, ""),
          email: `${item.name.replace(/\.[^/.]+$/, "").toLowerCase().replace(/\s+/g, ".")}@pending`,
          fileUrl: `${baseUrl}/api/v1/hr/storage/flexmng-cv/${urlResp.objectKey}`,
          fileHash,
          originalFilename: item.name,
          fileType: contentType,
          fileSizeBytes: item.file.size,
        });

        setFiles((prev) => prev.map((f) => (f.id === item.id ? { ...f, status: "success" as const, progress: 100 } : f)));
      } catch (err) {
        setFiles((prev) => prev.map((f) =>
          f.id === item.id
            ? { ...f, status: "error" as const, error: err instanceof Error ? err.message : "Upload failed. Please try again." }
            : f
        ));
      }
    });

    await Promise.allSettled(uploads);
    setIsUploading(false);
  }, [files, getUploadUrl, createCandidate]);

  return { files, addFiles, removeFile, uploadAll, isUploading, canUpload };
}
