import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { employeeProfileApi } from "./employeeProfileApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("employee profile api", () => {
  test("passes the read signal through current profile requests", () => {
    const controller = new AbortController();

    employeeProfileApi.current(controller.signal);

    expect(requestMock).toHaveBeenCalledWith("employee-profile/me", { signal: controller.signal });
  });

  test("normalizes contact and password reset payloads before sending them", () => {
    employeeProfileApi.update({
      recoveryEmail: " alice@example.com ",
      phone: " ",
    });
    employeeProfileApi.sendPasswordResetCode();
    employeeProfileApi.confirmPasswordReset({
      code: " 123456 ",
      newPassword: "secret123",
    });

    expect(requestMock).toHaveBeenNthCalledWith(1, "employee-profile/me", {
      method: "PUT",
      body: {
        recoveryEmail: "alice@example.com",
        phone: undefined,
      },
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employee-profile/me/password/send-code", {
      method: "POST",
    });
    expect(requestMock).toHaveBeenNthCalledWith(3, "employee-profile/me/password/confirm", {
      method: "POST",
      body: {
        code: "123456",
        newPassword: "secret123",
      },
    });
  });
});
