import { request } from "@shared/api/httpClient";
import type { EmployeeSchedule } from "../model/types";

const endpoint = "employee-schedules";

export const employeeScheduleApi = {
  list: (signal?: AbortSignal) => request<EmployeeSchedule[]>(endpoint, { signal }),
};
