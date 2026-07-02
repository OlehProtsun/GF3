import { describe, expect, test } from "vitest";
import { formatScheduleLastUpdate } from "./scheduleLastUpdate";

describe("formatScheduleLastUpdate", () => {
  test("formats a valid UTC timestamp for the user's local time zone", () => {
    const label = formatScheduleLastUpdate("2026-06-28T12:45:00Z");

    expect(label).toContain("28 Jun 2026");
    expect(label).not.toBe("Not recorded yet");
  });

  test("uses a safe legacy fallback for missing or invalid values", () => {
    expect(formatScheduleLastUpdate(null)).toBe("Not recorded yet");
    expect(formatScheduleLastUpdate("not-a-date")).toBe("Not recorded yet");
  });
});
