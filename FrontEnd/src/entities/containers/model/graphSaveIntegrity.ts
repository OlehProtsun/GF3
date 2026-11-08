import type { SaveGraphDto } from "../api/dto";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { buildGraphNoteContent, parseGraphNoteContent, serializeGraphNoteCellStyles } from "./graphNote";
import { buildGraphCellMap, buildGraphDraftSlots, parseGraphCellContent, sanitizeGraphCellMap } from "./graphWorkspace";
import type { Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "./types";

export type PersistedGraphState = {
  graph: Graph;
  employees: GraphEmployee[];
  slots: GraphSlot[];
  styles: GraphCellStyle[];
};

export type GraphSaveComparisonInput = {
  nonStyleFingerprint: string;
  styles: GraphCellStyle[];
};

const colorFields = ["backgroundColorArgb", "textColorArgb"] as const;
type ColorField = typeof colorFields[number];
export type GraphColorChange = {
  employeeId: number;
  dayOfMonth: number;
  field: ColorField;
  value: number | null;
};

function normalizeShiftRange(value: string) {
  const range = /^(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})$/.exec(value.trim());
  return range ? `${range[1].padStart(5, "0")} - ${range[2].padStart(5, "0")}` : value;
}

export function graphSaveFields(graph: SaveGraphDto | Graph) {
  return {
    shopId: graph.shopId,
    name: graph.name,
    year: graph.year,
    month: graph.month,
    publicationStatus: graph.publicationStatus ?? "private",
    allowSwap: graph.allowSwap ?? true,
    peoplePerShift: graph.peoplePerShift,
    shift1Time: normalizeShiftRange(graph.shift1Time),
    shift2Time: normalizeShiftRange(graph.shift2Time),
    maxHoursPerEmpMonth: graph.maxHoursPerEmpMonth,
    maxConsecutiveDays: graph.maxConsecutiveDays,
    maxConsecutiveFull: graph.maxConsecutiveFull,
    maxFullPerMonth: graph.maxFullPerMonth,
    note: graph.note ?? null,
    availabilityGroupId: graph.availabilityGroupId ?? null,
  };
}

export function normalizeGraphEmployees(employees: GraphEmployee[]) {
  return employees.map(employee => ({
    employeeId: employee.employeeId,
    minHoursMonth: employee.minHoursMonth ?? null,
    isPriority: employee.isPriority ?? false,
    displayOrder: employee.displayOrder,
  })).sort((left, right) => left.employeeId - right.employeeId);
}

function normalizeTime(time: string) {
  return time.replace(/^(\d{2}:\d{2}):00(?:\.0+)?$/, "$1");
}

const slotStatusValues: Record<string, number> = { free: 0, unfurnished: 0, working: 1, assigned: 1, blocked: 2 };

export function normalizeGraphSlots(slots: GraphSlot[]) {
  return slots.map(slot => ({
    dayOfMonth: slot.dayOfMonth,
    slotNo: slot.slotNo,
    fromTime: normalizeTime(slot.fromTime),
    toTime: normalizeTime(slot.toTime),
    employeeId: slot.employeeId ?? null,
    status: slotStatusValues[String(slot.status).toLowerCase()] ?? slot.status,
  })).sort((left, right) =>
    left.dayOfMonth - right.dayOfMonth ||
    left.slotNo - right.slotNo ||
    left.fromTime.localeCompare(right.fromTime) ||
    left.toTime.localeCompare(right.toTime) ||
    (left.employeeId ?? 0) - (right.employeeId ?? 0) ||
    String(left.status).localeCompare(String(right.status)));
}

export function buildGraphWorkspaceSaveSlots(params: Parameters<typeof buildGraphDraftSlots>[0], preserveExisting: boolean) {
  const draft = buildGraphDraftSlots(params);
  if (!preserveExisting || Object.keys(draft.errors).length) return draft;

  const employeeIds = new Set(params.employeeIds);
  if (params.existingSlots.some(slot => slot.employeeId && !employeeIds.has(slot.employeeId))) return draft;

  // Reconstructing the displayed matrix can merge or renumber slots even when no assignment was edited.
  const assignments = (cells: typeof params.cellMap) => stableSerialize(Object.fromEntries(
    Object.entries(sanitizeGraphCellMap(cells, params.employeeIds, params.year, params.month)).flatMap(([key, value]) => {
      const parsed = parseGraphCellContent(value);
      return parsed.kind === "intervals" ? [[key, parsed.value]] : [];
    }),
  ));
  return assignments(params.cellMap) === assignments(buildGraphCellMap(params.existingSlots))
    ? { ...draft, slots: params.existingSlots }
    : draft;
}

const styleKey = (style: Pick<GraphCellStyle, "employeeId" | "dayOfMonth">) => `${style.employeeId}:${style.dayOfMonth}`;
const indexStyles = (styles: GraphCellStyle[]) => new Map(styles.map(style => [styleKey(style), style]));

