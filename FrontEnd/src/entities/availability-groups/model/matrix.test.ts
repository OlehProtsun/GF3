import { describe, expect, test } from "vitest";
import {
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_KIND_ANY,
  AVAILABILITY_KIND_INTERVAL,
  AVAILABILITY_KIND_NONE,
  buildAvailabilityCellMap,
  buildAvailabilityCellMapFromItems,
  buildAvailabilityColumns,
  buildAvailabilityColumnsFromItems,
  clampAvailabilityMonth,
  clampAvailabilityYear,
  getAvailabilityCellKey,
  getAvailabilityCodeFromKind,
  getAvailabilityWeekdayLabel,
  getDaysInMonth,
  isAvailabilityWeekend,
  normalizeAvailabilityCellValue,
  parseAvailabilityCode,
  sanitizeAvailabilityCellMap,
  sortAvailabilityColumns,
  summarizeAvailabilityCellMap,
} from "./matrix";
import type { AvailabilityGroupItem, AvailabilityGroupMember, AvailabilitySlot } from "./types";

describe("availability matrix model", () => {
  test("clamps dates and calculates month metadata", () => {
    expect(clampAvailabilityMonth(13.8)).toBe(12);
    expect(clampAvailabilityMonth(Number.NaN)).toBe(1);
    expect(clampAvailabilityYear(2025)).toBe(2026);
    expect(clampAvailabilityYear(5000)).toBe(4000);
    expect(getDaysInMonth(2028, 2)).toBe(29);
    expect(getAvailabilityWeekdayLabel(2026, 5, 10)).toBe("su.");
    expect(isAvailabilityWeekend(2026, 5, 10)).toBe(true);
  });

  test("parses availability codes and normalizes flexible intervals", () => {
    expect(parseAvailabilityCode("+")).toEqual({
      ok: true,
      value: { normalizedCode: "+", kind: AVAILABILITY_KIND_ANY, intervalStr: null },
    });
    expect(parseAvailabilityCode(" 9-17 ")).toEqual({
      ok: true,
      value: { normalizedCode: "09:00 - 17:00", kind: AVAILABILITY_KIND_INTERVAL, intervalStr: "09:00 - 17:00" },
    });
    expect(parseAvailabilityCode("maybe")).toEqual({
      ok: true,
      value: { normalizedCode: "maybe", kind: AVAILABILITY_KIND_NONE, intervalStr: "maybe" },
    });
    expect(parseAvailabilityCode("25:00 - 26:00")).toMatchObject({ ok: false });
    expect(normalizeAvailabilityCellValue("9-12")).toBe("09:00 - 12:00");
  });

  test("converts kind and interval values into display codes", () => {
    expect(getAvailabilityCodeFromKind("Available")).toBe(AVAILABILITY_ANY_MARK);
    expect(getAvailabilityCodeFromKind("Preferred", " 10:00 - 14:00 ")).toBe("10:00 - 14:00");
    expect(getAvailabilityCodeFromKind("Unavailable", "comment")).toBe("comment");
    expect(getAvailabilityCodeFromKind(AVAILABILITY_KIND_NONE)).toBe("-");
  });

  test("sorts columns by display order, label, then employee id", () => {
    expect(sortAvailabilityColumns([
      { employeeId: 3, label: "Zoe", displayOrder: null },
      { employeeId: 2, label: "Amy", displayOrder: 1 },
      { employeeId: 1, label: "Amy", displayOrder: 1 },
    ])).toEqual([
      { employeeId: 1, label: "Amy", displayOrder: 1 },
      { employeeId: 2, label: "Amy", displayOrder: 1 },
      { employeeId: 3, label: "Zoe", displayOrder: null },
    ]);
  });

  test("builds columns and cell maps from members and slots", () => {
    const members: AvailabilityGroupMember[] = [
      { id: 10, availabilityGroupId: 1, employeeId: 2, displayOrder: 2, employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z" },
      { id: 11, availabilityGroupId: 1, employeeId: 1, displayOrder: 1, employeeLastModifiedAtUtc: null },
    ];
    const slots: AvailabilitySlot[] = [
      { id: 100, availabilityGroupMemberId: 10, dayOfMonth: 1, kind: "Available", intervalStr: null },
      { id: 101, availabilityGroupMemberId: 11, dayOfMonth: 2, kind: "Preferred", intervalStr: "09:00 - 12:00" },
      { id: 102, availabilityGroupMemberId: 999, dayOfMonth: 3, kind: "Unavailable", intervalStr: null },
    ];

    expect(buildAvailabilityColumns(members, new Map([[1, "Amy"], [2, "Ben"]]))).toEqual([
      { employeeId: 1, memberId: 11, displayOrder: 1, employeeLastModifiedAtUtc: null, label: "Amy" },
      { employeeId: 2, memberId: 10, displayOrder: 2, employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z", label: "Ben" },
    ]);
    expect(buildAvailabilityCellMap(members, slots)).toEqual({
      [getAvailabilityCellKey(2, 1)]: "+",
      [getAvailabilityCellKey(1, 2)]: "09:00 - 12:00",
    });
  });

  test("builds columns and cell maps from flat API items", () => {
    const items: AvailabilityGroupItem[] = [
      { memberId: 5, employeeId: 2, displayOrder: 2, dayId: 50, dayOfMonth: 1, kind: "Unavailable", intervalStr: null },
      { memberId: 4, employeeId: 1, displayOrder: 1, dayId: 40, dayOfMonth: 2, kind: "Available", intervalStr: null },
      { memberId: 5, employeeId: 2, displayOrder: 2, dayId: 51, dayOfMonth: 3, kind: "Preferred", intervalStr: "13:00 - 17:00" },
    ];

    expect(buildAvailabilityColumnsFromItems(items, new Map([[1, "Amy"]]))).toEqual([
      { employeeId: 1, memberId: 4, displayOrder: 1, employeeLastModifiedAtUtc: null, label: "Amy" },
      { employeeId: 2, memberId: 5, displayOrder: 2, employeeLastModifiedAtUtc: null, label: "Employee #2" },
    ]);
    expect(buildAvailabilityCellMapFromItems(items)).toEqual({
      "2:1": "-",
      "1:2": "+",
      "2:3": "13:00 - 17:00",
    });
  });

  test("summarizes and sanitizes cell maps", () => {
    const cellMap = {
      "1:1": "+",
      "1:2": "-",
      "2:3": "09:00 - 12:00",
      "2:40": "+",
      "9:1": "+",
      "bad:key": "+",
    };

    expect(summarizeAvailabilityCellMap(cellMap)).toEqual({ any: 4, none: 1, interval: 1 });
    expect(sanitizeAvailabilityCellMap(cellMap, [1, 2], 2026, 5)).toEqual({
      "1:1": "+",
      "1:2": "-",
      "2:3": "09:00 - 12:00",
    });
  });
});
