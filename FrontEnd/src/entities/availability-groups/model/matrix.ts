import { getLanguage } from "@shared/i18n";
import { t } from "@shared/i18n";
import type { AvailabilityGroupItem, AvailabilityGroupMember, AvailabilityKind, AvailabilitySlot } from "./types";
import { parseFlexibleTimeRange } from "@shared/lib/timeRange";

export const AVAILABILITY_ANY_MARK = "+";
export const AVAILABILITY_NONE_MARK = "-";
export const AVAILABILITY_KIND_ANY = 0;
export const AVAILABILITY_KIND_NONE = 1;
export const AVAILABILITY_KIND_INTERVAL = 2;

export type AvailabilityMatrixColumn = {
  employeeId: number;
  memberId?: number | null;
  label: string;
  displayOrder?: number | null;
  employeeLastModifiedAtUtc?: string | null;
};

export type AvailabilityMatrixCellMap = Record<string, string>;

export type AvailabilityCodeParseResult = {
  normalizedCode: string;
  kind: number;
  intervalStr: string | null;
};

function normalizeAvailabilityDisplayOrder(displayOrder?: number | null) {
  if (Number.isInteger(displayOrder) && (displayOrder as number) >= 0) {
    return displayOrder as number;
  }

  return Number.MAX_SAFE_INTEGER;
}

export function sortAvailabilityColumns<T extends AvailabilityMatrixColumn>(columns: T[]) {
  return [...columns].sort((left, right) => {
    const orderDifference =
      normalizeAvailabilityDisplayOrder(left.displayOrder) - normalizeAvailabilityDisplayOrder(right.displayOrder);

    if (orderDifference !== 0) {
      return orderDifference;
    }

    const labelDifference = left.label.localeCompare(right.label);
    if (labelDifference !== 0) {
      return labelDifference;
    }

    return left.employeeId - right.employeeId;
  });
}

export function getAvailabilityCellKey(employeeId: number, dayOfMonth: number) {
  return `${employeeId}:${dayOfMonth}`;
}

export function clampAvailabilityMonth(month: number) {
  if (!Number.isFinite(month)) {
    return 1;
  }

  return Math.min(12, Math.max(1, Math.round(month)));
}

export function clampAvailabilityYear(year: number) {
  if (!Number.isFinite(year)) {
    return new Date().getFullYear();
  }

  return Math.min(4000, Math.max(2026, Math.round(year)));
}

export function getDaysInMonth(year: number, month: number) {
  const safeYear = clampAvailabilityYear(year);
  const safeMonth = clampAvailabilityMonth(month);

  return new Date(safeYear, safeMonth, 0).getDate();
}

function getAvailabilityDate(year: number, month: number, dayOfMonth: number) {
  const safeYear = clampAvailabilityYear(year);
  const safeMonth = clampAvailabilityMonth(month);
  const safeDayOfMonth = Math.min(getDaysInMonth(safeYear, safeMonth), Math.max(1, Math.round(dayOfMonth)));

  return new Date(safeYear, safeMonth - 1, safeDayOfMonth);
}

export function getAvailabilityWeekdayLabel(year: number, month: number, dayOfMonth: number) {
  const weekdayIndex = getAvailabilityDate(year, month, dayOfMonth).getDay();
  const weekdayLabels = getLanguage() === "pl" ? ["nd", "pn", "wt", "śr", "cz", "pt", "so"] : ["su", "mo", "tu", "we", "th", "fr", "sa"];

  return `${weekdayLabels[weekdayIndex]}.`;
}

export function isAvailabilityWeekend(year: number, month: number, dayOfMonth: number) {
  const weekdayIndex = getAvailabilityDate(year, month, dayOfMonth).getDay();

  return weekdayIndex === 0 || weekdayIndex === 6;
}

function normalizeAvailabilityKind(kind: AvailabilityKind) {
  if (typeof kind === "number") {
    return kind;
  }

  const normalizedKind = kind.toUpperCase();

  if (normalizedKind === "ANY" || normalizedKind === "AVAILABLE") {
    return AVAILABILITY_KIND_ANY;
  }

  if (normalizedKind === "NONE" || normalizedKind === "UNAVAILABLE") {
    return AVAILABILITY_KIND_NONE;
  }

  if (normalizedKind === "INT" || normalizedKind === "PREFERRED") {
    return AVAILABILITY_KIND_INTERVAL;
  }

  return AVAILABILITY_KIND_NONE;
}

export function getAvailabilityCodeFromKind(kind: AvailabilityKind, intervalStr?: string | null) {
  const normalizedKind = normalizeAvailabilityKind(kind);
  const trimmedIntervalStr = (intervalStr ?? "").trim();

  if (normalizedKind === AVAILABILITY_KIND_ANY) {
    return AVAILABILITY_ANY_MARK;
  }

  if (normalizedKind === AVAILABILITY_KIND_INTERVAL) {
    return trimmedIntervalStr || AVAILABILITY_NONE_MARK;
  }

  if (trimmedIntervalStr && trimmedIntervalStr !== AVAILABILITY_NONE_MARK) {
    return trimmedIntervalStr;
  }

  return AVAILABILITY_NONE_MARK;
}

function tryNormalizeInterval(value: string) {
  return parseFlexibleTimeRange(value)?.label ?? null;
}

function containsAlphabeticCharacter(value: string) {
  return [...value].some(character => character.toLowerCase() !== character.toUpperCase());
}

function looksLikeAvailabilityTimeAttempt(value: string) {
  return /[:.,]/.test(value) || value.includes("-");
}

