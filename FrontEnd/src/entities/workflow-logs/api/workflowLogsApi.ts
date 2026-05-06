import { request } from "@shared/api/httpClient";
import type { WorkflowLog } from "../model/types";

const endpoint = "workflow-logs";

export const workflowLogsApi = {
  list: (signal?: AbortSignal) => request<WorkflowLog[]>(endpoint, { signal }),
};
