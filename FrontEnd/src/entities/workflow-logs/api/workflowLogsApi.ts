import { request } from "@shared/api/httpClient";
import type {
  WorkflowLog,
  WorkflowLogBulkDeleteRequest,
  WorkflowLogDeleteResult,
  WorkflowLogSettings,
} from "../model/types";

const endpoint = "workflow-logs";

export const workflowLogsApi = {
  list: (signal?: AbortSignal) => request<WorkflowLog[]>(endpoint, { signal }),
  settings: (signal?: AbortSignal) => request<WorkflowLogSettings>(`${endpoint}/settings`, { signal }),
  updateSettings: (payload: WorkflowLogSettings) =>
    request<WorkflowLogSettings>(`${endpoint}/settings`, { method: "PUT", body: payload }),
  deleteOne: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),
  bulkDelete: (payload: WorkflowLogBulkDeleteRequest) =>
    request<WorkflowLogDeleteResult>(`${endpoint}/bulk-delete`, { method: "POST", body: payload }),
};
