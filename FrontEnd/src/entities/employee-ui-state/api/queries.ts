import { useCallback, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { EmployeeUiState } from "../model/types";
import { employeeUiStateApi } from "./employeeUiStateApi";

export const useEmployeeUiStateQuery = (enabled = true) => {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: queryKeys.employeeUiState.current(),
    queryFn: ({ signal }) => employeeUiStateApi.get(signal),
    enabled,
    staleTime: 15_000,
  });
  const refetch = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: queryKeys.employeeUiState.current() });
  }, [queryClient]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const intervalId = window.setInterval(refetch, 30_000);
    return () => window.clearInterval(intervalId);
  }, [enabled, refetch]);

  return { ...query, refetch };
};

export function useSetEmployeeSwapPinMutation() {
  const queryClient = useQueryClient();
  const queryKey = queryKeys.employeeUiState.current();

  return useMutation({
    mutationFn: async ({ swapId, pinned }: { swapId: number; pinned: boolean }) => {
      queryClient.cancelQuery(queryKey);
      const previous = queryClient.getQueryState<EmployeeUiState>(queryKey).data;
      const currentPinnedIds = previous?.pinnedSwapIds ?? [];
      const pinnedSwapIds = pinned
        ? [swapId, ...currentPinnedIds.filter(id => id !== swapId)]
        : currentPinnedIds.filter(id => id !== swapId);
      queryClient.setQueryData<EmployeeUiState>(queryKey, previous
        ? { ...previous, pinnedSwapIds }
        : { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds });

      try {
        await (pinned ? employeeUiStateApi.pinSwap(swapId) : employeeUiStateApi.unpinSwap(swapId));
      } catch (error) {
        queryClient.setQueryData<EmployeeUiState>(
          queryKey,
          previous ?? { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] },
        );
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
