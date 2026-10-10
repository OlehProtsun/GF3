import { describe, expect, it } from "vitest";
import type { ShiftSwap } from "@entities/shift-swaps";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import {
  buildEmployeeNotificationItems,
  employeeNotificationRetentionMs,
  formatEmployeeNotificationExpiry,
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
    currentEmployeeWorkDaysBefore: 0,
    currentEmployeeWorkDaysAfter: 1,
    currentEmployeeFreeDaysBefore: 31,
    currentEmployeeFreeDaysAfter: 30,
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
        createSwap({ id: 3, isManagerCreated: false, isCreatedByCurrentEmployee: true, createdAtUtc: "2026-05-10T12:00:00.000Z" }),
      ],
      [],
      [],
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
      [],
      [],
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
      [],
      [],
      TEST_NOW_MS,
    );

    expect(items[0]?.id).toBe("open-shift:2");
  });

  it("requires an authorized open snapshot and never revives accepted or removed offers", () => {
    expect(buildEmployeeNotificationItems([createNotification()], [], [], [], TEST_NOW_MS)).toEqual([]);
    expect(buildEmployeeNotificationItems([createNotification()], [createSwap({status: "accepted"})], [], [], TEST_NOW_MS)).toEqual([]);
  });

  it("reading one open shift does not mark another shift in the same schedule read", () => {
    const items = buildEmployeeNotificationItems([], [createSwap(), createSwap({id: 2})], [], [], TEST_NOW_MS);
    const read = new Set(items[0]!.readIds);
    expect(isEmployeeNotificationRead(items[0]!, read)).toBe(true);
    expect(isEmployeeNotificationRead(items[1]!, read)).toBe(false);
  });

  it("hides open shift notifications after five days", () => {
    const nowMs = Date.parse("2026-05-18T12:00:00.000Z");
    const items = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "recent-live",
          shiftSwapId: 2,
          occurredAtUtc: "2026-05-14T12:00:00.000Z",
        }),
        createNotification({
          id: "expired-live",
          shiftSwapId: 3,
          occurredAtUtc: "2026-05-10T11:59:59.000Z",
        }),
      ],
      [
        createSwap({ id: 2, createdAtUtc: "2026-05-14T12:00:00.000Z" }),
        createSwap({ id: 3, createdAtUtc: "2026-05-10T11:59:59.000Z" }),
      ],
      [],
      [],
      nowMs,
    );

    expect(items.map(item => item.id)).toEqual(["open-shift:2"]);
  });

  it("marks alert and swap targets unread only for unread open shift events", () => {
    const openSwap = createSwap({ id: 4 });
    const unreadTargets = getUnreadEmployeeNotificationTargets([], [openSwap], [], [], new Set(), TEST_NOW_MS);
    const readTargets = getUnreadEmployeeNotificationTargets(
      [createNotification({ id: "open-shift:4", shiftSwapId: 4 })],
      [openSwap],
      [],
      [],
      new Set(["open-shift:4"]),
      TEST_NOW_MS,
    );

    expect(unreadTargets).toEqual({ alerts: true, availability: false, schedule: false, swap: true });
    expect(readTargets).toEqual({ alerts: false, availability: false, schedule: false, swap: false });
  });

  it("builds schedule, availability, public swap and accepted-own-swap notifications", () => {
    const schedule: EmployeeSchedule = {
      id: 21,
      containerId: 2,
      containerName: "Main container",
      shopId: 3,
      shopName: "Central shop",
      name: "June schedule",
      publishedAtUtc: "2026-05-10T10:00:00.000Z",
      year: 2026,
      month: 6,
      publicationStatus: "public",
      slots: [],
    };
    const availability: EmployeeAvailabilityGroup = {
      id: 31,
      name: "June availability",
      year: 2026,
      month: 6,
      visibleFromUtc: "2026-05-10T08:00:00.000Z",
      visibleToUtc: "2026-05-17T08:00:00.000Z",
      canSubmit: true,
      isEditLocked: false,
      slots: [],
    };
    const publicSwap = createSwap({
      id: 9,
      isManagerCreated: false,
      fromEmployeeName: "Alice Brown",
    });
    const acceptedOwnSwap = createSwap({
      id: 10,
      status: "accepted",
      isManagerCreated: false,
      isCreatedByCurrentEmployee: true,
      acceptedByEmployeeName: "Bob Stone",
      acceptedAtUtc: "2026-05-10T11:30:00.000Z",
    });

    const items = buildEmployeeNotificationItems(
      [
        createNotification({
          id: "schedule-public:21",
          kind: "schedule",
          reason: "manager-schedule-published",
          graphId: 21,
          scheduleId: 21,
          shiftSwapId: null,
        }),
        createNotification({
          id: "availability-public:31:current",
          kind: "availability",
          reason: "manager-availability-published",
          availabilityId: 31,
          graphId: null,
          scheduleId: null,
          shiftSwapId: null,
        }),
        createNotification({
          id: "swap-accepted:10",
          reason: "employee-swap-accepted",
          shiftSwapId: 10,
          occurredAtUtc: "2026-05-10T11:30:00.000Z",
        }),
      ],
      [publicSwap, acceptedOwnSwap],
      [schedule],
      [availability],
      TEST_NOW_MS,
    );

    expect(items.map(item => item.id)).toEqual([
      "swap-accepted:10",
      "schedule-public:21:2026-05-10T10:00:00.000Z",
      "swap-public:9",
      "availability-public:31:2026-05-10T08:00:00.000Z",
    ]);
    expect(items.map(item => item.title)).toEqual(expect.arrayContaining([
      "New schedule published",
      "New availability published",
      "Shift swap available",
      "Your shift was accepted",
    ]));

    expect(getUnreadEmployeeNotificationTargets(
      [],
      [publicSwap, acceptedOwnSwap],
      [schedule],
      [availability],
      new Set(),
      TEST_NOW_MS,
    )).toEqual({ alerts: true, availability: true, schedule: true, swap: true });
  });


  it("formats a live deletion countdown and expires all snapshot notification types", () => {
    const fiveHoursTwelveMinutes = (5 * 60 + 12) * 60 * 1000;
    const occurredAtUtc = new Date(
      TEST_NOW_MS - employeeNotificationRetentionMs + fiveHoursTwelveMinutes,
    ).toISOString();

    expect(formatEmployeeNotificationExpiry(occurredAtUtc, TEST_NOW_MS)).toBe(
      "This notification will be deleted in 5h 12m.",
    );

    const expiredSchedule: EmployeeSchedule = {
      id: 41,
      containerId: 2,
      containerName: "Main container",
      shopId: 3,
      shopName: "Central shop",
      name: "Expired schedule",
      year: 2026,
      month: 5,
      publicationStatus: "public",
      lastUpdatedAtUtc: "2026-05-02T11:59:59.000Z",
      slots: [],
    };
    const expiredAvailability: EmployeeAvailabilityGroup = {
      id: 42,
      name: "Expired availability",
      year: 2026,
      month: 5,
      visibleFromUtc: "2026-05-02T11:59:59.000Z",
      visibleToUtc: null,
      canSubmit: false,
      isEditLocked: false,
      slots: [],
    };

    expect(buildEmployeeNotificationItems(
      [],
      [],
      [expiredSchedule],
      [expiredAvailability],
      TEST_NOW_MS,
    )).toEqual([]);
  });
  it("recognizes only manager open shift realtime events", () => {
    expect(isOpenShiftPostedNotification(createNotification())).toBe(true);
    expect(isOpenShiftPostedNotification(createNotification({ reason: "manager-schedule-updated" }))).toBe(false);
    expect(isOpenShiftPostedNotification(createNotification({ kind: "schedule" }))).toBe(false);
  });
});

 describe("notification expiration regressions", () => {
  it("expires all notification types exactly five days after the source event", () => {
    const date = new Date(TEST_NOW_MS - 5 * 86_400_000).toISOString();
    const schedule = {id: 21, name: "Schedule", year: 2026, month: 5, publishedAtUtc: date, lastUpdatedAtUtc: new Date(TEST_NOW_MS).toISOString()} as EmployeeSchedule;
    const availability = {id: 31, name: "Availability", year: 2026, month: 5, visibleFromUtc: date} as EmployeeAvailabilityGroup;
    const swaps = [createSwap({createdAtUtc: date}), createSwap({id: 2, isManagerCreated: false, createdAtUtc: date}), createSwap({id: 3, status: "accepted", isCreatedByCurrentEmployee: true, acceptedAtUtc: date})];
    expect(buildEmployeeNotificationItems([], swaps, [schedule], [availability], TEST_NOW_MS - 1)).toHaveLength(5);
    expect(buildEmployeeNotificationItems([], swaps, [schedule], [availability], TEST_NOW_MS)).toEqual([]);
    expect(getUnreadEmployeeNotificationTargets([], swaps, [schedule], [availability], new Set(), TEST_NOW_MS)).toEqual({alerts: false, schedule: false, availability: false, swap: false});
  });
  it("rejects missing and invalid dates instead of keeping notifications forever", () => {
    const schedules = [undefined, null, "invalid"].map(publishedAtUtc => ({id: 1, name: "Schedule", year: 2026, month: 5, publishedAtUtc}) as EmployeeSchedule);
    expect(buildEmployeeNotificationItems([], [], schedules, [], TEST_NOW_MS)).toEqual([]);
  });
  it("republished schedules are unread and retain their publication date after an edit", () => {
    const publishedAtUtc = new Date(TEST_NOW_MS - 1000).toISOString();
    const schedule = {id: 21, name: "Schedule", year: 2026, month: 5, publishedAtUtc, lastUpdatedAtUtc: new Date(TEST_NOW_MS).toISOString()} as EmployeeSchedule;
    const [item] = buildEmployeeNotificationItems([], [], [schedule], [], TEST_NOW_MS);
    expect(item!.occurredAtUtc).toBe(publishedAtUtc);
    expect(isEmployeeNotificationRead(item!, new Set(["schedule-public:21", "schedule-public:21:old"]))).toBe(false);
  });
 });
