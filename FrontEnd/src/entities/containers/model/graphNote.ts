import {
  GRAPH_EMPTY_MARK,
  getGraphCellKey,
  getGraphDaysInMonth,
  parseGraphCellContent,
  type GraphMatrixCellMap,
} from "./graphWorkspace";
import type { GraphCellStyle } from "./types";

const GRAPH_NOTE_META_PREFIX = "[[GF3_GRAPH_META:";
const GRAPH_NOTE_META_SUFFIX = "]]";
const GRAPH_NOTE_META_REGEX = /(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:([\s\S]*?)-->|\[\[GF3_GRAPH_META:([\s\S]*?)\]\])$/;
const GRAPH_NOTE_STYLE_ID_BASE = 1_000_000;

export const GRAPH_DAY_STYLE_EMPLOYEE_ID = 0;

export type GraphManualColumnData = {
  id: number;
  label: string;
  cells: Record<string, string>;
};

export type GraphColumnOrderEntry = number;

export type GraphNoteCellStyleData = {
  employeeId: number;
  dayOfMonth: number;
  backgroundColorArgb: number | null;
  textColorArgb: number | null;
};

export type GraphNoteTextCellData = {
  employeeId: number;
  dayOfMonth: number;
  value: string;
};

type ParsedGraphNoteMeta = {
  manualColumns?: unknown;
  columnOrder?: unknown;
  cellStyles?: unknown;
  textCells?: unknown;
  m?: unknown;
  o?: unknown;
  s?: unknown;
  t?: unknown;
};

function sanitizeGraphManualColumnCells(rawCells: unknown) {
  if (!rawCells || typeof rawCells !== "object") {
    return {};
  }

  return Object.entries(rawCells).reduce<Record<string, string>>((accumulator, [day, value]) => {
    if (typeof value !== "string") {
      return accumulator;
    }

    accumulator[day] = value;
    return accumulator;
  }, {});
}

function sanitizeGraphColumnOrder(rawColumnOrder: unknown) {
  if (!Array.isArray(rawColumnOrder)) {
    return [] as GraphColumnOrderEntry[];
  }

  const seenEntries = new Set<number>();

  return rawColumnOrder.reduce<GraphColumnOrderEntry[]>((accumulator, entry) => {
    if (!Number.isInteger(entry) || entry === 0 || seenEntries.has(entry)) {
      return accumulator;
    }

    seenEntries.add(entry);
    accumulator.push(entry);
    return accumulator;
  }, []);
}

function sanitizeLegacyGraphManualColumns(rawManualColumns: unknown) {
  if (!Array.isArray(rawManualColumns)) {
    return [] as GraphManualColumnData[];
  }

  const seenIds = new Set<number>();

  return rawManualColumns.reduce<GraphManualColumnData[]>((accumulator, column) => {
    const id = Number.isInteger(column?.id) && (column?.id ?? 0) > 0 ? Number(column.id) : null;
    if (id === null || seenIds.has(id)) {
      return accumulator;
    }

    seenIds.add(id);
    accumulator.push({
      id,
      label: typeof column?.label === "string" ? column.label : "",
      cells: sanitizeGraphManualColumnCells(column?.cells),
    });
    return accumulator;
  }, []);
}

function createSanitizedGraphNoteCellStyle(
  rawEmployeeId: unknown,
  rawDayOfMonth: unknown,
  rawBackgroundColorArgb: unknown,
  rawTextColorArgb: unknown,
) {
  const employeeId = Number(rawEmployeeId);
  const dayOfMonth = Number(rawDayOfMonth);
  const backgroundColorArgb = typeof rawBackgroundColorArgb === "number" ? rawBackgroundColorArgb : null;
  const textColorArgb = typeof rawTextColorArgb === "number" ? rawTextColorArgb : null;

  if (!Number.isInteger(employeeId) || employeeId > 0) {
    return null;
  }

  if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31) {
    return null;
  }

  if (backgroundColorArgb === null && textColorArgb === null) {
    return null;
  }

  return {
    employeeId,
    dayOfMonth,
    backgroundColorArgb,
    textColorArgb,
  } satisfies GraphNoteCellStyleData;
}

function sanitizeGraphNoteCellStyles(rawCellStyles: unknown) {
  if (!Array.isArray(rawCellStyles)) {
    return [] as GraphNoteCellStyleData[];
  }

  const seenEntries = new Set<string>();

  return rawCellStyles.reduce<GraphNoteCellStyleData[]>((accumulator, style) => {
    const nextStyle = createSanitizedGraphNoteCellStyle(
      style?.employeeId,
      style?.dayOfMonth,
      style?.backgroundColorArgb,
      style?.textColorArgb,
    );
    if (!nextStyle) {
      return accumulator;
    }

    const key = `${nextStyle.employeeId}:${nextStyle.dayOfMonth}`;
    if (seenEntries.has(key)) {
      return accumulator;
    }

    seenEntries.add(key);
    accumulator.push(nextStyle);
    return accumulator;
  }, []);
}

