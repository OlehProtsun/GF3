import { dateTimeFormat, t } from "@shared/i18n";
import type { Employee } from "@entities/employees/model/types";
import type { Shop } from "@entities/shops/model/types";
import { getGraphVisibleNote } from "./graphNote";
import type { Graph, GraphEmployee, GraphSlot } from "./types";

export type ContainerGraphRecords = {
  employees: GraphEmployee[];
  slots: GraphSlot[];
};

export type ContainerGraphSummary = {
  graph: Graph;
  employeeCount: number;
  assignedHoursText: string;
  assignedSlotCount: number;
  coverageDays: number;
  shopName: string;
  monthYearLabel: string;
  availabilityLabel: string;
};

export type ContainerStatisticShopHeader = {
  key: string;
  name: string;
};

export type ContainerStatisticPivotRow = {
  employee: string;
  workDays: number;
  freeDays: number;
  hoursSum: string;
  hoursByShop: Record<string, string>;
  isTotal?: boolean;
};

export type ContainerWorkFreeRow = {
  employee: string;
  workDays: number;
  freeDays: number;
};

export type ContainerStatistics = {
  totalHoursText: string;
  totalEmployees: number;
  totalShops: number;
  totalEmployeesListText: string;
  totalShopsListText: string;
  shopHeaders: ContainerStatisticShopHeader[];
  pivotRows: ContainerStatisticPivotRow[];
  workFreeRows: ContainerWorkFreeRow[];
};

type BuildContainerStatisticsParams = {
  graphs: Graph[];
  graphRecordsById: Record<number, ContainerGraphRecords>;
  employeesById?: Map<number, Employee>;
  shopsById?: Map<number, Shop>;
};

type EmployeePeriodCalendar = {
  daysInMonth: number;
  workedDays: Set<number>;
};

export function buildContainerGraphSummaries(
  graphs: Graph[],
  graphRecordsById: Record<number, ContainerGraphRecords>,
  shopsById?: Map<number, Shop>,
): ContainerGraphSummary[] {
  return [...graphs]
    .sort((left, right) => {
      if (left.year !== right.year) return right.year - left.year;
      if (left.month !== right.month) return right.month - left.month;
      return left.name.localeCompare(right.name);
    })
    .map(graph => {
      const records = graphRecordsById[graph.id] ?? { employees: [], slots: [] };
      const employeeIds = new Set<number>();
      const workedDays = new Set<number>();
      let totalMinutes = 0;
      let assignedSlotCount = 0;

      records.employees.forEach(item => {
        if (item.employeeId > 0) {
          employeeIds.add(item.employeeId);
        }
      });

      records.slots.forEach(slot => {
        if (!slot.employeeId || slot.employeeId <= 0) {
          return;
        }

        employeeIds.add(slot.employeeId);
        workedDays.add(slot.dayOfMonth);
        assignedSlotCount += 1;
        totalMinutes += getSlotDurationMinutes(slot);
      });

      return {
        graph,
        employeeCount: employeeIds.size,
        assignedHoursText: formatHoursMinutes(totalMinutes),
        assignedSlotCount,
        coverageDays: workedDays.size,
        shopName: shopsById?.get(graph.shopId)?.name?.trim() || t("Shop {0}", graph.shopId),
        monthYearLabel: formatGraphMonthYear(graph.year, graph.month),
        availabilityLabel: graph.availabilityGroupId ? t("Group {0}", graph.availabilityGroupId) : t("None"),
      };
    });
}

