import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@shared/api/httpClient";
import { employeeUiStateApi } from "./employeeUiStateApi";
vi.mock("@shared/api/httpClient", () => ({ request: vi.fn() }));
beforeEach(() => vi.mocked(request).mockReset());
describe("notification read synchronization", () => {
  it("deduplicates and batches large read sets within the server limit", async () => {
    const ids = Array.from({length: 650}, (_, i) => "swap-public:" + i);
    await employeeUiStateApi.markNotificationsRead([...ids, ids[0]!, " "]);
    expect(request).toHaveBeenCalledTimes(3);
    expect(vi.mocked(request).mock.calls.map(call => (call[1]!.body as {notificationIds: string[]}).notificationIds.length)).toEqual([300, 300, 50]);
  });
  it("surfaces failed sync and skips empty input", async () => {
    await employeeUiStateApi.markNotificationsRead([]);
    expect(request).not.toHaveBeenCalled();
    vi.mocked(request).mockRejectedValueOnce(new Error("offline"));
    await expect(employeeUiStateApi.markNotificationsRead(["swap-public:1"])).rejects.toThrow("offline");
  });
});