function sanitizeCompactGraphManualColumns(rawManualColumns: unknown) {
  if (!Array.isArray(rawManualColumns)) {
    return [] as GraphManualColumnData[];
  }

  const seenIds = new Set<number>();

  return rawManualColumns.reduce<GraphManualColumnData[]>((accumulator, rawColumn) => {
    if (!Array.isArray(rawColumn)) {
      return accumulator;
    }

    const [rawId, rawLabel, rawCells] = rawColumn;
    const id = Number(rawId);
    if (!Number.isInteger(id) || id <= 0 || seenIds.has(id)) {
      return accumulator;
    }

    seenIds.add(id);
    accumulator.push({
      id,
      label: typeof rawLabel === "string" ? rawLabel : "",
      cells: sanitizeGraphManualColumnCells(rawCells),
    });
    return accumulator;
  }, []);
}

function sanitizeCompactGraphNoteCellStyles(rawCellStyles: unknown) {
  if (!Array.isArray(rawCellStyles)) {
    return [] as GraphNoteCellStyleData[];
  }

  const seenEntries = new Set<string>();

  return rawCellStyles.reduce<GraphNoteCellStyleData[]>((accumulator, rawStyle) => {
    if (!Array.isArray(rawStyle)) {
      return accumulator;
    }

    const [rawEmployeeId, rawDayOfMonth, rawBackgroundColorArgb, rawTextColorArgb] = rawStyle;
    const nextStyle = createSanitizedGraphNoteCellStyle(
      rawEmployeeId,
      rawDayOfMonth,
      rawBackgroundColorArgb,
      rawTextColorArgb,
    );
    if (!nextStyle) {
      return accumulator;
    }

    const key = `${nextStyle.employeeId}:${nextStyle.dayOfMonth}`;
    if (seenEntries.has(key)) {
      return accumulator;
    }

    seenEntries.add(key);
    accumulator.push(nextStyle);
    return accumulator;
  }, []);
}

function createSanitizedGraphNoteTextCell(
  rawEmployeeId: unknown,
  rawDayOfMonth: unknown,
  rawValue: unknown,
) {
  const employeeId = Number(rawEmployeeId);
  const dayOfMonth = Number(rawDayOfMonth);
  const value = typeof rawValue === "string" ? rawValue.trim() : "";

  if (!Number.isInteger(employeeId) || employeeId <= 0) {
    return null;
  }

  if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31) {
    return null;
  }

  if (!value || value === GRAPH_EMPTY_MARK) {
    return null;
  }

  return {
    employeeId,
    dayOfMonth,
    value,
  } satisfies GraphNoteTextCellData;
}

function sanitizeLegacyGraphNoteTextCells(rawTextCells: unknown) {
  if (!Array.isArray(rawTextCells)) {
    return [] as GraphNoteTextCellData[];
  }

  const seenEntries = new Set<string>();

  return rawTextCells.reduce<GraphNoteTextCellData[]>((accumulator, textCell) => {
    const nextTextCell = createSanitizedGraphNoteTextCell(
      textCell?.employeeId,
      textCell?.dayOfMonth,
      textCell?.value,
    );
    if (!nextTextCell) {
      return accumulator;
    }

    const key = `${nextTextCell.employeeId}:${nextTextCell.dayOfMonth}`;
    if (seenEntries.has(key)) {
      return accumulator;
    }

    seenEntries.add(key);
    accumulator.push(nextTextCell);
    return accumulator;
  }, []);
}

function sanitizeCompactGraphNoteTextCells(rawTextCells: unknown) {
  if (!Array.isArray(rawTextCells)) {
    return [] as GraphNoteTextCellData[];
  }

  const seenEntries = new Set<string>();

  return rawTextCells.reduce<GraphNoteTextCellData[]>((accumulator, rawTextCell) => {
    if (!Array.isArray(rawTextCell)) {
      return accumulator;
    }

    const [rawEmployeeId, rawDayOfMonth, rawValue] = rawTextCell;
    const nextTextCell = createSanitizedGraphNoteTextCell(rawEmployeeId, rawDayOfMonth, rawValue);
    if (!nextTextCell) {
      return accumulator;
    }

    const key = `${nextTextCell.employeeId}:${nextTextCell.dayOfMonth}`;
    if (seenEntries.has(key)) {
      return accumulator;
    }

    seenEntries.add(key);
    accumulator.push(nextTextCell);
    return accumulator;
  }, []);
}

