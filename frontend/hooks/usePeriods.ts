import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Period } from "../lib/period-types";
import { fetchPeriods, validateClose, closePeriod, reopenPeriod, generatePeriods } from "../lib/period-api";

/** Fetch list of periods */
export const usePeriods = () => {
  return useQuery<Period[]>({
    queryKey: ["periods"],
    queryFn: fetchPeriods,
  });
};

/** Validate a period can be closed */
export const useValidateClose = (periodId: string) => {
  return useQuery<boolean>({
    queryKey: ["periodValidate", periodId],
    queryFn: () => validateClose(periodId),
    enabled: !!periodId,
  });
};

/** Close a period */
export const useClosePeriod = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: string }) => closePeriod(payload.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["periods"] });
    },
  });
};

/** Reopen a period */
export const useReopenPeriod = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: string; reason: string }) => reopenPeriod(payload.id, payload.reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["periods"] });
    },
  });
};

/** Generate missing periods */
export const useGeneratePeriods = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => generatePeriods(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["periods"] });
    },
  });
};
