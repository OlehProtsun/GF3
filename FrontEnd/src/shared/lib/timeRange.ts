const DASH_PATTERN = /[\u2012\u2013\u2014\u2015]/g;

export type NormalizedTimeSegment = {
  label: string;
  totalMinutes: number;
};

export type NormalizedTimeRange = {
  from: string;
  to: string;
  fromMinutes: number;
  toMinutes: number;
  label: string;
};

function normalizeRangeInput(value: string) {
  return value.trim().replace(DASH_PATTERN, "-");
}

export function parseFlexibleTimeSegment(value: string): NormalizedTimeSegment | null {
  const normalized = value.trim().replace(/\s+/g, "");
  if (!normalized) {
    return null;
  }

  let hours: number | null = null;
  let minutes: number | null = null;

  const separatedMatch = normalized.match(/^(\d{1,2})(?:[:.](\d{1,2}))?$/);
  if (separatedMatch) {
    hours = Number(separatedMatch[1]);
    minutes = separatedMatch[2] === undefined ? 0 : Number(separatedMatch[2]);
  } else {
    const compactMatch = normalized.match(/^(\d{3,4})$/);
    if (!compactMatch) {
      return null;
    }

    const digits = compactMatch[1];
    if (digits.length === 3) {
      hours = Number(digits.slice(0, 1));
      minutes = Number(digits.slice(1));
    } else {
      hours = Number(digits.slice(0, 2));
      minutes = Number(digits.slice(2));
    }
  }

  if (
    hours === null ||
    minutes === null ||
    !Number.isInteger(hours) ||
    !Number.isInteger(minutes) ||
    hours < 0 ||
    hours > 23 ||
    minutes < 0 ||
    minutes > 59
  ) {
    return null;
  }

  return {
    totalMinutes: hours * 60 + minutes,
    label: `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`,
  };
}

export function parseFlexibleTimeRange(value: string): NormalizedTimeRange | null {
  const normalized = normalizeRangeInput(value);
  if (!normalized) {
    return null;
  }

  const parts = normalized
    .split("-")
    .map(part => part.trim())
    .filter(Boolean);

  if (parts.length !== 2) {
    return null;
  }

  const from = parseFlexibleTimeSegment(parts[0]);
  const to = parseFlexibleTimeSegment(parts[1]);

  if (!from || !to || to.totalMinutes <= from.totalMinutes) {
    return null;
  }

  return {
    from: from.label,
    to: to.label,
    fromMinutes: from.totalMinutes,
    toMinutes: to.totalMinutes,
    label: `${from.label} - ${to.label}`,
  };
}

export function parseFlexibleTimeRangeList(value: string) {
  const normalized = normalizeRangeInput(value);
  if (!normalized) {
    return [];
  }

  const parts = normalized
    .split(",")
    .map(part => part.trim())
    .filter(Boolean);

  const ranges: NormalizedTimeRange[] = [];

  for (const part of parts) {
    const range = parseFlexibleTimeRange(part);
    if (!range) {
      return null;
    }

    ranges.push(range);
  }

  return ranges;
}
