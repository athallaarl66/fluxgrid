import React, { useState, useEffect, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Period } from "../../lib/period-types";
import { useClosePeriod, useValidateClose } from "../../hooks/usePeriods";

interface ClosePeriodDialogProps {
  period: Period;
  open: boolean;
  onClose: () => void;
}

export default function ClosePeriodDialog({ period, open, onClose }: ClosePeriodDialogProps) {
  const [confirmText, setConfirmText] = useState("");
  const { data: canClose, isLoading: isValidating } = useValidateClose(period.id);
  const closeMutation = useClosePeriod();
  const inputRef = useRef<HTMLInputElement>(null);
  
  const canConfirm = confirmText.toUpperCase() === "CLOSE" && canClose && !isValidating;

  useEffect(() => {
    if (open && inputRef.current) {
      inputRef.current.focus();
    }
  }, [open]);

  const handleConfirm = () => {
    closeMutation.mutate({ id: period.id }, {
      onSuccess: () => {
        onClose();
        setConfirmText("");
      }
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Escape") {
      onClose();
    } else if (e.key === "Enter" && canConfirm) {
      handleConfirm();
    }
  };

  if (!open) return null;

  return (
    <div 
      className="fixed inset-0 bg-[#1c1b1a]/60 flex items-center justify-center z-50 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-labelledby="close-dialog-title"
    >
      <div 
        className="bg-white rounded-lg shadow-2xl max-w-md w-full p-6 border border-[#cac6bb]"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        <h2 id="close-dialog-title" className="text-lg font-semibold text-[#1c1b1a] mb-2 tracking-tight">Close Period</h2>
        <p className="text-[13px] text-[#49473e] mb-4 leading-relaxed">
          Please type <strong className="font-semibold text-[#1c1b1a]">CLOSE</strong> to confirm closing period <strong className="font-semibold text-[#1c1b1a]">{period.name}</strong>.
        </p>
        
        {isValidating && (
          <div className="mb-4 text-[13px] text-[#49473e]">Validating...</div>
        )}
        
        {!isValidating && !canClose && (
          <div className="mb-4 p-3 bg-[#ffdad6] border border-[#ba1a1a]/20 rounded flex items-start gap-2">
            <svg className="w-5 h-5 text-[#ba1a1a] flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span className="text-[13px] text-[#93000a] leading-relaxed">Period cannot be closed. There may be pending journal entries.</span>
          </div>
        )}
        
        <Input
          ref={inputRef}
          placeholder="Type CLOSE to confirm"
          value={confirmText}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setConfirmText(e.target.value)}
          className="mb-4 text-[13px]"
          aria-label="Confirmation input"
        />
        
        <div className="flex justify-end gap-2">
          <button 
            onClick={onClose}
            className="px-4 py-2 text-[13px] font-medium text-[#625f4b] hover:bg-[#f1edea] rounded transition-colors"
          >
            Cancel
          </button>
          <button
            disabled={!canConfirm || closeMutation.isPending}
            onClick={handleConfirm}
            className="px-4 py-2 text-[13px] font-medium bg-[#625f4b] text-white rounded hover:bg-[#706d59] disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {closeMutation.isPending ? "Closing…" : "Close Period"}
          </button>
        </div>
      </div>
    </div>
  );
}
