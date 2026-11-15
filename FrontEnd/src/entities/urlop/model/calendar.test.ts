import { describe, expect, test } from "vitest";
import { approvedKeys, calendarMonths, graphDateRange, isoDate, monthDays, monthStartOffset, occupiedDays, toggleDay, validSelection, warsawToday } from "./calendar";
import type { UrlopRequest } from "./types";

describe("vacation calendar", () => {
  test("historical years keep their calendar weekday instead of JavaScript's 1900 offset", () => {
    expect(monthStartOffset(1, 1)).toBe(0);
    expect(monthStartOffset(2026, 11)).toBe(6);
    expect(monthStartOffset(2028, 2)).toBe(1);
  });
  test("twelve months cross December without timezone conversion", () => {
    expect(calendarMonths(2026, 12)).toHaveLength(12);
    expect(calendarMonths(2026, 12).at(-1)).toEqual({ year: 2027, month: 11 });
    expect(isoDate(2028, 2, 29)).toBe("2028-02-29");
    expect(warsawToday(new Date("2026-10-31T23:30:00Z"))).toBe("2026-11-01");
  });
  test.each([[2026, 2, 28], [2028, 2, 29], [2026, 4, 30], [2026, 1, 31]])("month %s-%s contains %s days", (year, month, count) => {
    expect(monthDays(year, month)).toHaveLength(count);
    expect(validSelection(year, month, [count + 1], new Set())).toBe(false);
  });
  test("selection, duplicates, rejected retry and past days", () => {
    const requests = [{ status: "pending", days: [3] }, { status: "approved", days: [4] }, { status: "rejected", days: [5] }] as UrlopRequest[];
    expect([...occupiedDays(requests)]).toEqual([3, 4]);
    expect(toggleDay([3, 5], 3)).toEqual([5]);
    expect(toggleDay([5], 3)).toEqual([3, 5]);
    for (const days of [[], [3], [5, 5], [1]]) expect(validSelection(2026, 11, days, occupiedDays(requests), "2026-11-02")).toBe(false);
    expect(validSelection(2026, 11, [5], occupiedDays(requests), "2026-11-02")).toBe(true);
  });
  test("approved keys retain shop and period scope; range batches sparse graphs", () => {
    const days = [{ shopId: 1, employeeId: 2, date: "2026-11-03" }, { shopId: 2, employeeId: 2, date: "2026-11-04" }, { shopId: 1, employeeId: 2, date: "2026-12-05" }];
    expect([...approvedKeys(days, 1, 2026, 11)]).toEqual(["2:3"]);
    expect(graphDateRange([{ year: 2026, month: 12 }, { year: 2026, month: 2 }])).toEqual({ from: "2026-02-01", to: "2026-12-31" });
    expect(graphDateRange([])).toEqual({ from: "", to: "" });
  });
});
