import { expect, test } from "vitest";
import { readFileSync, writeFileSync } from "node:fs";
import { buildGraphCellMap } from "./graphWorkspace";
import { buildGraphNoteContent, parseGraphNoteContent } from "./graphNote";
import {
  buildStyleOnlyUpdatedNote, classifyGraphDraftChange, diffGraphColors, fingerprintPersistedGraph,
  buildGraphWorkspaceSaveSlots, graphSaveMismatch, hasPersistedWorkspaceChanges, matchGraphColors, mergeGraphColorStyles, type PersistedGraphState,
} from "./graphSaveIntegrity";
import type { GraphCellStyle, GraphSlot } from "./types";

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

test("replacement numbering is technical; slot multiplicity and status remain semantic", () => {
  const intended = { ...state, slots: [
    ...[1, 2, 4].map((slotNo, index) => ({ ...state.slots[0], dayOfMonth: 10, slotNo, employeeId: index + 1 })),
    ...[1, 3].map((slotNo, index) => ({ ...state.slots[0], dayOfMonth: 29, slotNo, employeeId: index + 1 })),
    ...[5, 8].map(slotNo => ({ ...state.slots[0], slotNo, employeeId: null, status: 0 })),
  ] };
  const actual = { ...intended, slots: intended.slots.map((slot, index) => ({ ...slot,
    slotNo: [1, 2, 3, 1, 2, 1, 2][index],
  })).toReversed() };
  expect(fingerprintPersistedGraph(actual)).toBe(fingerprintPersistedGraph(intended));
  expect(hasPersistedWorkspaceChanges(state, intended, actual)).toBe(true);
  for (const slots of [
    actual.slots.slice(1), [...actual.slots, actual.slots[0]],
    ...[{ employeeId: 99 }, { dayOfMonth: 7 }, { fromTime: "07:00" }, { toTime: "11:00" }, { status: 2 }].map(change =>
      actual.slots.map((slot, index) => index ? slot : { ...slot, ...change })),
  ]) expect(fingerprintPersistedGraph({ ...actual, slots })).not.toBe(fingerprintPersistedGraph(intended));
});

test("diagnostics identify semantic fields without exposing graph text", () => {
  const actual = { ...state, graph: { ...state.graph, note: "Private text" } };
  expect(graphSaveMismatch(state, actual)).toEqual({ field: "graph.note" });
  expect(JSON.stringify(graphSaveMismatch(state, actual))).not.toContain("Private text");
  expect(graphSaveMismatch(state, { ...state, slots: [] })).toEqual({ field: "slots.count", intended: 2, persisted: 0 });
  expect(graphSaveMismatch(state, { ...state, slots: state.slots.map(slot => ({ ...slot, slotNo: 90 })) })).toBeNull();
});

// Private input stays outside the repository; CI uses the synthetic regression above.
test.skipIf(!process.env.GF3_SAVE_PRIVATE_FIXTURE)("replays the supplied F42/F35 snapshots through the actual editor builder", () => {
  const fixture = JSON.parse(readFileSync(process.env.GF3_SAVE_PRIVATE_FIXTURE!, "utf8")) as {
    graphs: Array<{ graphId: number; before: { slots: GraphSlot[] }; after: { slots: GraphSlot[]; employeeOrder: number[] } }>;
  };
  const replay = fixture.graphs.map(graph => {
    const hydrate = (slots: GraphSlot[]) => slots.map((slot, index) => ({ ...slot, id: index + 1, scheduleId: graph.graphId }));
    const persisted = hydrate(graph.after.slots);
    const draft = buildGraphWorkspaceSaveSlots({ scheduleId: graph.graphId, year: 2026, month: 11,
      employeeIds: graph.after.employeeOrder, existingSlots: hydrate(graph.before.slots), cellMap: buildGraphCellMap(persisted),
    }, true);
    expect(draft.errors).toEqual({});
    expect(draft.slots).toHaveLength(graph.graphId === 39 ? 203 : 93);
    const key = (slot: GraphSlot) => `${slot.dayOfMonth}/${slot.fromTime}/${slot.toTime}/${slot.employeeId}`;
    const byKey = new Map(persisted.map(slot => [key(slot), slot]));
    expect(draft.slots.filter(slot => slot.slotNo !== byKey.get(key(slot))?.slotNo)).toHaveLength(graph.graphId === 39 ? 2 : 0);
    expect(fingerprintPersistedGraph({ ...state, slots: draft.slots })).toBe(fingerprintPersistedGraph({ ...state, slots: persisted }));
    return { graphId: graph.graphId, intended: draft.slots, persisted };
  });
  if (process.env.GF3_SAVE_REPLAY) writeFileSync(process.env.GF3_SAVE_REPLAY, JSON.stringify(replay));
});

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
