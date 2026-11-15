import { beforeEach, expect, test } from "vitest";
import { setLanguage } from "@shared/i18n";
import { formatUrlopConflictDates, formatUrlopDayRanges } from "./dateRanges";

beforeEach(() => setLanguage("en"));
test("sorts and deduplicates without bridging gaps or changing input", () => {
  const days = [8, 2, 1, 5, 7, 8];
  expect(formatUrlopDayRanges(2026, 11, days)).toBe("1–2, 5, 7–8 November 2026");
  expect(days).toEqual([8, 2, 1, 5, 7, 8]);
  expect(formatUrlopDayRanges(2026, 11, [1, 2, 3, 4, 5, 6, 7, 8])).toBe("1–8 November 2026");
});
test.each([[2026, 2, 28], [2028, 2, 29], [2026, 4, 30], [2026, 12, 31], [1, 1, 31], [9999, 12, 31]])("valid last day %i/%i/%i", (year, month, day) => {
  expect(formatUrlopDayRanges(year, month, [day])).toContain(String(day));
  expect(formatUrlopDayRanges(year, month, [day + 1])).toBe("");
});
test.each([[0, 1], [10000, 1], [2026, 0], [2026, 13], [2026.5, 1], [NaN, 1]])("invalid period %i/%i", (year, month) => {
  expect(formatUrlopDayRanges(year, month, [1])).toBe("");
});
test("invalid dates are omitted safely", () => {
  expect(formatUrlopDayRanges(2026, 2, [0, -1, 29, 30, 32, 1.5, NaN, Infinity])).toBe("");
  expect(formatUrlopDayRanges(2026, 11, [])).toBe("");
  expect(formatUrlopConflictDates(["invalid", "2026-02-29", "2026-11-31", "0000-01-01", "2026-1-1", "2026-11-13T00:00:00Z"])).toBe("");
});
test("ISO dates remain separate across months and years", () => {
  expect(formatUrlopConflictDates(["2027-01-01", "2026-12-31", "2026-12-30", "2026-12-31", "2026-11-13"]))
    .toBe("13 November 2026; 30–31 December 2026; 1 January 2027");
});
test.each([['pl', '1–3, 5 listopada 2026'], ['en', '1–3, 5 November 2026'], ['uk', '1–3, 5 листопада 2026 р.']] as const)("natural %s date grammar", (language, expected) => {
  setLanguage(language);
  expect(formatUrlopDayRanges(2026, 11, [1, 2, 3, 5])).toBe(expected);
  expect(formatUrlopConflictDates(["2026-11-13"])).toContain(language === "pl" ? "13 listopada" : language === "uk" ? "13 листопада" : "13 November");
});

test.each([
  [[1, 2, 3, 4, 5], "1–5 November 2026"],
  [[1, 2, 3, 5, 6, 7], "1–3, 5–7 November 2026"],
  [[1, 3, 5], "1, 3, 5 November 2026"],
  [[1, 2], "1–2 November 2026"],
  [[15], "15 November 2026"],
  [[1, 2, 3, 8, 12, 13], "1–3, 8, 12–13 November 2026"],
] as const)("requested day sequence %j formats as %s", (days, expected) => {
  expect(formatUrlopDayRanges(2026, 11, days)).toBe(expected);
});
