import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { parseFlexibleTimeSegment } from "@shared/lib/timeRange";
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
export type GraphMatrixVisualHintMap = Record<string, string>;
export type GraphRelatedScheduleHintDayValue = {
  dayOfMonth: number;
  weekdayLabel: string;
  value: string;
  isWorked: boolean;
  isWeekend: boolean;
};
export type GraphRelatedScheduleHintEntry = {
  graphId: number;
  graphName: string;
  intervals: Array<{ from: string; to: string }>;
  intervalsText: string;
  dayValues: GraphRelatedScheduleHintDayValue[];
};
export type GraphRelatedScheduleHintDetail = {
  employeeId: number;
  dayOfMonth: number;
  visualHint: string;
  relatedGraphs: GraphRelatedScheduleHintEntry[];
};
export type GraphRelatedScheduleHintDetailMap = Record<string, GraphRelatedScheduleHintDetail>;
export type GraphRelatedScheduleHintData = {
  visualHintMap: GraphMatrixVisualHintMap;
  detailMap: GraphRelatedScheduleHintDetailMap;
};

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

export type GraphCellContent =
  | { kind: "empty"; value: [] }
  | { kind: "intervals"; value: Array<{ from: string; to: string }> }
  | { kind: "text"; value: string }
  | { kind: "invalid"; error: string };

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

export function buildGraphRelatedScheduleHintData(params: {
  currentGraph: Pick<Graph, "id" | "year" | "month">;
  columns: Array<Pick<GraphMatrixColumn, "employeeId" | "kind">>;
  cellMap: GraphMatrixCellMap;
  relatedGraphs: Array<{
    graph: Pick<Graph, "id" | "name" | "year" | "month">;
    slots: GraphSlot[];
  }>;
}) {
  const { currentGraph, columns, relatedGraphs } = params;
  const employeeIdSet = new Set(
    columns
      .filter(column => column.kind === "employee" && column.employeeId > 0)
      .map(column => column.employeeId),
  );

  if (employeeIdSet.size === 0 || relatedGraphs.length === 0) {
    return {
      visualHintMap: {} satisfies GraphMatrixVisualHintMap,
      detailMap: {} satisfies GraphRelatedScheduleHintDetailMap,
    } satisfies GraphRelatedScheduleHintData;
  }

  const detailByCellKey = new Map<string, GraphRelatedScheduleHintDetail>();

  relatedGraphs.forEach(({ graph, slots }) => {
    if (
      graph.id === currentGraph.id ||
      graph.year !== currentGraph.year ||
      graph.month !== currentGraph.month
    ) {
      return;
    }

    const graphName = graph.name.trim();
    if (!graphName) {
      return;
    }

    const relatedGraphCellMap = buildGraphCellMap(slots);
    const dayValuesByEmployeeId = new Map<number, GraphRelatedScheduleHintDayValue[]>();
    const slotsByCellKey = new Map<string, GraphSlot[]>();

    slots.forEach(slot => {
      if (!slot.employeeId || !employeeIdSet.has(slot.employeeId)) {
        return;
      }

      const cellKey = getGraphCellKey(slot.employeeId, slot.dayOfMonth);
      const cellSlots = slotsByCellKey.get(cellKey) ?? [];
      cellSlots.push(slot);
      slotsByCellKey.set(cellKey, cellSlots);
    });

    slotsByCellKey.forEach((cellSlots, cellKey) => {
      const [employeeIdValue, dayOfMonthValue] = cellKey.split(":");
      const employeeId = Number(employeeIdValue);
      const dayOfMonth = Number(dayOfMonthValue);

      if (!Number.isInteger(employeeId) || !Number.isInteger(dayOfMonth)) {
        return;
      }

      const mergedIntervals = mergeGraphIntervalsForDisplay(cellSlots);
      const dayValues = dayValuesByEmployeeId.get(employeeId) ?? buildGraphRelatedScheduleHintDayValues({
        year: graph.year,
        month: graph.month,
        employeeId,
        cellMap: relatedGraphCellMap,
      });
      if (!dayValuesByEmployeeId.has(employeeId)) {
        dayValuesByEmployeeId.set(employeeId, dayValues);
      }
      const nextEntry = {
        graphId: graph.id,
        graphName,
        intervals: mergedIntervals,
        intervalsText: formatGraphIntervals(mergedIntervals),
        dayValues,
      } satisfies GraphRelatedScheduleHintEntry;
      const currentDetail = detailByCellKey.get(cellKey) ?? {
        employeeId,
        dayOfMonth,
        visualHint: "",
        relatedGraphs: [],
      };

      if (currentDetail.relatedGraphs.some(item => item.graphId === nextEntry.graphId)) {
        return;
      }

      currentDetail.relatedGraphs.push(nextEntry);
      detailByCellKey.set(cellKey, currentDetail);
    });
  });

  return [...detailByCellKey.entries()].reduce<GraphRelatedScheduleHintData>((accumulator, [cellKey, detail]) => {
    const orderedRelatedGraphs = [...detail.relatedGraphs].sort((left, right) => {
      const nameDifference = left.graphName.localeCompare(right.graphName);
      if (nameDifference !== 0) {
        return nameDifference;
      }

      return left.graphId - right.graphId;
    });
    const visualHint = [...new Set(orderedRelatedGraphs.map(item => item.graphName))].join(", ");

    if (!visualHint) {
      return accumulator;
    }

    accumulator.visualHintMap[cellKey] = visualHint;
    accumulator.detailMap[cellKey] = {
      ...detail,
      visualHint,
      relatedGraphs: orderedRelatedGraphs,
    };

    return accumulator;
  }, {
    visualHintMap: {} satisfies GraphMatrixVisualHintMap,
    detailMap: {} satisfies GraphRelatedScheduleHintDetailMap,
  });
}

