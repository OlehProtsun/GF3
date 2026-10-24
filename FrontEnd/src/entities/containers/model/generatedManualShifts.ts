import type { EditableGraphManualColumn } from "../ui/ContainerGraphEditor";
import type { GraphSlot } from "./types";
import { clampGraphYear, getGraphDaysInMonth, getGraphShiftMinimumStaffing, tryParseGraphShiftRange } from "./graphWorkspace";

export type GeneratedVacancy = {
  dayOfMonth: number;
  shiftIndex: 1 | 2;
  fromTime: string;
  toTime: string;
  position: number;
};

function formatTime(minutes: number) {
  const withinDay = minutes % 1440;
  return `${String(Math.floor(withinDay / 60)).padStart(2, "0")}:${String(withinDay % 60).padStart(2, "0")}`;
}

export function deriveGeneratedVacancies(input: {
  slots: GraphSlot[]; year: number; month: number; shift1Time: string; shift2Time: string; peoplePerShift: number;
}): GeneratedVacancy[] {
  const { year, month, peoplePerShift } = input;
  if (!Number.isInteger(year) || clampGraphYear(year) !== year || !Number.isInteger(month) || month < 1 || month > 12 ||
    !Number.isInteger(peoplePerShift) || peoplePerShift <= 0 || input.slots.length === 0) return [];
  const days = getGraphDaysInMonth(year, month);
  const slots = input.slots.filter(slot => Number.isInteger(slot.dayOfMonth) && slot.dayOfMonth >= 1 && slot.dayOfMonth <= days);
  if (!slots.length) return [];
  const result: GeneratedVacancy[] = [];
  for (let dayOfMonth = 1; dayOfMonth <= days; dayOfMonth++) {
    [input.shift1Time, input.shift2Time].forEach((template, index) => {
      const shift = tryParseGraphShiftRange(template);
      if (!shift) return;
      const fromTime = formatTime(shift.fromMinutes);
      const toTime = formatTime(shift.toMinutes);
      const unassigned = slots.filter(slot => {
        if (slot.dayOfMonth !== dayOfMonth || !(slot.employeeId == null || slot.employeeId <= 0) ||
          (slot.status !== 0 && String(slot.status).toUpperCase() !== "UNFURNISHED")) return false;
        const range = tryParseGraphShiftRange(`${slot.fromTime}-${slot.toTime}`);
        return range?.fromMinutes === shift.fromMinutes && range.toMinutes === shift.toMinutes;
      }).length;
      const minimum = getGraphShiftMinimumStaffing(slots, dayOfMonth, fromTime, toTime);
      const missing = Math.min(peoplePerShift, Math.max(unassigned, peoplePerShift - minimum, 0));
      for (let position = 1; position <= missing; position++) {
        result.push({ dayOfMonth, shiftIndex: index === 0 ? 1 : 2, fromTime, toTime, position });
      }
    });
  }
  return result;
}

export function reconcileGeneratedManualColumns(input: {
  vacancies: GeneratedVacancy[]; columns: EditableGraphManualColumn[]; columnOrder: number[]; protectedCells: ReadonlySet<string>;
}): { columns: EditableGraphManualColumn[]; columnOrder: number[]; removedColumnIds: number[] } {
  const { columns, columnOrder, protectedCells } = input;
  const generated = columns.filter(column => column.source === "generated-unfilled")
    .sort((a, b) => {
      const rank = (id: number) => {
        const index = columnOrder.indexOf(-id);
        return index < 0 ? columnOrder.length + columns.findIndex(column => column.columnId === id) : index;
      };
      return rank(a.columnId) - rank(b.columnId);
    }).map(column => ({ ...column, cells: Object.fromEntries(Object.entries(column.cells)
      .filter(([day]) => protectedCells.has(`${column.columnId}:${day}`))) }));
  const remaining = [...input.vacancies].sort((a, b) => a.dayOfMonth - b.dayOfMonth || a.shiftIndex - b.shiftIndex ||
    a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime) || a.position - b.position);
  for (const column of generated) {
    for (const [day, value] of Object.entries(column.cells)) {
      const range = tryParseGraphShiftRange(value);
      const index = remaining.findIndex(vacancy => vacancy.dayOfMonth === Number(day) && range &&
        vacancy.fromTime === formatTime(range.fromMinutes) && vacancy.toTime === formatTime(range.toMinutes));
      if (index >= 0) remaining.splice(index, 1);
    }
  }
  let nextId = Math.max(0, ...columns.map(column => column.columnId)) + 1;
  const added: EditableGraphManualColumn[] = [];
  for (const vacancy of remaining) {
    let column = generated.find(candidate => !(String(vacancy.dayOfMonth) in candidate.cells));
    if (!column) {
      const ordinal = generated.length + 1;
      column = { columnId: nextId++, label: ordinal === 1 ? "Do obsadzenia" : `Do obsadzenia ${ordinal}`,
        source: "generated-unfilled", cells: {} };
      generated.push(column);
      added.push(column);
    }
    column.cells[String(vacancy.dayOfMonth)] = `${vacancy.fromTime} - ${vacancy.toTime}`;
  }
  const surviving = new Map(generated.filter(column => Object.keys(column.cells).length > 0).map(column => [column.columnId, column]));
  const removedColumnIds = columns.filter(column => column.source === "generated-unfilled" && !surviving.has(column.columnId))
    .map(column => column.columnId);
  const removed = new Set(removedColumnIds.map(id => -id));
  return {
    columns: [...columns.flatMap(column => column.source !== "generated-unfilled" ? [column] :
      surviving.has(column.columnId) ? [surviving.get(column.columnId)!] : []), ...added],
    columnOrder: [...columnOrder.filter(id => !removed.has(id)), ...added.map(column => -column.columnId)],
    removedColumnIds,
  };
}
