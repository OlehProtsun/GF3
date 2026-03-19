const GRAPH_NOTE_META_PREFIX = "<!--GF3_GRAPH_META:";
const GRAPH_NOTE_META_SUFFIX = "-->";

export type GraphManualColumnData = {
  id: number;
  label: string;
  cells: Record<string, string>;
};

export type GraphColumnOrderEntry = number;

type ParsedGraphNoteMeta = {
  manualColumns?: GraphManualColumnData[];
  columnOrder?: GraphColumnOrderEntry[];
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

export function parseGraphNoteContent(rawNote?: string | null) {
  const source = rawNote ?? "";
  const match = source.match(/(?:\r?\n\r?\n)?<!--GF3_GRAPH_META:([\s\S]*?)-->$/);
  if (!match || typeof match.index !== "number") {
    return {
      note: source,
      manualColumns: [] as GraphManualColumnData[],
      columnOrder: [] as GraphColumnOrderEntry[],
    };
  }

  try {
    const decoded = decodeURIComponent(match[1]);
    const parsed = JSON.parse(decoded) as ParsedGraphNoteMeta;
    const seenIds = new Set<number>();
    const manualColumns = Array.isArray(parsed.manualColumns)
      ? parsed.manualColumns.reduce<GraphManualColumnData[]>((accumulator, column) => {
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
      }, [])
      : [];

    return {
      note: source.slice(0, match.index).trimEnd(),
      manualColumns,
      columnOrder: sanitizeGraphColumnOrder(parsed.columnOrder),
    };
  } catch {
    return {
      note: source,
      manualColumns: [] as GraphManualColumnData[],
      columnOrder: [] as GraphColumnOrderEntry[],
    };
  }
}

export function getGraphVisibleNote(rawNote?: string | null) {
  return parseGraphNoteContent(rawNote).note;
}

export function buildGraphNoteContent(
  visibleNote: string,
  manualColumns: GraphManualColumnData[],
  columnOrder: GraphColumnOrderEntry[] = [],
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

  if (sanitizedManualColumns.length === 0 && sanitizedColumnOrder.length === 0) {
    return trimmedVisibleNote;
  }

  const encodedMeta = encodeURIComponent(
    JSON.stringify({
      manualColumns: sanitizedManualColumns.map(column => ({
        id: column.id,
        label: column.label,
        cells: column.cells,
      })),
      columnOrder: sanitizedColumnOrder,
    }),
  );

  const metadataBlock = `${GRAPH_NOTE_META_PREFIX}${encodedMeta}${GRAPH_NOTE_META_SUFFIX}`;
  return trimmedVisibleNote ? `${trimmedVisibleNote}\n\n${metadataBlock}` : metadataBlock;
}
