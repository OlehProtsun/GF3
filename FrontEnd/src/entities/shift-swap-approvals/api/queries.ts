import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { shiftSwapApprovalsApi as api } from "./shiftSwapApprovalsApi";
export const useEmployeeContractApprovalsQuery = () => useQuery({ queryKey: queryKeys.shiftSwaps.employeeApprovals(),
  staleTime: 0, queryFn: ({ signal }) => api.mine(signal) });
export const useGraphContractApprovalsQuery = (containerId: number | null, graphId: number | null, reviewRequestId?: number) => useQuery({
  queryKey: reviewRequestId === undefined
    ? queryKeys.shiftSwaps.graphApprovals(containerId ?? 0, graphId ?? 0)
    : queryKeys.shiftSwaps.graphApprovalById(containerId ?? 0, graphId ?? 0, reviewRequestId),
  enabled: reviewRequestId === undefined ? Boolean(containerId && graphId) :
    Number.isInteger(containerId) && containerId! > 0 && Number.isInteger(graphId) && graphId! > 0 &&
    Number.isInteger(reviewRequestId) && reviewRequestId > 0,
  staleTime: reviewRequestId === undefined ? 30_000 : 0,
  cancelOnUnmount: reviewRequestId !== undefined,
  queryFn: async ({ signal }) => reviewRequestId === undefined
    ? api.graph(containerId!, graphId!, signal)
    : [await api.getGraphRequest(containerId!, graphId!, reviewRequestId, signal)],
});
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
  return useMutation({ mutationFn: api.review, onSuccess: (data, input) => {
    client.setQueryData(queryKeys.shiftSwaps.graphApprovalById(input.containerId, input.graphId, input.id), [data]);
    client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    client.invalidateQueries({ queryKey: queryKeys.home.activity() });
    client.invalidateQueries({ queryKey: queryKeys.home.navBadges() });
    if (input.action === "approve") {
      client.invalidateQueries({ queryKey: queryKeys.home.dashboard() });
      client.invalidateQueries({ queryKey: queryKeys.containers.all });
      client.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
      client.invalidateQueries({ queryKey: queryKeys.employeeUiState.current() });
    }
  } });
}
