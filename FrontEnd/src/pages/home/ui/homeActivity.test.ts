import { beforeEach, describe, expect, test } from "vitest";
import { setLanguage } from "@shared/i18n";
import type { HomeActivityKind, ManagerHomeActivityItem } from "../api/managerHomeActivityApi";
import { getHomeActivityPresentation, getHomeActivityRoute, getWarsawShiftStatus, moveSelectedDay, parseHomeDay, setSelectedMonth, warsawToday } from "./homeActivity";
const item: ManagerHomeActivityItem = { id: "event:1", kind: "swap_created", occurredAtUtc: "2026-11-04T10:00:00Z", employeeName: "Actual Person", status: "open", requiresApproval: false, targetType: "graph", containerId: 8, graphId: 20, shopName: "Shop", shiftDate: "2026-11-06", fromTime: "08:00", toTime: "16:00" };
beforeEach(() => setLanguage("en"));
test("routes use distinct owning IDs and validate all destinations", () => {
  expect(getHomeActivityRoute(item)).toBe("/container/8/graphs/20");
  expect(getHomeActivityRoute({ ...item, containerId: 0 })).toBeNull();
  expect(getHomeActivityRoute({ ...item, graphId: 1.5 })).toBeNull();
  expect(getHomeActivityRoute({ ...item, kind: "availability_updated", targetType: "availability", availabilityGroupId: 15 })).toBe("/availability/15");
  expect(getHomeActivityRoute({ ...item, kind: "availability_updated", targetType: "availability" })).toBeNull();
  expect(getHomeActivityRoute({ ...item, kind: "urlop_requested", targetType: "urlop", shopId: 2, year: 2026, month: 12 })).toBe("/urlop/2?period=2026-12");
  expect(getHomeActivityRoute({ ...item, kind: "urlop_requested", targetType: "urlop", shopId: 2, year: 2026, month: 13 })).toBeNull();
});
test.each([
 ["2026-12-31", 1, "2027-01-01"], ["2028-02-28", 1, "2028-02-29"], ["2028-03-01", -1, "2028-02-29"], ["2026-03-01", -1, "2026-02-28"], ["0099-12-31", 1, "0100-01-01"],
])("moves calendar day %s safely", (day, delta, expected) => expect(moveSelectedDay(day, delta)).toBe(expected));
test("month selection clamps day and strict dates reject rollover", () => {
 expect(setSelectedMonth("2028-01-31", "2028-02")).toBe("2028-02-29");
 expect(setSelectedMonth("2026-01-31", "2026-02")).toBe("2026-02-28");
 expect(setSelectedMonth("2026-11-04", "2027-01")).toBe("2027-01-04");
 expect(() => parseHomeDay("2026-02-29")).toThrow(); expect(() => parseHomeDay("0000-01-01")).toThrow();
});
test.each([
 ["2026-11-04", "08:00", "13:00", "2026-11-04T06:59:00Z", "upcoming"],
 ["2026-11-04", "08:00", "13:00", "2026-11-04T07:00:00Z", "in_progress"],
 ["2026-11-04", "08:00", "13:00", "2026-11-04T12:00:00Z", "completed"],
 ["2026-11-04", "22:00", "02:00", "2026-11-05T00:59:00Z", "in_progress"],
 ["2026-11-04", "22:00", "02:00", "2026-11-05T01:00:00Z", "completed"],
 ["2026-07-04", "08:00", "13:00", "2026-07-04T06:00:00Z", "in_progress"],
 ["2026-11-04", "08:00", "08:00", "2026-11-05T06:59:00Z", "in_progress"],
])("Warsaw scheduled status %s %s–%s at %s", (day, from, to, now, expected) => expect(getWarsawShiftStatus(day, from, to, new Date(now))).toBe(expected));
test("today uses Warsaw even when UTC day differs", () => expect(warsawToday(new Date("2026-11-03T23:30:00Z"))).toBe("2026-11-04"));
describe("structured activity narratives", () => {
 const kinds: HomeActivityKind[] = ["swap_created", "swap_accepted", "swap_cancelled", "correction_requested", "correction_reviewed", "day_off_requested", "day_off_reviewed", "approval_requested", "approval_reviewed", "urlop_requested", "urlop_reviewed", "urlop_manager_created", "availability_updated"];
 test.each(kinds)("formats %s without translating personal names", kind => { const view = getHomeActivityPresentation({ ...item, kind }); expect(view.title).toContain("Actual Person"); expect(view.description).toContain("Shop"); expect(view.time).toBe("11:00"); });
 test("reviewed submission presents persistent status and pending flag correctly", () => {
  expect(getHomeActivityPresentation({ ...item, kind: "correction_requested", status: "approved" }).localizedStatus).toBe("Approved");
  expect(getHomeActivityPresentation({ ...item, kind: "approval_requested", status: "pending", requiresApproval: true }).tone).toBe("pending");
  expect(getHomeActivityPresentation(item).localizedStatus).toBe("Open");
 });
 test("exchange, original/requested correction and vacation ranges show real details", () => {
  const changed = { ...item, returnShiftDate: "2026-11-07", returnFromTime: "09:00", returnToTime: "17:00" };
  expect(getHomeActivityPresentation(changed).title).toContain("exchange");
  expect(getHomeActivityPresentation({ ...changed, kind: "correction_requested" }).description).toContain("08:00 – 16:00 → 7 Nov, 09:00 – 17:00");
  expect(getHomeActivityPresentation({ ...item, kind: "urlop_requested", vacationDays: ["2026-11-12", "2026-11-10", "2026-11-11", "2026-11-16"] }).description).toContain("10 Nov – 12 Nov, 16 Nov");
 });
 test.each(["pl", "uk"] as const)("localizes %s titles and Warsaw dates", language => {
  setLanguage(language); const view = getHomeActivityPresentation({ ...item, kind: "availability_updated", status: "updated" });
  expect(view.title).toContain("Actual Person"); expect(view.title).not.toContain("updated availability"); expect(view.localizedStatus).not.toBe("Updated"); expect(view.time).toBe("11:00");
 });
});

test.each(["swap_created", "swap_accepted", "swap_cancelled", "correction_requested", "correction_reviewed", "day_off_requested", "day_off_reviewed", "approval_requested", "approval_reviewed"] as const)("%s always uses graph ownership, without label parsing or Urlop fallback", kind => {
 const graph = { ...item, kind, employeeName: "/urlop/999", scheduleName: "Availability 123", shopName: "Container 444" };
 expect(getHomeActivityRoute(graph)).toBe("/container/8/graphs/20");
 expect(getHomeActivityRoute({ ...graph, targetType: "urlop", shopId: 1, year: 2026, month: 11 })).toBeNull();
 expect(getHomeActivityRoute({ ...graph, graphId: undefined })).toBeNull();
 expect(getHomeActivityRoute({ ...graph, requiresApproval: true, status: "pending" })).toBe("/container/8/graphs/20");
});
test("availability requires the group identifier, never the member employee", () => {
 const availability = { ...item, kind: "availability_updated" as const, targetType: "availability" as const, availabilityGroupId: 15, employeeId: 99 };
 expect(getHomeActivityRoute(availability)).toBe("/availability/15");
 expect(getHomeActivityRoute({ ...availability, availabilityGroupId: undefined })).toBeNull();
});