export function buildContainerStatistics({
  graphs,
  graphRecordsById,
  employeesById,
  shopsById,
}: BuildContainerStatisticsParams): ContainerStatistics {
  const shopKeyToName = new Map<string, string>();
  const employeeNameById = new Map<number, string>();
  const employeeShopMinutes = new Map<number, Map<string, number>>();
  const employeeTotalMinutes = new Map<number, number>();
  const employeeWorkDays = new Map<number, number>();
  const employeeFreeDays = new Map<number, number>();
  const employeePeriods = new Map<number, Map<string, EmployeePeriodCalendar>>();
  const shopTotalMinutes = new Map<string, number>();
  let totalMinutes = 0;

  graphs.forEach(graph => {
    const records = graphRecordsById[graph.id] ?? { employees: [], slots: [] };
    const shopKey = String(graph.shopId);
    const shopName = shopsById?.get(graph.shopId)?.name?.trim() || t("Shop {0}", graph.shopId);
    const daysInMonth = new Date(graph.year, graph.month, 0).getDate();
    const periodKey = `${graph.year}-${String(graph.month).padStart(2, "0")}`;

    const ensureEmployeePeriod = (employeeId: number) => {
      const periods = employeePeriods.get(employeeId) ?? new Map<string, EmployeePeriodCalendar>();
      const period = periods.get(periodKey) ?? { daysInMonth, workedDays: new Set<number>() };
      periods.set(periodKey, period);
      employeePeriods.set(employeeId, periods);
      return period;
    };

    if (!shopKeyToName.has(shopKey)) {
      shopKeyToName.set(shopKey, shopName);
    }

    records.employees.forEach(item => {
      if (item.employeeId > 0 && !employeeNameById.has(item.employeeId)) {
        employeeNameById.set(item.employeeId, getEmployeeLabel(item.employeeId, employeesById));
      }

      if (item.employeeId > 0) {
        ensureEmployeePeriod(item.employeeId);
      }
    });

    records.slots.forEach(slot => {
      if (!slot.employeeId || slot.employeeId <= 0) {
        return;
      }

      const durationMinutes = getSlotDurationMinutes(slot);
      totalMinutes += durationMinutes;

      if (!employeeNameById.has(slot.employeeId)) {
        employeeNameById.set(slot.employeeId, getEmployeeLabel(slot.employeeId, employeesById));
      }

      employeeTotalMinutes.set(
        slot.employeeId,
        (employeeTotalMinutes.get(slot.employeeId) ?? 0) + durationMinutes,
      );

      const minutesByShop = employeeShopMinutes.get(slot.employeeId) ?? new Map<string, number>();
      minutesByShop.set(shopKey, (minutesByShop.get(shopKey) ?? 0) + durationMinutes);
      employeeShopMinutes.set(slot.employeeId, minutesByShop);

      shopTotalMinutes.set(shopKey, (shopTotalMinutes.get(shopKey) ?? 0) + durationMinutes);

      const period = ensureEmployeePeriod(slot.employeeId);
      if (slot.dayOfMonth >= 1 && slot.dayOfMonth <= period.daysInMonth) {
        period.workedDays.add(slot.dayOfMonth);
      }
    });

  });

  employeePeriods.forEach((periods, employeeId) => {
    let workDays = 0;
    let freeDays = 0;

    periods.forEach(period => {
      workDays += period.workedDays.size;
      freeDays += Math.max(0, period.daysInMonth - period.workedDays.size);
    });

    employeeWorkDays.set(employeeId, workDays);
    employeeFreeDays.set(employeeId, freeDays);
  });

  const shopHeaders = [...shopKeyToName.entries()]
    .map(([key, name]) => ({ key, name }))
    .sort((left, right) => left.name.localeCompare(right.name));

  const orderedEmployees = [...employeeNameById.entries()].sort((left, right) =>
    left[1].localeCompare(right[1]),
  );

  const pivotRows: ContainerStatisticPivotRow[] = orderedEmployees.map(([employeeId, employeeName]) => {
    const hoursByShop: Record<string, string> = {};
    const perShopMinutes = employeeShopMinutes.get(employeeId);

    shopHeaders.forEach(shopHeader => {
      hoursByShop[shopHeader.key] = formatHoursCell(perShopMinutes?.get(shopHeader.key) ?? 0);
    });

    return {
      employee: employeeName,
      workDays: employeeWorkDays.get(employeeId) ?? 0,
      freeDays: employeeFreeDays.get(employeeId) ?? 0,
      hoursSum: formatHoursCell(employeeTotalMinutes.get(employeeId) ?? 0),
      hoursByShop,
    };
  });

  const totalHoursByShop: Record<string, string> = {};
  shopHeaders.forEach(shopHeader => {
    totalHoursByShop[shopHeader.key] = formatHoursCell(shopTotalMinutes.get(shopHeader.key) ?? 0);
  });

  if (pivotRows.length > 0) {
    pivotRows.push({
      employee: "TOTAL",
      workDays: [...employeeWorkDays.values()].reduce((sum, value) => sum + value, 0),
      freeDays: [...employeeFreeDays.values()].reduce((sum, value) => sum + value, 0),
      hoursSum: formatHoursCell(totalMinutes),
      hoursByShop: totalHoursByShop,
      isTotal: true,
    });
  }

  const employeeNames = orderedEmployees.map(([, employeeName]) => employeeName);
  const shopNames = shopHeaders.map(shopHeader => shopHeader.name);
  const workFreeRows: ContainerWorkFreeRow[] = orderedEmployees.map(([employeeId, employeeName]) => ({
    employee: employeeName,
    workDays: employeeWorkDays.get(employeeId) ?? 0,
    freeDays: employeeFreeDays.get(employeeId) ?? 0,
  }));

  return {
    totalHoursText: formatHoursMinutes(totalMinutes),
    totalEmployees: employeeNameById.size,
    totalShops: shopHeaders.length,
    totalEmployeesListText: buildPreviewList(employeeNames),
    totalShopsListText: buildPreviewList(shopNames),
    shopHeaders,
    pivotRows,
    workFreeRows,
  };
}

