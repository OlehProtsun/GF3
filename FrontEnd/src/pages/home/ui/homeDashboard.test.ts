import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { containersApi } from "@entities/containers";
import { employeesApi } from "@entities/employees/api/employeesApi";
import { shopsApi } from "@entities/shops/api/shopsApi";
import { loadHomeDashboard } from "./homeDashboard";
vi.mock("@entities/containers", async importOriginal => ({ ...await importOriginal<typeof import("@entities/containers")>(), containersApi: { list: vi.fn(), listGraphs: vi.fn(), listGraphSlots: vi.fn(), listGraphEmployees: vi.fn(), listGraphCellStyles: vi.fn() } }));
vi.mock("@entities/employees/api/employeesApi", () => ({ employeesApi: { list: vi.fn() } }));
vi.mock("@entities/shops/api/shopsApi", () => ({ shopsApi: { list: vi.fn() } }));
const slot = { id: 1, scheduleId: 10, dayOfMonth: 4, slotNo: 1, fromTime: "08:00", toTime: "16:00", employeeId: 1, status: "Working" };
beforeEach(() => {
 vi.resetAllMocks(); setLanguage("en");
 vi.mocked(containersApi.list).mockResolvedValue([{ id: 8, name: "A" }, { id: 9, name: "B" }]);
 vi.mocked(employeesApi.list).mockResolvedValue([{ id: 1, firstName: "Actual", lastName: "Person", hasLoginAccount: false, isOnline: false }]);
 vi.mocked(shopsApi.list).mockResolvedValue([{ id: 2, name: "Shop", address: "Street" }]);
 vi.mocked(containersApi.listGraphs).mockImplementation(async containerId => [{ id: containerId + 2, containerId, shopId: 2, year: 2026, month: 11 } as never]);
 vi.mocked(containersApi.listGraphSlots).mockImplementation(async (_, graphId) => [{ ...slot, scheduleId: graphId }]);
});
test("selected date returns genuine shifts, owning containers, duration and distinct count without previews", async () => {
 const data = await loadHomeDashboard("2026-11-04");
 expect(data.todayRows).toHaveLength(2); expect(data.todayActiveEmployeesCount).toBe(1); expect(data.hasPartialData).toBe(false);
 expect(data.todayRows.map(row => [row.containerId, row.graphId])).toEqual([[8, 10], [9, 11]]);
 expect(data.todayRows[0]).toMatchObject({ employee: "Actual Person", shop: "Shop", hoursText: "8h 0m" });
 expect(containersApi.listGraphEmployees).not.toHaveBeenCalled(); expect(containersApi.listGraphCellStyles).not.toHaveBeenCalled();
});
test.each(["2026-11-03", "2026-11-05", "2027-01-01", "2025-12-31"])("loads selected historical/future day %s", async date => {
 expect((await loadHomeDashboard(date)).todayRows).toEqual([]);
 if (!date.startsWith("2026-11")) expect(containersApi.listGraphSlots).not.toHaveBeenCalled();
});
test("duplicate employee shifts retained, duplicate records removed, overnight hours correct", async () => {
 vi.mocked(containersApi.listGraphSlots).mockResolvedValue([slot, slot, { ...slot, id: 2, fromTime: "22:00", toTime: "02:00" }, { ...slot, id: 3, employeeId: -1 }]);
 const data = await loadHomeDashboard("2026-11-04"); expect(data.todayRows).toHaveLength(4); expect(data.todayActiveEmployeesCount).toBe(1);
 expect(data.todayRows[2].hoursText).toBe("4h 0m");
});
test("partial graph reads and missing names keep usable data with explicit warning", async () => {
 vi.mocked(containersApi.listGraphs).mockRejectedValueOnce(new Error("offline"));
 vi.mocked(employeesApi.list).mockRejectedValue(new Error("offline")); vi.mocked(shopsApi.list).mockRejectedValue(new Error("offline"));
 const data = await loadHomeDashboard("2026-11-04"); expect(data.hasPartialData).toBe(true); expect(data.todayRows).toHaveLength(1);
 expect(data.todayRows[0]).toMatchObject({ employee: "Employee 1", shop: "Shop 2" });
});
test("partial slot failures and an empty successful response remain distinguishable", async () => {
 vi.mocked(containersApi.listGraphSlots).mockRejectedValueOnce(new Error("offline"));
 expect((await loadHomeDashboard("2026-11-04")).hasPartialData).toBe(true);
 vi.mocked(containersApi.listGraphSlots).mockResolvedValue([]);
 expect(await loadHomeDashboard("2026-11-04")).toMatchObject({ todayRows: [], hasPartialData: false, todayActiveEmployeesCount: 0 });
});
test("aborts before starting and after an awaited source", async () => {
 const controller = new AbortController(); controller.abort();
 await expect(loadHomeDashboard("2026-11-04", controller.signal)).rejects.toMatchObject({ name: "AbortError" }); expect(containersApi.list).not.toHaveBeenCalled();
 const second = new AbortController(); vi.mocked(containersApi.list).mockImplementation(async () => { second.abort(); return []; });
 await expect(loadHomeDashboard("2026-11-04", second.signal)).rejects.toMatchObject({ name: "AbortError" });
});
test("invalid input and missing container list are visible failures", async () => {
 await expect(loadHomeDashboard("2026-02-30")).rejects.toThrow();
 vi.mocked(containersApi.list).mockRejectedValue(new Error("offline")); await expect(loadHomeDashboard("2026-11-04")).rejects.toThrow("offline");
});
