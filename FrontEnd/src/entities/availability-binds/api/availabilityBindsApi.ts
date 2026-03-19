import { request } from "@shared/api/httpClient";
import type { AvailabilityBindDto, SaveAvailabilityBindDto } from "./dto";

const endpoint = "availability-binds";

export const availabilityBindsApi = {
  list: (signal?: AbortSignal) => request<AvailabilityBindDto[]>(endpoint, { signal }),
  active: (signal?: AbortSignal) => request<AvailabilityBindDto[]>(`${endpoint}/active`, { signal }),
  byId: (id: number, signal?: AbortSignal) => request<AvailabilityBindDto>(`${endpoint}/${id}`, { signal }),
  create: (payload: SaveAvailabilityBindDto) => request<AvailabilityBindDto>(endpoint, { method: "POST", body: payload }),
  update: (id: number, payload: SaveAvailabilityBindDto) => request<void>(`${endpoint}/${id}`, { method: "PUT", body: payload }),
  remove: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),
};
