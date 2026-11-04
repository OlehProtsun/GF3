import type { Employee } from "@entities/employees/model/types";
import {
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_NONE_MARK,
  getAvailabilityCellKey,
  getDaysInMonth,
  isAvailabilityWeekend,
  type AvailabilityMatrixCellMap,
} from "./matrix";

export function getAutomaticManagerEmployeeIds(
  employeeIds: readonly number[],
  employees: readonly Employee[],
  automaticWeekdayAvailability: boolean,
): ReadonlySet<number> {
  const selectedIds = new Set(employeeIds);
  return new Set(automaticWeekdayAvailability
    ? employees.filter(employee => selectedIds.has(employee.id) && employee.contractType === "o_prace")
      .map(employee => employee.id)
    : []);
}

export function getAutomaticManagerAvailabilityCellKeys(
  automaticEmployeeIds: ReadonlySet<number>,
  year: number,
  month: number,
): ReadonlySet<string> {
  const keys = new Set<string>();
  const days = getDaysInMonth(year, month);
  automaticEmployeeIds.forEach(employeeId => {
    for (let day = 1; day <= days; day += 1) {
      keys.add(getAvailabilityCellKey(employeeId, day));
    }
  });
  return keys;
}

export function buildEffectiveManagerAvailabilityCellMap(
  rawCellMap: AvailabilityMatrixCellMap,
  automaticEmployeeIds: ReadonlySet<number>,
  year: number,
  month: number,
): AvailabilityMatrixCellMap {
  const effectiveCellMap = { ...rawCellMap };
  getAutomaticManagerAvailabilityCellKeys(automaticEmployeeIds, year, month).forEach(key => {
    const day = Number(key.split(":")[1]);
    effectiveCellMap[key] = isAvailabilityWeekend(year, month, day) ? AVAILABILITY_NONE_MARK : AVAILABILITY_ANY_MARK;
  });
  return effectiveCellMap;
}
