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

export const useEmployeeCorrectionSchedulesQuery = (enabled: boolean) =>
  useQuery({
    queryKey: queryKeys.employeeSchedules.correctionCandidates(),
    enabled,
    staleTime: 0,
    cancelOnUnmount: true,
    queryFn: ({ signal }) => shiftCorrectionsApi.listSchedules(signal),
  });

export const useGraphShiftCorrectionsQuery = (containerId: number | null, graphId: number | null, reviewRequestId?: number) =>
  useQuery({
    queryKey: reviewRequestId === undefined
      ? queryKeys.shiftSwaps.graphCorrections(containerId ?? 0, graphId ?? 0)
      : queryKeys.shiftSwaps.graphCorrectionById(containerId ?? 0, graphId ?? 0, reviewRequestId),
    enabled: Number.isInteger(containerId) && containerId! > 0 && Number.isInteger(graphId) && graphId! > 0 &&
      (reviewRequestId === undefined || (Number.isInteger(reviewRequestId) && reviewRequestId > 0)),
    staleTime: reviewRequestId === undefined ? 15_000 : 0,
    cancelOnUnmount: reviewRequestId !== undefined,
    queryFn: async ({ signal }) => reviewRequestId === undefined
      ? shiftCorrectionsApi.listGraph(containerId!, graphId!, signal)
      : [await shiftCorrectionsApi.getGraphRequest(containerId!, graphId!, reviewRequestId, signal)],
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
    mutationFn: ({ containerId, graphId, id, highlightColor }: { containerId: number; graphId: number; id: number; highlightColor?: string }) =>
      shiftCorrectionsApi.approve(containerId, graphId, id, highlightColor),
    onSuccess: (data, payload) => {
      queryClient.setQueryData(queryKeys.shiftSwaps.graphCorrectionById(payload.containerId, payload.graphId, payload.id), [data]);
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(payload.containerId, payload.graphId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphs(payload.containerId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.home.activity() });
      queryClient.invalidateQueries({ queryKey: queryKeys.home.navBadges() });
      queryClient.invalidateQueries({ queryKey: queryKeys.home.dashboard() });
    },
  });
}

export function useRejectShiftCorrectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, id }: { containerId: number; graphId: number; id: number }) =>
      shiftCorrectionsApi.reject(containerId, graphId, id),
    onSuccess: (data, payload) => {
      queryClient.setQueryData(queryKeys.shiftSwaps.graphCorrectionById(payload.containerId, payload.graphId, payload.id), [data]);
      queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.home.activity() });
      queryClient.invalidateQueries({ queryKey: queryKeys.home.navBadges() });
    },
  });
}
