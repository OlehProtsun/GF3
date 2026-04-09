import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { SaveShopInput, ShopsListParams } from "@entities/shops/model/types";
import { shopsApi } from "./shopsApi";

export function useShopsListQuery(params?: ShopsListParams) {
  return useQuery({
    queryKey: params?.refreshKey
      ? [...queryKeys.shops.list(), params.refreshKey]
      : queryKeys.shops.list(),
    staleTime: 300_000,
    // Keep the shops list request alive through StrictMode remounts in dev.
    queryFn: () => shopsApi.list(),
  });
}

export function useShopByIdQuery(id: number | null) {
  return useQuery({
    queryKey: id ? queryKeys.shops.byId(id) : queryKeys.shops.byId(0),
    enabled: id !== null,
    queryFn: ({ signal }) => shopsApi.byId(id as number, signal),
  });
}

export function useCreateShopMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SaveShopInput) => shopsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shops.all });
    },
  });
}

export function useUpdateShopMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveShopInput }) => shopsApi.update(id, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shops.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.shops.byId(variables.id) });
    },
  });
}

export function useDeleteShopMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => shopsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shops.all });
    },
  });
}
