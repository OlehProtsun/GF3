import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { employeeScheduleApi } from "./employeeScheduleApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("employee schedule api", () => {
  test("loads visible employee schedules with the supplied abort signal", () => {
    const controller = new AbortController();

    employeeScheduleApi.list(controller.signal);

    expect(requestMock).toHaveBeenCalledWith("employee-schedules", { signal: controller.signal });
  });
});
