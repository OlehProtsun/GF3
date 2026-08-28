import { request } from "@shared/api/httpClient";
import type { CreateManagerShiftSwapInput, CreateShiftSwapInput, ShiftSwap, ShiftSwapEmployee, ShiftSwapHighlightSetting } from "../model/types";

const employeeEndpoint = "employee-shift-swaps";
const containerEndpoint = "containers";

export const shiftSwapsApi = {
  listEmployee: (signal?: AbortSignal) => request<ShiftSwap[]>(employeeEndpoint, { signal }),
  listEmployees: (signal?: AbortSignal) => request<ShiftSwapEmployee[]>(`${employeeEndpoint}/employees`, { signal }),
  createEmployee: (payload: CreateShiftSwapInput) =>
    request<ShiftSwap>(employeeEndpoint, { method: "POST", body: payload }),
  acceptEmployee: (id: number) =>
    request<ShiftSwap>(`${employeeEndpoint}/${id}/accept`, { method: "POST" }),
  cancelEmployee: (id: number) =>
    request<ShiftSwap>(`${employeeEndpoint}/${id}/cancel`, { method: "POST" }),
  createManagerManual: ({ containerId, graphId, ...payload }: CreateManagerShiftSwapInput) =>
    request<ShiftSwap>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/manual`, {
      method: "POST",
      body: payload,
    }),
  cancelManagerManual: (containerId: number, graphId: number, id: number) =>
    request<void>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/${id}/cancel`, {
      method: "POST",
    }),
  listGraphLog: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<ShiftSwap[]>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps`, { signal }),
  getHighlightSetting: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<ShiftSwapHighlightSetting>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/highlight-setting`, { signal }),
  saveHighlightSetting: (containerId: number, graphId: number, highlightColor: string) =>
    request<ShiftSwapHighlightSetting>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/highlight-setting`, {
      method: "PUT",
      body: { highlightColor },
    }),
  listContainer: (containerId: number, signal?: AbortSignal) =>
    request<ShiftSwap[]>(`${containerEndpoint}/${containerId}/shift-swaps`, { signal }),
  cancelContainer: (containerId: number, id: number) =>
    request<ShiftSwap>(`${containerEndpoint}/${containerId}/shift-swaps/${id}/cancel`, { method: "POST" }),
  deleteContainer: (containerId: number, id: number) =>
    request<void>(`${containerEndpoint}/${containerId}/shift-swaps/${id}`, { method: "DELETE" }),
};