export function filterGraphSummaries(summaries: ContainerGraphSummary[], query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return summaries;
  }

  return summaries.filter(summary =>
    [
      summary.graph.name,
      getGraphVisibleNote(summary.graph.note),
      summary.shopName,
      summary.monthYearLabel,
      summary.availabilityLabel,
    ]
      .join(" ")
      .toLowerCase()
      .includes(normalizedQuery),
  );
}

export function getSlotDurationMinutes(slot: Pick<GraphSlot, "fromTime" | "toTime">) {
  const from = parseClockMinutes(slot.fromTime);
  const to = parseClockMinutes(slot.toTime);

  if (from === null || to === null) {
    return 0;
  }

  let duration = to - from;
  if (duration < 0) {
    duration += 24 * 60;
  }

  return duration;
}

export function formatHoursMinutes(totalMinutes: number) {
  const safeMinutes = Math.max(0, Math.round(totalMinutes));
  const hours = Math.floor(safeMinutes / 60);
  const minutes = safeMinutes % 60;

  return `${hours}h ${minutes}m`;
}

export function formatHoursCell(totalMinutes: number) {
  const safeMinutes = Math.max(0, Math.round(totalMinutes));
  if (safeMinutes <= 0) {
    return "0";
  }

  const hours = Math.floor(safeMinutes / 60);
  const minutes = safeMinutes % 60;

  return minutes === 0 ? `${hours}` : `${hours}h ${minutes}m`;
}

export function buildPreviewList(items: string[], previewCount = 8) {
  const sanitizedItems = items
    .map(item => item.trim())
    .filter(Boolean);

  if (sanitizedItems.length === 0) {
    return "-";
  }

  const visibleItems = sanitizedItems.slice(0, previewCount);
  const remainingCount = sanitizedItems.length - visibleItems.length;

  return remainingCount > 0
    ? t("{0}, +{1} more", visibleItems.join(", "), remainingCount)
    : visibleItems.join(", ");
}

function parseClockMinutes(value: string) {
  const match = /^(\d{1,2}):(\d{2})$/.exec(value.trim());
  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);

  if (!Number.isInteger(hours) || !Number.isInteger(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null;
  }

  return hours * 60 + minutes;
}

function formatGraphMonthYear(year: number, month: number) {
  const monthIndex = Math.min(Math.max(month - 1, 0), 11);
  const date = new Date(Date.UTC(year, monthIndex, 1));

  return dateTimeFormat("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

function getEmployeeLabel(employeeId: number, employeesById?: Map<number, Employee>) {
  const employee = employeesById?.get(employeeId);
  const fullName = [employee?.firstName, employee?.lastName].filter(Boolean).join(" ").trim();

  return fullName || t("Employee {0}", employeeId);
}
