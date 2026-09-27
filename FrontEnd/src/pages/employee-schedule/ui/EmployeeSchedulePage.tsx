import { t, dateTimeFormat, numberFormat } from "@shared/i18n";
import { useEffect, useLayoutEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useEmployeeScheduleListQuery,
  type EmployeeSchedule,
  type EmployeeScheduleEmployee,
  type EmployeeScheduleSlot,
} from "@entities/employee-schedule";
import {
  useCreateShiftCorrectionMutation,
  useEmployeeShiftCorrectionsQuery,
  type CreateShiftCorrectionInput,
} from "@entities/shift-corrections";
import {
  formatGraphIntervals,
  getGraphCellKey,
  mergeGraphIntervalsForDisplay,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
} from "@entities/containers/model/graphWorkspace";
import { parseGraphNoteContent } from "@entities/containers/model/graphNote";
import { ContainerGraphMatrix } from "@entities/containers/ui/ContainerGraphMatrix";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { employeeUiStateApi, useEmployeeUiStateQuery } from "@entities/employee-ui-state";
import { getErrorMessage } from "@shared/api/httpClient";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { AvailabilityIcon, ScheduleDetailsIcon, ScheduleIcon, StatisticsIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import { EmployeeScheduleColumnOrderDialog } from "./EmployeeScheduleColumnOrderDialog";
import { EmployeeShiftCorrectionDialog } from "./EmployeeShiftCorrectionDialog";
import styles from "./EmployeeSchedulePage.module.css";
import { EmployeeScheduleHero } from "./EmployeeScheduleHero";

const scheduleMonthFormatter = dateTimeFormat("en-GB", {
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

const scheduleMonthOnlyFormatter = dateTimeFormat("en-GB", {
  month: "long",
  timeZone: "UTC",
});

const scheduleWeekdayFormatter = dateTimeFormat("en-GB", {
  weekday: "long",
  timeZone: "UTC",
});

const scheduleWeekdayShortFormatter = dateTimeFormat("en-GB", {
  weekday: "short",
  timeZone: "UTC",
});

const salaryAmountFormatter = numberFormat("en-GB", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatSalaryHours(value: number) {
  return String(Math.round(value * 100) / 100);
}

function parseSalaryValue(value: string) {
  if (!value.trim()) {
    return null;
  }

  const parsedValue = Number(value.trim().replace(",", "."));
  return Number.isFinite(parsedValue) && parsedValue >= 0 ? parsedValue : null;
}
function formatScheduleMonth(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return scheduleMonthFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)));
}

function formatScheduleMonthOnly(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return scheduleMonthOnlyFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)));
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

function getWeekdayIndex(year: number, month: number, dayOfMonth: number) {
  return new Date(Date.UTC(year, month - 1, dayOfMonth)).getUTCDay();
}

function isWeekendDay(year: number, month: number, dayOfMonth: number) {
  const weekdayIndex = getWeekdayIndex(year, month, dayOfMonth);
  return weekdayIndex === 0 || weekdayIndex === 6;
}

function parseTimeMinutes(value: string) {
  const match = value.trim().match(/^(\d{1,2}):(\d{2})/);
  if (!match) {
    return null;
  }

  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (!Number.isInteger(hour) || !Number.isInteger(minute)) {
    return null;
  }

  return Math.min(23, Math.max(0, hour)) * 60 + Math.min(59, Math.max(0, minute));
}

function getSlotDurationHours(slot: EmployeeScheduleSlot) {
  const fromMinutes = parseTimeMinutes(slot.fromTime);
  const toMinutes = parseTimeMinutes(slot.toTime);

  if (fromMinutes === null || toMinutes === null) {
    return 0;
  }

  const durationMinutes = toMinutes > fromMinutes
    ? toMinutes - fromMinutes
    : toMinutes + 24 * 60 - fromMinutes;

  return durationMinutes / 60;
}

function formatHours(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  return Number.isInteger(roundedValue) ? `${roundedValue}h` : `${roundedValue.toFixed(1)}h`;
}

function isCurrentEmployeeSlot(slot: EmployeeScheduleSlot, employeeId: number | null) {
  if (employeeId === null) {
    return slot.employeeId === undefined || slot.employeeId === null;
  }

  return slot.employeeId === undefined || slot.employeeId === null || slot.employeeId === employeeId;
}

function getScheduleStats(
  schedules: EmployeeSchedule[],
  selectedSchedule: EmployeeSchedule | null,
  employeeId: number | null,
) {
  if (!selectedSchedule) {
    return {
      scheduleCount: 0,
      workDays: 0,
      freeDays: 0,
      totalHours: "0h",
      month: "-",
      year: "-",
      period: "-",
    };
  }

  const monthSchedules = schedules.filter(
    schedule => schedule.year === selectedSchedule.year && schedule.month === selectedSchedule.month,
  );
  const workDays = new Set<number>();
  const totalHours = monthSchedules.reduce((sum, schedule) => {
    const employeeSlots = schedule.slots.filter(slot => isCurrentEmployeeSlot(slot, employeeId));

    employeeSlots.forEach(slot => {
      if (slot.dayOfMonth >= 1 && slot.dayOfMonth <= getDaysInMonth(schedule.year, schedule.month)) {
        workDays.add(slot.dayOfMonth);
      }
    });

    return sum + employeeSlots.reduce((slotSum, slot) => slotSum + getSlotDurationHours(slot), 0);
  }, 0);
  const daysInMonth = getDaysInMonth(selectedSchedule.year, selectedSchedule.month);

  return {
    scheduleCount: monthSchedules.length,
    workDays: workDays.size,
    freeDays: Math.max(0, daysInMonth - workDays.size),
    totalHours: formatHours(totalHours),
    month: formatScheduleMonthOnly(selectedSchedule),
    year: String(selectedSchedule.year),
    period: formatScheduleMonth(selectedSchedule),
  };
}

const summaryWeekdayLabels = ["su.", "mo.", "tu.", "we.", "th.", "fr.", "sa."] as const;

type ScheduleHoursSummaryRow = {
  key: string;
  dayOfMonth: number;
  dayLabel: string;
  hours: number;
  hoursText: string;
  scheduleName: string;
  sortValue: number;
};

type ScheduleSummaryPeriod = {
  key: string;
  year: number;
  month: number;
};

function formatSummaryDay(schedule: Pick<EmployeeSchedule, "year" | "month">, dayOfMonth: number) {
  const weekdayLabel = summaryWeekdayLabels[getWeekdayIndex(schedule.year, schedule.month, dayOfMonth)];
  return `${weekdayLabel}/${String(dayOfMonth).padStart(2, "0")}`;
}

function getScheduleSummaryName(schedule: EmployeeSchedule) {
  return schedule.name.trim() || schedule.shopName.trim() || schedule.containerName.trim() || t("Schedule #{0}", schedule.id);
}

function getScheduleSummaryPeriods(schedules: EmployeeSchedule[]) {
  const periodsByKey = new Map<string, ScheduleSummaryPeriod>();

  schedules.forEach(schedule => {
    const key = `${schedule.year}:${schedule.month}`;
    periodsByKey.set(key, {
      key,
      year: schedule.year,
      month: schedule.month,
    });
  });

  return [...periodsByKey.values()].sort((left, right) =>
    right.year - left.year || right.month - left.month,
  );
}

