import { request, requestFile } from "@shared/api/httpClient";
import type { EmployeeSchedule, PublicScheduleSummary } from "../model/types";

const endpoint = "employee-schedules";

export const employeeScheduleApi = {
  list: (signal?: AbortSignal) => request<EmployeeSchedule[]>(endpoint, { signal }),
  listPublic: (signal?: AbortSignal) => request<PublicScheduleSummary[]>(`${endpoint}/public`, { signal }),
  getPublic: (scheduleId: number, signal?: AbortSignal) =>
    request<EmployeeSchedule>(`${endpoint}/public/${scheduleId}`, { signal }),
  exportPublicPdf: (scheduleId: number, signal?: AbortSignal) =>
    requestFile(`${endpoint}/public/${scheduleId}/export/pdf`, { signal }),
  exportPdf: (scheduleId: number, signal?: AbortSignal) =>
    requestFile(`${endpoint}/${scheduleId}/export/pdf`, { signal }),
};
