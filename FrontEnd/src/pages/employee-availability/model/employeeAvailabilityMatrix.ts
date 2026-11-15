import type { EmployeeAvailabilityGroup, EmployeeAvailabilityMember } from "@entities/employee-availability";
import type { AvailabilityKind } from "@entities/availability-groups/model/types";
import { AVAILABILITY_KIND_ANY, AVAILABILITY_KIND_INTERVAL, AVAILABILITY_KIND_NONE, AVAILABILITY_ANY_MARK, AVAILABILITY_NONE_MARK, getDaysInMonth, getAvailabilityCellKey, getAvailabilityCodeFromKind, type AvailabilityCodeParseResult } from "@entities/availability-groups/model/editor";
import { stableSerialize } from "@shared/lib/stableSerialize";

export type DayDraft = { available: boolean; intervalStr: string | null };
export type DayDraftMap = Record<number, DayDraft>;

function isAvailableKind(kind: AvailabilityKind, intervalStr?: string | null) {
  if (intervalStr?.trim()) {
    return true;
  }

  if (typeof kind === "number") {
    return kind === AVAILABILITY_KIND_ANY || kind === AVAILABILITY_KIND_INTERVAL;
  }

  const normalizedKind = kind.toUpperCase();
  return normalizedKind === "ANY" || normalizedKind === "INT" || normalizedKind === "AVAILABLE" || normalizedKind === "PREFERRED";
}

export function buildDraftFromAvailability(availability: EmployeeAvailabilityGroup): DayDraftMap {
  const daysInMonth = getDaysInMonth(availability.year, availability.month);
  const slotByDay = new Map(availability.slots.map(slot => [slot.dayOfMonth, slot]));

  return Array.from({ length: daysInMonth }, (_, index) => index + 1).reduce<DayDraftMap>((draft, dayOfMonth) => {
    const slot = slotByDay.get(dayOfMonth);
    const intervalStr = slot?.intervalStr?.trim() || null;

    draft[dayOfMonth] = {
      available: slot ? isAvailableKind(slot.kind, intervalStr) : false,
      intervalStr,
    };

    return draft;
  }, {});
}

export function buildDraftSnapshot(draft: DayDraftMap) {
  return stableSerialize(
    Object.entries(draft)
      .map(([dayOfMonth, day]) => ({
        dayOfMonth: Number(dayOfMonth),
        available: day.available,
        intervalStr: day.intervalStr ?? null,
      }))
      .sort((left, right) => left.dayOfMonth - right.dayOfMonth),
  );
}

export function buildAvailabilityDraftSource(availability: EmployeeAvailabilityGroup | null) {
  const draft = availability ? buildDraftFromAvailability(availability) : {};
  const snapshot = buildDraftSnapshot(draft);
  const sourceKey = availability
    ? `employee-availability:${availability.id}:${availability.year}:${availability.month}:${snapshot}`
    : "employee-availability:none";

  return {
    draft,
    snapshot,
    sourceKey,
  };
}

export function buildSaveSlots(availability: EmployeeAvailabilityGroup, draft: DayDraftMap) {
  const daysInMonth = getDaysInMonth(availability.year, availability.month);

  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;
    const day = draft[dayOfMonth];
    const intervalStr = day?.available ? day.intervalStr : null;
    const kind = !day?.available
      ? AVAILABILITY_KIND_NONE
      : intervalStr
        ? AVAILABILITY_KIND_INTERVAL
        : AVAILABILITY_KIND_ANY;

    return {
      dayOfMonth,
      kind,
      intervalStr,
    };
  });
}


export function buildEmployeeAvailabilityColumns(members: EmployeeAvailabilityMember[], currentEmployeeId: number) {
  const unique = [...new Map(members.map(member => [member.employeeId, member])).values()];
  return unique.sort((a, b) => Number(b.employeeId === currentEmployeeId) - Number(a.employeeId === currentEmployeeId)
    || a.displayOrder - b.displayOrder || (a.firstName ?? "").localeCompare(b.firstName ?? "")
    || (a.lastName ?? "").localeCompare(b.lastName ?? "") || a.employeeId - b.employeeId);
}

export function buildEmployeeAvailabilityCells(members: EmployeeAvailabilityMember[], year: number, month: number) {
  const cells: Record<string, string> = {};
  const days = getDaysInMonth(year, month);
  for (const member of members) {
    for (const slot of member.slots ?? []) {
      if (!Number.isInteger(slot.dayOfMonth) || slot.dayOfMonth < 1 || slot.dayOfMonth > days) continue;
      const key = getAvailabilityCellKey(member.employeeId, slot.dayOfMonth);
      cells[key] = getAvailabilityCodeFromKind(slot.kind, slot.intervalStr);
    }
  }
  return cells;
}

export function getContiguousAvailabilityDays(anchor: number, target: number, daysInMonth: number) {
  if (![anchor, target, daysInMonth].every(Number.isInteger) || daysInMonth < 1) return [];
  const first = Math.max(1, Math.min(anchor, target));
  const last = Math.min(daysInMonth, Math.max(anchor, target));
  return Array.from({ length: Math.max(0, last - first + 1) }, (_, index) => first + index);
}

export function applyEmployeeAvailabilityValue(draft: DayDraftMap, selectedDays: readonly number[], parsed: AvailabilityCodeParseResult): DayDraftMap {
  const next = { ...draft };
  const isNone = parsed.normalizedCode === AVAILABILITY_NONE_MARK && !parsed.intervalStr;
  for (const day of selectedDays) {
    if (!Number.isInteger(day) || !Object.hasOwn(draft, day)) continue;
    next[day] = isNone ? { available: false, intervalStr: null } : {
      available: true,
      intervalStr: parsed.normalizedCode === AVAILABILITY_ANY_MARK ? null : parsed.intervalStr ?? parsed.normalizedCode,
    };
  }
  return next;
}