function buildScheduleHoursSummary(
  schedules: EmployeeSchedule[],
  period: ScheduleSummaryPeriod | null,
  employeeId: number | null,
) {
  if (!period) {
    return {
      rows: [] as ScheduleHoursSummaryRow[],
      totalHours: 0,
      totalHoursText: "0h",
    };
  }

  const rowsByDay = new Map<number, Omit<ScheduleHoursSummaryRow, "hoursText" | "scheduleName"> & {
    scheduleNames: string[];
  }>();
  schedules
    .filter(schedule => schedule.year === period.year && schedule.month === period.month)
    .forEach(schedule => {
      schedule.slots
        .filter(slot => isCurrentEmployeeSlot(slot, employeeId))
        .forEach(slot => {
          const existingRow = rowsByDay.get(slot.dayOfMonth);
          const scheduleName = getScheduleSummaryName(schedule);
          const nextHours = (existingRow?.hours ?? 0) + getSlotDurationHours(slot);
          rowsByDay.set(slot.dayOfMonth, {
            key: `day:${period.year}:${period.month}:${slot.dayOfMonth}`,
            dayOfMonth: slot.dayOfMonth,
            dayLabel: formatSummaryDay(schedule, slot.dayOfMonth),
            hours: nextHours,
            scheduleNames: existingRow?.scheduleNames.includes(scheduleName)
              ? existingRow.scheduleNames
              : [...(existingRow?.scheduleNames ?? []), scheduleName],
            sortValue: Date.UTC(schedule.year, schedule.month - 1, slot.dayOfMonth),
          });
        });
    });

  const workedRows = [...rowsByDay.values()]
    .map(row => ({
      ...row,
      hoursText: formatHours(row.hours),
      scheduleName: row.scheduleNames.sort((left, right) => left.localeCompare(right)).join(", "),
    }))
    .sort((left, right) => left.dayOfMonth - right.dayOfMonth);

  const workedRowsByDay = new Map(workedRows.map(row => [row.dayOfMonth, row]));

  const daysInMonth = getDaysInMonth(period.year, period.month);
  const rows: ScheduleHoursSummaryRow[] = [];
  for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
    const dayRow = workedRowsByDay.get(dayOfMonth);
    if (dayRow) {
      rows.push(dayRow);
      continue;
    }

    rows.push({
      key: `empty:${period.year}:${period.month}:${dayOfMonth}`,
      dayOfMonth,
      dayLabel: formatSummaryDay(period, dayOfMonth),
      hours: 0,
      hoursText: "-",
      scheduleName: "-",
      sortValue: Date.UTC(period.year, period.month - 1, dayOfMonth),
    });
  }

  const totalHours = rows.reduce((sum, row) => sum + row.hours, 0);

  return {
    rows,
    totalHours,
    totalHoursText: formatHours(totalHours),
  };
}

function buildScheduleMatrixColumn(
  employeeId: number,
  label: string,
  graphEmployeeId: number | null = null,
  minHoursMonth: number | null = null,
): GraphMatrixColumn {
  return {
    employeeId,
    kind: "employee",
    manualColumnId: null,
    graphEmployeeId,
    label,
    minHoursMonth,
    totalMinutes: 0,
    totalText: "",
  };
}

function buildManualColumnEmployeeId(columnId: number) {
  return -Math.abs(columnId);
}

function buildManualScheduleMatrixColumn(column: { id: number; label: string }): GraphMatrixColumn {
  return {
    employeeId: buildManualColumnEmployeeId(column.id),
    kind: "manual",
    manualColumnId: column.id,
    graphEmployeeId: null,
    label: column.label,
    minHoursMonth: null,
    totalMinutes: 0,
    totalText: "",
  };
}

function getScheduleEmployeeLabel(employee: EmployeeScheduleEmployee) {
  return employee.displayName?.trim() || getEmployeeFullName(employee, t("Employee #{0}", employee.employeeId));
}

function sortScheduleEmployees(employees: EmployeeScheduleEmployee[]) {
  return [...employees].sort((left, right) => {
    if (left.displayOrder !== right.displayOrder) {
      return left.displayOrder - right.displayOrder;
    }

    return getScheduleEmployeeLabel(left).localeCompare(getScheduleEmployeeLabel(right));
  });
}

type DailyScheduleWorker = {
  employeeId: number;
  label: string;
  shifts: EmployeeScheduleSlot[];
  relatedScheduleNames: string[];
  isAllDay: boolean;
  firstStartMinutes: number;
};

function isAllDayScheduleSlot(slot: EmployeeScheduleSlot) {
  const fromTime = slot.fromTime.trim();
  const toTime = slot.toTime.trim();
  return fromTime === "00:00" && (toTime === "00:00" || toTime === "23:59" || toTime === "24:00");
}

function buildDailyScheduleWorkers(
  schedule: EmployeeSchedule,
  dayOfMonth: number,
  fallbackEmployeeId: number,
  fallbackLabel: string,
) {
  const employeesById = new Map((schedule.employees ?? []).map(employee => [employee.employeeId, employee] as const));
  const slotsByEmployeeId = new Map<number, EmployeeScheduleSlot[]>();
  const relatedScheduleNamesByEmployeeId = new Map<number, Set<string>>();

  (schedule.relatedScheduleAssignments ?? [])
    .filter(assignment => assignment.dayOfMonth === dayOfMonth)
    .forEach(assignment => {
      const scheduleName = assignment.scheduleName.trim() || t("Schedule #{0}", assignment.scheduleId);
      const scheduleNames = relatedScheduleNamesByEmployeeId.get(assignment.employeeId) ?? new Set<string>();
      scheduleNames.add(scheduleName);
      relatedScheduleNamesByEmployeeId.set(assignment.employeeId, scheduleNames);
    });

  relatedScheduleNamesByEmployeeId.forEach((_, employeeId) => {
    if (!slotsByEmployeeId.has(employeeId)) {
      slotsByEmployeeId.set(employeeId, []);
    }
  });

  schedule.slots
    .filter(slot => slot.dayOfMonth === dayOfMonth)
    .forEach(slot => {
      const employeeId = slot.employeeId && slot.employeeId > 0 ? slot.employeeId : fallbackEmployeeId;
      const employeeSlots = slotsByEmployeeId.get(employeeId) ?? [];
      employeeSlots.push(slot);
      slotsByEmployeeId.set(employeeId, employeeSlots);
    });

  const workers = [...slotsByEmployeeId.entries()].map<DailyScheduleWorker>(([employeeId, slots]) => {
    const sortedSlots = [...slots].sort((left, right) =>
      (parseTimeMinutes(left.fromTime) ?? Number.MAX_SAFE_INTEGER) -
        (parseTimeMinutes(right.fromTime) ?? Number.MAX_SAFE_INTEGER) ||
      left.toTime.localeCompare(right.toTime),
    );
    const employee = employeesById.get(employeeId);

    return {
      employeeId,
      label: employee ? getScheduleEmployeeLabel(employee) : employeeId === fallbackEmployeeId
        ? fallbackLabel
        : t("Employee #{0}", employeeId),
      shifts: sortedSlots,
      relatedScheduleNames: [...(relatedScheduleNamesByEmployeeId.get(employeeId) ?? [])]
        .sort((left, right) => left.localeCompare(right)),
      isAllDay: sortedSlots.some(isAllDayScheduleSlot),
      firstStartMinutes: parseTimeMinutes(sortedSlots[0]?.fromTime ?? "") ?? Number.MAX_SAFE_INTEGER,
    };
  });

  const timedWorkers = workers
    .filter(worker => !worker.isAllDay)
    .sort((left, right) =>
      left.firstStartMinutes - right.firstStartMinutes || left.label.localeCompare(right.label),
    );
  const allDayWorkers = workers
    .filter(worker => worker.isAllDay)
    .sort((left, right) => left.label.localeCompare(right.label));
  const middleIndex = Math.ceil(timedWorkers.length / 2);

  return [
    ...timedWorkers.slice(0, middleIndex),
    ...allDayWorkers,
    ...timedWorkers.slice(middleIndex),
  ];
}

