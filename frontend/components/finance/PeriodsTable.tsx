import React from "react";
import { Period } from "../../lib/period-types";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface PeriodsTableProps {
  periods: Period[];
  onActionMenu: (period: Period) => void;
}

export default function PeriodsTable({ periods, onActionMenu }: PeriodsTableProps) {
  return (
    <div className="overflow-x-auto rounded border border-[#cac6bb] bg-white -mx-5 sm:mx-0">
      <table className="w-full min-w-[640px]">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#f6f0d7]">
            <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Name</th>
            <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Start Date</th>
            <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">End Date</th>
            <th className="h-9 px-3 text-left text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Status</th>
            <th className="h-9 px-3 text-right text-[11px] font-semibold uppercase tracking-wider text-[#89986D]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {periods.map((p) => (
            <tr key={p.id} className="h-9 border-b border-[#e6e2df] hover:bg-[#f7f3f0] transition-colors">
              <td className="px-3 text-[13px] font-medium text-[#1c1b1a]">{p.name}</td>
              <td className="px-3 text-[13px] text-[#49473e] tabular-nums">{new Date(p.startDate).toLocaleDateString('en-CA')}</td>
              <td className="px-3 text-[13px] text-[#49473e] tabular-nums">{new Date(p.endDate).toLocaleDateString('en-CA')}</td>
              <td className="px-3">
                <span
                  className={cn(
                    "inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide",
                    p.status === "OPEN" 
                      ? "bg-[#d4e7ab] text-[#586838]" 
                      : "bg-[#e6e2df] text-[#49473e]"
                  )}
                >
                  {p.status}
                </span>
              </td>
              <td className="px-3 text-right">
                <button 
                  onClick={() => onActionMenu(p)}
                  className="text-[13px] font-medium text-[#625f4b] hover:text-[#706d59] hover:underline transition-colors"
                >
                  {p.status === "OPEN" ? "Close" : "Reopen"}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
