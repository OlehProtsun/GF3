import { request } from "@shared/api/httpClient";
import type { ShiftSwapCandidateParams, ShiftSwapReturnCandidatePage, ShiftSwapOfferPreview } from "../model/types";
import type { AcceptShiftSwapInput, CreateManagerShiftSwapInput, CreateShiftSwapInput, ShiftSwap, ShiftSwapEmployee, ShiftSwapHighlightSetting } from "../model/types";

const employeeEndpoint = "employee-shift-swaps";
const containerEndpoint = "containers";

export const shiftSwapsApi = {
  listReturnCandidates: (params: ShiftSwapCandidateParams, signal?: AbortSignal) => {
    const query = new URLSearchParams(Object.entries(params).filter(([, value]) => value !== undefined).map(([key, value]) => [key, String(value)]));
    return request<ShiftSwapReturnCandidatePage>(`${employeeEndpoint}/return-candidates?${query}`, { signal });
  },
  previewEmployee: (payload: CreateShiftSwapInput, signal?: AbortSignal) =>
    request<ShiftSwapOfferPreview>(`${employeeEndpoint}/preview`, { method: "POST", body: payload, signal }),
  listEmployee: (signal?: AbortSignal) => request<ShiftSwap[]>(employeeEndpoint, { signal }),
  listEmployees: (signal?: AbortSignal) => request<ShiftSwapEmployee[]>(`${employeeEndpoint}/employees`, { signal }),
  createEmployee: (payload: CreateShiftSwapInput) =>
    request<ShiftSwap>(employeeEndpoint, { method: "POST", body: payload }),
  acceptEmployee: ({ id, ...period }: AcceptShiftSwapInput) =>
    request<ShiftSwap>(`${employeeEndpoint}/${id}/accept`, { method: "POST", ...(period.fromTime !== undefined || period.toTime !== undefined ? { body: period } : {}) }),
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
  getManualHighlightSetting: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<ShiftSwapHighlightSetting>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/manual-highlight-setting`, { signal }),
  saveManualHighlightSetting: (containerId: number, graphId: number, highlightColor: string) =>
    request<ShiftSwapHighlightSetting>(`${containerEndpoint}/${containerId}/graphs/${graphId}/shift-swaps/manual-highlight-setting`, {
      method: "PUT", body: { highlightColor },
    }),
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
