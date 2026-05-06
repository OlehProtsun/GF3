import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { workflowLogsApi } from "./workflowLogsApi";

export const useWorkflowLogsQuery = () =>
  useQuery({
    queryKey: queryKeys.workflowLogs.list(),
    staleTime: 30_000,
    queryFn: ({ signal }) => workflowLogsApi.list(signal),
  });