function formatDailyWorkerShifts(worker: DailyScheduleWorker) {
  const relatedScheduleText = worker.relatedScheduleNames.join(", ");
  if (worker.shifts.length === 0) {
    return relatedScheduleText;
  }

  const shiftText = worker.isAllDay
    ? t("All day")
    : formatGraphIntervals(mergeGraphIntervalsForDisplay(worker.shifts, { mergeTouching: true }));

  return relatedScheduleText ? `${shiftText}, ${relatedScheduleText}` : shiftText;
}

function buildScheduleMatrixColumns(
  schedule: EmployeeSchedule | null,
  fallbackEmployeeId: number,
  fallbackLabel: string,
): GraphMatrixColumn[] {
  const employees = schedule?.employees ?? [];
  const parsedGraphNote = parseGraphNoteContent(schedule?.note);
  const manualColumns = parsedGraphNote.manualColumns.map(buildManualScheduleMatrixColumn);

  if (employees.length === 0 && manualColumns.length === 0) {
    return [buildScheduleMatrixColumn(fallbackEmployeeId, fallbackLabel)];
  }

  const usedEmployeeIds = new Set<number>();
  const employeeColumns = sortScheduleEmployees(employees).reduce<GraphMatrixColumn[]>((columns, employee) => {
    if (usedEmployeeIds.has(employee.employeeId)) {
      return columns;
    }

    usedEmployeeIds.add(employee.employeeId);
    columns.push(buildScheduleMatrixColumn(
      employee.employeeId,
      getScheduleEmployeeLabel(employee),
      employee.id,
      employee.minHoursMonth ?? null,
    ));
    return columns;
  }, []);
  const columnsByEmployeeId = new Map(
    [...employeeColumns, ...manualColumns].map(column => [column.employeeId, column] as const),
  );
  const fallbackColumnOrder = [...employeeColumns, ...manualColumns].map(column => column.employeeId);
  const requestedColumnOrder = parsedGraphNote.columnOrder.length > 0
    ? parsedGraphNote.columnOrder
    : fallbackColumnOrder;
  const resolvedColumns = requestedColumnOrder
    .map(columnId => columnsByEmployeeId.get(columnId))
    .filter((column): column is GraphMatrixColumn => Boolean(column));

  fallbackColumnOrder.forEach(columnId => {
    const column = columnsByEmployeeId.get(columnId);
    if (column && !resolvedColumns.some(item => item.employeeId === column.employeeId)) {
      resolvedColumns.push(column);
    }
  });

  return resolvedColumns;
}

function buildScheduleMatrixCellMap(schedule: EmployeeSchedule | null, fallbackEmployeeId: number): GraphMatrixCellMap {
  if (!schedule) {
    return {};
  }

  const parsedGraphNote = parseGraphNoteContent(schedule.note);
  const slotsByCell = new Map<string, EmployeeScheduleSlot[]>();
  schedule.slots.forEach(slot => {
    const employeeId = slot.employeeId && slot.employeeId > 0 ? slot.employeeId : fallbackEmployeeId;
    const cellKey = getGraphCellKey(employeeId, slot.dayOfMonth);
    const cellSlots = slotsByCell.get(cellKey) ?? [];
    cellSlots.push(slot);
    slotsByCell.set(cellKey, cellSlots);
  });

  const slotCellMap = [...slotsByCell.entries()].reduce<GraphMatrixCellMap>((cellMap, [cellKey, cellSlots]) => {
    const value = formatGraphIntervals(mergeGraphIntervalsForDisplay(cellSlots, { mergeTouching: true }));

    if (value) {
      cellMap[cellKey] = value;
    }

    return cellMap;
  }, {});

  const manualCellMap = parsedGraphNote.manualColumns.reduce<GraphMatrixCellMap>((cellMap, column) => {
    Object.entries(column.cells).forEach(([dayOfMonth, value]) => {
      const parsedDayOfMonth = Number(dayOfMonth);
      if (!value.trim() || !Number.isInteger(parsedDayOfMonth)) {
        return;
      }

      cellMap[getGraphCellKey(buildManualColumnEmployeeId(column.id), parsedDayOfMonth)] = value;
    });

    return cellMap;
  }, {});

  return {
    ...slotCellMap,
    ...manualCellMap,
  };
}

function buildScheduleMatrixDisplayCellMap(
  selectedSchedule: EmployeeSchedule | null,
  cellMap: GraphMatrixCellMap,
): { cellMap: GraphMatrixCellMap; mutedSuffixMap: GraphMatrixCellMap } {
  if (!selectedSchedule) {
    return { cellMap, mutedSuffixMap: {} };
  }

  const scheduleNamesByCell = new Map<string, Set<string>>();
  (selectedSchedule.relatedScheduleAssignments ?? []).forEach(assignment => {
    const cellKey = getGraphCellKey(assignment.employeeId, assignment.dayOfMonth);
    const scheduleName = assignment.scheduleName.trim() || t("Schedule #{0}", assignment.scheduleId);
    const scheduleNames = scheduleNamesByCell.get(cellKey) ?? new Set<string>();
    scheduleNames.add(scheduleName);
    scheduleNamesByCell.set(cellKey, scheduleNames);
  });

  const mutedSuffixMap: GraphMatrixCellMap = {};
  const displayCellMap = [...scheduleNamesByCell.entries()].reduce<GraphMatrixCellMap>((result, [cellKey, scheduleNames]) => {
    const sortedScheduleNames = [...scheduleNames].sort((left, right) => left.localeCompare(right));
    const currentValue = cellMap[cellKey]?.trim();
    const relatedScheduleNames = sortedScheduleNames.join(", ");
    mutedSuffixMap[cellKey] = relatedScheduleNames;
    result[cellKey] = currentValue && currentValue !== "-"
      ? `${currentValue}, ${relatedScheduleNames}`
      : relatedScheduleNames;
    return result;
  }, { ...cellMap });

  return { cellMap: displayCellMap, mutedSuffixMap };
}

const employeeScheduleColumnOrderStorageVersion = 1;
const employeeScheduleColumnOrderStoragePrefix = "gf3:employee-schedule-column-order";

type StoredEmployeeScheduleColumnOrders = {
  version: number;
  scheduleOrders: Record<string, number[]>;
};

function getEmployeeScheduleColumnOrderStorageKey(employeeId: number | null, userName?: string | null) {
  const identity = employeeId && employeeId > 0
    ? "employee-" + employeeId
    : "user-" + encodeURIComponent(userName?.trim() || "unknown");
  return employeeScheduleColumnOrderStoragePrefix
    + ":v"
    + employeeScheduleColumnOrderStorageVersion
    + ":"
    + identity;
}

function sanitizeEmployeeScheduleColumnOrder(columnOrder: number[], columns: GraphMatrixColumn[]) {
  const availableIds = new Set(columns.map(column => column.employeeId));
  const seenIds = new Set<number>();
  const sanitizedOrder: number[] = [];

  columnOrder.forEach(columnId => {
    if (availableIds.has(columnId) && !seenIds.has(columnId)) {
      seenIds.add(columnId);
      sanitizedOrder.push(columnId);
    }
  });

  columns.forEach(column => {
    if (!seenIds.has(column.employeeId)) {
      sanitizedOrder.push(column.employeeId);
    }
  });

  return sanitizedOrder;
}

