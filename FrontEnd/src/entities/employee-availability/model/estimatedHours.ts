import { getAvailabilityCodeFromKind } from "@entities/availability-groups/model/matrix";
import { parseFlexibleTimeRange } from "@shared/lib/timeRange";
import type { EmployeeAvailabilityGroup } from "./types";

export const POTENTIAL_HOURS_PER_DAY = 8;
export const ESTIMATED_UTILIZATION = 0.60;
export const ESTIMATE_ROUNDING_HOURS = 5;

export type EstimatedHours =
  | { status: "unsubmitted" }
  | { status: "unavailable" }
  | { status: "ready"; potentialHours: number; lowerEstimate: number; upperEstimate: number };

/** The group-level slots and timestamp belong to the authenticated employee, unlike members[].slots. */
export function estimateAvailabilityHours(
  availability: Pick<EmployeeAvailabilityGroup, "year" | "month" | "slots" | "employeeLastModifiedAtUtc">,
): EstimatedHours {
  if (!availability.employeeLastModifiedAtUtc || !Number.isFinite(Date.parse(availability.employeeLastModifiedAtUtc))) {
    return { status: "unsubmitted" };
  }
  const { year, month, slots } = availability;
  if (!Number.isInteger(year) || year < 1 || year > 9999 || !Number.isInteger(month) || month < 1 || month > 12) return { status: "unavailable" };
  const leapYear = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
  const daysInMonth = [31, leapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1];
  if (slots.length !== daysInMonth) return { status: "unavailable" };
  const days = new Set<number>();
  let potentialMinutes = 0;
  for (const slot of slots) {
    if (!Number.isInteger(slot.dayOfMonth) || slot.dayOfMonth < 1 || slot.dayOfMonth > daysInMonth || days.has(slot.dayOfMonth)) return { status: "unavailable" };
    days.add(slot.dayOfMonth);
    const kind = typeof slot.kind === "string" ? slot.kind.toUpperCase() : slot.kind;
    if (![0, 1, 2, "ANY", "AVAILABLE", "NONE", "UNAVAILABLE", "INT", "PREFERRED"].includes(kind)) return { status: "unavailable" };
    if ((kind === 2 || kind === "INT" || kind === "PREFERRED") && !slot.intervalStr?.trim()) return { status: "unavailable" };
    const value = slot.intervalStr?.trim() || getAvailabilityCodeFromKind(slot.kind, slot.intervalStr);
    if (value === "-") continue;
    if (value === "+") {
      potentialMinutes += POTENTIAL_HOURS_PER_DAY * 60;
      continue;
    }
    const interval = parseFlexibleTimeRange(value);
    if (!interval) return { status: "unavailable" };
    potentialMinutes += Math.min(POTENTIAL_HOURS_PER_DAY * 60, interval.toMinutes - interval.fromMinutes);
  }
  const potentialHours = potentialMinutes / 60;
  const rounded = (hours: number) => Math.round(hours / ESTIMATE_ROUNDING_HOURS) * ESTIMATE_ROUNDING_HOURS;
  return {
    status: "ready",
    potentialHours,
    lowerEstimate: rounded(potentialHours * ESTIMATED_UTILIZATION),
    upperEstimate: potentialHours === 0 ? 0 : Math.max(ESTIMATE_ROUNDING_HOURS, rounded(potentialHours)),
  };
}
