import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  employeeNotificationReadStateEventName,
  getEmployeeNotificationReadStorageKey,
  getEmployeeOpenShiftNotificationId,
  readEmployeeNotificationIds,
  writeEmployeeNotificationIds,
} from "./employeeNotificationReadState";

describe("employee notification read state", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("uses per-account storage keys and stable open-shift ids", () => {
    expect(getEmployeeNotificationReadStorageKey(" employee.one ")).toBe("gf3.employee-notifications.read.employee.one");
    expect(getEmployeeNotificationReadStorageKey("")).toBe("gf3.employee-notifications.read.employee");
    expect(getEmployeeOpenShiftNotificationId(15)).toBe("open-shift:15");
    expect(getEmployeeOpenShiftNotificationId(null)).toBe("open-shift:current");
  });

  it("persists only string ids and dispatches a read-state event", () => {
    const listener = vi.fn();
    const storageKey = getEmployeeNotificationReadStorageKey("worker");
    window.addEventListener(employeeNotificationReadStateEventName, listener);

    writeEmployeeNotificationIds(storageKey, new Set(["open-shift:1", "open-shift:2"]));

    expect(readEmployeeNotificationIds(storageKey)).toEqual(new Set(["open-shift:1", "open-shift:2"]));
    expect(listener).toHaveBeenCalledTimes(1);

    window.localStorage.setItem(storageKey, JSON.stringify(["open-shift:3", 10, null]));

    expect(readEmployeeNotificationIds(storageKey)).toEqual(new Set(["open-shift:3"]));
    window.removeEventListener(employeeNotificationReadStateEventName, listener);
  });

  it("recovers from corrupt localStorage data", () => {
    const storageKey = getEmployeeNotificationReadStorageKey("worker");
    window.localStorage.setItem(storageKey, "{bad-json");

    expect(readEmployeeNotificationIds(storageKey)).toEqual(new Set());
  });
});
