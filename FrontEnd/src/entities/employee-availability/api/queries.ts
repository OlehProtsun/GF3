import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { employeeAvailabilityApi } from "./employeeAvailabilityApi";
import type { SaveEmployeeAvailabilityPayload } from "../model/types";

export const useEmployeeAvailabilityListQuery = () =>
  useQuery({
    queryKey: queryKeys.employeeAvailability.list(),
    staleTime: 60_000,
    queryFn: ({ signal }) => employeeAvailabilityApi.list(signal),
  });

export const useEmployeeAvailabilityByIdQuery = (id: number | null) =>
  useQuery({
    queryKey: queryKeys.employeeAvailability.byId(id ?? 0),
    enabled: id !== null,
    queryFn: ({ signal }) => employeeAvailabilityApi.byId(id as number, signal),
  });

export function useSaveEmployeeAvailabilityMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveEmployeeAvailabilityPayload }) =>
      employeeAvailabilityApi.saveSlots(id, payload),
    onSuccess: (availability) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.employeeAvailability.all });
      queryClient.setQueryData(queryKeys.employeeAvailability.byId(availability.id), availability);
    },
  });
}