function encodeGraphNoteMetaValue(value: string) {
  const bytes = new TextEncoder().encode(value);
  let binary = "";

  for (let index = 0; index < bytes.length; index += 0x8000) {
    binary += String.fromCharCode(...bytes.subarray(index, index + 0x8000));
  }

  return btoa(binary)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/u, "");
}

function decodeGraphNoteMetaValue(value: string) {
  const normalizedBase64 = value
    .replace(/-/g, "+")
    .replace(/_/g, "/")
    .padEnd(Math.ceil(value.length / 4) * 4, "=");
  const binary = atob(normalizedBase64);
  const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));

  return new TextDecoder().decode(bytes);
}

function parseGraphNoteMeta(rawMeta: string) {
  const payload = rawMeta.trim();
  const tryParse = (value: string) => JSON.parse(value) as ParsedGraphNoteMeta;

  if (!payload) {
    return null;
  }

  if (payload.startsWith("b64:")) {
    try {
      return tryParse(decodeGraphNoteMetaValue(payload.slice(4)));
    } catch {
      return null;
    }
  }

  try {
    return tryParse(payload);
  } catch {
    try {
      return tryParse(decodeURIComponent(payload));
    } catch {
      return null;
    }
  }
}

export function parseGraphNoteContent(rawNote?: string | null) {
  const source = rawNote ?? "";
  const match = source.match(GRAPH_NOTE_META_REGEX);
  if (!match || typeof match.index !== "number") {
    return {
      note: source,
      manualColumns: [] as GraphManualColumnData[],
      columnOrder: [] as GraphColumnOrderEntry[],
      cellStyles: [] as GraphNoteCellStyleData[],
      textCells: [] as GraphNoteTextCellData[],
    };
  }

  const metadataPayload = match[2] ?? match[1];
  const parsed = parseGraphNoteMeta(metadataPayload);
  if (!parsed) {
    return {
      note: source,
      manualColumns: [] as GraphManualColumnData[],
      columnOrder: [] as GraphColumnOrderEntry[],
      cellStyles: [] as GraphNoteCellStyleData[],
      textCells: [] as GraphNoteTextCellData[],
    };
  }

  return {
    note: source.slice(0, match.index).trimEnd(),
    manualColumns: (() => {
      const nextLegacyManualColumns = sanitizeLegacyGraphManualColumns(parsed.manualColumns);
      return nextLegacyManualColumns.length > 0
        ? nextLegacyManualColumns
        : sanitizeCompactGraphManualColumns(parsed.m);
    })(),
    columnOrder: (() => {
      const nextLegacyColumnOrder = sanitizeGraphColumnOrder(parsed.columnOrder);
      return nextLegacyColumnOrder.length > 0 ? nextLegacyColumnOrder : sanitizeGraphColumnOrder(parsed.o);
    })(),
    cellStyles: (() => {
      const nextLegacyStyles = sanitizeGraphNoteCellStyles(parsed.cellStyles);
      return nextLegacyStyles.length > 0 ? nextLegacyStyles : sanitizeCompactGraphNoteCellStyles(parsed.s);
    })(),
    textCells: (() => {
      const nextLegacyTextCells = sanitizeLegacyGraphNoteTextCells(parsed.textCells);
      return nextLegacyTextCells.length > 0 ? nextLegacyTextCells : sanitizeCompactGraphNoteTextCells(parsed.t);
    })(),
  };
}

export function getGraphVisibleNote(rawNote?: string | null) {
  return parseGraphNoteContent(rawNote).note;
}