export function normalizeGraphStyles(styles: Array<Omit<GraphCellStyle, "id" | "scheduleId">>) {
  return styles.map(style => ({
    employeeId: style.employeeId,
    dayOfMonth: style.dayOfMonth,
    backgroundColorArgb: style.backgroundColorArgb ?? null,
    textColorArgb: style.textColorArgb ?? null,
  })).filter(style => style.backgroundColorArgb !== null || style.textColorArgb !== null)
    .sort((left, right) => left.employeeId - right.employeeId || left.dayOfMonth - right.dayOfMonth);
}

export function fingerprintPersistedGraph(state: PersistedGraphState): string {
  return stableSerialize({
    graph: graphSaveFields(state.graph),
    employees: normalizeGraphEmployees(state.employees),
    slots: normalizeGraphSlots(state.slots),
    styles: normalizeGraphStyles(state.styles.filter(style => style.employeeId > 0)),
  });
}

export function hasPersistedWorkspaceChanges(before: PersistedGraphState, intended: PersistedGraphState, actual: PersistedGraphState) {
  const beforeFields = graphSaveFields(before.graph);
  const intendedFields = graphSaveFields(intended.graph);
  const actualFields = graphSaveFields(actual.graph);
  if ((Object.keys(intendedFields) as Array<keyof typeof intendedFields>).some(field =>
    beforeFields[field] !== intendedFields[field] && actualFields[field] === intendedFields[field])) return true;

  const employees = (state: PersistedGraphState) => new Map(
    normalizeGraphEmployees(state.employees).map(row => [row.employeeId, stableSerialize(row)]),
  );
  const beforeEmployees = employees(before);
  const intendedEmployees = employees(intended);
  const actualEmployees = employees(actual);
  if ([...new Set([...beforeEmployees.keys(), ...intendedEmployees.keys()])].some(id =>
    beforeEmployees.get(id) !== intendedEmployees.get(id) && actualEmployees.get(id) === intendedEmployees.get(id))) return true;

  const intendedSlots = stableSerialize(normalizeGraphSlots(intended.slots));
  return (intendedSlots !== stableSerialize(normalizeGraphSlots(before.slots)) &&
    intendedSlots === stableSerialize(normalizeGraphSlots(actual.slots))) ||
    matchGraphColors(diffGraphColors(before.styles, intended.styles), actual.styles) > 0;
}

export function classifyGraphDraftChange(baseline: GraphSaveComparisonInput, current: GraphSaveComparisonInput) {
  if (baseline.nonStyleFingerprint !== current.nonStyleFingerprint) return "workspace";
  return stableSerialize(normalizeGraphStyles(baseline.styles)) === stableSerialize(normalizeGraphStyles(current.styles))
    ? "unchanged" : "style-only";
}

export function diffGraphColors(baseline: GraphCellStyle[], local: GraphCellStyle[]): GraphColorChange[] {
  const before = indexStyles(baseline);
  const after = indexStyles(local);
  return [...new Map([...before, ...after])].flatMap(([key, { employeeId, dayOfMonth }]) =>
    colorFields.flatMap(field => {
      const value = after.get(key)?.[field] ?? null;
      return value === (before.get(key)?.[field] ?? null) ? [] : [{ employeeId, dayOfMonth, field, value }];
    }));
}

export function matchGraphColors(changes: GraphColorChange[], styles: GraphCellStyle[]) {
  const byCell = indexStyles(styles);
  return changes.filter(change => (byCell.get(styleKey(change))?.[change.field] ?? null) === change.value).length;
}

export function mergeGraphColorStyles(baseline: GraphCellStyle[], local: GraphCellStyle[], remote: GraphCellStyle[]) {
  const before = indexStyles(baseline);
  const localByCell = indexStyles(local);
  const merged = indexStyles(remote);
  const conflicts: Array<Omit<GraphColorChange, "value">> = [];
  for (const { value, ...change } of diffGraphColors(baseline, local)) {
    const key = styleKey(change);
    const remoteRow = merged.get(key);
    const remoteValue = remoteRow?.[change.field] ?? null;
    if (remoteValue !== value && remoteValue !== (before.get(key)?.[change.field] ?? null)) {
      conflicts.push(change);
      continue;
    }
    const localRow = localByCell.get(key);
    const row = remoteRow ?? {
      id: localRow?.id ?? 0,
      scheduleId: localRow?.scheduleId ?? 0,
      employeeId: change.employeeId,
      dayOfMonth: change.dayOfMonth,
    };
    merged.set(key, { ...row, [change.field]: value });
  }
  return {
    merged: [...merged.values()].filter(style => style.backgroundColorArgb != null || style.textColorArgb != null),
    conflicts,
  };
}

export function buildStyleOnlyUpdatedNote(remoteNote: string | null, mergedSyntheticStyles: GraphCellStyle[]): string | null {
  const remote = parseGraphNoteContent(remoteNote);
  const styles = serializeGraphNoteCellStyles(mergedSyntheticStyles.filter(style => style.employeeId <= 0));
  if (stableSerialize(normalizeGraphStyles(styles)) === stableSerialize(normalizeGraphStyles(remote.cellStyles))) {
    return remoteNote;
  }
  return buildGraphNoteContent(remote.note, remote.manualColumns, remote.columnOrder, styles, remote.textCells,
    remote.autoAvailabilityStyleSuppressions) || null;
}
