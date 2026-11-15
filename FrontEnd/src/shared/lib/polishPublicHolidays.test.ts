import { describe, expect, test } from "vitest";
import { getPolishPublicHoliday, getPolishPublicHolidaysForYear } from "./polishPublicHolidays";

describe("getPolishPublicHolidaysForYear", () => {
  test("enumerates exactly 14 ordered, unique named dates in 2026", () => {
    const holidays = getPolishPublicHolidaysForYear(2026);
    expect(holidays.map(({ month, day }) => `${month}/${day}`)).toEqual([
      "1/1", "1/6", "4/5", "4/6", "5/1", "5/3", "5/24", "6/4", "8/15", "11/1", "11/11", "12/24", "12/25", "12/26",
    ]);
    expect(new Set(holidays.map(h => h.id)).size).toBe(14);
    expect(holidays.every(h => h.year === 2026)).toBe(true);
  });
  test("preserves the Christmas Eve cutover and year boundaries", () => {
    expect(getPolishPublicHolidaysForYear(2024).some(h => h.id === "christmas-eve")).toBe(false);
    expect(getPolishPublicHolidaysForYear(2025).some(h => h.id === "christmas-eve")).toBe(true);
    for (const year of [1, 9999]) {
      expect(getPolishPublicHolidaysForYear(year).every(h => h.year === year)).toBe(true);
      expect(getPolishPublicHolidaysForYear(year)[0]).toMatchObject({ month: 1, day: 1 });
    }
  });
  test.each([0, -1, 10000, 2026.5, NaN, Infinity])("rejects invalid year %s", year => {
    expect(getPolishPublicHolidaysForYear(year)).toEqual([]);
  });
});

describe("getPolishPublicHoliday", () => {
  test.each([
    [1, 1, "new-year"], [1, 6, "epiphany"], [5, 1, "labour-day"], [5, 3, "constitution-day"],
    [8, 15, "assumption"], [11, 1, "all-saints"], [11, 11, "independence-day"],
    [12, 24, "christmas-eve"], [12, 25, "christmas-day"], [12, 26, "second-christmas-day"],
  ])("detects fixed holiday %s/%s across supported years", (month, day, id) => {
    for (const year of [2025, 2026, 2028, 2100, 2400, 4000]) {
      expect(getPolishPublicHoliday(year, month, day)?.id).toBe(id);
    }
  });

  test.each([
    [2024, 3, 31, 4, 1, 5, 19, 5, 30],
    [2025, 4, 20, 4, 21, 6, 8, 6, 19],
    [2026, 4, 5, 4, 6, 5, 24, 6, 4],
    [2028, 4, 16, 4, 17, 6, 4, 6, 15],
    [2000, 4, 23, 4, 24, 6, 11, 6, 22],
    [2100, 3, 28, 3, 29, 5, 16, 5, 27],
    [1818, 3, 22, 3, 23, 5, 10, 5, 21],
    [1943, 4, 25, 4, 26, 6, 13, 6, 24],
  ])("calculates Gregorian movable holidays for %s", (year, em, ed, mm, md, pm, pd, cm, cd) => {
    expect(getPolishPublicHoliday(year, em, ed)?.id).toBe("easter-sunday");
    expect(getPolishPublicHoliday(year, mm, md)?.id).toBe("easter-monday");
    expect(getPolishPublicHoliday(year, pm, pd)?.id).toBe("pentecost");
    expect(getPolishPublicHoliday(year, cm, cd)?.id).toBe("corpus-christi");
  });

  test("Christmas Eve starts in 2025", () => {
    expect(getPolishPublicHoliday(2024, 12, 24)).toBeNull();
    expect(getPolishPublicHoliday(2025, 12, 24)?.id).toBe("christmas-eve");
    expect(getPolishPublicHoliday(2026, 12, 24)?.id).toBe("christmas-eve");
  });

  test.each([[2026, 5, 4], [2026, 5, 2], [2026, 5, 10], [2024, 2, 29], [2000, 2, 29], [2400, 2, 29]])(
    "ordinary days, weekends and leap days are not holidays: %s-%s-%s", (year, month, day) => {
      expect(getPolishPublicHoliday(year, month, day)).toBeNull();
    });

  test("includes only named holidays when they fall on weekends", () => {
    expect(getPolishPublicHoliday(2026, 5, 3)?.id).toBe("constitution-day");
    expect(getPolishPublicHoliday(2026, 8, 15)?.id).toBe("assumption");
    expect(getPolishPublicHoliday(2026, 5, 10)).toBeNull();
    expect(getPolishPublicHoliday(2026, 8, 17)).toBeNull();
  });

  test.each([
    [2026, 2, 29], [2100, 2, 29], [2026, 4, 31], [2026, 0, 1], [2026, 13, 1],
    [2026, 1, 0], [2026, 1, 32], [2026, 5, 1.5], [2026, 5.5, 1], [2026.5, 5, 1],
    [0, 1, 1], [-1, 1, 1], [10000, 1, 1], [NaN, 1, 1], [2026, Infinity, 1], [2026, 1, NaN],
  ])("rejects invalid date %s-%s-%s", (year, month, day) => {
    expect(getPolishPublicHoliday(year, month, day)).toBeNull();
  });
});
