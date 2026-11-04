import { request } from "@shared/api/httpClient";
import type { EmployeeAvailabilityGroup, SaveEmployeeAvailabilityPayload } from "../model/types";

const endpoint = "employee-availability";

export const employeeAvailabilityApi = {
  list: (signal?: AbortSignal) => request<EmployeeAvailabilityGroup[]>(endpoint, { signal }),
  byId: (id: number, signal?: AbortSignal) =>
    request<EmployeeAvailabilityGroup>(`${endpoint}/${id}`, { signal }),
  saveSlots: (id: number, payload: SaveEmployeeAvailabilityPayload) =>
    request<EmployeeAvailabilityGroup>(`${endpoint}/${id}/slots`, { method: "PUT", body: payload }),
};
