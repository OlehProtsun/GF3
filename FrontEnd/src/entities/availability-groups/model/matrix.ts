import type { AvailabilityGroupItem, AvailabilityGroupMember, AvailabilityKind, AvailabilitySlot } from "./types";

export const AVAILABILITY_ANY_MARK = "+";
export const AVAILABILITY_NONE_MARK = "-";
export const AVAILABILITY_KIND_ANY = 0;
export const AVAILABILITY_KIND_NONE = 1;
export const AVAILABILITY_KIND_INTERVAL = 2;

export type AvailabilityMatrixColumn = {
  employeeId: number;
  memberId?: number | null;
  label: string;
};

export type AvailabilityMatrixCellMap = Record<string, string>;

export type AvailabilityCodeParseResult = {
  normalizedCode: string;
  kind: number;
  intervalStr: string | null;
};

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
  const weekdayLabels = ["su", "mo", "tu", "we", "th", "fr", "sa"];

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

  if (normalizedKind === AVAILABILITY_KIND_ANY) {
    return AVAILABILITY_ANY_MARK;
  }

  if (normalizedKind === AVAILABILITY_KIND_INTERVAL) {
    return (intervalStr ?? "").trim() || AVAILABILITY_NONE_MARK;
  }

  return AVAILABILITY_NONE_MARK;
}

function tryNormalizeInterval(value: string) {
  const parts = value
    .split("-")
    .map(part => part.trim())
    .filter(Boolean);

  if (parts.length !== 2) {
    return null;
  }

  const start = normalizeTimeSegment(parts[0]);
  const end = normalizeTimeSegment(parts[1]);

  if (!start || !end) {
    return null;
  }

  if (end.totalMinutes <= start.totalMinutes) {
    return null;
  }

  return `${start.label} - ${end.label}`;
}

function normalizeTimeSegment(value: string) {
  const match = value.match(/^(\d{1,2}):(\d{2})$/);
  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);

  if (!Number.isInteger(hours) || !Number.isInteger(minutes)) {
    return null;
  }

  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null;
  }

  return {
    totalMinutes: hours * 60 + minutes,
    label: `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`,
  };
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
  if (!normalizedInterval) {
    return {
      ok: false,
      error: "Use +, -, or a valid time range like 08:00 - 16:00.",
    };
  }

  return {
    ok: true,
    value: {
      normalizedCode: normalizedInterval,
      kind: AVAILABILITY_KIND_INTERVAL,
      intervalStr: normalizedInterval,
    },
  };
}

export function buildAvailabilityColumns(
  members: AvailabilityGroupMember[],
  employeeNameById: Map<number, string>
): AvailabilityMatrixColumn[] {
  return [...members]
    .map(member => ({
      employeeId: member.employeeId,
      memberId: member.id,
      label: employeeNameById.get(member.employeeId) ?? `Employee #${member.employeeId}`,
    }))
    .sort((left, right) => left.label.localeCompare(right.label));
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
  const memberById = new Map<number, { employeeId: number; memberId: number }>();

  items.forEach(item => {
    if (!memberById.has(item.memberId)) {
      memberById.set(item.memberId, {
        employeeId: item.employeeId,
        memberId: item.memberId,
      });
    }
  });

  return [...memberById.values()]
    .map(member => ({
      employeeId: member.employeeId,
      memberId: member.memberId,
      label: employeeNameById.get(member.employeeId) ?? `Employee #${member.employeeId}`,
    }))
    .sort((left, right) => left.label.localeCompare(right.label));
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
      const trimmedValue = value.trim();

      if (!trimmedValue || trimmedValue === AVAILABILITY_NONE_MARK) {
        summary.none += 1;
      } else if (trimmedValue === AVAILABILITY_ANY_MARK) {
        summary.any += 1;
      } else {
        summary.interval += 1;
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
