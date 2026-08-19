import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { CreateShiftCorrectionInput } from "../model/types";
import { shiftCorrectionsApi } from "./shiftCorrectionsApi";

export const useEmployeeShiftCorrectionsQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.employeeCorrections(),
    enabled,
    staleTime: 30_000,
    queryFn: ({ signal }) => shiftCorrectionsApi.listEmployee(signal),
  });

export const useGraphShiftCorrectionsQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.graphCorrections(containerId ?? 0, graphId ?? 0),
    enabled: containerId !== null && graphId !== null,
    staleTime: 15_000,
    queryFn: ({ signal }) => shiftCorrectionsApi.listGraph(containerId as number, graphId as number, signal),
  });

export const useShiftCorrectionSettingQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.shiftSwaps.correctionSettings(),
    enabled,
    staleTime: 300_000,
    queryFn: ({ signal }) => shiftCorrectionsApi.getSetting(signal),
  });

export function useCreateShiftCorrectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateShiftCorrectionInput) => shiftCorrectionsApi.createEmployee(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employeeCorrections() }),
  });
}

export function useSaveShiftCorrectionSettingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (highlightColor: string) => shiftCorrectionsApi.saveSetting(highlightColor),
    onSuccess: data => queryClient.setQueryData(queryKeys.shiftSwaps.correctionSettings(), data),
  });
}

export function useApproveShiftCorrectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, id, highlightColor }: { containerId: number; graphId: number; id: number; highlightColor: string }) =>
      shiftCorrectionsApi.approve(containerId, graphId, id, highlightColor),
    onSuccess: (_data, payload) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(payload.containerId, payload.graphId) });
    },
  });
}

export function useRejectShiftCorrectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, id }: { containerId: number; graphId: number; id: number }) =>
      shiftCorrectionsApi.reject(containerId, graphId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all }),
  });
}
