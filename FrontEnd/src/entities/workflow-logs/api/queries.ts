import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { WorkflowLogBulkDeleteRequest, WorkflowLogSettings } from "../model/types";
import { workflowLogsApi } from "./workflowLogsApi";

export const useWorkflowLogsQuery = () =>
  useQuery({
    queryKey: queryKeys.workflowLogs.list(),
    staleTime: 30_000,
    queryFn: ({ signal }) => workflowLogsApi.list(signal),
  });

export const useWorkflowLogSettingsQuery = () =>
  useQuery({
    queryKey: queryKeys.workflowLogs.settings(),
    staleTime: 30_000,
    queryFn: ({ signal }) => workflowLogsApi.settings(signal),
  });

export function useUpdateWorkflowLogSettingsMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: WorkflowLogSettings) => workflowLogsApi.updateSettings(payload),
    onSuccess: settings => {
      queryClient.setQueryData(queryKeys.workflowLogs.settings(), settings);
      queryClient.invalidateQueries({ queryKey: queryKeys.workflowLogs.all });
    },
  });
}

export function useDeleteWorkflowLogMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => workflowLogsApi.deleteOne(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.workflowLogs.all }),
  });
}

export function useBulkDeleteWorkflowLogsMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: WorkflowLogBulkDeleteRequest) => workflowLogsApi.bulkDelete(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.workflowLogs.all }),
  });
}
