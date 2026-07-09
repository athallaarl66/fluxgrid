"use client";

import { useRef, useState, useCallback } from "react";
import type { OrgChartNode } from "@/lib/hr-types";
import { OrgChartNode as OrgChartNodeComponent } from "./OrgChartNode";

interface OrgChartTreeProps {
  nodes: OrgChartNode[];
}

export function OrgChartTree({ nodes }: OrgChartTreeProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [scale, setScale] = useState(1);
  const [translate, setTranslate] = useState({ x: 0, y: 0 });
  const isDragging = useRef(false);
  const lastPos = useRef({ x: 0, y: 0 });

  const zoom = useCallback((delta: number, cx: number, cy: number) => {
    setScale((prev) => {
      const next = Math.max(0.25, Math.min(3, prev - delta * 0.01));
      const rect = containerRef.current?.getBoundingClientRect();
      if (!rect) return next;
      const ox = (cx - rect.left - translate.x) / prev;
      const oy = (cy - rect.top - translate.y) / prev;
      setTranslate((t) => ({
        x: cx - rect.left - ox * next,
        y: cy - rect.top - oy * next,
      }));
      return next;
    });
  }, [translate]);

  const handleWheel = useCallback((e: React.WheelEvent) => {
    if (e.ctrlKey || e.metaKey) {
      e.preventDefault();
      zoom(e.deltaY, e.clientX, e.clientY);
    }
  }, [zoom]);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    isDragging.current = true;
    lastPos.current = { x: e.clientX - translate.x, y: e.clientY - translate.y };
  }, [translate]);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (!isDragging.current) return;
    setTranslate({
      x: e.clientX - lastPos.current.x,
      y: e.clientY - lastPos.current.y,
    });
  }, []);

  const handleMouseUp = useCallback(() => {
    isDragging.current = false;
  }, []);

  const resetView = useCallback(() => {
    setScale(1);
    setTranslate({ x: 0, y: 0 });
  }, []);

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => setScale((s) => Math.max(0.25, s - 0.1))}
          className="h-7 rounded border border-border px-2 text-xs text-foreground hover:bg-muted transition-colors cursor-pointer"
          aria-label="Zoom out"
        >
          −
        </button>
        <span className="text-xs text-muted-foreground tabular-nums w-10 text-center">
          {Math.round(scale * 100)}%
        </span>
        <button
          type="button"
          onClick={() => setScale((s) => Math.min(3, s + 0.1))}
          className="h-7 rounded border border-border px-2 text-xs text-foreground hover:bg-muted transition-colors cursor-pointer"
          aria-label="Zoom in"
        >
          +
        </button>
        <button
          type="button"
          onClick={resetView}
          className="h-7 rounded border border-border px-2 text-xs text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
        >
          Reset
        </button>
      </div>
      <div
        ref={containerRef}
        className="relative overflow-hidden rounded-lg border border-border bg-muted/20 h-[600px] cursor-grab active:cursor-grabbing select-none"
        onWheel={handleWheel}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
        role="tree"
        aria-label="Organization chart"
      >
        <div
          className="absolute inset-0 flex items-start justify-center transition-transform duration-75"
          style={{
            transform: `translate(${translate.x}px, ${translate.y}px) scale(${scale})`,
            transformOrigin: "0 0",
          }}
        >
          <div className="pt-12">
            {nodes.map((node) => (
              <OrgChartNodeComponent key={node.id} node={node} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
