import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { buildPreviewList, formatHoursMinutes, getSlotDurationMinutes } from "./statistics";
import type { Graph, GraphCellStyle, GraphEmployee, GraphSlot, SlotStatus } from "./types";

export const GRAPH_EMPTY_MARK = "-";

export type GraphMatrixColumn = {
  employeeId: number;
  kind: "employee" | "manual";
  manualColumnId: number | null;
  graphEmployeeId: number | null;
  label: string;
  minHoursMonth: number | null;
  totalMinutes: number;
  totalText: string;
};

export type GraphMatrixCellMap = Record<string, string>;

export type GraphMatrixStyle = {
  id: number | null;
  backgroundColor: string | null;
  textColor: string | null;
  backgroundColorArgb: number | null;
  textColorArgb: number | null;
};

export type GraphMatrixStyleMap = Record<string, GraphMatrixStyle>;

export type GraphSummaryDayHeader = {
  dayOfMonth: number;
  label: string;
};

export type GraphSummaryDayCell = {
  from: string;
  to: string;
  hours: string;
};

export type GraphSummaryRow = {
  employeeId: number;
  employee: string;
  workDays: number;
  freeDays: number;
  sum: string;
  days: GraphSummaryDayCell[];
};

export type GraphTotals = {
  totalEmployees: number;
  totalMinutes: number;
  totalHoursText: string;
  totalEmployeesListText: string;
  perEmployeeText: Record<number, string>;
};

export type GraphDraftSlotBuildResult = {
  slots: GraphSlot[];
  errors: Record<string, string>;
};

export type GraphSlotDiffResult = {
  create: GraphSlot[];
  update: GraphSlot[];
  remove: GraphSlot[];
};

export function getGraphCellKey(employeeId: number, dayOfMonth: number) {
  return `${employeeId}:${dayOfMonth}`;
}

export function getGraphStyleKey(employeeId: number, dayOfMonth: number) {
  return `${employeeId}:${dayOfMonth}`;
}

export function clampGraphMonth(month: number) {
  if (!Number.isFinite(month)) {
    return 1;
  }

  return Math.min(12, Math.max(1, Math.round(month)));
}

export function clampGraphYear(year: number) {
  if (!Number.isFinite(year)) {
    return new Date().getFullYear();
  }

  return Math.min(4000, Math.max(2000, Math.round(year)));
}

export function getGraphDaysInMonth(year: number, month: number) {
  return new Date(clampGraphYear(year), clampGraphMonth(month), 0).getDate();
}

function getGraphDate(year: number, month: number, dayOfMonth: number) {
  const safeYear = clampGraphYear(year);
  const safeMonth = clampGraphMonth(month);
  const safeDayOfMonth = Math.min(getGraphDaysInMonth(safeYear, safeMonth), Math.max(1, Math.round(dayOfMonth)));

  return new Date(safeYear, safeMonth - 1, safeDayOfMonth);
}

export function getGraphWeekdayLabel(year: number, month: number, dayOfMonth: number) {
  const weekdayIndex = getGraphDate(year, month, dayOfMonth).getDay();
  const weekdayLabels = ["su", "mo", "tu", "we", "th", "fr", "sa"];
  return `${weekdayLabels[weekdayIndex]}.`;
}

export function isGraphWeekend(year: number, month: number, dayOfMonth: number) {
  const weekdayIndex = getGraphDate(year, month, dayOfMonth).getDay();
  return weekdayIndex === 0 || weekdayIndex === 6;
}

