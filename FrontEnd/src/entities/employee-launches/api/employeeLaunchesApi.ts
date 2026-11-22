import { request } from "@shared/api/httpClient";
import type { PwaLaunchMode } from "@shared/lib/pwaDisplayMode";
import type { EmployeeLaunchUsage } from "../model/types";

const endpoint = "employee-launches";

export const employeeLaunchesApi = {
  record: (mode: PwaLaunchMode) => request<void>(endpoint, { method: "POST", body: { mode } }),
  list: (signal?: AbortSignal) => request<EmployeeLaunchUsage[]>(endpoint, { signal }),
};
