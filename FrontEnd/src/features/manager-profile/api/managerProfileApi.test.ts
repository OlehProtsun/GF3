import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { managerProfileApi } from "./managerProfileApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("manager profile api", () => {
  test("passes read signals through profile and manager list requests", () => {
    const controller = new AbortController();

    managerProfileApi.current(controller.signal);
    managerProfileApi.listManagers(controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "manager-profile/me", { signal: controller.signal });
    expect(requestMock).toHaveBeenNthCalledWith(2, "manager-profile/managers", { signal: controller.signal });
  });

  test("normalizes manager update and creation payloads before sending them", () => {
    managerProfileApi.update({
      userName: " chief ",
      displayName: " Chief Manager ",
      recoveryEmail: " ",
      newPassword: " secret123 ",
    });
    managerProfileApi.createManager({
      userName: " second ",
      displayName: " Second Manager ",
      recoveryEmail: " second@example.com ",
      password: "secret456",
    });

    expect(requestMock).toHaveBeenNthCalledWith(1, "manager-profile/me", {
      method: "PUT",
      body: {
        userName: "chief",
        displayName: "Chief Manager",
        recoveryEmail: undefined,
        newPassword: "secret123",
      },
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "manager-profile/managers", {
      method: "POST",
      body: {
        userName: "second",
        displayName: "Second Manager",
        recoveryEmail: "second@example.com",
        password: "secret456",
      },
    });
  });

  test("uses the manager delete endpoint without a request body", () => {
    managerProfileApi.deleteManager(4);

    expect(requestMock).toHaveBeenCalledWith("manager-profile/managers/4", { method: "DELETE" });
  });
});
