import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { shiftSwapsApi } from "./shiftSwapsApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("shift swaps api", () => {
  test("loads employee swap lists and available target employees with read signals", () => {
    const controller = new AbortController();

    shiftSwapsApi.listEmployee(controller.signal);
    shiftSwapsApi.listEmployees(controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "employee-shift-swaps", { signal: controller.signal });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employee-shift-swaps/employees", { signal: controller.signal });
  });

  test("creates accepts and cancels employee swap requests on the employee endpoints", () => {
    const payload = {
      scheduleId: 10,
      scheduleSlotId: 20,
      fromTime: "09:00",
      toTime: "13:00",
      targetEmployeeId: 7,
    };

    shiftSwapsApi.createEmployee(payload);
    shiftSwapsApi.acceptEmployee(5);
    shiftSwapsApi.cancelEmployee(6);

    expect(requestMock).toHaveBeenNthCalledWith(1, "employee-shift-swaps", {
      method: "POST",
      body: payload,
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employee-shift-swaps/5/accept", { method: "POST" });
    expect(requestMock).toHaveBeenNthCalledWith(3, "employee-shift-swaps/6/cancel", { method: "POST" });
  });

  test("uses container graph endpoints for manager manual swaps and graph logs", () => {
    const controller = new AbortController();

    shiftSwapsApi.createManagerManual({
      containerId: 2,
      graphId: 10,
      manualColumnId: 77,
      dayOfMonth: 15,
      fromTime: "10:00",
      toTime: "14:00",
      targetEmployeeId: null,
    });
    shiftSwapsApi.cancelManagerManual(2, 10, 99);
    shiftSwapsApi.listGraphLog(2, 10, controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "containers/2/graphs/10/shift-swaps/manual", {
      method: "POST",
      body: {
        manualColumnId: 77,
        dayOfMonth: 15,
        fromTime: "10:00",
        toTime: "14:00",
        targetEmployeeId: null,
      },
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "containers/2/graphs/10/shift-swaps/99/cancel", {
      method: "POST",
    });
    expect(requestMock).toHaveBeenNthCalledWith(3, "containers/2/graphs/10/shift-swaps", {
      signal: controller.signal,
    });
  });
});
