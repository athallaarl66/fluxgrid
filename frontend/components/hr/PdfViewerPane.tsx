"use client";

import { useState, useCallback } from "react";
import { Document, Page } from "react-pdf";
import "react-pdf/dist/Page/AnnotationLayer.css";
import "react-pdf/dist/Page/TextLayer.css";
import { ChevronLeft, ChevronRight } from "lucide-react";

export function PdfViewerPane({ fileUrl }: { fileUrl: string | null }) {
  const [numPages, setNumPages] = useState(0);
  const [page, setPage] = useState(1);

  const onLoadSuccess = useCallback(({ numPages: n }: { numPages: number }) => {
    setNumPages(n);
  }, []);

  if (!fileUrl) {
    return (
      <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
        No CV file available
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      {numPages > 1 && (
        <div className="flex items-center justify-between px-3 py-1.5 border-b border-border bg-muted/30">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="size-6 inline-flex items-center justify-center rounded hover:bg-muted disabled:opacity-30 cursor-pointer"
          >
            <ChevronLeft className="size-3.5" />
          </button>
          <span className="text-[11px] text-muted-foreground tabular-nums">
            {page} / {numPages}
          </span>
          <button
            type="button"
            disabled={page >= numPages}
            onClick={() => setPage((p) => Math.min(numPages, p + 1))}
            className="size-6 inline-flex items-center justify-center rounded hover:bg-muted disabled:opacity-30 cursor-pointer"
          >
            <ChevronRight className="size-3.5" />
          </button>
        </div>
      )}
      <div className="flex-1 overflow-y-auto p-2 bg-[#F5F5F0]">
        <Document
          file={fileUrl}
          onLoadSuccess={onLoadSuccess}
          loading={
            <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
              Loading PDF...
            </div>
          }
          error={
            <div className="flex items-center justify-center h-full text-xs text-destructive">
              Failed to load PDF
            </div>
          }
        >
          <Page
            pageIndex={page - 1}
            width={Math.min(600, typeof window !== "undefined" ? window.innerWidth / 2.5 : 600)}
            renderTextLayer
            renderAnnotationLayer
          />
        </Document>
      </div>
    </div>
  );
}
