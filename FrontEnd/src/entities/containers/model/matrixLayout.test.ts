import { describe, expect, test } from "vitest";
import {
  buildMatrixAutoColumnWidths,
  MATRIX_COLUMN_MAX_AUTO_WIDTH_PX,
  MATRIX_COLUMN_MIN_WIDTH_PX,
} from "./matrixLayout";

describe("buildMatrixAutoColumnWidths", () => {
  const columns = [
    {
      employeeId: 7,
      kind: "employee" as const,
      manualColumnId: null,
      graphEmployeeId: 12,
      label: "ANNA FERENC",
      minHoursMonth: null,
      totalMinutes: 0,
      totalText: "",
    },
    {
      employeeId: 8,
      kind: "employee" as const,
      manualColumnId: null,
      graphEmployeeId: 13,
      label: "OLEH",
      minHoursMonth: null,
      totalMinutes: 0,
      totalText: "",
    },
  ];

  test("widens only the column containing a long availability hint", () => {
    const widths = buildMatrixAutoColumnWidths({
      columns,
      cellMap: { "7:1": "-", "8:1": "09:00 - 15:00" },
      visualHintMap: { "7:1": "09:00 - 15:00 (F31 : 05.2026)" },
    });

    expect(widths[7]).toBeGreaterThan(MATRIX_COLUMN_MIN_WIDTH_PX);
    expect(widths[8]).toBe(MATRIX_COLUMN_MIN_WIDTH_PX);
  });

  test("widens a column for shift text with related schedule names", () => {
    const widths = buildMatrixAutoColumnWidths({
      columns,
      cellMap: {
        "7:1": "09:00 - 15:00, (Second May Schedule), (Late May Schedule)",
        "8:1": "09:00 - 15:00",
      },
    });

    expect(widths[7]).toBeGreaterThan(MATRIX_COLUMN_MIN_WIDTH_PX);
    expect(widths[8]).toBe(MATRIX_COLUMN_MIN_WIDTH_PX);
  });

  test("widens a column for a filled shift followed by a related schedule hint", () => {
    const widths = buildMatrixAutoColumnWidths({
      columns,
      cellMap: { "7:1": "15:00 - 21:00", "8:1": "09:00 - 15:00" },
      visualHintMap: { "7:1": "F35" },
    });

    expect(widths[7]).toBeGreaterThan(MATRIX_COLUMN_MIN_WIDTH_PX);
    expect(widths[8]).toBe(MATRIX_COLUMN_MIN_WIDTH_PX);
  });

  test("caps extremely long notes so one column cannot dominate the matrix", () => {
    const widths = buildMatrixAutoColumnWidths({
      columns: [columns[0]],
      cellMap: { "7:1": "x".repeat(500) },
    });

    expect(widths[7]).toBe(MATRIX_COLUMN_MAX_AUTO_WIDTH_PX);
  });
});
