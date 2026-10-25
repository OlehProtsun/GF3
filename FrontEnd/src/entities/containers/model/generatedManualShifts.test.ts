import { describe, expect, test } from "vitest";
import { deriveGeneratedVacancies, reconcileGeneratedManualColumns, type GeneratedVacancy } from "./generatedManualShifts";
import type { GraphSlot } from "./types";
import type { EditableGraphManualColumn } from "../ui/ContainerGraphEditor";

const slot = (dayOfMonth: number, employeeId: number | null, fromTime = "08:00", toTime = "12:00"): GraphSlot => ({
  id: dayOfMonth, scheduleId: 1, slotNo: 1, dayOfMonth, employeeId, fromTime, toTime, status: employeeId ? 1 : 0,
});
function derive(overrides: Partial<Parameters<typeof deriveGeneratedVacancies>[0]> = {}) {
  return deriveGeneratedVacancies({ year: 2026, month: 2, peoplePerShift: 1, shift1Time: "08:00-12:00", shift2Time: "",
    slots: Array.from({ length: 28 }, (_, index) => slot(index + 1, 1)), ...overrides });
}
const vacancy = (dayOfMonth = 1, fromTime = "08:00", toTime = "12:00"): GeneratedVacancy => ({
  dayOfMonth, fromTime, toTime, shiftIndex: 1, position: 1,
});
function reconcile(vacancies: GeneratedVacancy[], columns: EditableGraphManualColumn[] = [], protectedCells = new Set<string>(), columnOrder = [10, ...columns.map(c => -c.columnId)]) {
  return reconcileGeneratedManualColumns({ vacancies, columns, columnOrder, retainedGeneratedColumnIds: new Set(), reservations: columns.flatMap(c => Object.entries(c.cells).flatMap(([day, value]) => protectedCells.has(`${c.columnId}:${day}`) ? [{ manualColumnId: c.columnId, dayOfMonth: Number(day), fromTime: value.slice(0, 5), toTime: value.slice(-5) }] : [])) });
}

describe("generated vacancies", () => {
  test("only missing first shift and sufficient overlap creates no vacancy", () => {
    expect(derive()).toEqual([]);
    expect(derive({ slots: [...Array.from({ length: 28 }, (_, i) => slot(i + 1, 1)), slot(1, 1)] })).toEqual([]);
    expect(derive({ slots: Array.from({ length: 28 }, (_, i) => slot(i + 1, i ? 1 : null)) })).toEqual([vacancy()]);
  });
  test("second shift and both shifts retain independent seats", () => {
    const slots = Array.from({ length: 28 }, (_, i) => [slot(i + 1, 1), slot(i + 1, i ? 1 : null, "12:00", "16:00")]).flat();
    expect(derive({ slots, shift2Time: "12:00-16:00" })).toEqual([{ ...vacancy(1, "12:00", "16:00"), shiftIndex: 2 }]);
    slots[0].employeeId = null;
    slots[0].status = 0;
    expect(derive({ slots, shift2Time: "12:00-16:00" })).toHaveLength(2);
    const missing = derive({ slots: [slot(1, null), slot(1, null)], peoplePerShift: 2 });
    expect(missing.filter(v => v.dayOfMonth === 1).map(v => v.position)).toEqual([1, 2]);
  });
  test("partial gaps, continuous replacements, duplicate employees and invalid records", () => {
    const rest = Array.from({ length: 27 }, (_, i) => slot(i + 2, 1));
    expect(derive({ slots: [...rest, slot(1, 1, "08:00", "10:00"), slot(1, 2, "10:00", "12:00")] })).toEqual([]);
    expect(derive({ slots: [...rest, slot(1, 1, "08:00", "09:00"), slot(1, 2, "10:00", "12:00")] })).toEqual([vacancy()]);
    expect(derive({ slots: [...rest, slot(1, 1), slot(1, 1)], peoplePerShift: 2 }).filter(v => v.dayOfMonth === 1)).toEqual([vacancy()]);
    expect(derive({ slots: [...rest, slot(1, 1, "bad", "12:00"), slot(45, null)] })).toEqual([vacancy()]);
  });
  test.each([[2026, 2, 28], [2024, 2, 29], [2026, 4, 30], [2026, 1, 31]])("real calendar boundaries %i/%i", (year, month, days) => {
    const result = derive({ year, month, slots: [slot(1, null)] });
    expect(result).toHaveLength(days);
    expect(result.at(-1)?.dayOfMonth).toBe(days);
  });
  test("invalid or empty input", () => {
    expect(derive({ slots: [] })).toEqual([]);
    expect(derive({ month: 13 })).toEqual([]);
    expect(derive({ year: 1999 })).toEqual([]);
    expect(derive({ year: 4001 })).toEqual([]);
    expect(derive({ peoplePerShift: 0 })).toEqual([]);
    expect(derive({ shift1Time: "bad" })).toEqual([]);
    expect(derive({ slots: [slot(32, null)] })).toEqual([]);
  });
  test("serialized unfilled statuses normalize template times and cap duplicate demand", () => {
    const unfilled = { ...slot(1, null, "8:00", "12:00"), status: "UNFURNISHED" } as unknown as GraphSlot;
    const slots = [...Array.from({ length: 28 }, (_, i) => slot(i + 1, 1)), unfilled, unfilled];
    expect(derive({ slots })).toEqual([vacancy()]);
  });
});

