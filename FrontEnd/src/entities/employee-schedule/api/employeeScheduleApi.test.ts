import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());
const requestFileMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
  requestFile: requestFileMock,
}));

import { employeeScheduleApi } from "./employeeScheduleApi";

beforeEach(() => {
  requestMock.mockReset();
  requestFileMock.mockReset();
});

describe("employee schedule api", () => {
  test("exports PDF through the shared authenticated file transport", async () => {
    const controller = new AbortController();
    const file = { blob: new Blob(["%PDF-"], { type: "application/pdf" }), fileName: "schedule.pdf", contentType: "application/pdf" };
    requestFileMock.mockResolvedValue(file);
    expect(await employeeScheduleApi.exportPdf(33, controller.signal)).toBe(file);
    expect(requestFileMock).toHaveBeenCalledWith("employee-schedules/33/export/pdf", { signal: controller.signal });
  });

  test("propagates transport ProblemDetails errors", async () => {
    const error = new Error("Schedule was not found.");
    requestFileMock.mockRejectedValue(error);
    await expect(employeeScheduleApi.exportPdf(33)).rejects.toBe(error);
  });
  test("loads visible employee schedules with the supplied abort signal", () => {
    const controller = new AbortController();

    employeeScheduleApi.list(controller.signal);

    expect(requestMock).toHaveBeenCalledWith("employee-schedules", { signal: controller.signal });
  });
});
