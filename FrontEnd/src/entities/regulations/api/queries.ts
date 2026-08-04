import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { regulationsApi } from "./regulationsApi";

export function usePendingRegulationsQuery(accountKey: string) {
  return useQuery({ queryKey: queryKeys.regulations.pending(accountKey), queryFn: ({ signal }) => regulationsApi.pending({ signal }) });
}

export function useMyRegulationHistoryQuery(accountKey: string) {
  return useQuery({ queryKey: queryKeys.regulations.myHistory(accountKey), queryFn: ({ signal }) => regulationsApi.myHistory({ signal }) });
}

export function useEmployeeRegulationHistoryQuery(employeeId: number | null) {
  return useQuery({
    queryKey: queryKeys.regulations.employeeHistory(employeeId ?? 0),
    enabled: employeeId !== null,
    queryFn: ({ signal }) => regulationsApi.employeeHistory(employeeId as number, signal),
  });
}

export function useAcceptRegulationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: regulationsApi.accept,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.regulations.all });
    },
  });
}

export function useAdminRegulationsQuery() {
  return useQuery({ queryKey: queryKeys.regulations.admin(), queryFn: ({ signal }) => regulationsApi.adminList({ signal }) });
}

export function useAdminRegulationAcceptancesQuery() {
  return useQuery({ queryKey: queryKeys.regulations.adminAcceptances(), queryFn: ({ signal }) => regulationsApi.adminAcceptances({ signal }) });
}

function useAdminMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.regulations.all }),
  });
}

export const useCreateRegulationMutation = () => useAdminMutation(regulationsApi.adminCreate);
export const useUpdateRegulationMutation = () => useAdminMutation(
  ({ documentId, input }: { documentId: number; input: Parameters<typeof regulationsApi.adminUpdate>[1] }) =>
    regulationsApi.adminUpdate(documentId, input),
);
export const usePublishRegulationMutation = () => useAdminMutation(regulationsApi.adminPublish);
export const useDeleteRegulationMutation = () => useAdminMutation(regulationsApi.adminDelete);
