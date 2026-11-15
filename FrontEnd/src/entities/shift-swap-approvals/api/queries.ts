import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { shiftSwapApprovalsApi as api } from "./shiftSwapApprovalsApi";
export const useEmployeeContractApprovalsQuery = () => useQuery({ queryKey: queryKeys.shiftSwaps.employeeApprovals(),
  staleTime: 0, queryFn: ({ signal }) => api.mine(signal) });
export const useGraphContractApprovalsQuery = (containerId: number | null, graphId: number | null) => useQuery({
  queryKey: queryKeys.shiftSwaps.graphApprovals(containerId ?? 0, graphId ?? 0), enabled: Boolean(containerId && graphId),
  staleTime: 0, queryFn: ({ signal }) => api.graph(containerId!, graphId!, signal) });
export function useCreateContractClaimMutation() {
  const client = useQueryClient();
  return useMutation({ mutationFn: api.claim, onSuccess: () => client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all }) });
}
export function useCreateGiveawayExceptionMutation() {
  const client = useQueryClient();
  return useMutation({ mutationFn: api.giveawayException, onSuccess: () => client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all }) });
}
export function useCancelContractApprovalMutation() {
  const client = useQueryClient();
  return useMutation({ mutationFn: api.cancel, onSuccess: () => client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all }) });
}
export function useReviewContractApprovalMutation() {
  const client = useQueryClient();
  return useMutation({ mutationFn: api.review, onSuccess: (_, input) => {
    client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    if (input.action === "approve") {
      client.invalidateQueries({ queryKey: queryKeys.containers.all });
      client.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
      client.invalidateQueries({ queryKey: queryKeys.employeeUiState.current() });
    }
  } });
}
