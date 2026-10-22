import { request, requestFile } from "@shared/api/httpClient";
import type { EmployeeSchedule } from "../model/types";

const endpoint = "employee-schedules";

export const employeeScheduleApi = {
  list: (signal?: AbortSignal) => request<EmployeeSchedule[]>(endpoint, { signal }),
  exportPdf: (scheduleId: number, signal?: AbortSignal) =>
    requestFile(`${endpoint}/${scheduleId}/export/pdf`, { signal }),
};
