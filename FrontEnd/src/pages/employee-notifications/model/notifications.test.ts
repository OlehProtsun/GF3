import { describe, expect, it } from "vitest";
import type { ShiftSwap } from "@entities/shift-swaps";
import {
  buildEmployeeNotificationItems,
  getUnreadEmployeeNotificationTargets,
  isEmployeeNotificationRead,
  isOpenShiftPostedNotification,
  type EmployeeRealtimeNotificationLike,
} from "./notifications";

const TEST_NOW_MS = Date.parse("2026-05-10T12:00:00.000Z");

function createSwap(overrides: Partial<ShiftSwap> = {}): ShiftSwap {
  return {
    id: 1,
    scheduleId: 10,
    scheduleSlotId: 100,
    scheduleName: "Morning schedule",
    containerName: "Main container",
    shopName: "Central shop",
    year: 2026,
    month: 5,
    dayOfMonth: 10,
    fromTime: "09:00",
    toTime: "14:00",
    fromEmployeeId: null,
    fromEmployeeName: "Manual column",
    targetEmployeeId: null,
    targetEmployeeName: null,
    acceptedByEmployeeId: null,
    acceptedByEmployeeName: null,
    visibility: "public",
    status: "open",
    createdAtUtc: "2026-05-10T09:00:00.000Z",
    acceptedAtUtc: null,
    shiftHours: 5,
    currentEmployeeHoursBefore: 0,
    currentEmployeeHoursAfter: 5,
    fromEmployeeHoursBefore: 0,
    fromEmployeeHoursAfter: 0,
    isManagerCreated: true,
    manualColumnId: 55,
    manualColumnName: "Open shift",
    isCreatedByCurrentEmployee: false,
    isScheduleLocked: false,
    canAccept: true,
    canCancel: false,
    ...overrides,
  };
}

function createNotification(overrides: Partial<EmployeeRealtimeNotificationLike> = {}): EmployeeRealtimeNotificationLike {
  return {
    id: "open-shift:1",
    kind: "shiftSwap",
    reason: "manager-manual-shift-offer-created",
    occurredAtUtc: "2026-05-10T10:00:00.000Z",
    scheduleId: 10,
    graphId: 10,
    shiftSwapId: 1,
    ...overrides,
  };
}

describe("employee notification model", () => {
  it("keeps only manager-created open shifts and ignores schedule-change noise", () => {
    const items = buildEmployeeNotificationItems(
      [
        createNotification(),
        createNotification({
          id: "schedule:10",
          kind: "schedule",
          reason: "manager-schedule-updated",
          shiftSwapId: null,
        }),
      ],
      [
        createSwap(),
        createSwap({ id: 2, status: "accepted", createdAtUtc: "2026-05-10T11:00:00.000Z" }),
        createSwap({ id: 3, isManagerCreated: false, createdAtUtc: "2026-05-10T12:00:00.000Z" }),
      ],
      TEST_NOW_MS,
    );

    expect(items).toHaveLength(1);
    expect(items[0]).toMatchObject({
      id: "open-shift:1",
      title: "Open shift posted",
      actionPath: "/swap",
    });
    expect(items[0]?.body).toContain("Morning schedule");
  });

  it("deduplicates live and snapshot notifications and sorts newest first", () => {
    const items = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "open-shift:2",
          shiftSwapId: 2,
          scheduleId: 11,
          occurredAtUtc: "2026-05-10T12:00:00.000Z",
        }),
        createNotification({
          id: "open-shift:1",
          shiftSwapId: 1,
          occurredAtUtc: "2026-05-10T10:00:00.000Z",
        }),
      ],
      [
        createSwap({ id: 1, createdAtUtc: "2026-05-10T09:00:00.000Z" }),
        createSwap({ id: 2, scheduleId: 11, createdAtUtc: "2026-05-10T12:00:00.000Z" }),
      ],
      TEST_NOW_MS,
    );

    expect(items.map(item => item.id)).toEqual(["open-shift:2", "open-shift:1"]);
  });

  it("falls back to current open shift when realtime event has no shift id", () => {
    const items = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "open-shift:current",
          shiftSwapId: null,
          scheduleId: 10,
          occurredAtUtc: "2026-05-10T10:30:00.000Z",
        }),
      ],
      [
        createSwap({ id: 1, scheduleId: 10, createdAtUtc: "2026-05-10T08:00:00.000Z" }),
        createSwap({ id: 2, scheduleId: 10, createdAtUtc: "2026-05-10T09:00:00.000Z" }),
      ],
      TEST_NOW_MS,
    );

    expect(items[0]?.id).toBe("open-shift:2");
  });

  it("keeps read state when a schedule-only live event resolves to a swap snapshot after login", () => {
    const liveItems = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "open-shift-schedule:10",
          shiftSwapId: null,
          scheduleId: 10,
          graphId: 10,
        }),
      ],
      [],
      Date.parse("2026-05-10T12:00:00.000Z"),
    );
    const readIds = new Set(liveItems[0]?.readIds ?? []);
    const snapshotItems = buildEmployeeNotificationItems(
      [],
      [createSwap({ id: 7, scheduleId: 10 })],
      Date.parse("2026-05-10T12:00:00.000Z"),
    );

    expect(liveItems[0]?.id).toBe("open-shift-schedule:10");
    expect(snapshotItems[0]?.id).toBe("open-shift:7");
    expect(isEmployeeNotificationRead(snapshotItems[0]!, readIds)).toBe(true);
    expect(getUnreadEmployeeNotificationTargets([], [createSwap({ id: 7, scheduleId: 10 })], readIds, TEST_NOW_MS))
      .toEqual({ alerts: false, swap: false });
  });

  it("hides open shift notifications after seven days", () => {
    const nowMs = Date.parse("2026-05-18T12:00:00.000Z");
    const items = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "recent-live",
          shiftSwapId: 2,
          occurredAtUtc: "2026-05-12T12:00:00.000Z",
        }),
        createNotification({
          id: "expired-live",
          shiftSwapId: 3,
          occurredAtUtc: "2026-05-10T11:59:59.000Z",
        }),
      ],
      [
        createSwap({ id: 2, createdAtUtc: "2026-05-12T12:00:00.000Z" }),
        createSwap({ id: 3, createdAtUtc: "2026-05-10T11:59:59.000Z" }),
      ],
      nowMs,
    );

    expect(items.map(item => item.id)).toEqual(["open-shift:2"]);
  });

  it("marks alert and swap targets unread only for unread open shift events", () => {
    const openSwap = createSwap({ id: 4 });
    const unreadTargets = getUnreadEmployeeNotificationTargets([], [openSwap], new Set(), TEST_NOW_MS);
    const readTargets = getUnreadEmployeeNotificationTargets(
      [createNotification({ id: "open-shift:4", shiftSwapId: 4 })],
      [openSwap],
      new Set(["open-shift:4"]),
      TEST_NOW_MS,
    );

    expect(unreadTargets).toEqual({ alerts: true, swap: true });
    expect(readTargets).toEqual({ alerts: false, swap: false });
  });

  it("recognizes only manager open shift realtime events", () => {
    expect(isOpenShiftPostedNotification(createNotification())).toBe(true);
    expect(isOpenShiftPostedNotification(createNotification({ reason: "manager-schedule-updated" }))).toBe(false);
    expect(isOpenShiftPostedNotification(createNotification({ kind: "schedule" }))).toBe(false);
  });
});
