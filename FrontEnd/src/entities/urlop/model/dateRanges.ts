import { getLocale } from "@shared/i18n";
import { isoDate } from "./calendar";

export function formatUrlopDayRanges(year: number, month: number, days: readonly number[]): string {
  if (!Number.isInteger(year) || year < 1 || year > 9999 || !Number.isInteger(month) || month < 1 || month > 12) return "";
  const date = (day: number) => new Date(`${isoDate(year, month, day)}T12:00:00Z`);
  const valid = [...new Set(days)].filter(day => Number.isInteger(day) && day > 0 && day <= 31 && date(day).getUTCMonth() === month - 1).sort((a, b) => a - b);
  if (!valid.length) return "";
  const ranges: string[] = [];
  for (let i = 0; i < valid.length; i++) {
    const start = valid[i];
    while (i + 1 < valid.length && valid[i + 1] === valid[i] + 1) i++;
    ranges.push(start === valid[i] ? String(start) : `${start}–${valid[i]}`);
  }
  return new Intl.DateTimeFormat(getLocale(), { day: "numeric", month: "long", year: "numeric", timeZone: "UTC" })
    .formatToParts(date(valid[0])).map(part => part.type === "day" ? ranges.join(", ") : part.value).join("");
}

export function formatUrlopConflictDates(isoDates: readonly string[]): string {
  const groups = new Map<string, number[]>();
  for (const value of [...new Set(isoDates)].sort()) {
    if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)) continue;
    const [year, month, day] = value.split("-").map(Number);
    if (!formatUrlopDayRanges(year, month, [day])) continue;
    const period = value.slice(0, 7);
    groups.set(period, [...(groups.get(period) ?? []), day]);
  }
  return [...groups].map(([period, days]) => {
    const [year, month] = period.split("-").map(Number);
    return formatUrlopDayRanges(year, month, days);
  }).join("; ");
}