export function formatGraphMonthYear(year: number, month: number) {
  const date = new Date(Date.UTC(clampGraphYear(year), clampGraphMonth(month) - 1, 1));
  return new Intl.DateTimeFormat("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

function normalizeGraphDisplayOrder(displayOrder?: number | null) {
  if (Number.isInteger(displayOrder) && (displayOrder as number) >= 0) {
    return displayOrder as number;
  }

  return Number.MAX_SAFE_INTEGER;
}

export function sortGraphItemsByDisplayOrder<T extends { employeeId: number; label: string; displayOrder?: number | null }>(
  items: T[],
) {
  return [...items].sort((left, right) => {
    const orderDifference =
      normalizeGraphDisplayOrder(left.displayOrder) - normalizeGraphDisplayOrder(right.displayOrder);

    if (orderDifference !== 0) {
      return orderDifference;
    }

    const labelDifference = left.label.localeCompare(right.label);
    if (labelDifference !== 0) {
      return labelDifference;
    }

    return left.employeeId - right.employeeId;
  });
}

export function buildGraphMatrixColumns(
  graphEmployees: GraphEmployee[],
  employeesById?: Map<number, Employee>,
  slots: GraphSlot[] = [],
): GraphMatrixColumn[] {
  const totalMinutesByEmployeeId = new Map<number, number>();
  slots.forEach(slot => {
    if (!slot.employeeId || slot.employeeId <= 0) {
      return;
    }

    totalMinutesByEmployeeId.set(
      slot.employeeId,
      (totalMinutesByEmployeeId.get(slot.employeeId) ?? 0) + getSlotDurationMinutes(slot),
    );
  });

  return sortGraphItemsByDisplayOrder(
    [...graphEmployees].map(graphEmployee => ({
      employeeId: graphEmployee.employeeId,
      kind: "employee" as const,
      manualColumnId: null,
      graphEmployeeId: graphEmployee.id,
      displayOrder: graphEmployee.displayOrder,
      label: getEmployeeLabel(graphEmployee.employeeId, employeesById),
      minHoursMonth: graphEmployee.minHoursMonth ?? null,
      totalMinutes: totalMinutesByEmployeeId.get(graphEmployee.employeeId) ?? 0,
      totalText: formatHoursMinutes(totalMinutesByEmployeeId.get(graphEmployee.employeeId) ?? 0),
    })),
  );
}

export function buildGraphCellMap(slots: GraphSlot[]) {
  const slotsByEmployeeByDay = new Map<string, GraphSlot[]>();

  slots.forEach(slot => {
    if (!slot.employeeId || slot.employeeId <= 0) {
      return;
    }

    const key = getGraphCellKey(slot.employeeId, slot.dayOfMonth);
    const list = slotsByEmployeeByDay.get(key) ?? [];
    list.push(slot);
    slotsByEmployeeByDay.set(key, list);
  });

  const cellMap: GraphMatrixCellMap = {};
  slotsByEmployeeByDay.forEach((daySlots, key) => {
    cellMap[key] = formatGraphIntervals(mergeGraphIntervalsForDisplay(daySlots));
  });

  return cellMap;
}

export function buildGraphStyleMap(styles: GraphCellStyle[]): GraphMatrixStyleMap {
  return styles.reduce<GraphMatrixStyleMap>((accumulator, style) => {
    accumulator[getGraphStyleKey(style.employeeId, style.dayOfMonth)] = {
      id: style.id,
      backgroundColor: argbToCssColor(style.backgroundColorArgb ?? null),
      textColor: argbToCssColor(style.textColorArgb ?? null),
      backgroundColorArgb: style.backgroundColorArgb ?? null,
      textColorArgb: style.textColorArgb ?? null,
    };

    return accumulator;
  }, {});
}

export function argbToCssColor(argb?: number | null) {
  if (argb === undefined || argb === null) {
    return null;
  }

  const unsigned = argb >>> 0;
  const alpha = ((unsigned >>> 24) & 255) / 255;
  const red = (unsigned >>> 16) & 255;
  const green = (unsigned >>> 8) & 255;
  const blue = unsigned & 255;

  return `rgba(${red}, ${green}, ${blue}, ${Number(alpha.toFixed(3))})`;
}

export function rgbHexToArgb(value: string) {
  const normalized = value.trim().toLowerCase();
  const longHex = normalized.match(/^#([0-9a-f]{6})$/)?.[1];
  if (!longHex) {
    return null;
  }

  const red = Number.parseInt(longHex.slice(0, 2), 16);
  const green = Number.parseInt(longHex.slice(2, 4), 16);
  const blue = Number.parseInt(longHex.slice(4, 6), 16);

  return (255 << 24) | (red << 16) | (green << 8) | blue;
}

export function mergeGraphIntervalsForDisplay(slots: Pick<GraphSlot, "fromTime" | "toTime">[]) {
  const uniqueIntervals = new Set<string>();
  const intervals: Array<{ fromMinutes: number; toMinutes: number }> = [];

  slots.forEach(slot => {
    const fromMinutes = parseGraphTimeMinutes(slot.fromTime);
    let toMinutes = parseGraphTimeMinutes(slot.toTime);

    if (fromMinutes === null || toMinutes === null) {
      return;
    }

    if (toMinutes < fromMinutes) {
      toMinutes += 24 * 60;
    }

    const key = `${fromMinutes}:${toMinutes}`;
    if (uniqueIntervals.has(key)) {
      return;
    }

    uniqueIntervals.add(key);
    intervals.push({ fromMinutes, toMinutes });
  });

  if (intervals.length === 0) {
    return [];
  }

  intervals.sort((left, right) =>
    left.fromMinutes === right.fromMinutes
      ? left.toMinutes - right.toMinutes
      : left.fromMinutes - right.fromMinutes,
  );

  const merged: Array<{ fromMinutes: number; toMinutes: number }> = [];
  let current = { ...intervals[0] };

  for (let index = 1; index < intervals.length; index += 1) {
    const nextInterval = intervals[index];
    if (nextInterval.fromMinutes <= current.toMinutes) {
      current.toMinutes = Math.max(current.toMinutes, nextInterval.toMinutes);
      continue;
    }

    merged.push(current);
    current = { ...nextInterval };
  }

  merged.push(current);

  return merged.map(interval => ({
    from: minutesToTimeLabel(interval.fromMinutes),
    to: minutesToTimeLabel(interval.toMinutes),
  }));
}

export function formatGraphIntervals(intervals: Array<{ from: string; to: string }>) {
  if (intervals.length === 0) {
    return GRAPH_EMPTY_MARK;
  }

  return intervals.map(interval => `${interval.from} - ${interval.to}`).join(", ");
}

export function tryParseGraphIntervals(input: string):
  | { ok: true; value: Array<{ from: string; to: string }> }
  | { ok: false; error: string } {
  const trimmed = input.trim();

  if (!trimmed || trimmed === GRAPH_EMPTY_MARK) {
    return { ok: true, value: [] };
  }

  const parts = trimmed
    .split(",")
    .map(part => part.trim())
    .filter(Boolean);

  const uniqueIntervals = new Set<string>();
  const intervals: Array<{ from: string; to: string }> = [];

  for (const part of parts) {
    const segments = part
      .replace(/[–—]/g, "-")
      .split("-", 2)
      .map(segment => segment.trim())
      .filter(Boolean);

    if (segments.length !== 2) {
      return { ok: false, error: "Format: HH:mm - HH:mm (comma separated allowed)." };
    }

    const from = normalizeGraphTime(segments[0]);
    const to = normalizeGraphTime(segments[1]);

    if (!from || !to) {
      return { ok: false, error: "Time must be HH:mm (e.g. 09:00 - 14:30)." };
    }

    if ((parseGraphTimeMinutes(to) ?? 0) <= (parseGraphTimeMinutes(from) ?? 0)) {
      return { ok: false, error: "From must be earlier than To." };
    }

    const key = `${from}:${to}`;
    if (uniqueIntervals.has(key)) {
      continue;
    }

    uniqueIntervals.add(key);
    intervals.push({ from, to });
  }

  intervals.sort((left, right) => {
    const leftFrom = parseGraphTimeMinutes(left.from) ?? 0;
    const rightFrom = parseGraphTimeMinutes(right.from) ?? 0;
    if (leftFrom !== rightFrom) {
      return leftFrom - rightFrom;
    }

    return (parseGraphTimeMinutes(left.to) ?? 0) - (parseGraphTimeMinutes(right.to) ?? 0);
  });

  return { ok: true, value: intervals };
}

export function sanitizeGraphCellMap(
  cellMap: GraphMatrixCellMap,
  employeeIds: number[],
  year: number,
  month: number,
) {
  const employeeIdSet = new Set(employeeIds);
  const daysInMonth = getGraphDaysInMonth(year, month);

  return Object.entries(cellMap).reduce<GraphMatrixCellMap>((accumulator, [key, value]) => {
    const [employeeIdValue, dayOfMonthValue] = key.split(":");
    const employeeId = Number(employeeIdValue);
    const dayOfMonth = Number(dayOfMonthValue);

    if (!employeeIdSet.has(employeeId)) {
      return accumulator;
    }

    if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > daysInMonth) {
      return accumulator;
    }

    accumulator[key] = value;
    return accumulator;
  }, {});
}

export function validateGraphCellMap(
  cellMap: GraphMatrixCellMap,
  employeeIds: number[],
  year: number,
  month: number,
) {
  const errors: Record<string, string> = {};
  const daysInMonth = getGraphDaysInMonth(year, month);

  employeeIds.forEach(employeeId => {
    for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
      const key = getGraphCellKey(employeeId, dayOfMonth);
      const parsed = tryParseGraphIntervals(cellMap[key] ?? GRAPH_EMPTY_MARK);
      if (!parsed.ok) {
        errors[key] = parsed.error;
      }
    }
  });

  return errors;
}

export function buildGraphConflictDayMap(graph: Graph, slots: GraphSlot[]) {
  const daysInMonth = getGraphDaysInMonth(graph.year, graph.month);
  const conflictMap: Record<number, boolean> = {};

  for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
    conflictMap[dayOfMonth] = computeConflictForDayWithStaffing(
      slots,
      dayOfMonth,
      graph.peoplePerShift,
      graph.shift1Time,
      graph.shift2Time,
    );
  }

  return conflictMap;
}