describe("generated column reconciliation", () => {
  test("custom text and same-name manual columns remain untouched; repeated generation is stable", () => {
    const custom = { columnId: 5, label: "Do obsadzenia", cells: { "1": "free text" } };
    const first = reconcile([vacancy(), vacancy()], [custom]);
    expect(first.columns[0]).toBe(custom);
    expect(first.columns.map(c => c.label)).toEqual(["Do obsadzenia", "do obstawienia"]);
    expect(first.columns.map(c => c.columnId)).toEqual([5, 6]);
    expect(reconcile([vacancy(), vacancy()], first.columns, new Set(), first.columnOrder)).toEqual(first);
    expect(custom.cells).toEqual({ "1": "free text" });
  });
  test("changed deficits clear stale cells and remove unused columns/order", () => {
    const first = reconcile([vacancy(), vacancy()], [1, 2].map(columnId => ({ columnId, source: "generated-unfilled", label: "Old", cells: {} })));
    const next = reconcile([vacancy(2)], first.columns);
    expect(next.columns).toHaveLength(1);
    expect(next.columns[0].columnId).toBe(first.columns[0].columnId);
    expect(next.columns[0].cells).toEqual({ "2": "08:00 - 12:00" });
    expect(first.removedColumnIds).toEqual([2]);
    expect(next.removedColumnIds).toEqual([]);
    expect(next.columnOrder).toEqual([-first.columns[0].columnId, 10]);
  });
  test("protected offers reserve a single demand, keep edited values and allow another shortage", () => {
    const first = reconcile([vacancy()]);
    const id = first.columns[0].columnId;
    const protectedCells = new Set([`${id}:1`]);
    expect(reconcile([vacancy()], first.columns, protectedCells).columns).toEqual(first.columns);
    expect(reconcile([vacancy(), vacancy()], first.columns, protectedCells).columns[0].cells["1"]).toBe("08:00 - 12:00, 08:00 - 12:00");
    expect(reconcile([], first.columns, protectedCells).columns).toEqual(first.columns);
    expect(reconcile([], first.columns).columns).toEqual([]);
    const edited = [{ ...first.columns[0], cells: { "1": "10:00 - 14:00" } }];
    const changed = reconcile([vacancy()], edited, protectedCells);
    expect(changed.columns).toHaveLength(1);
    expect(changed.columns[0].cells).toEqual({ "1": "08:00 - 10:00, 10:00 - 14:00" });
  });
  test("uses existing displayed column order without mutating input", () => {
    const columns: EditableGraphManualColumn[] = [1, 2].map(columnId => ({ columnId, source: "generated-unfilled", label: "Renamed", cells: {} }));
    const result = reconcile([vacancy()], columns, new Set(), [10, -2, -1]);
    expect(result.columns.map(c => c.columnId)).toEqual([2]);
    expect(columns[1].cells).toEqual({});
    expect(result.columnOrder).toEqual([-2, 10]);
  });
});

test("legacy generated IDs with open offers survive; unreferenced IDs merge safely", () => {
  const columns: EditableGraphManualColumn[] = [1, 2, 3].map(columnId => ({ columnId, source:"generated-unfilled",label:"Old",cells:{"1":"08:00 - 12:00"} }));
  const input = { vacancies:[vacancy(),vacancy(),vacancy()],columns,columnOrder:[-1,-2,-3], retainedGeneratedColumnIds: new Set<number>(),
    reservations:[{ manualColumnId:2,dayOfMonth:1,fromTime:"08:00",toTime:"12:00" }] };
  const result=reconcileGeneratedManualColumns(input);
  expect(result.columns.map(c=>c.columnId)).toEqual([1,2]); expect(result.removedColumnIds).toEqual([3]);
  expect(result.columns[0].cells["1"]).toBe("08:00 - 12:00, 08:00 - 12:00");
  expect(result.columns[1].cells).toEqual(columns[1].cells);
  const closed=reconcileGeneratedManualColumns({...input,columns:result.columns,reservations:[]});
  expect(closed.columns).toHaveLength(1); expect(closed.columns[0].cells["1"]).toBe("08:00 - 12:00, 08:00 - 12:00, 08:00 - 12:00");
});

test("accepted style retains empty generated columns without reviving demand", () => {
  const columns: EditableGraphManualColumn[] = [1, 2].map(columnId => ({ columnId, source: "generated-unfilled", label: "Auto", cells: {} }));
  const input = { columns, vacancies: [], columnOrder: [10, -1, -2], reservations: [], retainedGeneratedColumnIds: new Set([2]) };
  const result = reconcileGeneratedManualColumns(input);
  expect(result.columns).toEqual([{ ...columns[1], label: "do obstawienia" }]);
  expect(result.columnOrder).toEqual([-2, 10]);
  expect(result.removedColumnIds).toEqual([1]);
  expect(reconcileGeneratedManualColumns({ ...input, columns: result.columns, columnOrder: result.columnOrder }).columns).toEqual(result.columns);
});
