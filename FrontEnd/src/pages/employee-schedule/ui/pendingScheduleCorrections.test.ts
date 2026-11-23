import { expect, test } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftCorrectionRequest } from "@entities/shift-corrections";
import { getPendingCorrectionDays } from "./pendingScheduleCorrections";

const schedule: EmployeeSchedule = {
  id: 9, containerId: 2, containerName: "Main", shopId: 3, shopName: "Central",
  name: "August", year: 2026, month: 8, publicationStatus: "public",
  slots: [{ id: 21, dayOfMonth: 12, slotNo: 1, employeeId: 7, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" }],
};
const request: ShiftCorrectionRequest = {
  id: 1, containerId: 2, scheduleId: 9, scheduleSlotId: 21, isDayOffRequest: false,
  scheduleName: "August", shopName: "Central", year: 2026, month: 8, dayOfMonth: 12,
  employeeId: 7, employeeName: "Fixture", originalFromTime: "08:00", originalToTime: "12:00",
  requestedFromTime: "08:00", requestedToTime: "12:30", comment: null, status: "pending",
  createdAtUtc: "2026-08-01T00:00:00Z",
};

test("shows badges for own pending time adjustments, additions and matching removals", () => {
  expect(getPendingCorrectionDays(schedule, 7, [
    request,
    { ...request, id: 2, scheduleSlotId: null, dayOfMonth: 13, isDayOffRequest: true, requestType: "addShift" },
  ])).toEqual(new Set([12, 13]));
  expect(getPendingCorrectionDays(schedule, 7, [{ ...request, scheduleSlotId: null, requestType: "removeShift",
    originalShifts: [{ slotId: 21, fromTime: "08:00", toTime: "12:00" }] }])).toEqual(new Set([12]));
});

test("ignores other staff, schedules, months, reviewed requests and stale original times", () => {
  const requests: ShiftCorrectionRequest[] = [
    { ...request, employeeId: 8 }, { ...request, scheduleId: 10 }, { ...request, month: 7 },
    { ...request, status: "approved" }, { ...request, status: "rejected" }, { ...request, originalToTime: "12:30" },
    { ...request, requestType: "removeShift", originalShifts: [{ slotId: 21, fromTime: "08:00", toTime: "12:30" }] },
  ];
  expect(getPendingCorrectionDays(schedule, 7, requests)).toEqual(new Set());
  expect(getPendingCorrectionDays(schedule, null, [request])).toEqual(new Set());
});
