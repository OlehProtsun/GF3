const readStoragePrefix = "gf3.employee-notifications.read";
const readStateStorageVersion = 2;
const maxStoredReadIds = 300;
const readStateRetentionMs = 7 * 24 * 60 * 60 * 1000;

export const employeeNotificationReadStateEventName = "gf3:employee-notifications-read-state-changed";

export function getEmployeeNotificationReadStorageKey(username: string | null | undefined) {
  return `${readStoragePrefix}.${username?.trim() || "employee"}`;
}

export function getEmployeeOpenShiftNotificationId(shiftSwapId: number | string | null | undefined) {
  return `open-shift:${shiftSwapId ?? "current"}`;
}

export function getEmployeeOpenShiftScheduleNotificationId(scheduleId: number | string | null | undefined) {
  return `open-shift-schedule:${scheduleId ?? "current"}`;
}

function isFreshReadRecord(readAtUtc: unknown, nowMs: number) {
  if (typeof readAtUtc !== "string") {
    return true;
  }

  const parsed = new Date(readAtUtc).getTime();
  return Number.isNaN(parsed) || nowMs - parsed <= readStateRetentionMs;
}

function readIdFromStoredItem(item: unknown, nowMs: number) {
  if (typeof item === "string") {
    return item;
  }

  if (!item || typeof item !== "object" || !("id" in item)) {
    return null;
  }

  const { id, readAtUtc } = item as { id?: unknown; readAtUtc?: unknown };
  return typeof id === "string" && isFreshReadRecord(readAtUtc, nowMs) ? id : null;
}

function parseStoredReadIds(parsed: unknown, nowMs: number) {
  const rawItems = Array.isArray(parsed)
    ? parsed
    : parsed && typeof parsed === "object" && "items" in parsed && Array.isArray((parsed as { items?: unknown }).items)
      ? (parsed as { items: unknown[] }).items
      : [];

  return rawItems
    .map(item => readIdFromStoredItem(item, nowMs))
    .filter((item): item is string => Boolean(item));
}

export function readEmployeeNotificationIds(storageKey: string, nowMs = Date.now()) {
  if (typeof window === "undefined") {
    return new Set<string>();
  }

  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? "[]");
    return new Set(parseStoredReadIds(parsed, nowMs));
  } catch {
    return new Set<string>();
  }
}

export function writeEmployeeNotificationIds(storageKey: string, ids: Set<string>, now = new Date()) {
  if (typeof window === "undefined") {
    return;
  }

  const readAtUtc = now.toISOString();
  const items = [...ids]
    .filter(id => id.trim().length > 0)
    .slice(-maxStoredReadIds)
    .map(id => ({ id, readAtUtc }));

  window.localStorage.setItem(storageKey, JSON.stringify({ version: readStateStorageVersion, items }));
  window.dispatchEvent(new Event(employeeNotificationReadStateEventName));
}
