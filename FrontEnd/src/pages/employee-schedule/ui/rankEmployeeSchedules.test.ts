import { expect, test } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { rankEmployeeSchedules } from "./rankEmployeeSchedules";

test("ranks the current Warsaw month first, then the period, distinct own shifts and name", () => {
  const slot = { id: 7, slotNo: 1, employeeId: 1, dayOfMonth: 12, fromTime: "09:00", toTime: "13:00", status: "ASSIGNED" };
  const schedule = (id: number, name: string, month: number, slots: EmployeeSchedule["slots"]) =>
    ({ id, name, year: 2026, month, slots }) as EmployeeSchedule;
  const entries = [
    schedule(42, "Later", 9, [slot]),
    schedule(4, "Amber", 8, [{ ...slot, employeeId: 2 }]),
    schedule(5, "Zulu", 8, [slot, { ...slot, id: 9, fromTime: "9:00" }]),
    schedule(8, "Amber", 8, [slot]),
    schedule(6, "Two shifts", 8, [slot, { ...slot, id: 8, dayOfMonth: 13 }]),
  ];
  const ranked = rankEmployeeSchedules(entries, 1, "2026-08");
  expect(ranked.map(graph => graph.id)).toEqual([6, 8, 5, 4, 42]);
  expect(entries.map(graph => graph.id)).toEqual([42, 4, 5, 8, 6]);
});
