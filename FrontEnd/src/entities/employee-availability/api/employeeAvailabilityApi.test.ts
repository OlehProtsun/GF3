import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { employeeAvailabilityApi } from "./employeeAvailabilityApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("employee availability api", () => {
  test("passes read signals through list and byId requests", () => {
    const controller = new AbortController();

    employeeAvailabilityApi.list(controller.signal);
    employeeAvailabilityApi.byId(12, controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "employee-availability", { signal: controller.signal });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employee-availability/12", { signal: controller.signal });
  });

  test("saves employee day slots to the scoped slots endpoint", () => {
    const payload = {
      slots: [
        { dayOfMonth: 1, kind: 0, intervalStr: null },
        { dayOfMonth: 2, kind: 2, intervalStr: "09:00 - 15:00" },
        { dayOfMonth: 3, kind: 1, intervalStr: null },
      ],
    };

    employeeAvailabilityApi.saveSlots(12, payload);

    expect(requestMock).toHaveBeenCalledWith("employee-availability/12/slots", {
      method: "PUT",
      body: payload,
    });
  });
});
