import { describe, expect, test } from "vitest";
import type { EmployeeAvailabilityGroup } from "./types";
import { estimateAvailabilityHours } from "./estimatedHours";

function savedMonth(days = 31, kind = 1, intervalStr: string | null = null, month = 5, year = 2026): EmployeeAvailabilityGroup {
  return { id: 5, name: "May", year, month, canSubmit: true, isEditLocked: false, employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z",
    slots: Array.from({ length: days }, (_, index) => ({ id: index + 1, availabilityGroupMemberId: 50, dayOfMonth: index + 1, kind, intervalStr })) };
}

describe("estimateAvailabilityHours", () => {
  test("a fully unavailable submitted month is zero", () => {
    expect(estimateAvailabilityHours(savedMonth())).toEqual({ status: "ready", potentialHours: 0, lowerEstimate: 0, upperEstimate: 0 });
  });
  test("all-day availability uses eight hours per day", () => {
    expect(estimateAvailabilityHours(savedMonth(31, 0))).toEqual({ status: "ready", potentialHours: 248, lowerEstimate: 150, upperEstimate: 250 });
  });
  test("twenty six-hour days produce the requested indicative range", () => {
    const group = savedMonth();
    group.slots.slice(0, 20).forEach(slot => { slot.kind = 2; slot.intervalStr = "09:00–15:00"; });
    expect(estimateAvailabilityHours(group)).toEqual({ status: "ready", potentialHours: 120, lowerEstimate: 70, upperEstimate: 120 });
  });
  test("mixes available days, caps long intervals, and keeps minute precision before rounding", () => {
    const group = savedMonth();
    group.slots[0].kind = 0;
    group.slots[1] = { ...group.slots[1], kind: 2, intervalStr: "09:00 - 21:00" };
    group.slots[2] = { ...group.slots[2], kind: 2, intervalStr: "09:00 - 15:30" };
    expect(estimateAvailabilityHours(group)).toEqual({ status: "ready", potentialHours: 22.5, lowerEstimate: 15, upperEstimate: 25 });
  });
  test("short valid intervals never misleadingly display 0–0", () => {
    const group = savedMonth();
    group.slots[0] = { ...group.slots[0], kind: 2, intervalStr: "09:00 - 09:15" };
    expect(estimateAvailabilityHours(group)).toEqual({ status: "ready", potentialHours: .25, lowerEstimate: 0, upperEstimate: 5 });
  });
  test.each([[2026, 2, 28], [2028, 2, 29], [2100, 2, 28], [2400, 2, 29], [2026, 6, 30], [2026, 5, 31]])(
    "respects month length for %s-%s (%s days)", (year, month, days) => {
      expect(estimateAvailabilityHours(savedMonth(days, 0, null, month, year))).toMatchObject({ status: "ready", potentialHours: days * 8 });
      expect(estimateAvailabilityHours(savedMonth(days - 1, 0, null, month, year))).toEqual({ status: "unavailable" });
    });
  test.each([null, undefined, "", "invalid"])("requires a valid saved modification timestamp: %s", employeeLastModifiedAtUtc => {
    expect(estimateAvailabilityHours({ ...savedMonth(31, 0), employeeLastModifiedAtUtc })).toEqual({ status: "unsubmitted" });
  });
  test.each(["text", "21:00 - 09:00", "24:00 - 25:00", "09:00 - 09:00", ""])("does not estimate unsupported intervals: %s", intervalStr => {
    expect(estimateAvailabilityHours(savedMonth(31, 2, intervalStr))).toEqual({ status: "unavailable" });
  });
  test("rejects duplicates, missing, out-of-month days, unknown kinds and invalid calendar values", () => {
    const group = savedMonth();
    for (const day of [0, 32, 2, 1.5]) {
      expect(estimateAvailabilityHours({ ...group, slots: [{ ...group.slots[0], dayOfMonth: day }, ...group.slots.slice(1)] })).toEqual({ status: "unavailable" });
    }
    expect(estimateAvailabilityHours({ ...group, slots: [] })).toEqual({ status: "unavailable" });
    expect(estimateAvailabilityHours(savedMonth(31, 99))).toEqual({ status: "unavailable" });
    for (const invalid of [{ year: NaN }, { year: 0 }, { month: 0 }, { month: 13 }, { month: 5.5 }]) {
      expect(estimateAvailabilityHours({ ...group, ...invalid })).toEqual({ status: "unavailable" });
    }
  });
  test("supports serialized kind names and never includes team members' values", () => {
    const group = savedMonth();
    group.slots[0].kind = "Available";
    group.slots[1] = { ...group.slots[1], kind: "Preferred", intervalStr: "09:00 - 15:00" };
    group.slots[2].kind = "Unavailable";
    group.members = [{ employeeId: 99, firstName: "Other", lastName: "Employee", displayOrder: 0, slots: savedMonth(31, 0).slots }];
    expect(estimateAvailabilityHours(group)).toEqual({ status: "ready", potentialHours: 14, lowerEstimate: 10, upperEstimate: 15 });
    expect(estimateAvailabilityHours(savedMonth())).toMatchObject({ potentialHours: 0 });
  });
  test("legacy interval values retain their own day-editor meaning, regardless of the stored kind", () => {
    const group = savedMonth();
    group.slots[0] = { ...group.slots[0], kind: 0, intervalStr: "09:00 - 15:00" };
    group.slots[1] = { ...group.slots[1], kind: 1, intervalStr: "09:00 - 15:00" };
    expect(estimateAvailabilityHours(group)).toMatchObject({ status: "ready", potentialHours: 12 });
    group.slots[0].intervalStr = "morning";
    expect(estimateAvailabilityHours(group)).toEqual({ status: "unavailable" });
  });
});
