import { useCallback, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { publicHolidaySettingsApi } from "./publicHolidaySettingsApi";

export function usePublicHolidaySettingsQuery(enabled = true) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: queryKeys.publicHolidaySettings.current(),
    queryFn: ({ signal }) => publicHolidaySettingsApi.get(signal),
    enabled,
    staleTime: 0,
  });

  useEffect(() => {
    if (!enabled) return;
    const refresh = () => {
      if (document.visibilityState !== "hidden") {
        queryClient.invalidateQueries({ queryKey: queryKeys.publicHolidaySettings.current() });
      }
    };
    const interval = window.setInterval(refresh, 60_000);
    window.addEventListener("focus", refresh);
    return () => {
      window.clearInterval(interval);
      window.removeEventListener("focus", refresh);
    };
  }, [enabled, queryClient]);

  const refetch = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: queryKeys.publicHolidaySettings.current() });
  }, [queryClient]);
  return { ...query, refetch };
}

export function useUpdatePublicHolidaySettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (showPublicHolidays: boolean) => publicHolidaySettingsApi.update(showPublicHolidays),
    onSuccess: data => {
      const queryKey = queryKeys.publicHolidaySettings.current();
      queryClient.cancelQuery(queryKey);
      queryClient.setQueryData(queryKey, data);
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
