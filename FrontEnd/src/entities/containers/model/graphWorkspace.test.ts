import { describe, expect, test } from "vitest";
import {
  GRAPH_EMPTY_MARK,
  applyIntervalsToGraphSlots,
  argbToCssColor,
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphDraftSlots,
  buildGraphMatrixColumns,
  buildGraphRelatedScheduleHintData,
  buildGraphStyleMap,
  buildGraphSummaryRows,
  buildGraphTotals,
  clampGraphMonth,
  clampGraphYear,
  diffGraphSlots,
  formatGraphIntervals,
  formatGraphMonthYear,
  getGraphCellKey,
  getGraphDaysInMonth,
  getGraphWeekdayLabel,
  isGraphWeekend,
  mergeGraphIntervalsForDisplay,
  normalizeGraphCellValue,
  parseGraphCellContent,
  rgbHexToArgb,
  sanitizeGraphCellMap,
  sortGraphItemsByDisplayOrder,
  tryParseGraphIntervals,
  validateGraphCellMap,
} from "./graphWorkspace";
import type { Employee } from "@entities/employees/model/types";
import type { Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "./types";

const graph: Graph = {
  id: 1,
  containerId: 1,
  shopId: 1,
  name: "May graph",
  year: 2026,
  month: 5,
  publicationStatus: "private",
  peoplePerShift: 2,
  shift1Time: "08:00 - 12:00",
  shift2Time: "",
  maxHoursPerEmpMonth: 160,
  maxConsecutiveDays: 5,
  maxConsecutiveFull: 3,
  maxFullPerMonth: 10,
  note: null,
  availabilityGroupId: null,
};

const employeesById = new Map<number, Employee>([
  [1, { id: 1, firstName: "Ada", lastName: "Lovelace", hasLoginAccount: true, isOnline: false }],
  [2, { id: 2, firstName: "Grace", lastName: "Hopper", hasLoginAccount: true, isOnline: false }],
]);

describe("container graph workspace model", () => {
  test("clamps graph dates and formats month and weekday metadata", () => {
    expect(clampGraphMonth(Number.NaN)).toBe(1);
    expect(clampGraphMonth(13.8)).toBe(12);
    expect(clampGraphYear(1999)).toBe(2000);
    expect(clampGraphYear(5001)).toBe(4000);
    expect(getGraphDaysInMonth(2028, 2)).toBe(29);
    expect(formatGraphMonthYear(2026, 5)).toBe("May 2026");
    expect(getGraphWeekdayLabel(2026, 5, 10)).toBe("su.");
    expect(isGraphWeekend(2026, 5, 10)).toBe(true);
  });

  test("sorts graph columns and calculates employee totals", () => {
    const graphEmployees: GraphEmployee[] = [
      { id: 3, scheduleId: 1, employeeId: 3, minHoursMonth: null, displayOrder: -1 },
      { id: 2, scheduleId: 1, employeeId: 2, minHoursMonth: 80, displayOrder: 1 },
      { id: 1, scheduleId: 1, employeeId: 1, minHoursMonth: 100, displayOrder: 1 },
    ];
    const slots: GraphSlot[] = [
      { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:30", employeeId: 1, status: 1 },
      { id: 2, scheduleId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "22:00", toTime: "02:00", employeeId: 2, status: 1 },
    ];

    expect(sortGraphItemsByDisplayOrder([
      { employeeId: 3, label: "Zoe", displayOrder: null },
      { employeeId: 2, label: "Amy", displayOrder: 1 },
      { employeeId: 1, label: "Amy", displayOrder: 1 },
    ])).toEqual([
      { employeeId: 1, label: "Amy", displayOrder: 1 },
      { employeeId: 2, label: "Amy", displayOrder: 1 },
      { employeeId: 3, label: "Zoe", displayOrder: null },
    ]);
    expect(buildGraphMatrixColumns(graphEmployees, employeesById, slots)).toMatchObject([
      { employeeId: 1, label: "Ada Lovelace", totalText: "4h 30m", minHoursMonth: 100 },
      { employeeId: 2, label: "Grace Hopper", totalText: "4h 0m", minHoursMonth: 80 },
      { employeeId: 3, label: "Employee 3", totalText: "0h 0m", minHoursMonth: null },
    ]);
  });

  test("merges, formats, parses, and normalizes graph cell intervals", () => {
    const merged = mergeGraphIntervalsForDisplay([
      { fromTime: "09:00", toTime: "11:00" },
      { fromTime: "10:30", toTime: "12:00" },
      { fromTime: "13:00", toTime: "15:00" },
      { fromTime: "09:00", toTime: "11:00" },
      { fromTime: "bad", toTime: "15:00" },
    ]);

    expect(merged).toEqual([
      { from: "09:00", to: "12:00" },
      { from: "13:00", to: "15:00" },
    ]);
    expect(formatGraphIntervals(merged)).toBe("09:00 - 12:00, 13:00 - 15:00");
    expect(formatGraphIntervals([])).toBe(GRAPH_EMPTY_MARK);
    expect(tryParseGraphIntervals("13-15, 09:00 - 12:00")).toEqual({
      ok: true,
      value: [
        { from: "09:00", to: "12:00" },
        { from: "13:00", to: "15:00" },
      ],
    });
    expect(tryParseGraphIntervals("9-15,15-21")).toEqual({
      ok: true,
      value: [
        { from: "09:00", to: "15:00" },
        { from: "15:00", to: "21:00" },
      ],
    });
    expect(normalizeGraphCellValue("9:30-15,17:45-22")).toBe("09:30 - 15:00, 17:45 - 22:00");
    expect(normalizeGraphCellValue("8-14,17-22:30")).toBe("08:00 - 14:00, 17:00 - 22:30");
    expect(tryParseGraphIntervals("09-12, 09:00-12:00")).toEqual({
      ok: false,
      error: "The same time range cannot be entered more than once.",
    });
    expect(tryParseGraphIntervals("09-13, 12-15")).toEqual({
      ok: false,
      error: "Time ranges in one cell cannot overlap.",
    });
    expect(tryParseGraphIntervals("08-09, 10-11, 12-13, 14-15, 16-17")).toEqual({
      ok: false,
      error: "A cell can contain no more than 4 time ranges.",
    });
    expect(tryParseGraphIntervals("15:00 - 13:00")).toMatchObject({ ok: false });
    expect(parseGraphCellContent("Manager note")).toEqual({ kind: "text", value: "Manager note" });
    expect(parseGraphCellContent("15:00 - 13:00")).toMatchObject({ kind: "invalid" });
    expect(normalizeGraphCellValue("9-12")).toBe("09:00 - 12:00");
    expect(normalizeGraphCellValue("  note  ")).toBe("note");
  });

  test("keeps touching split shifts separate when rebuilding cell values", () => {
    const touchingSlots: GraphSlot[] = [
      { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "15:00", employeeId: 1, status: 1 },
      { id: 2, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "15:00", toTime: "21:00", employeeId: 1, status: 1 },
    ];

    expect(buildGraphCellMap(touchingSlots)).toEqual({
      "1:1": "09:00 - 15:00, 15:00 - 21:00",
    });
  });

  test("builds and sanitizes graph cell and style maps", () => {
    const slots: GraphSlot[] = [
      { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "10:00", employeeId: 1, status: 1 },
      { id: 2, scheduleId: 1, dayOfMonth: 1, slotNo: 2, fromTime: "09:30", toTime: "12:00", employeeId: 1, status: 1 },
      { id: 3, scheduleId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "10:00", toTime: "12:00", employeeId: null, status: 0 },
    ];
    const styles: GraphCellStyle[] = [
      { id: 5, scheduleId: 1, employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -65536, textColorArgb: -16777216 },
    ];

    expect(buildGraphCellMap(slots)).toEqual({ "1:1": "08:00 - 12:00" });
    expect(sanitizeGraphCellMap({ "1:1": "A", "1:32": "B", "2:1": "C", "bad:key": "D" }, [1], 2026, 5))
      .toEqual({ "1:1": "A" });
    expect(validateGraphCellMap({ "1:1": "15:00 - 13:00" }, [1], 2026, 5)).toEqual({
      "1:1": "From must be earlier than To.",
    });
    expect(argbToCssColor(-65536)).toBe("rgba(255, 0, 0, 1)");
    expect(rgbHexToArgb("#336699")).toBe(-13408615);
    expect(rgbHexToArgb("336699")).toBeNull();
    expect(buildGraphStyleMap(styles)).toEqual({
      "1:1": {
        id: 5,
        backgroundColor: "rgba(255, 0, 0, 1)",
        textColor: "rgba(0, 0, 0, 1)",
        backgroundColorArgb: -65536,
        textColorArgb: -16777216,
      },
    });
  });

  test("builds draft slots by assigning matching vacant slots, reusing ids, and reporting invalid cells", () => {
    const existingSlots: GraphSlot[] = [
      { id: 10, scheduleId: 20, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: null, status: 0 },
      { id: 11, scheduleId: 20, dayOfMonth: 2, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 5, status: 1 },
      { id: 12, scheduleId: 20, dayOfMonth: 3, slotNo: 1, fromTime: "10:00", toTime: "12:00", employeeId: 5, status: 1 },
      { id: 13, scheduleId: 20, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "10:00", employeeId: 99, status: 1 },
    ];

    const result = buildGraphDraftSlots({
      scheduleId: 20,
      existingSlots,
      employeeIds: [5],
      year: 2026,
      month: 2,
      cellMap: {
        [getGraphCellKey(5, 1)]: "09:00 - 13:00",
        [getGraphCellKey(5, 2)]: "08:00 - 12:00, 13:00 - 15:00",
        [getGraphCellKey(5, 3)]: "-",
      },
    });

    expect(result.errors).toEqual({});
    expect(result.slots).toEqual([
      { id: 10, scheduleId: 20, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 5, status: 1 },
      { id: 11, scheduleId: 20, dayOfMonth: 2, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 5, status: 1 },
      { id: -2, scheduleId: 20, dayOfMonth: 2, slotNo: 1, fromTime: "13:00", toTime: "15:00", employeeId: 5, status: 1 },
    ]);
    expect(diffGraphSlots(existingSlots, result.slots)).toEqual({
      create: [result.slots[2]],
      update: [result.slots[0]],
      remove: [existingSlots[2], existingSlots[3]],
    });

    const invalid = buildGraphDraftSlots({
      scheduleId: 20,
      existingSlots,
      employeeIds: [5],
      year: 2026,
      month: 2,
      cellMap: { [getGraphCellKey(5, 1)]: "13:00 - 09:00" },
    });

    expect(invalid.slots).toEqual(existingSlots);
    expect(invalid.errors).toEqual({ "5:1": "From must be earlier than To." });
  });

  test("applies intervals directly with slot number gaps and no-op clears", () => {
    const slots: GraphSlot[] = [
      { id: 1, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "12:00", employeeId: 7, status: 1 },
      { id: 2, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "13:00", toTime: "17:00", employeeId: null, status: 0 },
      { id: 3, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "18:00", toTime: "20:00", employeeId: 8, status: 1 },
    ];

    applyIntervalsToGraphSlots(2, slots, 1, 7, [
      { from: "13:00", to: "17:00" },
      { from: "18:00", to: "20:00" },
    ]);

    expect(slots).toEqual([
      { id: 2, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "13:00", toTime: "17:00", employeeId: 7, status: 1 },
      { id: 3, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "18:00", toTime: "20:00", employeeId: 8, status: 1 },
      { id: -1, scheduleId: 2, dayOfMonth: 1, slotNo: 2, fromTime: "18:00", toTime: "20:00", employeeId: 7, status: 1 },
    ]);

    applyIntervalsToGraphSlots(2, slots, 1, 7, []);
    expect(slots).toEqual([
      { id: 3, scheduleId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "18:00", toTime: "20:00", employeeId: 8, status: 1 },
    ]);
  });

  test("detects staffing conflicts for coverage gaps, unassigned slots, and overlaps", () => {
    const conflictMap = buildGraphConflictDayMap(graph, [
      { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 1, status: 1 },
      { id: 2, scheduleId: 1, dayOfMonth: 1, slotNo: 2, fromTime: "08:00", toTime: "12:00", employeeId: 2, status: 1 },
      { id: 3, scheduleId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "08:00", toTime: "10:00", employeeId: 1, status: 1 },
      { id: 4, scheduleId: 1, dayOfMonth: 2, slotNo: 2, fromTime: "08:00", toTime: "12:00", employeeId: 2, status: 1 },
      { id: 5, scheduleId: 1, dayOfMonth: 3, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 1, status: 1 },
      { id: 6, scheduleId: 1, dayOfMonth: 3, slotNo: 2, fromTime: "09:00", toTime: "11:00", employeeId: 1, status: 1 },
      { id: 7, scheduleId: 1, dayOfMonth: 4, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: null, status: 0 },
    ]);

    expect(conflictMap[1]).toBe(false);
    expect(conflictMap[2]).toBe(true);
    expect(conflictMap[3]).toBe(true);
    expect(conflictMap[4]).toBe(true);
  });

  test("builds related schedule hints for both empty and filled current employee cells", () => {
    const data = buildGraphRelatedScheduleHintData({
      currentGraph: graph,
      columns: [
        { employeeId: 1, kind: "employee" },
        { employeeId: -1, kind: "manual" },
      ],
      cellMap: {
        [getGraphCellKey(1, 1)]: "08:00 - 12:00",
        [getGraphCellKey(1, 2)]: GRAPH_EMPTY_MARK,
      },
      relatedGraphs: [
        {
          graph: { id: 2, name: "Beta", year: 2026, month: 5 },
          slots: [
            { id: 20, scheduleId: 2, dayOfMonth: 2, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 1, status: 1 },
          ],
        },
        {
          graph: { id: 3, name: "Alpha", year: 2026, month: 5 },
          slots: [
            { id: 30, scheduleId: 3, dayOfMonth: 2, slotNo: 1, fromTime: "10:00", toTime: "14:00", employeeId: 1, status: 1 },
            { id: 31, scheduleId: 3, dayOfMonth: 1, slotNo: 1, fromTime: "10:00", toTime: "14:00", employeeId: 1, status: 1 },
          ],
        },
        {
          graph,
          slots: [
            { id: 40, scheduleId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "12:00", toTime: "16:00", employeeId: 1, status: 1 },
          ],
        },
      ],
    });

    expect(data.visualHintMap).toEqual({
      "1:1": "Alpha",
      "1:2": "Alpha, Beta",
    });
    expect(data.detailMap["1:1"]).toMatchObject({
      employeeId: 1,
      dayOfMonth: 1,
      visualHint: "Alpha",
      relatedGraphs: [
        { graphId: 3, graphName: "Alpha", intervalsText: "10:00 - 14:00" },
      ],
    });
    expect(data.detailMap["1:2"]).toMatchObject({
      employeeId: 1,
      dayOfMonth: 2,
      visualHint: "Alpha, Beta",
      relatedGraphs: [
        { graphId: 3, graphName: "Alpha", intervalsText: "10:00 - 14:00" },
        { graphId: 2, graphName: "Beta", intervalsText: "09:00 - 13:00" },
      ],
    });
    expect(data.detailMap["1:2"].relatedGraphs[0].dayValues[1]).toMatchObject({
      dayOfMonth: 2,
      value: "10:00 - 14:00",
      isWorked: true,
    });
  });

  test("builds totals and summary rows from graph employees and slots", () => {
    const graphEmployees: GraphEmployee[] = [
      { id: 1, scheduleId: 1, employeeId: 1, minHoursMonth: 80, displayOrder: 1 },
      { id: 2, scheduleId: 1, employeeId: 2, minHoursMonth: 80, displayOrder: 2 },
    ];
    const slots: GraphSlot[] = [
      { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 1, status: 1 },
      { id: 2, scheduleId: 1, dayOfMonth: 1, slotNo: 1, fromTime: "13:00", toTime: "16:30", employeeId: 1, status: 1 },
      { id: 3, scheduleId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "22:00", toTime: "02:00", employeeId: 2, status: 1 },
    ];

    expect(buildGraphTotals(graphEmployees, slots, employeesById)).toMatchObject({
      totalEmployees: 2,
      totalMinutes: 690,
      totalHoursText: "11h 30m",
      totalEmployeesListText: "Ada Lovelace, Grace Hopper",
      perEmployeeText: {
        1: "7h 30m",
        2: "4h 0m",
      },
    });

    const rows = buildGraphSummaryRows({ year: 2026, month: 5 }, graphEmployees, employeesById, slots);

    expect(rows[0]).toMatchObject({
      employeeId: 1,
      employee: "Ada Lovelace",
      workDays: 1,
      freeDays: 30,
      sum: "7h 30m",
    });
    expect(rows[0].dayRows).toHaveLength(2);
    expect(rows[0].days[0]).toEqual({ from: "08:00", to: "12:00", hours: "4" });
    expect(rows[0].dayRows[1][0]).toEqual({ from: "13:00", to: "16:30", hours: "3h 30m" });
    expect(rows[0].dayRows[1][1]).toEqual({ from: "", to: "", hours: "" });
    expect(rows[1]).toMatchObject({
      employeeId: 2,
      workDays: 1,
      sum: "4",
    });
    expect(rows[1].days[1]).toEqual({ from: "22:00", to: "02:00", hours: "4" });
  });
});