export function buildGraphTotals(
  graphEmployees: GraphEmployee[],
  slots: GraphSlot[],
  employeesById?: Map<number, Employee>,
): GraphTotals {
  const employeeIds = new Set<number>();
  const totalMinutesByEmployeeId = new Map<number, number>();
  let totalMinutes = 0;

  graphEmployees.forEach(graphEmployee => {
    if (graphEmployee.employeeId > 0) {
      employeeIds.add(graphEmployee.employeeId);
    }
  });

  slots.forEach(slot => {
    if (!slot.employeeId || slot.employeeId <= 0) {
      return;
    }

    employeeIds.add(slot.employeeId);
    const durationMinutes = getSlotDurationMinutes(slot);
    totalMinutes += durationMinutes;
    totalMinutesByEmployeeId.set(
      slot.employeeId,
      (totalMinutesByEmployeeId.get(slot.employeeId) ?? 0) + durationMinutes,
    );
  });

  const orderedEmployees = [...employeeIds]
    .map(employeeId => ({
      employeeId,
      label: getEmployeeLabel(employeeId, employeesById),
    }))
    .sort((left, right) => left.label.localeCompare(right.label));

  const perEmployeeText = orderedEmployees.reduce<Record<number, string>>((accumulator, employee) => {
    accumulator[employee.employeeId] = formatHoursMinutes(totalMinutesByEmployeeId.get(employee.employeeId) ?? 0);
    return accumulator;
  }, {});

  return {
    totalEmployees: employeeIds.size,
    totalMinutes,
    totalHoursText: formatHoursMinutes(totalMinutes),
    totalEmployeesListText: buildPreviewList(orderedEmployees.map(employee => employee.label)),
    perEmployeeText,
  };
}

