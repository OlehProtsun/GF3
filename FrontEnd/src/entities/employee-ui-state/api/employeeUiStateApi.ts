import { request } from "@shared/api/httpClient";
import type { EmployeeUiState } from "../model/types";

const endpoint = "employee-ui-state";

export const employeeUiStateApi = {
  get: (signal?: AbortSignal) => request<EmployeeUiState>(endpoint, { signal }),
  saveScheduleColumnOrder: (scheduleId: number, columnOrder: number[]) =>
    request<void>(`${endpoint}/schedule-columns/${scheduleId}`, {
      method: "PUT",
      body: { columnOrder },
    }),
  markNotificationsRead: (notificationIds: string[]) =>
    request<void>(`${endpoint}/notifications/read`, {
      method: "POST",
      body: { notificationIds },
    }),
  pinSwap: (swapId: number) =>
    request<void>(`${endpoint}/swap-pins/${swapId}`, { method: "PUT" }),
  unpinSwap: (swapId: number) =>
    request<void>(`${endpoint}/swap-pins/${swapId}`, { method: "DELETE" }),
};
