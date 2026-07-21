import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { CreateManagerShiftSwapInput, CreateShiftSwapInput } from "../model/types";
import { shiftSwapsApi } from "./shiftSwapsApi";

export const useEmployeeShiftSwapsQuery = () =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.employee(),
    staleTime: 30_000,
    queryFn: ({ signal }) => shiftSwapsApi.listEmployee(signal),
  });

export const useEmployeeShiftSwapEmployeesQuery = () =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.employees(),
    staleTime: 300_000,
    queryFn: ({ signal }) => shiftSwapsApi.listEmployees(signal),
  });

export const useGraphShiftSwapLogQuery = (containerId: number | null, graphId: number | null, enabled = true) =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.graphLog(containerId ?? 0, graphId ?? 0),
    enabled: enabled && containerId !== null && graphId !== null,
    staleTime: 30_000,
    queryFn: ({ signal }) => shiftSwapsApi.listGraphLog(containerId as number, graphId as number, signal),
  });

export const useContainerShiftSwapsQuery = (
  containerId: number | null,
  enabled = true,
) =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.container(containerId ?? 0),
    enabled: enabled && containerId !== null,
    staleTime: 30_000,
    queryFn: ({ signal }) => shiftSwapsApi.listContainer(containerId as number, signal),
  });

export function useCreateEmployeeShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateShiftSwapInput) => shiftSwapsApi.createEmployee(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employee() });
    },
  });
}

export function useCreateManagerManualShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateManagerShiftSwapInput) => shiftSwapsApi.createManagerManual(payload),
    onSuccess: (_data, payload) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.graphLog(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    },
  });
}

export function useCancelManagerManualShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ containerId, graphId, id }: { containerId: number; graphId: number; id: number }) =>
      shiftSwapsApi.cancelManagerManual(containerId, graphId, id),
    onSuccess: (_data, payload) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.graphLog(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(payload.containerId, payload.graphId) });
    },
  });
}

export function useCancelContainerShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ containerId, id }: { containerId: number; id: number }) =>
      shiftSwapsApi.cancelContainer(containerId, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    },
  });
}

export function useDeleteContainerShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ containerId, id }: { containerId: number; id: number }) =>
      shiftSwapsApi.deleteContainer(containerId, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    },
  });
}

export function useAcceptEmployeeShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => shiftSwapsApi.acceptEmployee(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employee() });
      queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
    },
  });
}

export function useCancelEmployeeShiftSwapMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => shiftSwapsApi.cancelEmployee(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employee() });
    },
  });
}
