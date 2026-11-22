import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { employeeLaunchesApi } from "./employeeLaunchesApi";

export function useEmployeeLaunchUsageQuery(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.employeeLaunches.list(),
    queryFn: ({ signal }) => employeeLaunchesApi.list(signal),
    staleTime: 30_000,
    enabled,
  });
}
