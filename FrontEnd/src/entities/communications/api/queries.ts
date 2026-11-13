import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { CreateCommunicationMessageDto, UpdateCommunicationMessageDto } from "./dto";
import { communicationsApi } from "./communicationsApi";

export function useManagerCommunicationsQuery() {
  return useQuery({
    queryKey: queryKeys.communications.managerList(),
    queryFn: ({ signal }) => communicationsApi.listForManager(signal),
  });
}

export function useCreateCommunicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateCommunicationMessageDto) => communicationsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.managerList() });
    },
  });
}

export function useUpdateCommunicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateCommunicationMessageDto) => communicationsApi.update(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.managerList() });
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.employeePendingAll });
    },
  });
}

export function useDeleteCommunicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (communicationId: number) => communicationsApi.delete(communicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.managerList() });
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.employeePendingAll });
    },
  });
}

export function useEmployeePendingCommunicationsQuery(employeeId: number | null) {
  return useQuery({
    queryKey: queryKeys.communications.employeePending(employeeId ?? 0),
    queryFn: ({ signal }) => communicationsApi.pendingForEmployee(signal),
    enabled: employeeId !== null && employeeId > 0,
    staleTime: 0,
  });
}

export function useDismissEmployeeCommunicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (communicationId: number) => communicationsApi.dismissForEmployee(communicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.communications.employeePendingAll });
    },
  });
}