export function buildGraphNoteContent(
  visibleNote: string,
  manualColumns: GraphManualColumnData[],
  columnOrder: GraphColumnOrderEntry[] = [],
  cellStyles: GraphNoteCellStyleData[] = [],
  textCells: GraphNoteTextCellData[] = [],
) {
  const trimmedVisibleNote = visibleNote.trimEnd();
  const sanitizedManualColumns = manualColumns.reduce<GraphManualColumnData[]>((accumulator, column) => {
    const id = Number.isInteger(column.id) && column.id > 0 ? column.id : null;
    if (id === null || accumulator.some(item => item.id === id)) {
      return accumulator;
    }

    accumulator.push({
      id,
      label: typeof column.label === "string" ? column.label : "",
      cells: sanitizeGraphManualColumnCells(column.cells),
    });
    return accumulator;
  }, []);
  const sanitizedColumnOrder = sanitizeGraphColumnOrder(columnOrder);
  const sanitizedCellStyles = sanitizeGraphNoteCellStyles(cellStyles);
  const sanitizedTextCells = sanitizeLegacyGraphNoteTextCells(textCells);

  if (
    sanitizedManualColumns.length === 0 &&
    sanitizedColumnOrder.length === 0 &&
    sanitizedCellStyles.length === 0 &&
    sanitizedTextCells.length === 0
  ) {
    return trimmedVisibleNote;
  }

  const encodedMeta = `b64:${encodeGraphNoteMetaValue(JSON.stringify({
    ...(sanitizedManualColumns.length > 0
      ? {
        m: sanitizedManualColumns.map(column => (
          Object.keys(column.cells).length > 0
            ? [column.id, column.label, column.cells]
            : [column.id, column.label]
        )),
      }
      : {}),
    ...(sanitizedColumnOrder.length > 0 ? { o: sanitizedColumnOrder } : {}),
    ...(sanitizedCellStyles.length > 0
      ? {
        s: sanitizedCellStyles.map(style => (
          style.textColorArgb === null
            ? [style.employeeId, style.dayOfMonth, style.backgroundColorArgb]
            : [style.employeeId, style.dayOfMonth, style.backgroundColorArgb, style.textColorArgb]
        )),
      }
      : {}),
    ...(sanitizedTextCells.length > 0
      ? {
        t: sanitizedTextCells.map(textCell => [textCell.employeeId, textCell.dayOfMonth, textCell.value]),
      }
      : {}),
  }))}`;

  const metadataBlock = `${GRAPH_NOTE_META_PREFIX}${encodedMeta}${GRAPH_NOTE_META_SUFFIX}`;
  return trimmedVisibleNote ? `${trimmedVisibleNote}\n\n${metadataBlock}` : metadataBlock;
}

export function serializeGraphNoteCellStyles(styles: GraphCellStyle[]) {
  return sanitizeGraphNoteCellStyles(
    styles.map(style => ({
      employeeId: style.employeeId,
      dayOfMonth: style.dayOfMonth,
      backgroundColorArgb: style.backgroundColorArgb ?? null,
      textColorArgb: style.textColorArgb ?? null,
    })),
  );
}

export function serializeGraphNoteTextCells(
  cellMap: GraphMatrixCellMap,
  employeeIds: number[],
  year: number,
  month: number,
) {
  const employeeIdSet = new Set(employeeIds.filter(employeeId => Number.isInteger(employeeId) && employeeId > 0));
  const daysInMonth = getGraphDaysInMonth(year, month);
  const textCells: GraphNoteTextCellData[] = [];
  const seenEntries = new Set<string>();

  Object.entries(cellMap).forEach(([cellKey, value]) => {
    const [employeeIdValue, dayOfMonthValue] = cellKey.split(":");
    const employeeId = Number(employeeIdValue);
    const dayOfMonth = Number(dayOfMonthValue);
    const trimmedValue = value.trim();

    if (!employeeIdSet.has(employeeId)) {
      return;
    }

    if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > daysInMonth) {
      return;
    }

    if (!trimmedValue || trimmedValue === GRAPH_EMPTY_MARK) {
      return;
    }

    const parsed = parseGraphCellContent(trimmedValue);
    if (parsed.kind !== "text") {
      return;
    }

    const entryKey = getGraphCellKey(employeeId, dayOfMonth);
    if (seenEntries.has(entryKey)) {
      return;
    }

    seenEntries.add(entryKey);
    textCells.push({
      employeeId,
      dayOfMonth,
      value: parsed.value,
    });
  });

  return textCells;
}

export function rehydrateGraphNoteCellStyles(styles: GraphNoteCellStyleData[], scheduleId: number) {
  return sanitizeGraphNoteCellStyles(styles).map((style, index) => ({
    id: -(GRAPH_NOTE_STYLE_ID_BASE + index + 1),
    scheduleId,
    employeeId: style.employeeId,
    dayOfMonth: style.dayOfMonth,
    backgroundColorArgb: style.backgroundColorArgb,
    textColorArgb: style.textColorArgb,
  }));
}

export function rehydrateGraphNoteTextCells(textCells: GraphNoteTextCellData[]) {
  return sanitizeLegacyGraphNoteTextCells(textCells).reduce<GraphMatrixCellMap>((accumulator, textCell) => {
    accumulator[getGraphCellKey(textCell.employeeId, textCell.dayOfMonth)] = textCell.value;
    return accumulator;
  }, {});
}