export function buildGraphSummaryHeaders(year: number, month: number) {
  const daysInMonth = getGraphDaysInMonth(year, month);

  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;
    const date = getGraphDate(year, month, dayOfMonth);
    const weekday = new Intl.DateTimeFormat("en-US", {
      weekday: "short",
      timeZone: "UTC",
    }).format(date);

    return {
      dayOfMonth,
      label: `${weekday} (${String(dayOfMonth).padStart(2, "0")}.${String(month).padStart(2, "0")}.${year})`,
    };
  });
}

export function buildGraphSummaryRows(
  graph: Pick<Graph, "year" | "month">,
  graphEmployees: GraphEmployee[],
  employeesById?: Map<number, Employee>,
  slots: GraphSlot[] = [],
) {
  const columns = buildGraphMatrixColumns(graphEmployees, employeesById, slots);
  const cellMap = buildGraphCellMap(slots);
  const daysInMonth = getGraphDaysInMonth(graph.year, graph.month);

  return columns.map(column => {
    const days = Array.from({ length: daysInMonth }, (_, index) => {
      const dayOfMonth = index + 1;
      const cellValue = cellMap[getGraphCellKey(column.employeeId, dayOfMonth)] ?? GRAPH_EMPTY_MARK;
      return buildGraphSummaryDayCell(cellValue);
    });

    const workDays = days.filter(day => day.hours !== "" || day.from !== "" || day.to !== "").length;
    const freeDays = Math.max(0, daysInMonth - workDays);

    return {
      employeeId: column.employeeId,
      employee: column.label,
      workDays,
      freeDays,
      sum: formatSummaryMinutes(column.totalMinutes),
      days,
    } satisfies GraphSummaryRow;
  });
}

