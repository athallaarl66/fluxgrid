"use client";

import { useState } from "react";
import { ChevronRight, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";
import { formatBalance, type ReportRow } from "@/lib/report-types";

interface StatementRowProps {
  row: ReportRow;
  showType?: "tb" | "pl" | "bs";
  onDrillDown: (row: ReportRow) => void;
}

export function StatementRow({ row, showType, onDrillDown }: StatementRowProps) {
  const [expanded, setExpanded] = useState(row.depth < 2);
  const hasChildren = row.children.length > 0;
  const isParent = hasChildren;

  return (
    <>
      <tr
        className={cn(
          "h-9 border-b border-[#E5DEBF] transition-colors hover:bg-[#F7F3F0]",
        )}
      >
        <td className="px-3 text-[13px]">
          <div
            className="flex items-center gap-1"
            style={{ paddingLeft: `${row.depth * 20}px` }}
          >
            {hasChildren ? (
              <button
                type="button"
                onClick={() => setExpanded(!expanded)}
                className="flex size-5 items-center justify-center rounded text-muted-foreground hover:bg-muted cursor-pointer"
              >
                {expanded ? (
                  <ChevronDown className="size-3.5" />
                ) : (
                  <ChevronRight className="size-3.5" />
                )}
              </button>
            ) : (
              <span className="inline-block size-5" />
            )}
            <span
              className={cn(
                "tabular-nums text-muted-foreground font-mono",
                isParent && "font-semibold text-foreground",
              )}
            >
              {row.code}
            </span>
            <span
              className={cn(
                "ml-1.5",
                isParent
                  ? "font-semibold text-foreground"
                  : "text-foreground",
              )}
            >
              {row.name}
            </span>
          </div>
        </td>
        {(showType === "tb") && (
          <>
            <td className="px-3 text-right text-[13px] tabular-nums text-foreground">
              {row.debit !== 0 ? formatBalance(row.debit) : ""}
            </td>
            <td className="px-3 text-right text-[13px] tabular-nums text-foreground">
              {row.credit !== 0 ? formatBalance(row.credit) : ""}
            </td>
          </>
        )}
        <td
          className={cn(
            "px-3 text-right text-[13px] tabular-nums",
            isParent ? "font-semibold text-foreground" : "text-foreground",
          )}
        >
          <button
            type="button"
            onClick={() => onDrillDown(row)}
            className="hover:underline cursor-pointer"
          >
            {row.balance !== 0 ? formatBalance(row.balance) : "0"}
          </button>
        </td>
      </tr>
      {expanded &&
        hasChildren &&
        row.children.map((child) => (
          <StatementRow
            key={child.accountId}
            row={child}
            showType={showType}
            onDrillDown={onDrillDown}
          />
        ))}
    </>
  );
}
