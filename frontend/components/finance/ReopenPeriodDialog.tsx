import React, { useState, useEffect, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Period } from "../../lib/period-types";
import { useReopenPeriod } from "../../hooks/usePeriods";

interface ReopenPeriodDialogProps {
  period: Period;
  open: boolean;
  onClose: () => void;
}

export default function ReopenPeriodDialog({ period, open, onClose }: ReopenPeriodDialogProps) {
  const [reason, setReason] = useState("");
  const reopenMutation = useReopenPeriod();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const isValid = reason.trim().length >= 10;

  useEffect(() => {
    if (open && textareaRef.current) {
      textareaRef.current.focus();
    }
  }, [open]);

  const handleConfirm = () => {
    reopenMutation.mutate({ id: period.id, reason }, {
      onSuccess: () => {
        onClose();
        setReason("");
      }
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Escape") {
      onClose();
    }
  };

  if (!open) return null;

  return (
    <div 
      className="fixed inset-0 bg-[#1c1b1a]/60 flex items-center justify-center z-50 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-labelledby="reopen-dialog-title"
    >
      <div 
        className="bg-white rounded-lg shadow-2xl max-w-md w-full p-6 border border-[#cac6bb]"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        <h2 id="reopen-dialog-title" className="text-lg font-semibold text-[#1c1b1a] mb-2 tracking-tight">Reopen Period</h2>
        <p className="text-[13px] text-[#49473e] mb-4 leading-relaxed">
          Provide a reason for reopening period <strong className="font-semibold text-[#1c1b1a]">{period.name}</strong> (minimum 10 characters).
        </p>
        
        <div className="mb-4 p-3 bg-[#ffdad6] border border-[#ba1a1a]/20 rounded flex items-start gap-2">
          <svg className="w-5 h-5 text-[#ba1a1a] flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <p className="text-[13px] text-[#93000a] leading-relaxed">
            Warning: Reopening a closed period is an audited action and should only be done when necessary.
          </p>
        </div>
        
        <textarea
          ref={textareaRef}
          placeholder="Reason for reopening..."
          value={reason}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setReason(e.target.value)}
          rows={4}
          className="w-full border border-[#cac6bb] rounded px-3 py-2 text-[13px] text-[#1c1b1a] placeholder:text-[#7a776d] focus:outline-none focus:ring-2 focus:ring-[#625f4b] focus:border-transparent mb-2"
          aria-label="Reason for reopening"
        />
        <div className="text-[11px] text-[#49473e] mb-4">
          {reason.length}/10 characters minimum
        </div>
        
        <div className="flex justify-end gap-2">
          <button 
            onClick={onClose}
            className="px-4 py-2 text-[13px] font-medium text-[#625f4b] hover:bg-[#f1edea] rounded transition-colors"
          >
            Cancel
          </button>
          <button 
            disabled={!isValid || reopenMutation.isPending} 
            onClick={handleConfirm}
            className="px-4 py-2 text-[13px] font-medium bg-[#ba1a1a] text-white rounded hover:bg-[#93000a] disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {reopenMutation.isPending ? "Reopening…" : "Reopen Period"}
          </button>
        </div>
      </div>
    </div>
  );
}
