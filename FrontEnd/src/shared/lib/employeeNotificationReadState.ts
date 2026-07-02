const readStoragePrefix = "gf3.employee-notifications.read";
const readStateStorageVersion = 2;
const maxStoredReadIds = 300;
const readStateRetentionMs = 7 * 24 * 60 * 60 * 1000;

export const employeeNotificationReadStateEventName = "gf3:employee-notifications-read-state-changed";

export function getEmployeeNotificationReadStorageKey(username: string | null | undefined) {
  return `${readStoragePrefix}.${username?.trim() || "employee"}`;
}

export function getEmployeeNotificationReadStorageKeys(
  username: string | null | undefined,
  employeeId?: number | null,
) {
  const legacyKey = getEmployeeNotificationReadStorageKey(username);
  const normalizedUsername = username?.trim().toLowerCase() || "employee";
  const normalizedUsernameKey = `${readStoragePrefix}.${normalizedUsername}`;
  const employeeKey = employeeId && employeeId > 0
    ? `${readStoragePrefix}.employee-${employeeId}`
    : legacyKey;

  return [...new Set([employeeKey, legacyKey, normalizedUsernameKey])];
}

export function getEmployeeOpenShiftNotificationId(shiftSwapId: number | string | null | undefined) {
  return `open-shift:${shiftSwapId ?? "current"}`;
}

export function getEmployeeOpenShiftScheduleNotificationId(scheduleId: number | string | null | undefined) {
  return `open-shift-schedule:${scheduleId ?? "current"}`;
}

export function getEmployeeScheduleNotificationId(scheduleId: number | string | null | undefined) {
  return `schedule-public:${scheduleId ?? "current"}`;
}

export function getEmployeeAvailabilityNotificationId(
  availabilityId: number | string | null | undefined,
  visibleFromUtc?: string | null,
) {
  return `availability-public:${availabilityId ?? "current"}:${visibleFromUtc ?? "current"}`;
}

export function getEmployeeSwapNotificationId(shiftSwapId: number | string | null | undefined) {
  return `swap-public:${shiftSwapId ?? "current"}`;
}

export function getEmployeeSwapAcceptedNotificationId(shiftSwapId: number | string | null | undefined) {
  return `swap-accepted:${shiftSwapId ?? "current"}`;
}

function isPersistentNotificationId(id: string) {
  return id.startsWith("schedule-public:") || id.startsWith("availability-public:");
}

function isFreshReadRecord(id: string, readAtUtc: unknown, nowMs: number) {
  if (isPersistentNotificationId(id)) {
    return true;
  }

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
  return typeof id === "string" && isFreshReadRecord(id, readAtUtc, nowMs) ? id : null;
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
export function readEmployeeNotificationIdsForAccount(
  username: string | null | undefined,
  employeeId?: number | null,
  nowMs = Date.now(),
) {
  return getEmployeeNotificationReadStorageKeys(username, employeeId).reduce<Set<string>>((ids, storageKey) => {
    readEmployeeNotificationIds(storageKey, nowMs).forEach(id => ids.add(id));
    return ids;
  }, new Set<string>());
}

export function writeEmployeeNotificationIdsForAccount(
  username: string | null | undefined,
  employeeId: number | null | undefined,
  ids: Set<string>,
  now = new Date(),
) {
  if (typeof window === "undefined") {
    return;
  }

  const readAtUtc = now.toISOString();
  const items = [...ids]
    .filter(id => id.trim().length > 0)
    .slice(-maxStoredReadIds)
    .map(id => ({ id, readAtUtc }));
  const value = JSON.stringify({ version: readStateStorageVersion, items });

  getEmployeeNotificationReadStorageKeys(username, employeeId).forEach(storageKey => {
    window.localStorage.setItem(storageKey, value);
  });
  window.dispatchEvent(new Event(employeeNotificationReadStateEventName));
}
