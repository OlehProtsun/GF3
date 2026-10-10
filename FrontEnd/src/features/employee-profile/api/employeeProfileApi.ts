import { request } from "@shared/api/httpClient";
import type {
  CompletePasswordResetDto,
  EmployeeProfileDto,
  PasswordResetCodeDispatchDto,
  UpdateEmployeeProfileDto,
} from "./dto";

const endpoint = "employee-profile/me";

function normalizeTextValue(value?: string | null) {
  const trimmedValue = value?.trim();
  return trimmedValue ? trimmedValue : undefined;
}

export const employeeProfileApi = {
  current: (signal?: AbortSignal) => request<EmployeeProfileDto>(endpoint, { signal }),
  update: (payload: UpdateEmployeeProfileDto) =>
    request<EmployeeProfileDto>(endpoint, {
      method: "PUT",
      body: {
        recoveryEmail: normalizeTextValue(payload.recoveryEmail),
        phone: normalizeTextValue(payload.phone),
      },
    }),
  sendPasswordResetCode: () =>
    request<PasswordResetCodeDispatchDto>(`${endpoint}/password/send-code`, {
      method: "POST",
    }),
  confirmPasswordReset: (payload: CompletePasswordResetDto) =>
    request<void>(`${endpoint}/password/confirm`, {
      method: "POST",
      body: {
        code: payload.code.trim(),
        newPassword: payload.newPassword,
      },
    }),
};
