import { request } from "@shared/api/httpClient";
import type {
  CreateShiftCorrectionInput,
  ShiftCorrectionRequest,
  ShiftCorrectionSchedule,
  ShiftCorrectionSetting,
} from "../model/types";

export const shiftCorrectionsApi = {
  listEmployee: (signal?: AbortSignal) =>
    request<ShiftCorrectionRequest[]>("employee-shift-corrections", { signal }),
  listSchedules: (signal?: AbortSignal) =>
    request<ShiftCorrectionSchedule[]>("employee-shift-corrections/schedules", { signal }),
  createEmployee: (payload: CreateShiftCorrectionInput) =>
    request<ShiftCorrectionRequest>("employee-shift-corrections", { method: "POST", body: payload }),
  listGraph: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<ShiftCorrectionRequest[]>(`containers/${containerId}/graphs/${graphId}/shift-corrections`, { signal }),
  getGraphRequest: (containerId: number, graphId: number, id: number, signal?: AbortSignal) =>
    request<ShiftCorrectionRequest>(`containers/${containerId}/graphs/${graphId}/shift-corrections/${id}`, { signal }),
  approve: (containerId: number, graphId: number, id: number, highlightColor?: string) =>
    request<ShiftCorrectionRequest>(`containers/${containerId}/graphs/${graphId}/shift-corrections/${id}/approve`, {
      method: "POST",
      body: { highlightColor },
    }),
  reject: (containerId: number, graphId: number, id: number) =>
    request<ShiftCorrectionRequest>(`containers/${containerId}/graphs/${graphId}/shift-corrections/${id}/reject`, {
      method: "POST",
    }),
  getSetting: (signal?: AbortSignal) =>
    request<ShiftCorrectionSetting>("manager-shift-correction-settings", { signal }),
  saveSetting: (highlightColor: string) =>
    request<ShiftCorrectionSetting>("manager-shift-correction-settings", {
      method: "PUT",
      body: { highlightColor },
    }),
};
