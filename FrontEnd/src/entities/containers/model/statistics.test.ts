import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { describe, expect, test } from "vitest";
import {
  buildContainerGraphSummaries,
  buildContainerStatistics,
  buildPreviewList,
  filterGraphSummaries,
  formatHoursCell,
  formatHoursMinutes,
  getSlotDurationMinutes,
} from "./statistics";
import type { Graph, GraphEmployee, GraphSlot } from "./types";
import type { Employee } from "@entities/employees/model/types";
import type { Shop } from "@entities/shops/model/types";

beforeEnglishTest(() => setTestLanguage("en"));

const graph: Graph = {
  id: 10,
  containerId: 1,
  shopId: 2,
  name: "May Schedule",
  year: 2026,
  month: 5,
  publicationStatus: "public",
  peoplePerShift: 1,
  shift1Time: "08:00 - 16:00",
  shift2Time: "16:00 - 20:00",
  maxHoursPerEmpMonth: 160,
  maxConsecutiveDays: 5,
  maxConsecutiveFull: 3,
  maxFullPerMonth: 10,
  note: "Visible note",
  availabilityGroupId: 4,
};

const graphEmployees: GraphEmployee[] = [
  { id: 1, scheduleId: 10, employeeId: 1, minHoursMonth: 80, displayOrder: 1 },
  { id: 2, scheduleId: 10, employeeId: 2, minHoursMonth: 80, displayOrder: 2 },
];

