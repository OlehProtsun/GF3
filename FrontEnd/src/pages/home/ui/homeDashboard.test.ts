import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import type { Container, Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "@entities/containers";
import type { Employee } from "@entities/employees/model/types";
import type { Shop } from "@entities/shops/model/types";

const mocks = vi.hoisted(() => ({
  containersApi: {
    list: vi.fn(),
    listGraphs: vi.fn(),
    listGraphEmployees: vi.fn(),
    listGraphSlots: vi.fn(),
    listGraphCellStyles: vi.fn(),
  },
  employeesApi: {
    list: vi.fn(),
  },
  shopsApi: {
    list: vi.fn(),
  },
}));

vi.mock("@entities/containers", async importOriginal => {
  const actual = await importOriginal<typeof import("@entities/containers")>();
  return {
    ...actual,
    containersApi: mocks.containersApi,
  };
});

vi.mock("@entities/employees/api/employeesApi", () => ({
  employeesApi: mocks.employeesApi,
}));

vi.mock("@entities/shops/api/shopsApi", () => ({
  shopsApi: mocks.shopsApi,
}));

import { loadHomeDashboard } from "./homeDashboard";

const currentDate = new Date("2026-05-10T12:00:00");

const containers: Container[] = [
  { id: 1, name: "Alpha Container" },
  { id: 2, name: "Beta Container" },
];

const employees: Employee[] = [
  { id: 1, firstName: "Ada", lastName: "Lovelace", hasLoginAccount: true, isOnline: false },
  { id: 2, firstName: "Grace", lastName: "Hopper", hasLoginAccount: true, isOnline: true },
  { id: 3, firstName: "Linus", lastName: "Torvalds", hasLoginAccount: false, isOnline: false },
];

const shops: Shop[] = [
  { id: 1, name: "North Shop", address: "North Street" },
  { id: 2, name: "Central Shop", address: "Main Street" },
];

const graphEmployeesByKey = new Map<string, GraphEmployee[]>();
const graphSlotsByKey = new Map<string, GraphSlot[]>();
const graphCellStylesByKey = new Map<string, GraphCellStyle[]>();

function detailKey(containerId: number, graphId: number) {
  return `${containerId}:${graphId}`;
}

const centralGraph: Graph = {
  id: 10,
  containerId: 1,
  shopId: 2,
  name: "Beta graph",
  year: 2026,
  month: 5,
  publicationStatus: "public",
  peoplePerShift: 1,
  shift1Time: "08:00 - 12:00",
  shift2Time: "",
  maxHoursPerEmpMonth: 160,
  maxConsecutiveDays: 5,
  maxConsecutiveFull: 3,
  maxFullPerMonth: 10,
  note: null,
  availabilityGroupId: null,
};

const northGraph: Graph = {
  ...centralGraph,
  id: 20,
  containerId: 2,
  shopId: 1,
  name: "Alpha graph",
};

function mockBaseLists() {
  mocks.containersApi.list.mockResolvedValue(containers);
  mocks.employeesApi.list.mockResolvedValue(employees);
  mocks.shopsApi.list.mockResolvedValue(shops);
}

function mockGraphDetails(params: {
  containerId: number;
  graphId: number;
  graphEmployees?: GraphEmployee[];
  slots?: GraphSlot[];
  cellStyles?: GraphCellStyle[];
}) {
  const {
    containerId,
    graphId,
    graphEmployees = [],
    slots = [],
    cellStyles = [],
  } = params;
  const key = detailKey(containerId, graphId);

  graphEmployeesByKey.set(key, graphEmployees);
  graphSlotsByKey.set(key, slots);
  graphCellStylesByKey.set(key, cellStyles);
}

beforeEach(() => {
  vi.useFakeTimers();
  vi.setSystemTime(currentDate);
  vi.clearAllMocks();
  graphEmployeesByKey.clear();
  graphSlotsByKey.clear();
  graphCellStylesByKey.clear();
  mockBaseLists();
  mocks.containersApi.listGraphEmployees.mockImplementation((containerId: number, graphId: number) =>
    Promise.resolve(graphEmployeesByKey.get(detailKey(containerId, graphId)) ?? []),
  );
  mocks.containersApi.listGraphSlots.mockImplementation((containerId: number, graphId: number) =>
    Promise.resolve(graphSlotsByKey.get(detailKey(containerId, graphId)) ?? []),
  );
  mocks.containersApi.listGraphCellStyles.mockImplementation((containerId: number, graphId: number) =>
    Promise.resolve(graphCellStylesByKey.get(detailKey(containerId, graphId)) ?? []),
  );
});

afterEach(() => {
  vi.useRealTimers();
});

describe("home dashboard loader", () => {
  test("aggregates current month schedules, today rows, totals, and previews", async () => {
    mocks.containersApi.listGraphs.mockImplementation((containerId: number) => {
      if (containerId === 1) {
        return Promise.resolve([
          centralGraph,
          { ...centralGraph, id: 99, year: 2026, month: 4, name: "Old graph" },
        ]);
      }

      return Promise.resolve([northGraph]);
    });

    mockGraphDetails({
      containerId: 1,
      graphId: 10,
      graphEmployees: [
        { id: 1, scheduleId: 10, employeeId: 1, minHoursMonth: 80, displayOrder: 2 },
        { id: 2, scheduleId: 10, employeeId: 2, minHoursMonth: 80, displayOrder: 1 },
      ],
      slots: [
        { id: 1, scheduleId: 10, dayOfMonth: 10, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 1, status: "Working" },
        { id: 2, scheduleId: 10, dayOfMonth: 11, slotNo: 1, fromTime: "22:00", toTime: "02:00", employeeId: 2, status: "Working" },
      ],
      cellStyles: [
        { id: 1, scheduleId: 10, dayOfMonth: 10, employeeId: 1, backgroundColorArgb: -65536, textColorArgb: null },
      ],
    });
    mockGraphDetails({
      containerId: 2,
      graphId: 20,
      graphEmployees: [
        { id: 3, scheduleId: 20, employeeId: 2, minHoursMonth: 60, displayOrder: 1 },
      ],
      slots: [
        { id: 3, scheduleId: 20, dayOfMonth: 10, slotNo: 1, fromTime: "10:00", toTime: "13:30", employeeId: 2, status: "Working" },
      ],
    });

    const dashboard = await loadHomeDashboard();

    expect(dashboard.statusText).toBe("Home data is up to date.");
    expect(dashboard.monthSchedulesCount).toBe(2);
    expect(dashboard.totalContainersCount).toBe(2);
    expect(dashboard.overallTotalEmployees).toBe(3);
    expect(dashboard.overallTotalShops).toBe(2);
    expect(dashboard.activeShopsCount).toBe(2);
    expect(dashboard.currentMonthContainerName).toBe("2 active containers");
    expect(dashboard.currentMonthLabel).toBe("May 2026");
    expect(dashboard.currentMonthScheduleNames).toEqual(["Alpha graph", "Beta graph"]);
    expect(dashboard.currentMonthShopNames).toEqual(["Central Shop", "North Shop"]);
    expect(dashboard.currentMonthEmployeeNames).toEqual(["Ada Lovelace", "Grace Hopper"]);
    expect(dashboard.currentMonthTotalEmployees).toBe(2);
    expect(dashboard.currentMonthTotalSchedules).toBe(2);
    expect(dashboard.currentMonthTotalHoursText).toBe("11h 30m");
    expect(dashboard.todayAssignmentsCount).toBe(2);
    expect(dashboard.todayRows).toEqual([
      {
        id: "10:Ada Lovelace:08:00 - 12:00:Central Shop",
        dateLabel: "10.05.2026",
        employee: "Ada Lovelace",
        shift: "08:00 - 12:00",
        shop: "Central Shop",
        route: "/container/1/graphs/10",
      },
      {
        id: "10:Grace Hopper:10:00 - 13:30:North Shop",
        dateLabel: "10.05.2026",
        employee: "Grace Hopper",
        shift: "10:00 - 13:30",
        shop: "North Shop",
        route: "/container/2/graphs/20",
      },
    ]);

    expect(dashboard.activeSchedules.map(schedule => schedule.shop?.name)).toEqual(["Central Shop", "North Shop"]);
    expect(dashboard.activeSchedules[0]).toMatchObject({
      monthLabel: "May 2026",
      route: "/container/1/graphs/10",
      totals: {
        totalEmployees: 2,
        totalHoursText: "8h 0m",
      },
      cellMap: {
        "1:10": "08:00 - 12:00",
        "2:11": "22:00 - 02:00",
      },
      visualHintMap: {
        "2:10": "Alpha graph",
      },
      styleMap: {
        "1:10": {
          backgroundColor: "rgba(255, 0, 0, 1)",
        },
      },
    });
    expect(dashboard.activeSchedules[1].visualHintMap).toEqual({
      "2:11": "Beta graph",
    });
  });

  test("keeps partial dashboard data when one graph list or detail request fails", async () => {
    mocks.containersApi.listGraphs.mockImplementation((containerId: number) => {
      if (containerId === 1) {
        return Promise.resolve([centralGraph]);
      }

      return Promise.reject(new Error("Graph list failed"));
    });
    mocks.containersApi.listGraphEmployees.mockResolvedValue([
      { id: 1, scheduleId: 10, employeeId: 1, minHoursMonth: 80, displayOrder: 1 },
    ]);
    mocks.containersApi.listGraphSlots.mockRejectedValue(new Error("Slots failed"));
    mocks.containersApi.listGraphCellStyles.mockResolvedValue([]);

    const dashboard = await loadHomeDashboard();

    expect(dashboard.statusText).toBe("Home data loaded with a few missing schedule previews.");
    expect(dashboard.monthSchedulesCount).toBe(1);
    expect(dashboard.activeSchedules).toEqual([]);
    expect(dashboard.todayRows).toEqual([]);
  });

  test("throws an AbortError after base data loads when the supplied signal is canceled", async () => {
    const controller = new AbortController();
    controller.abort();
    mocks.containersApi.listGraphs.mockResolvedValue([]);

    await expect(loadHomeDashboard(controller.signal)).rejects.toMatchObject({
      name: "AbortError",
      message: "Request was canceled.",
    });
  });
});
