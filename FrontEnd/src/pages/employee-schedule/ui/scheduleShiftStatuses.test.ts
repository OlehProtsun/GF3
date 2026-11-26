import { expect, test } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwap } from "@entities/shift-swaps";
import { buildScheduleShiftStatuses, getScheduleStatusTone } from "./scheduleShiftStatuses";

test("shows own pending correction and relevant current or historical swap badges", () => {
  const graph = { id: 3, year: 2026, month: 8, slots: [
    { id: 1, slotNo: 1, employeeId: 1, dayOfMonth: 12, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" },
  ] } as EmployeeSchedule;
  const open = { id: 1, scheduleId: 3, scheduleSlotId: 1, year: 2026, month: 8, dayOfMonth: 12,
    fromEmployeeId: 1, fromEmployeeName: "Fixture", fromTime: "08:00", toTime: "12:00", status: "open", visibility: "public" } as ShiftSwap;
  const accepted = { ...open, id: 2, dayOfMonth: 10, status: "accepted" as const, acceptedByEmployeeId: 2 };
  graph.approvedCorrectionDays = [10, 12, 14, 14, 0, 32, 1.5];
  const statuses = buildScheduleShiftStatuses(graph, 1, "Fixture", new Set([12]), [
    open, accepted, { ...accepted }, { ...open, id: 3, dayOfMonth: 13 }, { ...open, id: 4, isFinished: true },
  ]);
  expect([...statuses.keys()]).toEqual([12, 10, 14]);
  expect(statuses.get(12)?.get(1)?.open).toHaveLength(1);
  expect(getScheduleStatusTone(statuses.get(12)!.get(1)!)).toBe("correction");
  expect(statuses.get(10)?.get(2)?.accepted).toHaveLength(1);
  expect(getScheduleStatusTone(statuses.get(10)!.get(2)!)).toBe("accepted");
  expect(statuses.get(12)?.get(1)?.correctionApproved).toBe(true);
  expect(getScheduleStatusTone({ ...statuses.get(12)!.get(1)!, correctionPending: false })).toBe("open");
  expect(statuses.get(10)?.get(2)?.correctionApproved).toBeUndefined();
  expect(getScheduleStatusTone(statuses.get(10)!.get(1)!)).toBe("corrected");
  expect(statuses.get(14)?.size).toBe(1);
  expect(getScheduleStatusTone(statuses.get(14)!.get(1)!)).toBe("corrected");
});