export function parseAvailabilityCode(input: string):
  | { ok: true; value: AvailabilityCodeParseResult }
  | { ok: false; error: string } {
  const trimmed = input.trim();

  if (!trimmed || trimmed === AVAILABILITY_NONE_MARK) {
    return {
      ok: true,
      value: {
        normalizedCode: AVAILABILITY_NONE_MARK,
        kind: AVAILABILITY_KIND_NONE,
        intervalStr: null,
      },
    };
  }

  if (trimmed === AVAILABILITY_ANY_MARK) {
    return {
      ok: true,
      value: {
        normalizedCode: AVAILABILITY_ANY_MARK,
        kind: AVAILABILITY_KIND_ANY,
        intervalStr: null,
      },
    };
  }

  const normalizedInterval = tryNormalizeInterval(trimmed);
  if (normalizedInterval) {
    return {
      ok: true,
      value: {
        normalizedCode: normalizedInterval,
        kind: AVAILABILITY_KIND_INTERVAL,
        intervalStr: normalizedInterval,
      },
    };
  }

  if (containsAlphabeticCharacter(trimmed) || !looksLikeAvailabilityTimeAttempt(trimmed)) {
    return {
      ok: true,
      value: {
        normalizedCode: trimmed,
        kind: AVAILABILITY_KIND_NONE,
        intervalStr: trimmed,
      },
    };
  }

  return {
    ok: false,
    error: t("Use +, -, text, or a valid time range like 09:00 - 15:00."),
  };
}

export function normalizeAvailabilityCellValue(input: string) {
  const parsed = parseAvailabilityCode(input);
  return parsed.ok ? parsed.value.normalizedCode : input.trim();
}

export function buildAvailabilityColumns(
  members: AvailabilityGroupMember[],
  employeeNameById: Map<number, string>
): AvailabilityMatrixColumn[] {
  return sortAvailabilityColumns(
    [...members]
    .map(member => ({
      employeeId: member.employeeId,
      memberId: member.id,
      displayOrder: member.displayOrder,
      employeeLastModifiedAtUtc: member.employeeLastModifiedAtUtc ?? null,
      label: employeeNameById.get(member.employeeId) ?? t("Employee #{0}", member.employeeId),
    }))
  );
}

export function buildAvailabilityCellMap(
  members: AvailabilityGroupMember[],
  slots: AvailabilitySlot[]
): AvailabilityMatrixCellMap {
  const employeeIdByMemberId = new Map(members.map(member => [member.id, member.employeeId]));

  return slots.reduce<AvailabilityMatrixCellMap>((accumulator, slot) => {
    const employeeId = employeeIdByMemberId.get(slot.availabilityGroupMemberId);
    if (!employeeId) {
      return accumulator;
    }

    accumulator[getAvailabilityCellKey(employeeId, slot.dayOfMonth)] = getAvailabilityCodeFromKind(
      slot.kind,
      slot.intervalStr
    );

    return accumulator;
  }, {});
}

export function buildAvailabilityColumnsFromItems(
  items: AvailabilityGroupItem[],
  employeeNameById: Map<number, string>
): AvailabilityMatrixColumn[] {
  const memberById = new Map<
    number,
    { employeeId: number; memberId: number; displayOrder: number; employeeLastModifiedAtUtc: string | null }
  >();

  items.forEach(item => {
    if (!memberById.has(item.memberId)) {
      memberById.set(item.memberId, {
        employeeId: item.employeeId,
        memberId: item.memberId,
        displayOrder: item.displayOrder,
        employeeLastModifiedAtUtc: null,
      });
    }
  });

  return sortAvailabilityColumns(
    [...memberById.values()]
    .map(member => ({
      employeeId: member.employeeId,
      memberId: member.memberId,
      displayOrder: member.displayOrder,
      employeeLastModifiedAtUtc: member.employeeLastModifiedAtUtc,
      label: employeeNameById.get(member.employeeId) ?? t("Employee #{0}", member.employeeId),
    }))
  );
}

export function buildAvailabilityCellMapFromItems(
  items: AvailabilityGroupItem[]
): AvailabilityMatrixCellMap {
  return items.reduce<AvailabilityMatrixCellMap>((accumulator, item) => {
    accumulator[getAvailabilityCellKey(item.employeeId, item.dayOfMonth)] = getAvailabilityCodeFromKind(
      item.kind,
      item.intervalStr
    );

    return accumulator;
  }, {});
}

export function summarizeAvailabilityCellMap(cellMap: AvailabilityMatrixCellMap) {
  return Object.values(cellMap).reduce(
    (summary, value) => {
      const parsed = parseAvailabilityCode(value);

      if (!parsed.ok) {
        summary.none += 1;
      } else if (parsed.value.kind === AVAILABILITY_KIND_ANY) {
        summary.any += 1;
      } else if (parsed.value.kind === AVAILABILITY_KIND_INTERVAL) {
        summary.interval += 1;
      } else {
        summary.none += 1;
      }

      return summary;
    },
    { any: 0, none: 0, interval: 0 }
  );
}

export function sanitizeAvailabilityCellMap(
  cellMap: AvailabilityMatrixCellMap,
  employeeIds: number[],
  year: number,
  month: number
): AvailabilityMatrixCellMap {
  const selectedEmployeeIds = new Set(employeeIds);
  const daysInMonth = getDaysInMonth(year, month);

  return Object.entries(cellMap).reduce<AvailabilityMatrixCellMap>((accumulator, [key, value]) => {
    const [employeeIdValue, dayValue] = key.split(":");
    const employeeId = Number(employeeIdValue);
    const dayOfMonth = Number(dayValue);

    if (!selectedEmployeeIds.has(employeeId)) {
      return accumulator;
    }

    if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > daysInMonth) {
      return accumulator;
    }

    accumulator[key] = value;
    return accumulator;
  }, {});
}
