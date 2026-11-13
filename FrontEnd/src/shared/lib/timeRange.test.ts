import { describe, expect, test } from "vitest";
import {
  parseFlexibleTimeRange,
  parseFlexibleTimeRangeList,
  parseFlexibleTimeSegment,
} from "./timeRange";

describe("timeRange", () => {
  test("parses separated and compact time segments", () => {
    expect(parseFlexibleTimeSegment("9")).toEqual({ label: "09:00", totalMinutes: 540 });
    expect(parseFlexibleTimeSegment("09.30")).toEqual({ label: "09:30", totalMinutes: 570 });
    expect(parseFlexibleTimeSegment("930")).toEqual({ label: "09:30", totalMinutes: 570 });
    expect(parseFlexibleTimeSegment("1730")).toEqual({ label: "17:30", totalMinutes: 1050 });
  });

  test("rejects invalid time segments", () => {
    expect(parseFlexibleTimeSegment("")).toBeNull();
    expect(parseFlexibleTimeSegment("24:00")).toBeNull();
    expect(parseFlexibleTimeSegment("12:60")).toBeNull();
    expect(parseFlexibleTimeSegment("abc")).toBeNull();
  });

  test("normalizes ranges with unicode dashes and whitespace", () => {
    expect(parseFlexibleTimeRange(" 9:00 – 17:30 ")).toEqual({
      from: "09:00",
      to: "17:30",
      fromMinutes: 540,
      toMinutes: 1050,
      label: "09:00 - 17:30",
    });
  });

  test("rejects empty, same-time, reversed, and partial ranges", () => {
    expect(parseFlexibleTimeRange("")).toBeNull();
    expect(parseFlexibleTimeRange("09:00 - 09:00")).toBeNull();
    expect(parseFlexibleTimeRange("17:00 - 09:00")).toBeNull();
    expect(parseFlexibleTimeRange("09:00 - ")).toBeNull();
  });

  test("parses comma-separated range lists and fails the whole list on invalid parts", () => {
    expect(parseFlexibleTimeRangeList("09-12, 1300-1730")).toEqual([
      { from: "09:00", to: "12:00", fromMinutes: 540, toMinutes: 720, label: "09:00 - 12:00" },
      { from: "13:00", to: "17:30", fromMinutes: 780, toMinutes: 1050, label: "13:00 - 17:30" },
    ]);
    expect(parseFlexibleTimeRangeList("09-12, nope")).toBeNull();
    expect(parseFlexibleTimeRangeList("   ")).toEqual([]);
  });
});
