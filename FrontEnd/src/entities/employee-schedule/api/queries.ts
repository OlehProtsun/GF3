import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { employeeScheduleApi } from "./employeeScheduleApi";

export const useEmployeeScheduleListQuery = () =>
  useQuery({
    queryKey: queryKeys.employeeSchedules.list(),
    staleTime: 60_000,
    queryFn: ({ signal }) => employeeScheduleApi.list(signal),
  });
