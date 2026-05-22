import { describe, expect, test } from "vitest";
import {
  buildGraphNoteContent,
  getGraphVisibleNote,
  parseGraphNoteContent,
  rehydrateGraphNoteCellStyles,
  rehydrateGraphNoteTextCells,
  serializeGraphNoteCellStyles,
  serializeGraphNoteTextCells,
} from "./graphNote";
import type { GraphCellStyle } from "./types";

describe("container graph note metadata model", () => {
  test("round-trips compact graph metadata while sanitizing invalid entries", () => {
    const content = buildGraphNoteContent(
      "Visible note  ",
      [
        { id: 7, label: "Manual", cells: { "1": "Audit", "2": "" } },
        { id: 7, label: "Duplicate", cells: { "1": "Skipped" } },
        { id: 0, label: "Invalid", cells: { "1": "Skipped" } },
      ],
      [10, -7, 10, 0],
      [
        { employeeId: 0, dayOfMonth: 2, backgroundColorArgb: -1, textColorArgb: null },
        { employeeId: 1, dayOfMonth: 2, backgroundColorArgb: -1, textColorArgb: null },
      ],
      [
        { employeeId: 3, dayOfMonth: 4, value: "Callout" },
        { employeeId: 3, dayOfMonth: 4, value: "Duplicate" },
      ],
      { "9": ["3:4", "3:4", "bad", "3:32"], "0": ["3:4"] },
    );

    const parsed = parseGraphNoteContent(content);

    expect(getGraphVisibleNote(content)).toBe("Visible note");
    expect(parsed.note).toBe("Visible note");
    expect(parsed.manualColumns).toEqual([{ id: 7, label: "Manual", cells: { "1": "Audit", "2": "" } }]);
    expect(parsed.columnOrder).toEqual([10, -7]);
    expect(parsed.cellStyles).toEqual([
      { employeeId: 0, dayOfMonth: 2, backgroundColorArgb: -1, textColorArgb: null },
    ]);
    expect(parsed.textCells).toEqual([{ employeeId: 3, dayOfMonth: 4, value: "Callout" }]);
    expect(parsed.autoAvailabilityStyleSuppressions).toEqual({ "9": ["3:4"] });
  });

  test("parses and sanitizes legacy graph metadata blocks", () => {
    const rawMetadata = JSON.stringify({
      manualColumns: [
        { id: 1, label: "Notes", cells: { "1": "A", "2": 42 } },
        { id: 1, label: "Duplicate", cells: { "1": "B" } },
      ],
      columnOrder: [1, -2, 1, 0, 3],
      cellStyles: [
        { employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 123, textColorArgb: 456 },
        { employeeId: 0, dayOfMonth: 32, backgroundColorArgb: 123 },
      ],
      textCells: [
        { employeeId: 2, dayOfMonth: 3, value: "  Prep  " },
        { employeeId: 2, dayOfMonth: 3, value: "Duplicate" },
        { employeeId: 4, dayOfMonth: 5, value: "-" },
      ],
      autoAvailabilityStyleSuppressions: {
        "5": ["2:1", "2:1", "2:31", "2:32", "-1:2"],
        "-1": ["2:1"],
      },
    });

    expect(parseGraphNoteContent(`Visible\n\n[[GF3_GRAPH_META:${rawMetadata}]]`)).toMatchObject({
      note: "Visible",
      manualColumns: [{ id: 1, label: "Notes", cells: { "1": "A" } }],
      columnOrder: [1, -2, 3],
      cellStyles: [{ employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 123, textColorArgb: 456 }],
      textCells: [{ employeeId: 2, dayOfMonth: 3, value: "Prep" }],
      autoAvailabilityStyleSuppressions: { "5": ["2:1", "2:31"] },
    });
  });

  test("leaves malformed metadata visible instead of dropping user notes", () => {
    const malformed = "Keep me\n\n[[GF3_GRAPH_META:{not-json}]]";

    expect(parseGraphNoteContent(malformed)).toEqual({
      note: malformed,
      manualColumns: [],
      columnOrder: [],
      cellStyles: [],
      textCells: [],
      autoAvailabilityStyleSuppressions: {},
    });
  });

  test("serializes only text cells that belong to visible employees and valid days", () => {
    expect(serializeGraphNoteTextCells({
      "1:1": "Training",
      "1:2": "09:00 - 12:00",
      "1:3": "-",
      "1:32": "Too late",
      "2:1": "Meeting",
      "9:1": "Hidden employee",
    }, [1, 2], 2026, 2)).toEqual([
      { employeeId: 1, dayOfMonth: 1, value: "Training" },
      { employeeId: 2, dayOfMonth: 1, value: "Meeting" },
    ]);
  });

  test("serializes and rehydrates graph note styles and text cells", () => {
    const styles: GraphCellStyle[] = [
      { id: 10, scheduleId: 1, employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 111, textColorArgb: null },
      { id: 11, scheduleId: 1, employeeId: 2, dayOfMonth: 1, backgroundColorArgb: 222, textColorArgb: null },
      { id: 12, scheduleId: 1, employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 333, textColorArgb: null },
      { id: 13, scheduleId: 1, employeeId: 0, dayOfMonth: 2, backgroundColorArgb: null, textColorArgb: 444 },
    ];

    const serializedStyles = serializeGraphNoteCellStyles(styles);

    expect(serializedStyles).toEqual([
      { employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 111, textColorArgb: null },
      { employeeId: 0, dayOfMonth: 2, backgroundColorArgb: null, textColorArgb: 444 },
    ]);
    expect(rehydrateGraphNoteCellStyles(serializedStyles, 99)).toEqual([
      { id: -1000001, scheduleId: 99, employeeId: 0, dayOfMonth: 1, backgroundColorArgb: 111, textColorArgb: null },
      { id: -1000002, scheduleId: 99, employeeId: 0, dayOfMonth: 2, backgroundColorArgb: null, textColorArgb: 444 },
    ]);
    expect(rehydrateGraphNoteTextCells([
      { employeeId: 1, dayOfMonth: 2, value: "Note" },
      { employeeId: 1, dayOfMonth: 2, value: "Duplicate" },
      { employeeId: 1, dayOfMonth: 3, value: "-" },
    ])).toEqual({ "1:2": "Note" });
  });
});