export function buildGraphRelatedScheduleHintMap(params: {
  currentGraph: Pick<Graph, "id" | "year" | "month">;
  columns: Array<Pick<GraphMatrixColumn, "employeeId" | "kind">>;
  cellMap: GraphMatrixCellMap;
  relatedGraphs: Array<{
    graph: Pick<Graph, "id" | "name" | "year" | "month">;
    slots: GraphSlot[];
  }>;
}) {
  return buildGraphRelatedScheduleHintData(params).visualHintMap;
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
      .split("-")
      .map(segment => segment.trim())
      .filter(Boolean);

    if (segments.length !== 2) {
      return { ok: false, error: "Use time ranges like 09:00 - 15:00. Comma-separated ranges are allowed." };
    }

    const from = normalizeGraphTime(segments[0]);
    const to = normalizeGraphTime(segments[1]);

    if (!from || !to) {
      return { ok: false, error: "Use time ranges like 09:00 - 15:00." };
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

export function normalizeGraphCellValue(input: string) {
  const parsed = parseGraphCellContent(input);

  if (parsed.kind === "empty") {
    return GRAPH_EMPTY_MARK;
  }

  if (parsed.kind === "intervals") {
    return formatGraphIntervals(parsed.value);
  }

  return input.trim();
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
      const parsed = parseGraphCellContent(cellMap[key] ?? GRAPH_EMPTY_MARK);
      if (parsed.kind === "invalid") {
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
      const parsed = parseGraphCellContent(cellMap[getGraphCellKey(employeeId, dayOfMonth)] ?? GRAPH_EMPTY_MARK);
      if (parsed.kind === "invalid") {
        errors[getGraphCellKey(employeeId, dayOfMonth)] = parsed.error;
        continue;
      }

      applyIntervalsToGraphSlots(
        scheduleId,
        nextSlots,
        dayOfMonth,
        employeeId,
        parsed.kind === "intervals" ? parsed.value : [],
      );
    }
  });

  return {
    slots: reuseExistingAssignedSlotIds(existingSlots, nextSlots),
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

function reuseExistingAssignedSlotIds(existingSlots: GraphSlot[], nextSlots: GraphSlot[]) {
  const reusableSlotsByKey = new Map<string, GraphSlot[]>();

  existingSlots.forEach(slot => {
    if (slot.id <= 0 || !slot.employeeId || slot.employeeId <= 0) {
      return;
    }

    const key = getAssignedSlotReuseKey(slot);
    const list = reusableSlotsByKey.get(key) ?? [];
    list.push(slot);
    reusableSlotsByKey.set(key, list);
  });

  return nextSlots.map(slot => {
    if (slot.id > 0 || !slot.employeeId || slot.employeeId <= 0) {
      return slot;
    }

    const key = getAssignedSlotReuseKey(slot);
    const reusableSlot = reusableSlotsByKey.get(key)?.shift();
    if (!reusableSlot) {
      return slot;
    }

    return {
      ...slot,
      id: reusableSlot.id,
      scheduleId: reusableSlot.scheduleId,
    };
  });
}

function getAssignedSlotReuseKey(slot: Pick<GraphSlot, "dayOfMonth" | "fromTime" | "toTime" | "employeeId">) {
  return `${slot.dayOfMonth}:${slot.fromTime}:${slot.toTime}:${slot.employeeId ?? 0}`;
}

export function applyIntervalsToGraphSlots(
  scheduleId: number,
  slots: GraphSlot[],
  dayOfMonth: number,
  employeeId: number,
  intervals: Array<{ from: string; to: string }>,
) {
  const preservedStatus: SlotStatus = 1;

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
      vacantSlot.status = 1;
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
    if (!hasRequiredGraphStaffingCoverage(intervalsByEmployeeId, shift, normalizedPeoplePerShift)) {
      return true;
    }
  }

  return false;
}

function hasRequiredGraphStaffingCoverage(
  intervalsByEmployeeId: Map<number, Array<{ fromMinutes: number; toMinutes: number }>>,
  shift: { fromMinutes: number; toMinutes: number },
  requiredPeople: number,
) {
  const relevantIntervalsByEmployeeId = new Map<number, Array<{ fromMinutes: number; toMinutes: number }>>();
  const coveragePoints = new Set<number>([shift.fromMinutes, shift.toMinutes]);

  intervalsByEmployeeId.forEach((intervals, employeeId) => {
    intervals.forEach(interval => {
      const fromMinutes = Math.max(interval.fromMinutes, shift.fromMinutes);
      const toMinutes = Math.min(interval.toMinutes, shift.toMinutes);

      if (toMinutes <= fromMinutes) {
        return;
      }

      coveragePoints.add(fromMinutes);
      coveragePoints.add(toMinutes);

      const relevantIntervals = relevantIntervalsByEmployeeId.get(employeeId) ?? [];
      relevantIntervals.push({ fromMinutes, toMinutes });
      relevantIntervalsByEmployeeId.set(employeeId, relevantIntervals);
    });
  });

  const orderedCoveragePoints = [...coveragePoints].sort((left, right) => left - right);
  for (let index = 0; index < orderedCoveragePoints.length - 1; index += 1) {
    const segmentStart = orderedCoveragePoints[index];
    const segmentEnd = orderedCoveragePoints[index + 1];

    if (segmentEnd <= segmentStart) {
      continue;
    }

    let activeEmployees = 0;
    for (const intervals of relevantIntervalsByEmployeeId.values()) {
      const coversSegment = intervals.some(interval =>
        interval.fromMinutes <= segmentStart && interval.toMinutes >= segmentEnd,
      );

      if (coversSegment) {
        activeEmployees += 1;
      }
    }

    if (activeEmployees < requiredPeople) {
      return false;
    }
  }

  return true;
}

function tryParseGraphShiftRange(value?: string | null) {
  const normalized = (value ?? "").trim().replace(/[–—]/g, "-");
  if (!normalized) {
    return null;
  }

  const parts = normalized
    .split("-")
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

function containsAlphabeticCharacter(value: string) {
  return [...value].some(character => character.toLowerCase() !== character.toUpperCase());
}

function looksLikeGraphTimeAttempt(value: string) {
  return /[:.,]/.test(value) || /[-–—]/.test(value);
}

export function parseGraphCellContent(input: string): GraphCellContent {
  const trimmed = input.trim();
  const parsed = tryParseGraphIntervals(trimmed);

  if (parsed.ok) {
    if (parsed.value.length === 0) {
      return { kind: "empty", value: [] };
    }

    return { kind: "intervals", value: parsed.value };
  }

  if (containsAlphabeticCharacter(trimmed) || !looksLikeGraphTimeAttempt(trimmed)) {
    return { kind: "text", value: trimmed };
  }

  return { kind: "invalid", error: parsed.error };
}

function normalizeGraphTime(value: string) {
  return parseFlexibleTimeSegment(value)?.label ?? null;
}

function parseGraphTimeMinutes(value: string) {
  const normalized = normalizeGraphTime(value);
  if (!normalized) {
    return null;
  }

  const [hours, minutes] = normalized.split(":").map(Number);
  return hours * 60 + minutes;
}

function buildGraphRelatedScheduleHintDayValues(params: {
  year: number;
  month: number;
  employeeId: number;
  cellMap: GraphMatrixCellMap;
}) {
  const { year, month, employeeId, cellMap } = params;
  const daysInMonth = getGraphDaysInMonth(year, month);

  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;
    const value = cellMap[getGraphCellKey(employeeId, dayOfMonth)] ?? GRAPH_EMPTY_MARK;

    return {
      dayOfMonth,
      weekdayLabel: getGraphWeekdayLabel(year, month, dayOfMonth),
      value,
      isWorked: value.trim() !== GRAPH_EMPTY_MARK,
      isWeekend: isGraphWeekend(year, month, dayOfMonth),
    } satisfies GraphRelatedScheduleHintDayValue;
  });
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
