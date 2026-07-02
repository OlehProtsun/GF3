import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  employeeNotificationReadStateEventName,
  getEmployeeAvailabilityNotificationId,
  getEmployeeNotificationReadStorageKey,
  getEmployeeNotificationReadStorageKeys,
  getEmployeeOpenShiftNotificationId,
  getEmployeeOpenShiftScheduleNotificationId,
  getEmployeeScheduleNotificationId,
  getEmployeeSwapAcceptedNotificationId,
  getEmployeeSwapNotificationId,
  readEmployeeNotificationIds,
  readEmployeeNotificationIdsForAccount,
  writeEmployeeNotificationIds,
  writeEmployeeNotificationIdsForAccount,
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
    expect(getEmployeeOpenShiftScheduleNotificationId(10)).toBe("open-shift-schedule:10");
    expect(getEmployeeScheduleNotificationId(10)).toBe("schedule-public:10");
    expect(getEmployeeAvailabilityNotificationId(12, "2026-05-10T08:00:00.000Z"))
      .toBe("availability-public:12:2026-05-10T08:00:00.000Z");
    expect(getEmployeeSwapNotificationId(13)).toBe("swap-public:13");
    expect(getEmployeeSwapAcceptedNotificationId(13)).toBe("swap-accepted:13");
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

  it("drops timestamped read ids after seven days", () => {
    const storageKey = getEmployeeNotificationReadStorageKey("worker");
    const nowMs = Date.parse("2026-05-18T12:00:00.000Z");
    window.localStorage.setItem(storageKey, JSON.stringify({
      version: 2,
      items: [
        { id: "open-shift:old", readAtUtc: "2026-05-10T11:59:59.000Z" },
        { id: "open-shift:fresh", readAtUtc: "2026-05-12T12:00:00.000Z" },
        { id: "open-shift:legacy" },
        { id: "schedule-public:10", readAtUtc: "2026-05-01T00:00:00.000Z" },
      ],
    }));

    expect(readEmployeeNotificationIds(storageKey, nowMs)).toEqual(new Set([
      "open-shift:fresh",
      "open-shift:legacy",
      "schedule-public:10",
    ]));
  });

  it("recovers from corrupt localStorage data", () => {
    const storageKey = getEmployeeNotificationReadStorageKey("worker");
    window.localStorage.setItem(storageKey, "{bad-json");

    expect(readEmployeeNotificationIds(storageKey)).toEqual(new Set());
  });

  it("uses employee id persistence and migrates legacy username read state", () => {
    const legacyKey = getEmployeeNotificationReadStorageKey("OPR");
    window.localStorage.setItem(legacyKey, JSON.stringify({
      version: 2,
      items: [{ id: "schedule-public:10", readAtUtc: "2026-05-01T00:00:00.000Z" }],
    }));

    expect(getEmployeeNotificationReadStorageKeys("OPR", 42)).toEqual([
      "gf3.employee-notifications.read.employee-42",
      "gf3.employee-notifications.read.OPR",
      "gf3.employee-notifications.read.opr",
    ]);
    expect(readEmployeeNotificationIdsForAccount("OPR", 42)).toEqual(new Set(["schedule-public:10"]));

    writeEmployeeNotificationIdsForAccount(
      "OPR",
      42,
      new Set(["schedule-public:10", "swap-public:7"]),
      new Date("2026-05-10T12:00:00.000Z"),
    );

    const nowMs = Date.parse("2026-05-10T12:00:00.000Z");
    expect(readEmployeeNotificationIds("gf3.employee-notifications.read.employee-42", nowMs)).toEqual(
      new Set(["schedule-public:10", "swap-public:7"]),
    );
    expect(readEmployeeNotificationIds(legacyKey, nowMs)).toEqual(
      new Set(["schedule-public:10", "swap-public:7"]),
    );
  });
});
