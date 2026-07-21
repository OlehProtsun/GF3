import { describe, expect, it } from "vitest";
import { filterAcceptedShiftSwapHistory, filterShiftSwaps } from "./history";
import type { ShiftSwap } from "./types";

const baseSwap: ShiftSwap = {
  id: 1,
  scheduleId: 2,
  scheduleSlotId: 3,
  scheduleName: "June schedule",
  containerName: "Main",
  shopName: "Central",
  year: 2026,
  month: 6,
  dayOfMonth: 1,
  fromTime: "08:00",
  toTime: "16:00",
  fromEmployeeName: "Oleh Operator",
  acceptedByEmployeeName: "Amin Worker",
  visibility: "public",
  status: "accepted",
  createdAtUtc: "2026-06-24T08:00:00Z",
  acceptedAtUtc: "2026-06-25T09:30:00Z",
  shiftHours: 8,
  currentEmployeeHoursBefore: 0,
  currentEmployeeHoursAfter: 8,
  currentEmployeeWorkDaysBefore: 0,
  currentEmployeeWorkDaysAfter: 1,
  currentEmployeeFreeDaysBefore: 30,
  currentEmployeeFreeDaysAfter: 29,
  fromEmployeeHoursBefore: 8,
  fromEmployeeHoursAfter: 0,
  isManagerCreated: false,
  isCreatedByCurrentEmployee: false,
  isScheduleLocked: false,
  canAccept: false,
  canCancel: false,
};

describe("filterAcceptedShiftSwapHistory", () => {
  it("matches employee names case-insensitively", () => {
    expect(filterAcceptedShiftSwapHistory([baseSwap], "amin worker")).toEqual([baseSwap]);
  });

  it.each(["2026-06-01", "01/06/2026", "01 Jun 2026", "25/06/2026", "25 Jun 2026"])("matches date value %s", query => {
    expect(filterAcceptedShiftSwapHistory([baseSwap], query)).toEqual([baseSwap]);
  });

  it("requires every search term to match", () => {
    expect(filterAcceptedShiftSwapHistory([baseSwap], "oleh 25/06/2026")).toEqual([baseSwap]);
    expect(filterAcceptedShiftSwapHistory([baseSwap], "oleh july")).toEqual([]);
  });

  it("uses the same giver receiver date and schedule search for open swaps", () => {
    const openSwap = {
      ...baseSwap,
      status: "open" as const,
      targetEmployeeName: "Marta Receiver",
      acceptedByEmployeeName: null,
      acceptedAtUtc: null,
    };

    expect(filterShiftSwaps([openSwap], "marta 01/06/2026 june")).toEqual([openSwap]);
    expect(filterShiftSwaps([openSwap], "amin")).toEqual([]);
  });
});
