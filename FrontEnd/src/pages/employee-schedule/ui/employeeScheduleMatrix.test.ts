import { expect, test } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { buildScheduleMatrixCellMap, buildScheduleMatrixDisplayCellMap } from "./employeeScheduleMatrix";

test("merges repeated and touching shifts for display while preserving related cells", () => {
  const slot = { id: 7, slotNo: 1, employeeId: 1, dayOfMonth: 12, fromTime: "09:00", toTime: "13:00", status: "ASSIGNED" };
  const graph = {
    slots: [slot, { ...slot, id: 8 }, { ...slot, id: 9, fromTime: "13:00", toTime: "15:00" }],
    relatedScheduleAssignments: [
      { employeeId: 1, dayOfMonth: 12, scheduleId: 2, scheduleName: "Early shop" },
      { employeeId: 2, dayOfMonth: 12, scheduleId: 3, scheduleName: " Other shop " },
      { employeeId: 2, dayOfMonth: 12, scheduleId: 4, scheduleName: "Other shop" },
    ],
  } as EmployeeSchedule;
  const own = buildScheduleMatrixCellMap(graph, 1);
  expect(own).toEqual({ "1:12": "09:00 - 15:00" });
  expect(buildScheduleMatrixDisplayCellMap(graph, own)).toEqual({
    cellMap: { "1:12": "09:00 - 15:00, Early shop", "2:12": "Other shop" },
    mutedSuffixMap: { "1:12": "Early shop", "2:12": "Other shop" },
  });
  expect(own).not.toHaveProperty("2:12");
  expect(graph.slots).toHaveLength(3);
});
