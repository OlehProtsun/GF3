import { manualShiftCapacity, intervalsFromCapacity, formatManualShiftIntervals, type ManualShiftReservation } from "./manualShiftIntervals";
import { pinGeneratedColumnsFirst } from "./graphColumnOrder";
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
  vacancies: GeneratedVacancy[]; columns: EditableGraphManualColumn[]; columnOrder: number[]; reservations: ManualShiftReservation[];
  retainedGeneratedColumnIds: ReadonlySet<number>;
}): { columns: EditableGraphManualColumn[]; columnOrder: number[]; removedColumnIds: number[] } {
  const { columns, columnOrder, reservations } = input;
  const rank = (id: number) => { const index = columnOrder.indexOf(-id); return index < 0 ? columnOrder.length + columns.findIndex(c => c.columnId === id) : index; };
  const generated = columns.filter(c => c.source === "generated-unfilled").sort((a, b) => rank(a.columnId) - rank(b.columnId));
  const canonicalId = generated[0]?.columnId ?? Math.max(0, ...columns.map(c => c.columnId)) + 1;
  const protectedIds = new Set([...reservations.map(r => r.manualColumnId), ...input.retainedGeneratedColumnIds]);
  const keep = generated.filter(c => c.columnId === canonicalId || protectedIds.has(c.columnId))
    .map(c => ({ ...c, label: c.columnId === canonicalId ? "do obstawienia" : c.label, cells: {} as Record<string, string> }));
  if (!keep.length && input.vacancies.length) keep.push({ columnId: canonicalId, label: "do obstawienia", source: "generated-unfilled", cells: {} });
  const canonical = keep.find(c => c.columnId === canonicalId);
  const days = new Set([...input.vacancies.map(v => v.dayOfMonth), ...reservations.map(r => r.dayOfMonth)]);
  for (const day of days) {
    const demand = manualShiftCapacity(input.vacancies.filter(v => v.dayOfMonth === day));
    const protectedCapacity = Array<number>(1440).fill(0);
    for (const column of keep) {
      const reserved = reservations.filter(r => r.manualColumnId === column.columnId && r.dayOfMonth === day);
      if (reserved.length) column.cells[String(day)] = formatManualShiftIntervals(reserved);
      const capacity = manualShiftCapacity(reserved);
      capacity.forEach((value, m) => { protectedCapacity[m] += value; });
    }
    if (canonical) {
      const own = manualShiftCapacity(reservations.filter(r => r.manualColumnId === canonicalId && r.dayOfMonth === day));
      const free = demand.map((value, m) => Math.max(0, value - protectedCapacity[m]));
      // Preserve original vacancy units when unreserved, including touching intervals and duplicates.
      const remaining = [...free];
      const units: GeneratedVacancy[] = [];
      for (const vacancy of input.vacancies.filter(v => v.dayOfMonth === day)) {
        const coverage = manualShiftCapacity([vacancy]);
        if (coverage.every((value, m) => !value || remaining[m] > 0)) {
          units.push(vacancy); coverage.forEach((value, m) => { remaining[m] -= value; });
        }
      }
      const ownIntervals = reservations.filter(r => r.manualColumnId === canonicalId && r.dayOfMonth === day);
      const all = [...ownIntervals, ...units, ...intervalsFromCapacity(remaining)];
      if (all.length || own.some(Boolean)) canonical.cells[String(day)] = formatManualShiftIntervals(all);
    }
  }
  const surviving = new Map(keep.filter(c => Object.keys(c.cells).length || protectedIds.has(c.columnId)).map(c => [c.columnId, c]));
  const survivingCanonical = keep.find(c => surviving.has(c.columnId));
  if (survivingCanonical) survivingCanonical.label = "do obstawienia";
  const removedColumnIds = generated.filter(c => !surviving.has(c.columnId)).map(c => c.columnId);
  const removed = new Set(removedColumnIds.map(id => -id));
  const added = [...surviving.values()].filter(c => !columns.some(old => old.columnId === c.columnId));
  return { columns: [...columns.flatMap(c => c.source !== "generated-unfilled" ? [c] : surviving.has(c.columnId) ? [surviving.get(c.columnId)!] : []), ...added],
    columnOrder: pinGeneratedColumnsFirst([...columnOrder.filter(id => !removed.has(id)), ...added.map(c => -c.columnId)],
      [...surviving.keys()].map(id => -id)), removedColumnIds };
}
