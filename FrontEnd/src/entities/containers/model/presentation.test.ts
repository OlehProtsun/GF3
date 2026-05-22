import { describe, expect, test } from "vitest";
import {
  getContainerDisplayName,
  getContainerInitials,
  getContainerProfileDetails,
  getContainerState,
  getGraphMonthYearLabel,
  matchesContainerSearch,
} from "./presentation";
import type { Container } from "./types";

describe("container presentation model", () => {
  test("formats display names, initials, states, and details with fallbacks", () => {
    expect(getContainerDisplayName({ name: " Main Warehouse " })).toBe("Main Warehouse");
    expect(getContainerDisplayName({ name: " " }, "Fallback")).toBe("Fallback");
    expect(getContainerInitials({ name: "Main Warehouse East" })).toBe("MW");
    expect(getContainerInitials({ name: " " }, "FB")).toBe("FB");
    expect(getContainerState(3, "24h 0m", "important")).toBe("3 schedules tracked, note available");
    expect(getContainerState(3, "24h 0m")).toBe("3 schedules tracked, 24h 0m assigned");
    expect(getContainerState(0, "0h 0m", "note")).toBe("Profile note available");
    expect(getContainerState(0, "0h 0m")).toBe("No schedules yet");

    expect(getContainerProfileDetails({ note: "  Note text  " }, {
      scheduleCount: 2,
      totalEmployees: 5,
      totalShops: 1,
      totalHoursText: "40h 0m",
    }).map(item => item.value)).toEqual(["Note text", "2", "5", "1", "40h 0m"]);
  });

  test("formats graph month labels and matches search text", () => {
    const container: Container = { id: 1, name: "North Team", note: "Weekend coverage" };

    expect(getGraphMonthYearLabel({ year: 2026, month: 5 })).toBe("May 2026");
    expect(getGraphMonthYearLabel({ year: 2026, month: 99 })).toBe("December 2026");
    expect(matchesContainerSearch(container, " north ")).toBe(true);
    expect(matchesContainerSearch(container, "coverage")).toBe(true);
    expect(matchesContainerSearch(container, "south")).toBe(false);
    expect(matchesContainerSearch(container, " ")).toBe(true);
  });
});