function applyEmployeeScheduleColumnOrder(columns: GraphMatrixColumn[], columnOrder: number[]) {
  const columnById = new Map(columns.map(column => [column.employeeId, column] as const));
  return sanitizeEmployeeScheduleColumnOrder(columnOrder, columns)
    .map(columnId => columnById.get(columnId))
    .filter((column): column is GraphMatrixColumn => Boolean(column));
}

function readEmployeeScheduleColumnOrders(storageKey: string): Record<string, number[]> {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? "null") as StoredEmployeeScheduleColumnOrders | null;
    if (!parsed || parsed.version !== employeeScheduleColumnOrderStorageVersion || !parsed.scheduleOrders) {
      return {};
    }

    return Object.entries(parsed.scheduleOrders).reduce<Record<string, number[]>>((result, [scheduleId, columnOrder]) => {
      if (Array.isArray(columnOrder)) {
        result[scheduleId] = columnOrder.filter(columnId => Number.isInteger(columnId));
      }
      return result;
    }, {});
  } catch {
    return {};
  }
}

function writeEmployeeScheduleColumnOrders(storageKey: string, scheduleOrders: Record<string, number[]>) {
  const payload: StoredEmployeeScheduleColumnOrders = {
    version: employeeScheduleColumnOrderStorageVersion,
    scheduleOrders,
  };

  try {
    window.localStorage.setItem(storageKey, JSON.stringify(payload));
  } catch {
    // Keep customization available for this session when browser storage is unavailable.
  }
}
const pdfTemplateEmployeeCapacity = 25;
const pdfTemplateDayCount = 31;
const pdfTemplateWeekdayLabels = ["niedz.", "pon.", "wt.", "\u015br.", "czw.", "pt.", "sob."];
function escapeHtml(value: string | number | null | undefined) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function buildPdfFileName(schedule: EmployeeSchedule) {
  const normalizedName = `${schedule.name}-${schedule.month}-${schedule.year}`
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return `${normalizedName || "open-schedule"}.pdf`;
}

function formatTemplateDateCell(schedule: EmployeeSchedule, dayOfMonth: number) {
  const weekdayLabel = pdfTemplateWeekdayLabels[getWeekdayIndex(schedule.year, schedule.month, dayOfMonth)];
  const dayText = String(dayOfMonth).padStart(2, "0");
  const monthText = String(schedule.month).padStart(2, "0");
  return `${weekdayLabel}, ${dayText}/${monthText}`;
}

// eslint-disable-next-line react-refresh/only-export-components -- exported for focused PDF layout verification
export function buildSchedulePdfHtml(
  schedule: EmployeeSchedule,
  columns: GraphMatrixColumn[],
  cellMap: GraphMatrixCellMap,
) {
  const daysInMonth = getDaysInMonth(schedule.year, schedule.month);
  const templateColumnCount = Math.max(pdfTemplateEmployeeCapacity, columns.length);
  const templateColumns = Array.from({ length: templateColumnCount }, (_, index) => columns[index] ?? null);
  const columnHeaders = templateColumns
    .map(column => `
          <td class="template-cell employee-header">
            <span>${escapeHtml(column?.label ?? "0")}</span>
          </td>`)
    .join("");
  const rows = Array.from({ length: pdfTemplateDayCount }, (_, index) => {
    const dayOfMonth = index + 1;
    const isRealMonthDay = dayOfMonth <= daysInMonth;
    const rowClass = isRealMonthDay && isWeekendDay(schedule.year, schedule.month, dayOfMonth)
      ? " template-weekend-row"
      : "";
    const dateCellValue = isRealMonthDay ? formatTemplateDateCell(schedule, dayOfMonth) : "";
    const cells = templateColumns
      .map(column => {
        const cellValue = column && isRealMonthDay ? cellMap[getGraphCellKey(column.employeeId, dayOfMonth)] ?? "-" : "-";
        return `<td class="template-cell matrix-cell">${escapeHtml(cellValue)}</td>`;
      })
      .join("");

    return `
        <tr class="template-row${rowClass}">
          <td class="template-cell date-cell">${escapeHtml(dateCellValue)}</td>
          ${cells}
        </tr>`;
  }).join("");
  const shopName = schedule.shopName?.trim() || schedule.name;

  return `<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <title>${escapeHtml(buildPdfFileName(schedule))}</title>
    <style>
      @page {
        size: A4 landscape;
        margin: 5mm;
      }

      * {
        box-sizing: border-box;
      }

      body {
        margin: 0;
        color: #000000;
        background: #ffffff;
        font-family: Calibri, Arial, Helvetica, sans-serif;
      }

      .template-sheet {
        width: auto;
        border-collapse: collapse;
        table-layout: auto;
        font-size: 10pt;
        line-height: 1.15;
      }

      .date-column {
        width: 10.42578125ch;
      }

      .employee-column {
        width: auto;
      }

      .template-cell {
        height: 15pt;
        padding: 0 3px;
        border: 1px solid #d9d9d9;
        background: #ffffff;
        color: #000000;
        font-weight: 400;
        vertical-align: middle;
        overflow: hidden;
      }

      .template-header-row .template-cell {
        height: 104.45pt;
      }

      .shop-header {
        position: relative;
        text-align: center;
      }

      .shop-header span {
        display: inline-block;
        max-width: 24mm;
        white-space: nowrap;
        transform: rotate(-45deg);
        transform-origin: center;
      }

      .employee-header {
        padding: 0;
        text-align: left;
        vertical-align: bottom;
      }

      .employee-header span {
        display: inline-block;
        max-height: 26mm;
        padding: 0 2px 3px;
        white-space: nowrap;
        writing-mode: vertical-rl;
        transform: rotate(180deg);
        text-align: left;
      }

      .date-cell {
        text-align: right;
        white-space: nowrap;
      }

      .matrix-cell {
        padding: 0 1px;
        text-align: center;
        white-space: nowrap;
        text-overflow: clip;
      }

      .template-weekend-row .template-cell {
        background-color: #ffffff;
        background-image: repeating-linear-gradient(
          45deg,
          rgba(0, 0, 0, 0.11) 0,
          rgba(0, 0, 0, 0.11) 1px,
          transparent 1px,
          transparent 3px
        );
      }

      @media print {
        body {
          print-color-adjust: exact;
          -webkit-print-color-adjust: exact;
        }
      }
    </style>
  </head>
  <body>
    <table class="template-sheet" aria-label="${escapeHtml(schedule.name)}">
      <colgroup>
        <col class="date-column" />
        ${templateColumns.map(() => "<col class=\"employee-column\" />").join("")}
      </colgroup>
      <tbody>
        <tr class="template-header-row">
          <td class="template-cell shop-header">
            <span>${escapeHtml(shopName)}</span>
          </td>
          ${columnHeaders}
        </tr>
        ${rows}
      </tbody>
    </table>
  </body>
</html>`;
}

function printSchedulePdf(
  schedule: EmployeeSchedule,
  columns: GraphMatrixColumn[],
  cellMap: GraphMatrixCellMap,
) {
  const printWindow = window.open("", "_blank", "width=1200,height=800");
  if (!printWindow) {
    throw new Error(t("Could not open the PDF export window."));
  }

  printWindow.document.open();
  printWindow.document.write(buildSchedulePdfHtml(schedule, columns, cellMap));
  printWindow.document.close();
  printWindow.document.title = buildPdfFileName(schedule);
  printWindow.focus();
  printWindow.setTimeout(() => printWindow.print(), 250);
}

