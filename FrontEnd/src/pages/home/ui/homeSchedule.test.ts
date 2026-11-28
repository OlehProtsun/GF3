import { afterEach, expect, test, vi } from "vitest";
import { queryKeys } from "@shared/api/queryKeys";
import * as dashboard from "./homeDashboard";
import type { HomeDashboardData, HomeWhoWorksTodayRow } from "./homeDashboard";
import { getHomeDashboardQuery, getHomeEmployeeInitials, getHomeScheduleRows } from "./homeSchedule";

const day = "2026-11-26";
const row = (id: string, employeeId: number, shopId: number, fromTime: string, toTime: string): HomeWhoWorksTodayRow => ({
  id, employeeId, shopId, fromTime, toTime, containerId: 4, graphId: 9, dateLabel: day,
  employee: `Worker ${employeeId}`, shop: `Shop ${shopId}`, shift: `${fromTime} – ${toTime}`, hoursText: "2h",
});
const rows = [row("later", 1, 2, "13:00", "18:00"), row("past", 1, 2, "08:00", "10:00"),
  row("now", 2, 3, "10:00", "12:00"), row("split", 1, 2, "10:00", "13:00")];
const data: HomeDashboardData = { todayRows: rows, todayActiveEmployeesCount: 5, hasPartialData: false, shops: [], employees: new Map() };

afterEach(() => vi.restoreAllMocks());

test("shares the date-scoped Home query and passes the caller's abort signal", async () => {
  const load = vi.spyOn(dashboard, "loadHomeDashboard").mockResolvedValue(data);
  const query = getHomeDashboardQuery(day);
  const signal = new AbortController().signal;
  expect(query.queryKey).toEqual(queryKeys.home.dashboard(day));
  expect(query.staleTime).toBe(30_000);
  expect(await query.queryFn({ signal })).toBe(data);
  expect(load).toHaveBeenCalledExactlyOnceWith(day, signal);
  expect(getHomeDashboardQuery("2026-12-01").queryKey).not.toEqual(query.queryKey);
});

test("uses Warsaw time, counts unique workers at the selected shop and preserves cached order", () => {
  const now = new Date("2026-11-26T09:00:00Z");
  const all = getHomeScheduleRows(data, day, null, now);
  expect(all.employeeCount).toBe(5);
  expect(all.todayRows.map(({ row, status }) => [row.id, status])).toEqual([
    ["now", "in_progress"], ["split", "in_progress"], ["past", "completed"], ["later", "upcoming"],
  ]);
  const shop = getHomeScheduleRows(data, day, 2, now);
  expect(shop.employeeCount).toBe(1);
  expect(shop.todayRows.map(({ row }) => row.id)).toEqual(["split", "past", "later"]);
  expect(rows.map(item => item.id)).toEqual(["later", "past", "now", "split"]);
  expect(getHomeScheduleRows(undefined, day, null, now)).toEqual({ employeeCount: undefined, todayRows: [] });
  expect(getHomeScheduleRows(data, day, 999, now)).toEqual({ employeeCount: 0, todayRows: [] });
});

test("computes statuses at the UTC month boundary and handles Unicode employee initials", () => {
  const midnight = { ...data, todayRows: [row("midnight", 1, 2, "00:00", "01:00")] };
  const status = (at: string) => getHomeScheduleRows(midnight, "2026-12-01", null, new Date(at)).todayRows[0].status;
  expect(status("2026-11-30T22:59:59Z")).toBe("upcoming");
  expect(status("2026-11-30T23:00:00Z")).toBe("in_progress");
  expect(status("2026-12-01T00:00:00Z")).toBe("completed");
  expect(getHomeEmployeeInitials("  Łukasz\tЇвга  Nowak")).toBe("ŁЇ");
  expect(getHomeEmployeeInitials("   ")).toBe("");
});
