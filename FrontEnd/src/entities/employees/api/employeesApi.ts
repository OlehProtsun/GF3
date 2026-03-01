import { request } from "@shared/api/httpClient";
import type { SaveEmployeeInput } from "@entities/employees/model/types";
import type { EmployeeDto, SaveEmployeeDto } from "./dto";

const endpoint = "employees";

function toSaveDto(input: SaveEmployeeInput): SaveEmployeeDto {
  return {
    firstName: input.firstName.trim(),
    lastName: input.lastName.trim(),
    phone: input.phone?.trim() || undefined,
    email: input.email?.trim() || undefined,
  };
}

export const employeesApi = {
  list: (signal?: AbortSignal) => request<EmployeeDto[]>(endpoint, { signal }),
  byId: (id: number, signal?: AbortSignal) => request<EmployeeDto>(`${endpoint}/${id}`, { signal }),
  create: (payload: SaveEmployeeInput) => request<EmployeeDto>(endpoint, { method: "POST", body: toSaveDto(payload) }),
  update: (id: number, payload: SaveEmployeeInput) =>
    request<void>(`${endpoint}/${id}`, { method: "PUT", body: toSaveDto(payload) }),
  remove: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),
};
