import { expect, test } from "vitest";
import { buildGraphNoteContent, parseGraphNoteContent } from "./graphNote";
import {
  buildStyleOnlyUpdatedNote, classifyGraphDraftChange, diffGraphColors, fingerprintPersistedGraph,
  hasPersistedWorkspaceChanges, matchGraphColors, mergeGraphColorStyles, type PersistedGraphState,
} from "./graphSaveIntegrity";
import type { GraphCellStyle } from "./types";

const style = (backgroundColorArgb: number | null, textColorArgb: number | null, employeeId = 2, dayOfMonth = 3): GraphCellStyle =>
  ({ id: 1, scheduleId: 4, employeeId, dayOfMonth, backgroundColorArgb, textColorArgb });

const state: PersistedGraphState = {
  graph: {
    id: 4, containerId: 1, shopId: 1, name: "F04", year: 2026, month: 11, publicationStatus: "private",
    peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 6, maxConsecutiveFull: 2, maxFullPerMonth: 5, note: "Memo",
  },
  employees: [2, 3].map((employeeId, displayOrder) => ({ id: employeeId, scheduleId: 4, employeeId, displayOrder, isPriority: false })),
  slots: [1, 2].map(dayOfMonth => ({ id: dayOfMonth, scheduleId: 4, dayOfMonth, slotNo: 1, employeeId: 2, status: 1, fromTime: "08:00", toTime: "12:00" })),
  styles: [style(-1, null), style(null, -2, 3)],
};

test("fingerprints persisted values independently of timestamps, API ordering and row IDs", () => {
  expect(fingerprintPersistedGraph({
    graph: { ...state.graph, shift1Time: "8:00-12:00", lastUpdatedAtUtc: "2026-11-08T12:00:00Z" },
    employees: state.employees.toReversed().map(row => ({ ...row, id: row.id + 10 })),
    slots: state.slots.toReversed().map(row => ({ ...row, id: row.id + 10, status: "Working", fromTime: "08:00:00" })),
    styles: [...state.styles.toReversed(), style(-3, null, -1)],
  })).toBe(fingerprintPersistedGraph(state));
  for (const change of [
    { graph: { ...state.graph, publicationStatus: "public" as const } },
    { graph: { ...state.graph, note: "Memo " } },
    { employees: state.employees.map(row => ({ ...row, minHoursMonth: 10 })) },
    { slots: state.slots.map(row => ({ ...row, employeeId: 3 })) },
    { styles: [style(-9, null)] },
  ]) {
    expect(fingerprintPersistedGraph({ ...state, ...change })).not.toBe(fingerprintPersistedGraph(state));
  }
});

test("classifies only content changes and treats a one-field clear as a color change", () => {
  const baseline = { nonStyleFingerprint: "same", styles: [style(-1, -2)] };
  expect(classifyGraphDraftChange(baseline, { ...baseline, styles: [{ ...style(-1, -2), id: 9 }] })).toBe("unchanged");
  expect(classifyGraphDraftChange(baseline, { ...baseline, styles: [style(null, -2)] })).toBe("style-only");
  expect(classifyGraphDraftChange(baseline, { ...baseline, nonStyleFingerprint: "other" })).toBe("workspace");
});

test("recognizes only submitted values when checking a partially saved workspace", () => {
  const intended = {
    ...state,
    graph: { ...state.graph, name: "New name", note: "New note" },
    employees: state.employees.map(employee => ({ ...employee, isPriority: true })),
  };
  expect(hasPersistedWorkspaceChanges(state, intended, {
    ...state, slots: state.slots.map(slot => ({ ...slot, fromTime: "07:00" })),
  })).toBe(false);
  expect(hasPersistedWorkspaceChanges(state, intended, {
    ...state, graph: { ...state.graph, note: intended.graph.note },
  })).toBe(true);
  expect(hasPersistedWorkspaceChanges(state, intended, {
    ...state, employees: [intended.employees[0], state.employees[1]],
  })).toBe(true);
});

test("merges disjoint colors and cells, preserves row IDs and verifies just the intended fields", () => {
  const baseline = [style(-1, -2)];
  const local = [style(null, -2)];
  const remote = [{ ...style(-1, -3), id: 20 }, style(-4, null, 3)];
  const { merged, conflicts } = mergeGraphColorStyles(baseline, local, remote);
  expect(conflicts).toEqual([]);
  expect(merged).toEqual([{ ...style(null, -3), id: 20 }, style(-4, null, 3)]);
  const changes = diffGraphColors(baseline, local);
  expect(matchGraphColors(changes, remote)).toBe(0);
  expect(matchGraphColors(changes, merged)).toBe(1);
});

test("accepts equal concurrent updates and deletes, and detects a same-field collision", () => {
  const baseline = [style(-1, null)];
  expect(mergeGraphColorStyles(baseline, [], [])).toEqual({ merged: [], conflicts: [] });
  expect(mergeGraphColorStyles(baseline, [], baseline)).toEqual({ merged: [], conflicts: [] });
  const local = [style(-2, null)];
  expect(mergeGraphColorStyles(baseline, local, local)).toEqual({ merged: local, conflicts: [] });
  expect(mergeGraphColorStyles(baseline, local, [style(-3, null)]).conflicts).toEqual([
    { employeeId: 2, dayOfMonth: 3, field: "backgroundColorArgb" },
  ]);
});

test("replaces only synthetic note styles and leaves equivalent notes untouched", () => {
  const note = buildGraphNoteContent("Remote memo", [{ id: 1, label: "Column", cells: { "3": "08:00 - 12:00" } }],
    [-1, 2], [], [{ employeeId: 2, dayOfMonth: 3, value: "Training" }], { 5: ["2:3"] });
  expect(buildStyleOnlyUpdatedNote(note, [style(-9, -8)])).toBe(note);
  const updated = buildStyleOnlyUpdatedNote(note, [style(-7, null, -1), style(null, -8, 0), style(-9, -8)]);
  expect(parseGraphNoteContent(updated)).toEqual({
    ...parseGraphNoteContent(note),
    cellStyles: [expect.objectContaining({ employeeId: -1, backgroundColorArgb: -7 }),
      expect.objectContaining({ employeeId: 0, textColorArgb: -8 })],
  });
  expect(buildStyleOnlyUpdatedNote(updated, [])).toBe(note);
});
