import { useCallback, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { SaveSystemNewsInput, SystemNewsMessage } from "../model/types";
import { systemNewsApi } from "./systemNewsApi";

export const useSystemNewsQuery = (enabled = true) => {
  const queryClient = useQueryClient();
  const query = useQuery<SystemNewsMessage[]>({
    queryKey: queryKeys.systemNews.visible(),
    queryFn: ({ signal }) => systemNewsApi.list(signal),
    enabled,
    staleTime: 15_000,
  });
  const refetch = useCallback(() => queryClient.invalidateQueries({ queryKey: queryKeys.systemNews.visible() }), [queryClient]);
  useEffect(() => {
    if (!enabled) return;
    const intervalId = window.setInterval(refetch, 30_000);
    return () => window.clearInterval(intervalId);
  }, [enabled, refetch]);
  return { ...query, refetch };
};

export function useMarkSystemNewsReadMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (messageId: number) => systemNewsApi.markRead(messageId),
    onSuccess: (_data, messageId) => {
      const key = queryKeys.systemNews.visible();
      const current = queryClient.getQueryState<SystemNewsMessage[]>(key).data ?? [];
      queryClient.setQueryData(key, current.map(item => item.id === messageId ? { ...item, isRead: true } : item));
    },
  });
}

export function useMarkAllSystemNewsReadMutation() {
  const queryClient = useQueryClient();
  return useMutation<void, undefined>({
    mutationFn: () => systemNewsApi.markAllRead(),
    onSuccess: () => {
      const key = queryKeys.systemNews.visible();
      const current = queryClient.getQueryState<SystemNewsMessage[]>(key).data ?? [];
      queryClient.setQueryData(key, current.map(item => ({ ...item, isRead: true })));
    },
  });
}

export function useCreateSystemNewsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveSystemNewsInput) => systemNewsApi.create(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.systemNews.all }),
  });
}

export function useUpdateSystemNewsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ messageId, payload }: { messageId: number; payload: SaveSystemNewsInput }) => systemNewsApi.update(messageId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.systemNews.all }),
  });
}

export function useDeleteSystemNewsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (messageId: number) => systemNewsApi.delete(messageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.systemNews.all }),
  });
}
