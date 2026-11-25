import type { EmployeeSchedule, EmployeeScheduleEmployee, EmployeeScheduleSlot } from "@entities/employee-schedule";
import {
  formatGraphIntervals,
  getGraphCellKey,
  mergeGraphIntervalsForDisplay,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
} from "@entities/containers/model/graphWorkspace";
import { parseGraphNoteContent } from "@entities/containers/model/graphNote";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { t } from "@shared/i18n";

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

export function getScheduleEmployeeLabel(employee: EmployeeScheduleEmployee) {
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

export function buildScheduleMatrixColumns(
  schedule: EmployeeSchedule | null,
  fallbackEmployeeId: number | null,
  fallbackLabel: string,
): GraphMatrixColumn[] {
  const employees = schedule?.employees ?? [];
  const parsedGraphNote = parseGraphNoteContent(schedule?.note);
  const manualColumns = parsedGraphNote.manualColumns.map(buildManualScheduleMatrixColumn);

  if (employees.length === 0 && manualColumns.length === 0) {
    return fallbackEmployeeId === null ? [] : [buildScheduleMatrixColumn(fallbackEmployeeId, fallbackLabel)];
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

export function buildScheduleMatrixCellMap(schedule: EmployeeSchedule | null, fallbackEmployeeId: number): GraphMatrixCellMap {
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

export function buildScheduleMatrixDisplayCellMap(
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