const slots: GraphSlot[] = [
  { id: 1, scheduleId: 10, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:30", employeeId: 1, status: "Working" },
  { id: 2, scheduleId: 10, dayOfMonth: 2, slotNo: 1, fromTime: "22:00", toTime: "02:00", employeeId: 2, status: "Working" },
  { id: 3, scheduleId: 10, dayOfMonth: 3, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: null, status: "Free" },
];

const employeesById = new Map<number, Employee>([
  [1, { id: 1, firstName: "Ada", lastName: "Lovelace", hasLoginAccount: true, isOnline: false }],
  [2, { id: 2, firstName: "Grace", lastName: "Hopper", hasLoginAccount: true, isOnline: false }],
]);

const shopsById = new Map<number, Shop>([
  [2, { id: 2, name: "Central Shop", address: "Main Street" }],
]);

describe("container statistics model", () => {
  test("formats durations and preview lists", () => {
    expect(getSlotDurationMinutes({ fromTime: "22:00", toTime: "02:00" })).toBe(240);
    expect(getSlotDurationMinutes({ fromTime: "bad", toTime: "02:00" })).toBe(0);
    expect(formatHoursMinutes(510)).toBe("8h 30m");
    expect(formatHoursCell(0)).toBe("0");
    expect(formatHoursCell(90)).toBe("1h 30m");
    expect(buildPreviewList([" Ada ", "", "Grace"], 1)).toBe("Ada, +1 more");
    expect(buildPreviewList([])).toBe("-");
  });

  test("builds graph summaries and filters them by searchable fields", () => {
    const summaries = buildContainerGraphSummaries(
      [graph],
      { [graph.id]: { employees: graphEmployees, slots } },
      shopsById,
    );

    expect(summaries).toMatchObject([
      {
        employeeCount: 2,
        assignedHoursText: "8h 30m",
        assignedSlotCount: 2,
        coverageDays: 2,
        shopName: "Central Shop",
        monthYearLabel: "May 2026",
        availabilityLabel: "Group 4",
      },
    ]);
    expect(filterGraphSummaries(summaries, "visible")).toHaveLength(1);
    expect(filterGraphSummaries(summaries, "missing")).toHaveLength(0);
  });

  test("builds pivot and work-free statistics across shops and employees", () => {
    const statistics = buildContainerStatistics({
      graphs: [graph],
      graphRecordsById: { [graph.id]: { employees: graphEmployees, slots } },
      employeesById,
      shopsById,
    });

    expect(statistics.totalHoursText).toBe("8h 30m");
    expect(statistics.totalEmployees).toBe(2);
    expect(statistics.totalShops).toBe(1);
    expect(statistics.totalEmployeesListText).toBe("Ada Lovelace, Grace Hopper");
    expect(statistics.shopHeaders).toEqual([{ key: "2", name: "Central Shop" }]);
    expect(statistics.pivotRows).toEqual([
      { employee: "Ada Lovelace", workDays: 1, freeDays: 30, holidayDays: 1, hoursSum: "4h 30m", hoursByShop: { "2": "4h 30m" } },
      { employee: "Grace Hopper", workDays: 1, freeDays: 30, holidayDays: 0, hoursSum: "4", hoursByShop: { "2": "4" } },
      { employee: "TOTAL", workDays: 2, freeDays: 60, holidayDays: 1, hoursSum: "8h 30m", hoursByShop: { "2": "8h 30m" }, isTotal: true },
    ]);
    statistics.pivotRows.forEach(row => expect(row.holidayDays).toBeLessThanOrEqual(row.workDays));
    expect(statistics.workFreeRows).toEqual([
      { employee: "Ada Lovelace", workDays: 1, freeDays: 30 },
      { employee: "Grace Hopper", workDays: 1, freeDays: 30 },
    ]);
  });

  test("counts unique calendar days once across multiple graphs in the same month", () => {
    const secondGraph: Graph = {
      ...graph,
      id: 11,
      shopId: 3,
      name: "Second May Schedule",
    };
    const secondGraphEmployees: GraphEmployee[] = [
      { id: 3, scheduleId: 11, employeeId: 1, minHoursMonth: 80, displayOrder: 1 },
    ];
    const secondGraphSlots: GraphSlot[] = [
      { id: 4, scheduleId: 11, dayOfMonth: 1, slotNo: 1, fromTime: "13:00", toTime: "17:00", employeeId: 1, status: "Working" },
      { id: 5, scheduleId: 11, dayOfMonth: 4, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 1, status: "Working" },
    ];

    const statistics = buildContainerStatistics({
      graphs: [graph, secondGraph],
      graphRecordsById: {
        [graph.id]: { employees: graphEmployees, slots },
        [secondGraph.id]: { employees: secondGraphEmployees, slots: secondGraphSlots },
      },
      employeesById,
      shopsById,
    });

    expect(statistics.pivotRows).toEqual(expect.arrayContaining([
      expect.objectContaining({ employee: "Ada Lovelace", workDays: 2, freeDays: 29, holidayDays: 1 }),
      expect.objectContaining({ employee: "Grace Hopper", workDays: 1, freeDays: 30 }),
    ]));
    statistics.pivotRows.forEach(row => expect(row.holidayDays).toBeLessThanOrEqual(row.workDays));
    expect(statistics.workFreeRows).toEqual([
      { employee: "Ada Lovelace", workDays: 2, freeDays: 29 },
      { employee: "Grace Hopper", workDays: 1, freeDays: 30 },
    ]);
  });
});


test("two employees on one holiday contribute two employee-days", () => {
  const statistics = buildContainerStatistics({ graphs: [graph], employeesById,
    graphRecordsById: { [graph.id]: { employees: graphEmployees,
      slots: [slots[0], { ...slots[1], dayOfMonth: 1 }] } } });
  expect(statistics.pivotRows.map(row => row.holidayDays)).toEqual([1, 1, 2]);
  statistics.pivotRows.forEach(row => expect(row.holidayDays).toBeLessThanOrEqual(row.workDays));
});

test("holiday dates add across months and years without counting weekends or vacancies", () => {
  const graphs = [graph, { ...graph, id: 11, month: 4 }, { ...graph, id: 12, year: 2025 }];
  const statistics = buildContainerStatistics({ graphs, employeesById,
    graphRecordsById: {
      10: { employees: graphEmployees, slots },
      11: { employees: [], slots: [{ ...slots[0], scheduleId: 11, dayOfMonth: 6 }] },
      12: { employees: [], slots: [{ ...slots[0], scheduleId: 12 }] },
    } });
  expect(statistics.pivotRows.map(row => row.holidayDays)).toEqual([3, 0, 3]);
  statistics.pivotRows.forEach(row => expect(row.holidayDays).toBeLessThanOrEqual(row.workDays));
});

test("empty and registered unscheduled employees retain zero counts and row structure", () => {
  expect(buildContainerStatistics({ graphs: [], graphRecordsById: {} }).pivotRows).toEqual([]);
  const statistics = buildContainerStatistics({ graphs: [graph], employeesById,
    graphRecordsById: { 10: { employees: [graphEmployees[0]], slots: [slots[2]] } } });
  expect(statistics.pivotRows).toHaveLength(2);
  expect(statistics.pivotRows.map(row => row.holidayDays)).toEqual([0, 0]);
  statistics.pivotRows.forEach(row => {
    expect(row.holidayDays).toBeLessThanOrEqual(row.workDays);
    expect(Number.isFinite(row.holidayDays)).toBe(true);
  });
});