export function buildGraphDraftSlots(params: {
  scheduleId: number;
  existingSlots: GraphSlot[];
  employeeIds: number[];
  year: number;
  month: number;
  cellMap: GraphMatrixCellMap;
}) {
  const { scheduleId, existingSlots, employeeIds, year, month, cellMap } = params;
  const errors = validateGraphCellMap(cellMap, employeeIds, year, month);
  const employeeIdSet = new Set(employeeIds);
  const daysInMonth = getGraphDaysInMonth(year, month);

  if (Object.keys(errors).length > 0) {
    return {
      slots: existingSlots.map(slot => ({ ...slot })),
      errors,
    } satisfies GraphDraftSlotBuildResult;
  }

  const nextSlots = existingSlots
    .filter(slot => !slot.employeeId || employeeIdSet.has(slot.employeeId))
    .map(slot => ({ ...slot }));

  employeeIds.forEach(employeeId => {
    for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
      const parsed = tryParseGraphIntervals(cellMap[getGraphCellKey(employeeId, dayOfMonth)] ?? GRAPH_EMPTY_MARK);
      if (!parsed.ok) {
        errors[getGraphCellKey(employeeId, dayOfMonth)] = parsed.error;
        continue;
      }

      applyIntervalsToGraphSlots(scheduleId, nextSlots, dayOfMonth, employeeId, parsed.value);
    }
  });

  return {
    slots: nextSlots,
    errors,
  } satisfies GraphDraftSlotBuildResult;
}

export function diffGraphSlots(existingSlots: GraphSlot[], nextSlots: GraphSlot[]): GraphSlotDiffResult {
  const existingSlotById = new Map(existingSlots.filter(slot => slot.id > 0).map(slot => [slot.id, slot]));
  const nextSlotById = new Map(nextSlots.filter(slot => slot.id > 0).map(slot => [slot.id, slot]));

  const create = nextSlots.filter(slot => slot.id <= 0);
  const update = nextSlots.filter(slot => {
    if (slot.id <= 0) {
      return false;
    }

    const existing = existingSlotById.get(slot.id);
    return existing ? !areGraphSlotsEqual(existing, slot) : false;
  });
  const remove = existingSlots.filter(slot => slot.id > 0 && !nextSlotById.has(slot.id));

  return { create, update, remove };
}

