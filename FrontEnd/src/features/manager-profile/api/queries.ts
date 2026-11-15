import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { CreateManagerDto, UpdateManagerProfileDto } from "./dto";
import { managerProfileApi } from "./managerProfileApi";
import { useAuth } from "@app/providers/AuthProvider";
import { getAuthAccessToken, RequestCanceledError } from "@shared/api/httpClient";

export function useManagerProfileQuery() {
  return useQuery({
    queryKey: queryKeys.managerProfile.me(),
    queryFn: ({ signal }) => managerProfileApi.current(signal),
  });
}

export function useManagerListQuery() {
  return useQuery({
    queryKey: queryKeys.managerProfile.list(),
    queryFn: ({ signal }) => managerProfileApi.listManagers(signal),
  });
}

export function useUpdateManagerProfileMutation() {
  const queryClient = useQueryClient();
  const { replaceLoginResult } = useAuth();

  return useMutation({
    mutationFn: async (payload: UpdateManagerProfileDto) => {
      const requestToken = getAuthAccessToken();
      queryClient.setQueryData(queryKeys.managerProfile.updating(), true);
      try {
        const result = await managerProfileApi.update(payload);
        if (requestToken !== getAuthAccessToken()) throw new RequestCanceledError();
        replaceLoginResult(result);
        return result;
      } finally {
        queryClient.setQueryData(queryKeys.managerProfile.updating(), false);
      }
    },
    onSuccess: (result) => {
      queryClient.setQueryData(queryKeys.managerProfile.me(), result.profile);
      queryClient.invalidateQueries({ queryKey: queryKeys.managerProfile.list() });
    },
  });
}

export function useCreateManagerMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateManagerDto) => managerProfileApi.createManager(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.managerProfile.list() });
    },
  });
}

export function useDeleteManagerMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (managerId: number) => managerProfileApi.deleteManager(managerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.managerProfile.list() });
    },
  });
}
