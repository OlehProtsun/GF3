import type { ApprovedUrlopDay, UrlopRequest } from "./types";

export function isoDate(year: number, month: number, day: number) {
  return `${String(year).padStart(4, "0")}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
}
export function monthDays(year: number, month: number) {
  return Array.from({ length: new Date(year, month, 0).getDate() }, (_, i) => i + 1);
}
export function monthStartOffset(year: number, month: number) {
  const date = new Date(0);
  date.setUTCFullYear(year, month - 1, 1);
  return (date.getUTCDay() + 6) % 7;
}
export function calendarMonths(year: number, month: number) {
  return Array.from({ length: 12 }, (_, i) => ({ year: year + Math.floor((month - 1 + i) / 12), month: (month - 1 + i) % 12 + 1 }));
}
export function warsawToday(now = new Date()) {
  const parts = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Warsaw", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(now);
  const get = (type: string) => parts.find(x => x.type === type)!.value;
  return `${get("year")}-${get("month")}-${get("day")}`;
}
export function occupiedDays(requests: readonly UrlopRequest[]) {
  return new Set(requests.filter(x => x.status !== "rejected").flatMap(x => x.days));
}
export function toggleDay(selected: readonly number[], day: number) {
  return selected.includes(day) ? selected.filter(x => x !== day) : [...selected, day].sort((a, b) => a - b);
}
export function validSelection(year: number, month: number, days: readonly number[], blocked: ReadonlySet<number>, today?: string) {
  return days.length > 0 && new Set(days).size === days.length && days.every(day => monthDays(year, month).includes(day) && !blocked.has(day) && (!today || isoDate(year, month, day) >= today));
}
export function approvedKeys(days: readonly ApprovedUrlopDay[], shopId: number, year: number, month: number) {
  const prefix = isoDate(year, month, 1).slice(0, 7);
  return new Set(days.filter(x => x.shopId === shopId && x.date.startsWith(prefix)).map(x => `${x.employeeId}:${Number(x.date.slice(8, 10))}`));
}
export function graphDateRange(graphs: readonly { year: number; month: number }[]) {
  const starts = graphs.map(x => isoDate(x.year, x.month, 1)).sort();
  const ends = graphs.map(x => isoDate(x.year, x.month, monthDays(x.year, x.month).length)).sort();
  return { from: starts[0] ?? "", to: ends.at(-1) ?? "" };
}
export function vacationShopNames(days: readonly ApprovedUrlopDay[], shops: readonly { id: number; name: string }[], employeeId: number, date: string) {
  return [...new Set(days.filter(x => x.employeeId === employeeId && x.date === date).map(x => shops.find(s => s.id === x.shopId)?.name ?? String(x.shopId)))];
}