export function EmployeeSchedulePage() {
  const { session } = useAuth();
  const scheduleQuery = useEmployeeScheduleListQuery();
  const shiftCorrectionsQuery = useEmployeeShiftCorrectionsQuery(Boolean(session?.employeeId));
  const createShiftCorrectionMutation = useCreateShiftCorrectionMutation();
  const { data: uiState, refetch: refetchUiState } = useEmployeeUiStateQuery(Boolean(session?.employeeId));
  const schedules = useMemo(() => scheduleQuery.data ?? [], [scheduleQuery.data]);
  const columnOrderStorageKey = getEmployeeScheduleColumnOrderStorageKey(
    session?.employeeId ?? null,
    session?.userName,
  );
  const [savedColumnOrders, setSavedColumnOrders] = useState<Record<string, number[]>>(
    () => readEmployeeScheduleColumnOrders(columnOrderStorageKey),
  );
  const [columnOrderDialogEmployeeId, setColumnOrderDialogEmployeeId] = useState<number | null>(null);
  const [selectedScheduleId, setSelectedScheduleId] = useState<number | null>(null);
  const [scheduleViewMode, setScheduleViewMode] = useState<"matrix" | "daily">("matrix");
  const [selectedDayOfMonth, setSelectedDayOfMonth] = useState<number | null>(null);
  const activeDailyScheduleDayRef = useRef<HTMLButtonElement | null>(null);
  const [isShiftCorrectionDialogOpen, setIsShiftCorrectionDialogOpen] = useState(false);
  const [shiftCorrectionError, setShiftCorrectionError] = useState<string | null>(null);
  const [shiftCorrectionSuccess, setShiftCorrectionSuccess] = useState<string | null>(null);
  const [preferenceSaveError, setPreferenceSaveError] = useState<string | null>(null);

  useEffect(() => {
    setSavedColumnOrders(readEmployeeScheduleColumnOrders(columnOrderStorageKey));
    setColumnOrderDialogEmployeeId(null);
  }, [columnOrderStorageKey]);

  useEffect(() => {
    if (!uiState) {
      return;
    }

    const localOrders = readEmployeeScheduleColumnOrders(columnOrderStorageKey);
    const serverOrders = uiState.scheduleColumnOrders ?? {};
    const mergedOrders = { ...localOrders, ...serverOrders };
    setSavedColumnOrders(mergedOrders);
    writeEmployeeScheduleColumnOrders(columnOrderStorageKey, mergedOrders);

    const localOnlyOrders = Object.entries(localOrders)
      .filter(([scheduleId]) => !Object.prototype.hasOwnProperty.call(serverOrders, scheduleId));
    if (localOnlyOrders.length > 0) {
      void Promise.all(localOnlyOrders.map(([scheduleId, columnOrder]) =>
        employeeUiStateApi.saveScheduleColumnOrder(Number(scheduleId), columnOrder),
      )).then(() => refetchUiState())
        .catch(error => setPreferenceSaveError(getErrorMessage(error, t("Could not sync the column order."))));
    }
  }, [columnOrderStorageKey, refetchUiState, setPreferenceSaveError, uiState]);
  const [selectedSummaryPeriodKey, setSelectedSummaryPeriodKey] = useState<string | null>(null);
  const [pdfExportError, setPdfExportError] = useState<string | null>(null);

  const selectedSchedule = useMemo(
    () => schedules.find(schedule => schedule.id === selectedScheduleId) ?? schedules[0] ?? null,
    [schedules, selectedScheduleId],
  );

  useEffect(() => {
    setSelectedDayOfMonth(null);
  }, [selectedSchedule?.id]);

  const summaryPeriods = useMemo(
    () => getScheduleSummaryPeriods(schedules),
    [schedules],
  );
  const activeSummaryPeriod = useMemo(() => {
    const selectedPeriod = summaryPeriods.find(period => period.key === selectedSummaryPeriodKey);
    if (selectedPeriod) {
      return selectedPeriod;
    }

    const openedSchedulePeriod = selectedSchedule
      ? summaryPeriods.find(period => period.year === selectedSchedule.year && period.month === selectedSchedule.month)
      : null;
    return openedSchedulePeriod ?? summaryPeriods[0] ?? null;
  }, [selectedSchedule, selectedSummaryPeriodKey, summaryPeriods]);
  const summaryYears = useMemo(
    () => Array.from(new Set(summaryPeriods.map(period => period.year))),
    [summaryPeriods],
  );
  const summaryMonths = useMemo(() => {
    if (!activeSummaryPeriod) {
      return [];
    }

    return summaryPeriods
      .filter(period => period.year === activeSummaryPeriod.year)
      .slice()
      .sort((left, right) => left.month - right.month);
  }, [activeSummaryPeriod, summaryPeriods]);
  const summaryMonthOptions = useMemo<SearchableSelectOption[]>(
    () => summaryMonths.map(period => ({
      value: String(period.month),
      label: formatScheduleMonthOnly(period),
    })),
    [summaryMonths],
  );
  const summaryYearOptions = useMemo<SearchableSelectOption[]>(
    () => summaryYears.map(year => ({ value: String(year), label: String(year) })),
    [summaryYears],
  );
  const displayName = session?.displayName?.trim() || session?.userName || t("Employee");
  const currentEmployeeId = session?.employeeId && session.employeeId > 0 ? session.employeeId : null;
  const fallbackMatrixEmployeeId = currentEmployeeId ?? 1;
  const dailyScheduleDays = useMemo(() => {
    if (!selectedSchedule) {
      return [];
    }

    return Array.from({ length: getDaysInMonth(selectedSchedule.year, selectedSchedule.month) }, (_, index) => {
      const dayOfMonth = index + 1;
      const date = new Date(Date.UTC(selectedSchedule.year, selectedSchedule.month - 1, dayOfMonth));
      const isWorkingDay = selectedSchedule.slots.some(slot =>
        slot.dayOfMonth === dayOfMonth && isCurrentEmployeeSlot(slot, currentEmployeeId),
      ) || Boolean(currentEmployeeId && selectedSchedule.relatedScheduleAssignments?.some(assignment =>
        assignment.dayOfMonth === dayOfMonth && assignment.employeeId === currentEmployeeId,
      ));

      return {
        dayOfMonth,
        weekdayLong: scheduleWeekdayFormatter.format(date),
        weekdayShort: scheduleWeekdayShortFormatter.format(date).replace(".", ""),
        isWorkingDay,
      };
    });
  }, [currentEmployeeId, selectedSchedule]);
  const defaultDailyScheduleDay = useMemo(() => {
    if (!selectedSchedule || dailyScheduleDays.length === 0) {
      return 1;
    }

    const now = new Date();
    if (now.getUTCFullYear() === selectedSchedule.year && now.getUTCMonth() + 1 === selectedSchedule.month) {
      return Math.min(now.getUTCDate(), dailyScheduleDays.length);
    }

    return dailyScheduleDays.find(day => day.isWorkingDay)?.dayOfMonth ?? 1;
  }, [dailyScheduleDays, selectedSchedule]);
  const activeDailyScheduleDay = selectedDayOfMonth && selectedDayOfMonth <= dailyScheduleDays.length
    ? selectedDayOfMonth
    : defaultDailyScheduleDay;
  const activeDailyScheduleDayInfo = dailyScheduleDays.find(day => day.dayOfMonth === activeDailyScheduleDay) ?? null;

  useLayoutEffect(() => {
    if (scheduleViewMode !== "daily") {
      return;
    }

    activeDailyScheduleDayRef.current?.scrollIntoView({
      behavior: "auto",
      block: "nearest",
      inline: "center",
    });
  }, [activeDailyScheduleDay, scheduleViewMode, selectedSchedule?.id]);

  const dailyScheduleWorkers = useMemo(
    () => selectedSchedule
      ? buildDailyScheduleWorkers(selectedSchedule, activeDailyScheduleDay, fallbackMatrixEmployeeId, displayName)
      : [],
    [activeDailyScheduleDay, displayName, fallbackMatrixEmployeeId, selectedSchedule],
  );
  const scheduleStats = useMemo(
    () => getScheduleStats(schedules, selectedSchedule, currentEmployeeId),
    [currentEmployeeId, schedules, selectedSchedule],
  );
  const scheduleHoursSummary = useMemo(
    () => buildScheduleHoursSummary(schedules, activeSummaryPeriod, currentEmployeeId),
    [activeSummaryPeriod, currentEmployeeId, schedules],
  );
  const [salaryHoursInput, setSalaryHoursInput] = useState(() => formatSalaryHours(scheduleHoursSummary.totalHours));
  const [salaryRateInput, setSalaryRateInput] = useState("");
  const [salaryResult, setSalaryResult] = useState<number | null>(null);
  const parsedSalaryHours = parseSalaryValue(salaryHoursInput);
  const parsedSalaryRate = parseSalaryValue(salaryRateInput);
  const canCalculateSalary = parsedSalaryHours !== null && parsedSalaryRate !== null;

  useEffect(() => {
    setSalaryHoursInput(formatSalaryHours(scheduleHoursSummary.totalHours));
    setSalaryResult(null);
  }, [scheduleHoursSummary.totalHours]);  const defaultScheduleMatrixColumns = useMemo(
    () => buildScheduleMatrixColumns(selectedSchedule, fallbackMatrixEmployeeId, displayName),
    [displayName, fallbackMatrixEmployeeId, selectedSchedule],
  );
  const scheduleMatrixColumns = useMemo(() => {
    if (!selectedSchedule) {
      return defaultScheduleMatrixColumns;
    }

    const savedOrder = savedColumnOrders[String(selectedSchedule.id)] ?? [];
    return applyEmployeeScheduleColumnOrder(defaultScheduleMatrixColumns, savedOrder);
  }, [defaultScheduleMatrixColumns, savedColumnOrders, selectedSchedule]);
  const scheduleMatrixCellMap = useMemo(
    () => buildScheduleMatrixCellMap(selectedSchedule, fallbackMatrixEmployeeId),
    [fallbackMatrixEmployeeId, selectedSchedule],
  );
  const scheduleMatrixDisplay = useMemo(
    () => buildScheduleMatrixDisplayCellMap(selectedSchedule, scheduleMatrixCellMap),
    [scheduleMatrixCellMap, selectedSchedule],
  );
  const queryErrorMessage = scheduleQuery.error
    ? getErrorMessage(scheduleQuery.error, t("Could not load published schedules."))
    : null;

  const handleSummaryYearChange = (value: string) => {
    const year = Number(value);
    const nextPeriod = summaryPeriods.find(period =>
      period.year === year && period.month === activeSummaryPeriod?.month,
    ) ?? summaryPeriods.find(period => period.year === year);

    if (nextPeriod) {
      setSelectedSummaryPeriodKey(nextPeriod.key);
    }
  };

  const handleSummaryMonthChange = (value: string) => {
    const month = Number(value);
    const nextPeriod = summaryPeriods.find(period =>
      period.year === activeSummaryPeriod?.year && period.month === month,
    );

    if (nextPeriod) {
      setSelectedSummaryPeriodKey(nextPeriod.key);
    }
  };

  const handleCalculateSalary = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (parsedSalaryHours === null || parsedSalaryRate === null) {
      return;
    }

    setSalaryResult(parsedSalaryHours * parsedSalaryRate);
  };

  const handleResetSalaryHours = () => {
    setSalaryHoursInput(formatSalaryHours(scheduleHoursSummary.totalHours));
    setSalaryResult(null);
  };
  const handleSaveColumnOrder = (columnOrder: number[]) => {
    if (!selectedSchedule) {
      return;
    }

    const scheduleKey = String(selectedSchedule.id);
    const sanitizedOrder = sanitizeEmployeeScheduleColumnOrder(columnOrder, defaultScheduleMatrixColumns);
    const defaultOrder = defaultScheduleMatrixColumns.map(column => column.employeeId);
    const nextSavedOrders = { ...savedColumnOrders };

    if (sanitizedOrder.every((columnId, index) => columnId === defaultOrder[index])) {
      delete nextSavedOrders[scheduleKey];
    } else {
      nextSavedOrders[scheduleKey] = sanitizedOrder;
    }

    setSavedColumnOrders(nextSavedOrders);
    writeEmployeeScheduleColumnOrders(columnOrderStorageKey, nextSavedOrders);
    setPreferenceSaveError(null);
    void employeeUiStateApi
      .saveScheduleColumnOrder(selectedSchedule.id, sanitizedOrder)
      .catch(error => setPreferenceSaveError(getErrorMessage(error, t("Could not sync the column order."))));
    setColumnOrderDialogEmployeeId(null);
  };

  const handleExportPdf = () => {
    if (!selectedSchedule) {
      return;
    }

    setPdfExportError(null);

    try {
      printSchedulePdf(selectedSchedule, scheduleMatrixColumns, scheduleMatrixDisplay.cellMap);
    } catch (error) {
      setPdfExportError(error instanceof Error ? error.message : t("Could not export this schedule to PDF."));
    }
  };

  const handleSubmitShiftCorrection = (input: CreateShiftCorrectionInput) => {
    setShiftCorrectionError(null);
    setShiftCorrectionSuccess(null);
    createShiftCorrectionMutation.mutate(input, {
      onSuccess: () => {
        setIsShiftCorrectionDialogOpen(false);
        setShiftCorrectionSuccess(t("Shift correction request sent to your manager."));
      },
      onError: error => setShiftCorrectionError(getErrorMessage(error, t("Could not send the shift correction request."))),
    });
  };

  return (
    <div className={workspaceStyles.page}>
      {queryErrorMessage ? <ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner> : null}
      {pdfExportError ? <ErrorBanner dismissible={false}>{pdfExportError}</ErrorBanner> : null}
      {preferenceSaveError ? <ErrorBanner dismissible>{preferenceSaveError}</ErrorBanner> : null}
      {shiftCorrectionSuccess ? <div className={styles.shiftCorrectionSuccess} role="status">{shiftCorrectionSuccess}</div> : null}

      <EmployeeScheduleHero
        schedules={schedules}
        employeeId={currentEmployeeId}
        displayName={displayName}
        isLoading={scheduleQuery.isLoading}
        hasError={Boolean(scheduleQuery.error)}
      />

      {scheduleQuery.isLoading ? (
        <section className={workspaceStyles.panel}>
          <span className={workspaceStyles.panelEyebrow}>{t("Loading")}</span>
          <p className={workspaceStyles.panelText}>{t("Checking public schedules for your account.")}</p>
        </section>
      ) : null}

      {!scheduleQuery.isLoading && schedules.length === 0 ? (
        <section className={workspaceStyles.panel}>
          <span className={workspaceStyles.panelEyebrow}>{t("No published schedules")}</span>
          <h2 className={workspaceStyles.panelTitle}>{t("Nothing is public for your account yet.")}</h2>
          <NavLink to="/availability" className={workspaceStyles.linkCard}>
            <span className={workspaceStyles.linkIcon}>
              <AvailabilityIcon size={18} />
            </span>
            <span className={workspaceStyles.linkTitle}>{t("Go to Availability")}</span>
            <span className={workspaceStyles.linkText}>{t("Submit availability when a manager opens a window.")}</span>
          </NavLink>
        </section>
      ) : null}

      {schedules.length > 0 ? (
        <section className={`${workspaceStyles.panel} ${styles.publicSchedulesPanel}`}>
          <div className={styles.publicSchedulesHeader}>
            <div className={styles.publicSchedulesHeading}>
              <span className={styles.summaryIcon} aria-hidden="true">
                <ScheduleDetailsIcon size={20} />
              </span>
              <div>
                <span className={workspaceStyles.panelEyebrow}>{t("Public schedules")}</span>
                <strong>{t("{0} schedules", schedules.length)}</strong>
              </div>
            </div>

            <div className={styles.publicSchedulesHeaderMeta}>
              <span className={styles.selectedSchedulePill}>{scheduleStats.period}</span>
            </div>
          </div>
          <div className={styles.scheduleSwitcher}>
            {schedules.map(schedule => {
              const isSelected = schedule.id === selectedSchedule?.id;
              const scheduleLastUpdateLabel = formatScheduleLastUpdate(schedule.lastUpdatedAtUtc);

              return (
                <button
                  key={schedule.id}
                  type="button"
                  className={[styles.scheduleButton, isSelected ? styles.scheduleButtonActive : ""].filter(Boolean).join(" ")}
                  aria-pressed={isSelected}
                  onClick={() => setSelectedScheduleId(schedule.id)}
                >
                  <span className={styles.scheduleCardDate} aria-hidden="true">
                    <span>{formatScheduleMonthOnly(schedule).slice(0, 3)}</span>
                    <strong>{schedule.year}</strong>
                  </span>

                  <span className={styles.scheduleButtonDetails}>
                    <span className={styles.scheduleButtonContent}>
                      <span className={styles.scheduleButtonName}>{schedule.name}</span>
                      <span className={styles.scheduleButtonMeta}>
                        {`${schedule.shopName || t("Shop {0}", schedule.shopId)} / ${schedule.containerName || t("Container {0}", schedule.containerId)}`}
                      </span>
                    </span>

                    <span className={styles.lastUpdateField} aria-label={t("Last Update: {0}", scheduleLastUpdateLabel)}>
                      <span>
                        <span className={styles.lastUpdateDot} aria-hidden="true" />
                        {t("Last Update")}</span>
                      <strong>
                        {schedule.lastUpdatedAtUtc ? (
                          <time dateTime={schedule.lastUpdatedAtUtc}>{scheduleLastUpdateLabel}</time>
                        ) : scheduleLastUpdateLabel}
                      </strong>
                    </span>
                  </span>
                </button>
              );
            })}
          </div>
        </section>
      ) : null}

      {selectedSchedule && scheduleViewMode === "matrix" ? (
        <ContainerGraphMatrix
          className={styles.openScheduleMatrix}
          graph={selectedSchedule}
          columns={scheduleMatrixColumns}
          cellMap={scheduleMatrixDisplay.cellMap}
          mutedSuffixMap={scheduleMatrixDisplay.mutedSuffixMap}
          title={
            <span className={styles.openScheduleTitleBlock}>
              <span className={styles.openScheduleTitleLabel}>{t("Schedules")}</span>
              <span className={styles.openScheduleMeta}>
                <span>{selectedSchedule.name}</span>
                <span>{formatScheduleMonthOnly(selectedSchedule)}</span>
                <span>{selectedSchedule.year}</span>
              </span>
            </span>
          }
          icon={
            <button
              type="button"
              className={styles.scheduleViewToggle}
              aria-label={t("Show daily schedule view")}
              aria-pressed="false"
              title={t("Show daily schedule view")}
              onClick={() => setScheduleViewMode("daily")}
            >
              <ScheduleIcon size={20} />
            </button>
          }
          readOnly
          compactSize
          compactHeader
          neutralStyle
          showColumnTotals={false}
          allowColumnResize={false}
          stretchColumns={false}
          emptyMessage={t("No assigned shifts in this schedule yet.")}
          onColumnHeaderClick={column => setColumnOrderDialogEmployeeId(column.employeeId)}
          headerRightSlot={
            <div className={styles.openScheduleActions}>
              <button type="button" className={styles.openSchedulePdfButton} onClick={handleExportPdf}
                title={t("Export schedule to PDF")} aria-label={t("Export schedule to PDF")}>PDF</button>
              <button type="button" className={`${styles.openSchedulePdfButton} ${styles.openScheduleCorrectionButton}`}
                onClick={() => { setShiftCorrectionError(null); setIsShiftCorrectionDialogOpen(true); }}
                title={t("Request a shift correction")} aria-label={t("Request a shift correction")}>{t("Adjust")}</button>
            </div>
          }
        />
      ) : null}

      {selectedSchedule && scheduleViewMode === "daily" ? (
        <CardSection
          className={styles.dailyScheduleCard}
          title={
            <span className={styles.openScheduleTitleBlock}>
              <span className={styles.openScheduleTitleLabel}>{t("Schedules")}</span>
              <span className={styles.openScheduleMeta}>
                <span>{selectedSchedule.name}</span>
                <span>{formatScheduleMonthOnly(selectedSchedule)}</span>
                <span>{selectedSchedule.year}</span>
              </span>
            </span>
          }
          icon={
            <button
              type="button"
              className={`${styles.scheduleViewToggle} ${styles.scheduleViewToggleActive}`}
              aria-label={t("Show schedule matrix view")}
              aria-pressed="true"
              title={t("Show schedule matrix view")}
              onClick={() => setScheduleViewMode("matrix")}
            >
              <ScheduleIcon size={20} />
            </button>
          }
          headerRightSlot={
            <div className={styles.openScheduleActions}>
              <button type="button" className={styles.openSchedulePdfButton} onClick={handleExportPdf}
                title={t("Export schedule to PDF")} aria-label={t("Export schedule to PDF")}>PDF</button>
              <button type="button" className={`${styles.openSchedulePdfButton} ${styles.openScheduleCorrectionButton}`}
                onClick={() => { setShiftCorrectionError(null); setIsShiftCorrectionDialogOpen(true); }}
                title={t("Request a shift correction")} aria-label={t("Request a shift correction")}>{t("Adjust")}</button>
            </div>
          }
        >
          <div className={styles.dailyScheduleShell}>
            <div className={styles.dailyScheduleDays} role="tablist" aria-label={t("Schedule days")}>
              {dailyScheduleDays.map(day => {
                const isSelected = day.dayOfMonth === activeDailyScheduleDay;
                return (
                  <button
                    key={day.dayOfMonth}
                    ref={isSelected ? activeDailyScheduleDayRef : undefined}
                    type="button"
                    role="tab"
                    aria-selected={isSelected}
                    aria-controls="daily-schedule-content"
                    aria-label={`${day.weekdayLong} ${day.dayOfMonth}, ${day.isWorkingDay ? t("working day") : t("day off")}`}
                    className={[
                      styles.dailyScheduleDay,
                      day.isWorkingDay ? styles.dailyScheduleDayWorking : styles.dailyScheduleDayOff,
                      isSelected ? styles.dailyScheduleDaySelected : "",
                    ].filter(Boolean).join(" ")}
                    onClick={() => setSelectedDayOfMonth(day.dayOfMonth)}
                  >
                    <span>{day.weekdayShort}</span>
                    <strong>{day.dayOfMonth}</strong>
                  </button>
                );
              })}
            </div>

            <div
              id="daily-schedule-content"
              className={styles.dailyScheduleBody}
              role="tabpanel"
              aria-label={`${activeDailyScheduleDayInfo?.weekdayLong ?? t("Day")} ${activeDailyScheduleDay}`}
            >
              <div className={styles.dailyScheduleDate} aria-hidden="true">
                <span>{activeDailyScheduleDayInfo?.weekdayLong ?? t("Day")}</span>
                <strong>{activeDailyScheduleDay}</strong>
              </div>

              <div className={styles.dailyScheduleWorkers}>
                {dailyScheduleWorkers.length > 0 ? dailyScheduleWorkers.map(worker => (
                  <article key={worker.employeeId} className={styles.dailyScheduleWorker}>
                    <span>{worker.label}</span>
                    <strong>{formatDailyWorkerShifts(worker)}</strong>
                  </article>
                )) : (
                  <div className={styles.dailyScheduleEmpty}>
                    <strong>{t("No one is scheduled.")}</strong>
                    <span>{t("This day has no assigned shifts.")}</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </CardSection>
      ) : null}

      <EmployeeScheduleColumnOrderDialog
        open={columnOrderDialogEmployeeId !== null}
        columns={scheduleMatrixColumns}
        defaultColumns={defaultScheduleMatrixColumns}
        activeEmployeeId={columnOrderDialogEmployeeId}
        onCancel={() => setColumnOrderDialogEmployeeId(null)}
        onSave={handleSaveColumnOrder}
      />

      <EmployeeShiftCorrectionDialog
        open={isShiftCorrectionDialogOpen}
        schedule={selectedSchedule}
        employeeId={currentEmployeeId}
        existingRequests={shiftCorrectionsQuery.data ?? []}
        isSending={createShiftCorrectionMutation.isPending}
        errorMessage={shiftCorrectionError}
        onCancel={() => { setIsShiftCorrectionDialogOpen(false); setShiftCorrectionError(null); }}
        onSubmit={handleSubmitShiftCorrection}
      />

      {selectedSchedule ? (
        <section className={`${workspaceStyles.panel} ${styles.hoursSummaryPanel}`}>
          <span className={styles.hoursSummaryTotalPill}>{scheduleHoursSummary.totalHoursText}</span>

          <div className={styles.hoursSummaryHeader}>
            <div className={styles.hoursSummaryHeading}>
              <span className={styles.summaryIcon} aria-hidden="true">
                <StatisticsIcon size={20} />
              </span>
              <div>
                <span className={workspaceStyles.panelEyebrow}>{t("Work hours")}</span>
                <h2 className={workspaceStyles.panelTitle}>{t("Summary")}</h2>
              </div>
            </div>

            <div className={styles.hoursSummaryActions}>
              <div className={styles.summaryPeriodControls} role="group" aria-label={t("Summary period")}>
                <div className={styles.summaryPeriodField}>
                  <span>{t("Month")}</span>
                  <SearchableSelect
                    ariaLabel={t("Summary month")}
                    value={activeSummaryPeriod ? String(activeSummaryPeriod.month) : ""}
                    options={summaryMonthOptions}
                    placeholder={t("Month")}
                    dropdownTitle={t("Month")}
                    size="summary"
                    shadow="soft"
                    searchEnabled={false}
                    showSelectedHint={false}
                    disabled={summaryMonths.length <= 1}
                    className={styles.summaryMonthSelect}
                    onChange={handleSummaryMonthChange}
                  />
                </div>

                <div className={styles.summaryPeriodField}>
                  <span>{t("Year")}</span>
                  <SearchableSelect
                    ariaLabel={t("Summary year")}
                    value={activeSummaryPeriod ? String(activeSummaryPeriod.year) : ""}
                    options={summaryYearOptions}
                    placeholder={t("Year")}
                    dropdownTitle={t("Year")}
                    size="summary"
                    shadow="soft"
                    searchEnabled={false}
                    showSelectedHint={false}
                    disabled={summaryYears.length <= 1}
                    className={styles.summaryYearSelect}
                    onChange={handleSummaryYearChange}
                  />
                </div>
              </div>
            </div>
          </div>

          {scheduleHoursSummary.rows.length > 0 ? (
            <div className={styles.hoursSummaryGrid} role="table" aria-label={t("Schedule hours summary")}>
              <div className={styles.hoursSummaryGridHeader} role="row">
                <span role="columnheader">{t("Day")}</span>
                <span role="columnheader">{t("Hours")}</span>
                <span role="columnheader">{t("Schedule")}</span>
              </div>

              {scheduleHoursSummary.rows.map(row => (
                <div key={row.key} className={styles.hoursSummaryGridRow} role="row">
                  <span role="cell">{row.dayLabel}</span>
                  <strong role="cell">{row.hoursText}</strong>
                  <span role="cell">{row.scheduleName}</span>
                </div>
              ))}

              <div className={styles.hoursSummaryTotalRow} role="row">
                <span role="cell">{t("Total")}</span>
                <strong role="cell">{scheduleHoursSummary.totalHoursText}</strong>
                <span role="cell">{t("All schedules")}</span>
              </div>
            </div>
          ) : (
            <p className={styles.hoursSummaryEmpty}>{t("No assigned shifts in this period.")}</p>
          )}
          <section className={styles.salaryCalculator} aria-labelledby="salary-calculator-title">
            <div className={styles.salaryCalculatorHeader}>
              <span className={styles.salaryCalculatorMark} aria-hidden="true">=</span>
              <div>
                <span>{t("Quick estimate")}</span>
                <h3 id="salary-calculator-title">{t("Salary calculator")}</h3>
              </div>
            </div>

            <form className={styles.salaryCalculatorForm} onSubmit={handleCalculateSalary}>
              <label className={styles.salaryCalculatorField}>
                <span>{t("Hours")}</span>
                <span className={styles.salaryCalculatorInputShell}>
                  <input
                    type="text"
                    aria-label={t("Hours")}
                    pattern="[0-9]*([.,][0-9]*)?"
                    inputMode="decimal"
                    value={salaryHoursInput}
                    onChange={event => {
                      setSalaryHoursInput(event.target.value);
                      setSalaryResult(null);
                    }}
                  />
                  <small>{t("h")}</small>
                </span>
              </label>

              <label className={styles.salaryCalculatorField}>
                <span>{t("Hourly rate")}</span>
                <span className={styles.salaryCalculatorInputShell}>
                  <input
                    type="text"
                    aria-label={t("Hourly rate")}
                    pattern="[0-9]*([.,][0-9]*)?"
                    inputMode="decimal"
                    placeholder="0.00"
                    value={salaryRateInput}
                    onChange={event => {
                      setSalaryRateInput(event.target.value);
                      setSalaryResult(null);
                    }}
                  />
                  <small>{t("/ h")}</small>
                </span>
              </label>

              <button
                type="submit"
                className={styles.salaryCalculatorEquals}
                aria-label={t("Calculate salary")}
                disabled={!canCalculateSalary}
              >
                =
              </button>

              <output className={styles.salaryCalculatorResult} aria-label={t("Estimated pay")} aria-live="polite">
                <span>{t("Estimated pay")}</span>
                <strong>{salaryResult === null ? "—" : salaryAmountFormatter.format(salaryResult)}</strong>
              </output>

              <button type="button" className={styles.salaryCalculatorReset} onClick={handleResetSalaryHours}>
                {t("Reset")}</button>
            </form>
          </section>
        </section>
      ) : null}
    </div>
  );
}
