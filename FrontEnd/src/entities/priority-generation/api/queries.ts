import { useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { PriorityGenerationSettings } from "../model/types";
import { priorityGenerationSettingsApi } from "./priorityGenerationSettingsApi";

export function usePriorityGenerationSettingsQuery(enabled = true) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: queryKeys.priorityGenerationSettings.current(),
    queryFn: ({ signal }) => priorityGenerationSettingsApi.get(signal),
    enabled,
    staleTime: 0,
  });
  const refetch = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: queryKeys.priorityGenerationSettings.current() });
  }, [queryClient]);
  return { ...query, refetch };
}

export function useUpdatePriorityGenerationSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (settings: PriorityGenerationSettings) => priorityGenerationSettingsApi.update(settings),
    onSuccess: data => {
      const queryKey = queryKeys.priorityGenerationSettings.current();
      queryClient.cancelQuery(queryKey);
      queryClient.setQueryData(queryKey, data);
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
