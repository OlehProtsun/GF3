import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { SaveAvailabilityBindInput } from "@entities/availability-binds/model/types";
import { availabilityBindsApi } from "./availabilityBindsApi";

export const useAvailabilityBindsListQuery = () =>
  useQuery({
    queryKey: queryKeys.availabilityBinds.list(),
    // Keep the binds request alive through StrictMode remounts in dev.
    queryFn: () => availabilityBindsApi.list(),
  });

export const useAvailabilityActiveBindsQuery = () =>
  useQuery({
    queryKey: queryKeys.availabilityBinds.active(),
    // Same reason as the full list query: avoid aborting the request during dev remounts.
    queryFn: () => availabilityBindsApi.active(),
  });

export function useCreateAvailabilityBindMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SaveAvailabilityBindInput) => availabilityBindsApi.create(payload),
    onSuccess: created => {
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.byId(created.id) });
    },
  });
}

export function useUpdateAvailabilityBindMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveAvailabilityBindInput }) => availabilityBindsApi.update(id, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.byId(variables.id) });
    },
  });
}

export function useDeleteAvailabilityBindMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => availabilityBindsApi.remove(id),
    onSuccess: (_, deletedId) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.availabilityBinds.byId(deletedId) });
    },
  });
}