export function applyIntervalsToGraphSlots(
  scheduleId: number,
  slots: GraphSlot[],
  dayOfMonth: number,
  employeeId: number,
  intervals: Array<{ from: string; to: string }>,
) {
  let preservedStatus: SlotStatus = 0;

  for (const slot of slots) {
    if (slot.dayOfMonth === dayOfMonth && slot.employeeId === employeeId) {
      preservedStatus = slot.status;
      break;
    }
  }

  for (let index = slots.length - 1; index >= 0; index -= 1) {
    const slot = slots[index];
    if (slot.dayOfMonth === dayOfMonth && slot.employeeId === employeeId) {
      slots.splice(index, 1);
    }
  }

  if (intervals.length === 0) {
    return;
  }

  const intervalsToAdd: Array<{ from: string; to: string }> = [];

  intervals.forEach(interval => {
    const normalizedFrom = normalizeGraphTime(interval.from);
    const normalizedTo = normalizeGraphTime(interval.to);

    if (!normalizedFrom || !normalizedTo) {
      intervalsToAdd.push(interval);
      return;
    }

    const vacantSlot = slots.find(slot =>
      slot.dayOfMonth === dayOfMonth &&
      (!slot.employeeId || slot.employeeId <= 0) &&
      normalizeGraphTime(slot.fromTime) === normalizedFrom &&
      normalizeGraphTime(slot.toTime) === normalizedTo,
    );

    if (vacantSlot) {
      vacantSlot.employeeId = employeeId;
      return;
    }

    intervalsToAdd.push({ from: normalizedFrom, to: normalizedTo });
  });

  if (intervalsToAdd.length === 0) {
    return;
  }

  const usedSlotNumbersByKey = new Map<string, Set<number>>();
  slots.forEach(slot => {
    if (slot.dayOfMonth !== dayOfMonth) {
      return;
    }

    const key = `${slot.dayOfMonth}:${slot.fromTime}:${slot.toTime}`;
    const used = usedSlotNumbersByKey.get(key) ?? new Set<number>();
    used.add(slot.slotNo);
    usedSlotNumbersByKey.set(key, used);
  });

  intervalsToAdd.forEach(interval => {
    const key = `${dayOfMonth}:${interval.from}:${interval.to}`;
    const used = usedSlotNumbersByKey.get(key) ?? new Set<number>();
    let slotNo = 1;
    while (used.has(slotNo)) {
      slotNo += 1;
    }

    used.add(slotNo);
    usedSlotNumbersByKey.set(key, used);

    slots.push({
      id: getNextDraftSlotId(slots),
      scheduleId,
      dayOfMonth,
      slotNo,
      fromTime: interval.from,
      toTime: interval.to,
      employeeId,
      status: preservedStatus,
    });
  });
}

function computeConflictForDayWithStaffing(
  slots: GraphSlot[],
  dayOfMonth: number,
  peoplePerShift: number,
  shift1Range?: string | null,
  shift2Range?: string | null,
) {
  const normalizedPeoplePerShift = peoplePerShift > 0 ? peoplePerShift : 1;
  const shifts = [tryParseGraphShiftRange(shift1Range), tryParseGraphShiftRange(shift2Range)].filter(Boolean) as Array<{
    fromMinutes: number;
    toMinutes: number;
  }>;

  const assignedSlots = slots.filter(slot => slot.dayOfMonth === dayOfMonth && slot.employeeId && slot.employeeId > 0);
  const hasUnassignedSlot = slots.some(slot => slot.dayOfMonth === dayOfMonth && (!slot.employeeId || slot.employeeId <= 0));

  const intervalsByEmployeeId = new Map<number, Array<{ fromMinutes: number; toMinutes: number }>>();
  assignedSlots.forEach(slot => {
    const fromMinutes = parseGraphTimeMinutes(slot.fromTime);
    let toMinutes = parseGraphTimeMinutes(slot.toTime);

    if (fromMinutes === null || toMinutes === null || !slot.employeeId) {
      return;
    }

    if (toMinutes < fromMinutes) {
      toMinutes += 24 * 60;
    }

    const list = intervalsByEmployeeId.get(slot.employeeId) ?? [];
    list.push({ fromMinutes, toMinutes });
    intervalsByEmployeeId.set(slot.employeeId, list);
  });

  for (const employeeIntervals of intervalsByEmployeeId.values()) {
    if (hasGraphIntervalOverlap(employeeIntervals)) {
      return true;
    }
  }

  if (shifts.length === 0) {
    return hasUnassignedSlot;
  }

  for (const shift of shifts) {
    const coveredEmployeeIds = new Set<number>();
    assignedSlots.forEach(slot => {
      if (!slot.employeeId) {
        return;
      }

      const fromMinutes = parseGraphTimeMinutes(slot.fromTime);
      let toMinutes = parseGraphTimeMinutes(slot.toTime);

      if (fromMinutes === null || toMinutes === null) {
        return;
      }

      if (toMinutes < fromMinutes) {
        toMinutes += 24 * 60;
      }

      if (fromMinutes <= shift.fromMinutes && toMinutes >= shift.toMinutes) {
        coveredEmployeeIds.add(slot.employeeId);
      }
    });

    if (coveredEmployeeIds.size < normalizedPeoplePerShift) {
      return true;
    }
  }

  return false;
}

