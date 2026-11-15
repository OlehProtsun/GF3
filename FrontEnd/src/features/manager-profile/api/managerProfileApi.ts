import { request } from "@shared/api/httpClient";
import type {
  CreateManagerDto,
  ManagerProfileDto,
  ManagerProfileUpdateResponseDto,
  UpdateManagerProfileDto,
} from "./dto";

const endpoint = "manager-profile";

function normalizeOptionalText(value?: string | null) {
  const trimmedValue = value?.trim();
  return trimmedValue ? trimmedValue : undefined;
}

export const managerProfileApi = {
  current: (signal?: AbortSignal) => request<ManagerProfileDto>(`${endpoint}/me`, { signal }),
  update: (payload: UpdateManagerProfileDto) =>
    request<ManagerProfileUpdateResponseDto>(`${endpoint}/me`, {
      method: "PUT",
      body: {
        userName: payload.userName.trim(),
        displayName: payload.displayName.trim(),
        recoveryEmail: normalizeOptionalText(payload.recoveryEmail),
        newPassword: normalizeOptionalText(payload.newPassword),
      },
    }),
  listManagers: (signal?: AbortSignal) => request<ManagerProfileDto[]>(`${endpoint}/managers`, { signal }),
  createManager: (payload: CreateManagerDto) =>
    request<ManagerProfileDto>(`${endpoint}/managers`, {
      method: "POST",
      body: {
        userName: payload.userName.trim(),
        displayName: payload.displayName.trim(),
        recoveryEmail: normalizeOptionalText(payload.recoveryEmail),
        password: payload.password,
      },
    }),
  deleteManager: (managerId: number) =>
    request<void>(`${endpoint}/managers/${managerId}`, {
      method: "DELETE",
    }),
};
