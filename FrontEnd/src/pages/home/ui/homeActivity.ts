import { dateTimeFormat, t } from "@shared/i18n";
import { warsawToday } from "@entities/urlop/model/calendar";
import type { ManagerHomeActivityItem } from "../api/managerHomeActivityApi";
export { warsawToday };
export function parseHomeDay(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value < "0001-01-01") throw new RangeError("Invalid calendar date");
  const date = new Date(`${value}T00:00:00Z`);
  if (!Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== value) throw new RangeError("Invalid calendar date");
  return date;
}
export function moveSelectedDay(selectedDay: string, delta: number) {
  const date = parseHomeDay(selectedDay); date.setUTCDate(date.getUTCDate() + delta);
  return date.getUTCFullYear() < 1 ? "0001-01-01" : date.getUTCFullYear() > 9999 ? "9999-12-31" : date.toISOString().slice(0, 10);
}
export function setSelectedMonth(selectedDay: string, yyyyMm: string) {
  const day = parseHomeDay(selectedDay).getUTCDate();
  const end = parseHomeDay(`${yyyyMm}-01`); end.setUTCMonth(end.getUTCMonth() + 1, 0);
  return `${yyyyMm}-${String(Math.min(day, end.getUTCDate())).padStart(2, "0")}`;
}
const positiveId = (value: number | null | undefined): value is number => Number.isSafeInteger(value) && value! > 0;
export function getHomeActivityRoute(item: ManagerHomeActivityItem): string | null {
  switch (item.kind) {
    case "swap_created":
    case "swap_accepted":
    case "swap_cancelled":
    case "correction_requested":
    case "correction_reviewed":
    case "day_off_requested":
    case "day_off_reviewed":
    case "approval_requested":
    case "approval_reviewed":
      return item.targetType === "graph" && positiveId(item.containerId) && positiveId(item.graphId)
        ? `/container/${item.containerId}/graphs/${item.graphId}` : null;
    case "availability_updated":
      return item.targetType === "availability" && positiveId(item.availabilityGroupId)
        ? `/availability/${item.availabilityGroupId}` : null;
    case "urlop_requested":
    case "urlop_reviewed":
    case "urlop_manager_created":
      return item.targetType === "urlop" && positiveId(item.shopId) && Number.isInteger(item.year) && item.year! >= 1 && item.year! <= 9999 && Number.isInteger(item.month) && item.month! >= 1 && item.month! <= 12
        ? `/urlop/${item.shopId}?period=${String(item.year).padStart(4, "0")}-${String(item.month).padStart(2, "0")}` : null;
  }
  return null;
}
export function getWarsawShiftStatus(selectedDay: string, fromTime: string, toTime: string, now: Date): "upcoming" | "in_progress" | "completed" {
  const minutes = (time: string) => Number(time.slice(0, 2)) * 60 + Number(time.slice(3, 5));
  const day = parseHomeDay(selectedDay).getTime();
  const start = day + minutes(fromTime) * 60_000;
  let end = day + minutes(toTime) * 60_000;
  if (end <= start) end += 86_400_000;
  const parts = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Warsaw", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(now);
  const wallNow = parseHomeDay(warsawToday(now)).getTime() + (Number(parts.find(p => p.type === "hour")!.value) * 60 + Number(parts.find(p => p.type === "minute")!.value)) * 60_000;
  return wallNow < start ? "upcoming" : wallNow < end ? "in_progress" : "completed";
}
export function formatHomeDate(value: string, long = false) {
  return dateTimeFormat("en-GB", { day: "numeric", month: long ? "long" : "short", ...(long ? { year: "numeric" as const } : {}), timeZone: "UTC" }).format(parseHomeDay(value));
}
function vacationRanges(days: readonly string[]) {
  const sorted = [...new Set(days)].sort(); const ranges: string[] = [];
  for (let i = 0; i < sorted.length; i++) {
    const first = sorted[i]; let last = first;
    while (i + 1 < sorted.length && moveSelectedDay(last, 1) === sorted[i + 1]) last = sorted[++i];
    ranges.push(first === last ? formatHomeDate(first) : `${formatHomeDate(first)} – ${formatHomeDate(last)}`);
  }
  return ranges.join(", ");
}
export function getHomeActivityPresentation(item: ManagerHomeActivityItem) {
  const name = item.employeeName;
  const titles = {
    swap_created: item.returnShiftDate ? t("{0} requested a shift exchange", name) : t("{0} gave up a shift", name),
    swap_accepted: t("{0} picked up a shift from {1}", item.counterpartyName ?? name, name),
    swap_cancelled: t("{0} cancelled a shift offer", name),
    correction_requested: t("{0} requested a schedule correction", name), correction_reviewed: t("Schedule correction for {0} was reviewed", name),
    day_off_requested: t("{0} requested a day-off correction", name), day_off_reviewed: t("Day-off correction for {0} was reviewed", name),
    approval_requested: t("Shift approval requested for {0}", name), approval_reviewed: t("Shift approval for {0} was reviewed", name),
    urlop_requested: t("{0} requested time off", name), urlop_reviewed: t("Time off for {0} was reviewed", name),
    urlop_manager_created: t("Manager added time off for {0}", name), availability_updated: t("{0} updated availability", name),
  };
  const statuses: Record<string, string> = { open: t("Open"), accepted: t("Accepted"), cancelled: t("Cancelled"), pending: t("Requires approval"), approved: t("Approved"), rejected: t("Rejected"), updated: t("Updated"), expired: t("Expired") };
  const interval = (date?: string | null, from?: string | null, to?: string | null) => [date ? formatHomeDate(date) : "", from && to ? `${from} – ${to}` : ""].filter(Boolean).join(", ");
  let description = interval(item.shiftDate, item.fromTime, item.toTime);
  if (item.returnFromTime && item.returnToTime) description = [description, interval(item.returnShiftDate, item.returnFromTime, item.returnToTime)].filter(Boolean).join(" → ");
  if (item.vacationDays?.length) description = vacationRanges(item.vacationDays);
  if (item.kind === "swap_created" && item.counterpartyName) description = [item.counterpartyName, description].filter(Boolean).join(" · ");
  description = [description, item.shopName, item.scheduleName].filter(Boolean).join(" · ");
  const iconKind = item.kind.startsWith("swap_") ? "swap" : item.kind === "availability_updated" ? "availability" : item.kind.startsWith("urlop_") || item.kind.startsWith("day_off_") ? "calendar" : "correction";
  const tone = item.requiresApproval ? "pending" : ["rejected", "cancelled", "expired"].includes(item.status) ? "neutral" : ["approved", "accepted", "updated"].includes(item.status) ? "success" : "open";
  return { title: titles[item.kind], description, localizedStatus: statuses[item.status] ?? t("Unknown status"), iconKind, tone,
    time: dateTimeFormat("en-GB", { hour: "2-digit", minute: "2-digit", hourCycle: "h23", timeZone: "Europe/Warsaw" }).format(new Date(item.occurredAtUtc)),
    dateTimeLabel: dateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Warsaw" }).format(new Date(item.occurredAtUtc)),
  };
}
