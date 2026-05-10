const readStoragePrefix = "gf3.employee-notifications.read";

export const employeeNotificationReadStateEventName = "gf3:employee-notifications-read-state-changed";

export function getEmployeeNotificationReadStorageKey(username: string | null | undefined) {
  return `${readStoragePrefix}.${username?.trim() || "employee"}`;
}

export function getEmployeeOpenShiftNotificationId(shiftSwapId: number | string | null | undefined) {
  return `open-shift:${shiftSwapId ?? "current"}`;
}

export function readEmployeeNotificationIds(storageKey: string) {
  if (typeof window === "undefined") {
    return new Set<string>();
  }

  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? "[]");
    return new Set(Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === "string") : []);
  } catch {
    return new Set<string>();
  }
}

export function writeEmployeeNotificationIds(storageKey: string, ids: Set<string>) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(storageKey, JSON.stringify([...ids].slice(-300)));
  window.dispatchEvent(new Event(employeeNotificationReadStateEventName));
}