function tryParseGraphShiftRange(value?: string | null) {
  const normalized = (value ?? "").trim().replace(/[–—]/g, "-");
  if (!normalized) {
    return null;
  }

  const parts = normalized
    .split("-", 2)
    .map(part => part.trim())
    .filter(Boolean);

  if (parts.length !== 2) {
    return null;
  }

  const fromMinutes = parseGraphTimeMinutes(parts[0]);
  let toMinutes = parseGraphTimeMinutes(parts[1]);

  if (fromMinutes === null || toMinutes === null) {
    return null;
  }

  if (toMinutes < fromMinutes) {
    toMinutes += 24 * 60;
  }

  if (toMinutes === fromMinutes) {
    return null;
  }

  return { fromMinutes, toMinutes };
}

function hasGraphIntervalOverlap(intervals: Array<{ fromMinutes: number; toMinutes: number }>) {
  if (intervals.length <= 1) {
    return false;
  }

  const orderedIntervals = [...intervals].sort((left, right) =>
    left.fromMinutes === right.fromMinutes
      ? left.toMinutes - right.toMinutes
      : left.fromMinutes - right.fromMinutes,
  );

  let lastEnd = orderedIntervals[0].toMinutes;
  for (let index = 1; index < orderedIntervals.length; index += 1) {
    const currentInterval = orderedIntervals[index];
    if (currentInterval.fromMinutes < lastEnd) {
      return true;
    }

    lastEnd = Math.max(lastEnd, currentInterval.toMinutes);
  }

  return false;
}

function buildGraphSummaryDayCell(value: string): GraphSummaryDayCell {
  const trimmed = value.trim();

  if (!trimmed || trimmed === GRAPH_EMPTY_MARK) {
    return { from: "", to: "", hours: "" };
  }

  const matches = [...trimmed.matchAll(/\b([01]?\d|2[0-3]):[0-5]\d\b/g)].map(match => match[0]);
  if (matches.length < 2) {
    return { from: trimmed, to: "", hours: "" };
  }

  const totalMinutes = matches.reduce((sum, _, index) => {
    if (index % 2 !== 0) {
      return sum;
    }

    const fromMinutes = parseGraphTimeMinutes(matches[index]);
    const toMinutes = parseGraphTimeMinutes(matches[index + 1] ?? "");

    if (fromMinutes === null || toMinutes === null || toMinutes <= fromMinutes) {
      return sum;
    }

    return sum + (toMinutes - fromMinutes);
  }, 0);

  return {
    from: matches[0],
    to: matches[matches.length - 1],
    hours: formatSummaryMinutes(totalMinutes),
  };
}

function formatSummaryMinutes(totalMinutes: number) {
  if (totalMinutes <= 0) {
    return "";
  }

  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  return minutes === 0 ? `${hours}` : `${hours}h ${minutes}m`;
}

function getEmployeeLabel(employeeId: number, employeesById?: Map<number, Employee>) {
  return getEmployeeFullName(employeesById?.get(employeeId), `Employee ${employeeId}`);
}

function normalizeGraphTime(value: string) {
  const match = value.trim().match(/^(\d{1,2}):(\d{2})$/);
  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);

  if (!Number.isInteger(hours) || !Number.isInteger(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null;
  }

  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
}

function parseGraphTimeMinutes(value: string) {
  const normalized = normalizeGraphTime(value);
  if (!normalized) {
    return null;
  }

  const [hours, minutes] = normalized.split(":").map(Number);
  return hours * 60 + minutes;
}

function minutesToTimeLabel(totalMinutes: number) {
  const normalizedMinutes = ((totalMinutes % (24 * 60)) + (24 * 60)) % (24 * 60);
  const hours = Math.floor(normalizedMinutes / 60);
  const minutes = normalizedMinutes % 60;
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
}

function getNextDraftSlotId(slots: GraphSlot[]) {
  let minId = 0;
  slots.forEach(slot => {
    if (slot.id < minId) {
      minId = slot.id;
    }
  });

  return minId - 1;
}

function areGraphSlotsEqual(left: GraphSlot, right: GraphSlot) {
  return (
    left.scheduleId === right.scheduleId &&
    left.dayOfMonth === right.dayOfMonth &&
    left.slotNo === right.slotNo &&
    left.fromTime === right.fromTime &&
    left.toTime === right.toTime &&
    (left.employeeId ?? null) === (right.employeeId ?? null) &&
    String(left.status) === String(right.status)
  );
}
