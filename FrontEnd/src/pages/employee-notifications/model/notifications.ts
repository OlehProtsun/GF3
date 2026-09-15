import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import type { ShiftSwap } from "@entities/shift-swaps";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import {
  getEmployeeAvailabilityNotificationId,
  getEmployeeOpenShiftNotificationId,
  getEmployeeOpenShiftScheduleNotificationId,
  getEmployeeScheduleNotificationId,
  getEmployeeSwapAcceptedNotificationId,
  getEmployeeSwapNotificationId,
} from "@shared/lib/employeeNotificationReadState";

export type NotificationTone = "schedule" | "availability" | "swap";

export type EmployeeRealtimeNotificationLike = {
  id: string;
  kind: "schedule" | "availability" | "shiftSwap";
  reason: string;
  occurredAtUtc: string;
  containerId?: number | null;
  graphId?: number | null;
  scheduleId?: number | null;
  availabilityId?: number | null;
  shiftSwapId?: number | null;
};

export type EmployeeNotificationItem = {
  id: string;
  readIds: string[];
  title: string;
  body: string;
  meta: string;
  tone: NotificationTone;
  occurredAtUtc?: string | null;
  actionPath: string;
  actionLabel: string;
};

export type EmployeeNavNotificationTarget = "alerts" | "availability" | "schedule" | "swap";

export const employeeNotificationRetentionMs = 7 * 24 * 60 * 60 * 1000;

export function getEmployeeNotificationExpiryAtMs(value?: string | null) {
  if (!value) {
    return null;
  }

  const occurredAtMs = new Date(value).getTime();
  return Number.isNaN(occurredAtMs) ? null : occurredAtMs + employeeNotificationRetentionMs;
}

export function formatEmployeeNotificationExpiry(value?: string | null, nowMs = Date.now()) {
  const expiresAtMs = getEmployeeNotificationExpiryAtMs(value);
  if (expiresAtMs === null) {
    return t("This notification will be deleted automatically after 7 days.");
  }

  const remainingMs = expiresAtMs - nowMs;
  if (remainingMs <= 0) {
    return t("This notification is being deleted.");
  }

  const totalMinutes = Math.max(1, Math.ceil(remainingMs / 60_000));
  const days = Math.floor(totalMinutes / (24 * 60));
  const hours = Math.floor((totalMinutes % (24 * 60)) / 60);
  const minutes = totalMinutes % 60;

  if (days > 0) {
    return t("This notification will be deleted in {0}d{1}.", days, hours > 0 ? ` ${hours}h` : "");
  }

  if (hours > 0) {
    return t("This notification will be deleted in {0}h{1}.", hours, minutes > 0 ? ` ${minutes}m` : "");
  }

  return t("This notification will be deleted in {0}m.", minutes);
}

const dayFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const notificationTimeFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
});

const monthFormatter = dateTimeFormat("en-GB", {
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

function formatMonth(year: number, month: number) {
  return monthFormatter.format(new Date(Date.UTC(year, month - 1, 1)));
}

export function formatSwapDay(swap: Pick<ShiftSwap, "year" | "month" | "dayOfMonth">) {
  return dayFormatter.format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
}

export function formatNotificationTime(value?: string | null) {
  if (!value) {
    return t("Current");
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? t("Just now") : notificationTimeFormatter.format(date);
}

export function isOpenShiftPostedNotification(event: EmployeeRealtimeNotificationLike) {
  return event.kind === "shiftSwap" && event.reason === "manager-manual-shift-offer-created";
}

function uniqueIds(ids: Array<string | null | undefined>) {
  return [...new Set(ids.filter((id): id is string => Boolean(id)))];
}

function getRelatedScheduleId(event: EmployeeRealtimeNotificationLike) {
  return event.scheduleId ?? event.graphId ?? null;
}

function getOpenShiftSwapReadIds(swap: Pick<ShiftSwap, "id" | "scheduleId">) {
  return uniqueIds([
    getEmployeeOpenShiftNotificationId(swap.id),
    getEmployeeOpenShiftScheduleNotificationId(swap.scheduleId),
    getEmployeeOpenShiftNotificationId(swap.scheduleId),
  ]);
}

function getOpenShiftEventReadIds(event: EmployeeRealtimeNotificationLike, swap: ShiftSwap | null) {
  const relatedScheduleId = getRelatedScheduleId(event);

  return uniqueIds([
    event.id,
    ...(swap ? getOpenShiftSwapReadIds(swap) : []),
    event.shiftSwapId != null ? getEmployeeOpenShiftNotificationId(event.shiftSwapId) : null,
    relatedScheduleId != null ? getEmployeeOpenShiftScheduleNotificationId(relatedScheduleId) : null,
    relatedScheduleId != null ? getEmployeeOpenShiftNotificationId(relatedScheduleId) : null,
    event.shiftSwapId == null && relatedScheduleId == null ? getEmployeeOpenShiftNotificationId(null) : null,
  ]);
}

export function isEmployeeNotificationRead(item: Pick<EmployeeNotificationItem, "id" | "readIds">, readIds: Set<string>) {
  return item.readIds.some(id => readIds.has(id)) || readIds.has(item.id);
}

export function isWithinEmployeeNotificationRetention(value?: string | null, nowMs = Date.now()) {
  const expiresAtMs = getEmployeeNotificationExpiryAtMs(value);
  return expiresAtMs === null || nowMs < expiresAtMs;
}

export function buildOpenShiftLiveNotification(
  event: EmployeeRealtimeNotificationLike,
  swap: ShiftSwap | null,
): EmployeeNotificationItem {
  const relatedScheduleId = getRelatedScheduleId(event);
  const readIds = getOpenShiftEventReadIds(event, swap);
  const id = swap
    ? getEmployeeOpenShiftNotificationId(swap.id)
    : event.shiftSwapId != null
      ? getEmployeeOpenShiftNotificationId(event.shiftSwapId)
      : relatedScheduleId != null
        ? getEmployeeOpenShiftScheduleNotificationId(relatedScheduleId)
        : event.id || getEmployeeOpenShiftNotificationId(null);

  return {
    id,
    readIds,
    title: t("Open shift posted"),
    body: swap
      ? t("Open shift for {0}: {1} {2} - {3}.", swap.scheduleName, formatSwapDay(swap), swap.fromTime, swap.toTime)
      : t("A new open shift is available."),
    meta: formatNotificationTime(event.occurredAtUtc),
    tone: "swap",
    occurredAtUtc: event.occurredAtUtc,
    actionPath: "/swap",
    actionLabel: t("Open swap"),
  };
}

export function buildOpenShiftSnapshotNotification(swap: ShiftSwap): EmployeeNotificationItem {
  return {
    id: getEmployeeOpenShiftNotificationId(swap.id),
    readIds: getOpenShiftSwapReadIds(swap),
    title: t("Open shift posted"),
    body: t("Open shift for {0}: {1} {2} - {3}.", swap.scheduleName, formatSwapDay(swap), swap.fromTime, swap.toTime),
    meta: `${swap.shopName || t("Shop")} / ${swap.containerName || t("Container")}`,
    tone: "swap",
    occurredAtUtc: swap.createdAtUtc,
    actionPath: "/swap",
    actionLabel: t("Open swap"),
  };
}

export function getNotificationSortValue(item: Pick<EmployeeNotificationItem, "occurredAtUtc">) {
  if (!item.occurredAtUtc) {
    return 0;
  }

  const parsed = new Date(item.occurredAtUtc).getTime();
  return Number.isNaN(parsed) ? 0 : parsed;
}

function findSchedulePublicationEvent(
  notifications: EmployeeRealtimeNotificationLike[],
  scheduleId: number,
) {
  return notifications.find(event =>
    event.kind === "schedule" &&
    event.reason === "manager-schedule-published" &&
    getRelatedScheduleId(event) === scheduleId,
  );
}

function findAvailabilityPublicationEvent(
  notifications: EmployeeRealtimeNotificationLike[],
  availabilityId: number,
) {
  return notifications.find(event =>
    event.kind === "availability" &&
    event.reason === "manager-availability-published" &&
    event.availabilityId === availabilityId,
  );
}

function findSwapEvent(
  notifications: EmployeeRealtimeNotificationLike[],
  swap: Pick<ShiftSwap, "id" | "scheduleId">,
  reasons: readonly string[],
  allowScheduleFallback = false,
) {
  return notifications.find(event =>
    event.kind === "shiftSwap" &&
    (
      event.shiftSwapId === swap.id ||
      (allowScheduleFallback && event.shiftSwapId == null && getRelatedScheduleId(event) === swap.scheduleId)
    ) &&
    reasons.includes(event.reason),
  );
}

function buildScheduleNotification(
  schedule: EmployeeSchedule,
  event?: EmployeeRealtimeNotificationLike,
): EmployeeNotificationItem {
  const id = getEmployeeScheduleNotificationId(schedule.id);
  return {
    id,
    readIds: uniqueIds([id, event?.id]),
    title: t("New schedule published"),
    body: t("\"{0}\" for {1} is now available at {2}.", schedule.name, formatMonth(schedule.year, schedule.month), schedule.shopName || "your shop"),
    meta: `${schedule.shopName || t("Shop")} / ${schedule.containerName || t("Container")}`,
    tone: "schedule",
    occurredAtUtc: event?.occurredAtUtc ?? schedule.lastUpdatedAtUtc ?? null,
    actionPath: "/schedule",
    actionLabel: t("Open schedule"),
  };
}

function buildAvailabilityNotification(
  availability: EmployeeAvailabilityGroup,
  event?: EmployeeRealtimeNotificationLike,
): EmployeeNotificationItem {
  const id = getEmployeeAvailabilityNotificationId(availability.id, availability.visibleFromUtc);
  return {
    id,
    readIds: uniqueIds([id, event?.id]),
    title: t("New availability published"),
    body: t("\"{0}\" for {1} is open for your availability.", availability.name, formatMonth(availability.year, availability.month)),
    meta: availability.visibleToUtc
      ? t("Submit by {0}", formatNotificationTime(availability.visibleToUtc))
      : t("Open now"),
    tone: "availability",
    occurredAtUtc: event?.occurredAtUtc ?? availability.visibleFromUtc ?? null,
    actionPath: "/availability",
    actionLabel: t("Open availability"),
  };
}

function buildSwapAvailableNotification(
  swap: ShiftSwap,
  event?: EmployeeRealtimeNotificationLike,
): EmployeeNotificationItem {
  if (swap.isManagerCreated) {
    const item = buildOpenShiftSnapshotNotification(swap);
    return {
      ...item,
      readIds: uniqueIds([...item.readIds, event?.id]),
      occurredAtUtc: event?.occurredAtUtc ?? item.occurredAtUtc,
    };
  }

  const id = getEmployeeSwapNotificationId(swap.id);
  return {
    id,
    readIds: uniqueIds([id, event?.id]),
    title: t("Shift swap available"),
    body: t("{0} offered {1} {2} - {3} from \"{4}\".", swap.fromEmployeeName || "A coworker", formatSwapDay(swap), swap.fromTime, swap.toTime, swap.scheduleName),
    meta: `${swap.shopName || t("Shop")} / ${swap.containerName || t("Container")}`,
    tone: "swap",
    occurredAtUtc: event?.occurredAtUtc ?? swap.createdAtUtc,
    actionPath: "/swap",
    actionLabel: t("Open swap"),
  };
}

function buildAcceptedOwnSwapNotification(
  swap: ShiftSwap,
  event?: EmployeeRealtimeNotificationLike,
): EmployeeNotificationItem {
  const id = getEmployeeSwapAcceptedNotificationId(swap.id);
  return {
    id,
    readIds: uniqueIds([id, event?.id]),
    title: t("Your shift was accepted"),
    body: t("{0} accepted your {1} {2} - {3} shift from \"{4}\".", swap.acceptedByEmployeeName || "A coworker", formatSwapDay(swap), swap.fromTime, swap.toTime, swap.scheduleName),
    meta: `${swap.shopName || t("Shop")} / ${swap.containerName || t("Container")}`,
    tone: "swap",
    occurredAtUtc: event?.occurredAtUtc ?? swap.acceptedAtUtc ?? swap.createdAtUtc,
    actionPath: "/swap",
    actionLabel: t("View swap"),
  };
}

export function buildEmployeeNotificationItems(
  realtimeNotifications: EmployeeRealtimeNotificationLike[],
  swaps: ShiftSwap[],
  schedules: EmployeeSchedule[],
  availabilityGroups: EmployeeAvailabilityGroup[],
  nowMs = Date.now(),
) {
  const items: EmployeeNotificationItem[] = [];
  const latestOpenSwapByScheduleId = new Map<number, ShiftSwap>();

  swaps.forEach(swap => {
    if (swap.status !== "open") {
      return;
    }

    const current = latestOpenSwapByScheduleId.get(swap.scheduleId);
    if (!current || getNotificationSortValue({ occurredAtUtc: swap.createdAtUtc }) > getNotificationSortValue({ occurredAtUtc: current.createdAtUtc })) {
      latestOpenSwapByScheduleId.set(swap.scheduleId, swap);
    }
  });

  schedules.forEach(schedule => {
    items.push(buildScheduleNotification(schedule, findSchedulePublicationEvent(realtimeNotifications, schedule.id)));
  });

  availabilityGroups.forEach(availability => {
    items.push(buildAvailabilityNotification(
      availability,
      findAvailabilityPublicationEvent(realtimeNotifications, availability.id),
    ));
  });

  realtimeNotifications.forEach(event => {
    if (!isOpenShiftPostedNotification(event) || !isWithinEmployeeNotificationRetention(event.occurredAtUtc, nowMs)) {
      return;
    }

    const hasMatchingSnapshot = swaps.some(swap =>
      swap.isManagerCreated &&
      swap.status === "open" &&
      (
        event.shiftSwapId === swap.id ||
        (event.shiftSwapId == null && getRelatedScheduleId(event) === swap.scheduleId)
      ),
    );
    if (!hasMatchingSnapshot) {
      items.push(buildOpenShiftLiveNotification(event, null));
    }
  });

  swaps.forEach(swap => {
    if (
      swap.status === "open" &&
      !swap.isCreatedByCurrentEmployee &&
      isWithinEmployeeNotificationRetention(swap.createdAtUtc, nowMs)
    ) {
      items.push(buildSwapAvailableNotification(
        swap,
        findSwapEvent(
          realtimeNotifications,
          swap,
          ["employee-swap-created", "manager-manual-shift-offer-created"],
          latestOpenSwapByScheduleId.get(swap.scheduleId)?.id === swap.id,
        ),
      ));
    }

    if (
      swap.status === "accepted" &&
      swap.isCreatedByCurrentEmployee &&
      isWithinEmployeeNotificationRetention(swap.acceptedAtUtc, nowMs)
    ) {
      items.push(buildAcceptedOwnSwapNotification(
        swap,
        findSwapEvent(realtimeNotifications, swap, ["employee-swap-accepted"]),
      ));
    }
  });

  return [...new Map(items.map(item => [item.id, item])).values()]
    .filter(item => isWithinEmployeeNotificationRetention(item.occurredAtUtc, nowMs))
    .sort((left, right) => getNotificationSortValue(right) - getNotificationSortValue(left))
    .slice(0, 80);
}

export function getUnreadEmployeeNotificationTargets(
  notifications: EmployeeRealtimeNotificationLike[],
  swaps: ShiftSwap[],
  schedules: EmployeeSchedule[],
  availabilityGroups: EmployeeAvailabilityGroup[],
  readNotificationIds: Set<string>,
  nowMs = Date.now(),
): Record<EmployeeNavNotificationTarget, boolean> {
  const unreadItems = buildEmployeeNotificationItems(
    notifications,
    swaps,
    schedules,
    availabilityGroups,
    nowMs,
  ).filter(item => !isEmployeeNotificationRead(item, readNotificationIds));

  return {
    alerts: unreadItems.length > 0,
    availability: unreadItems.some(item => item.tone === "availability"),
    schedule: unreadItems.some(item => item.tone === "schedule"),
    swap: unreadItems.some(item => item.tone === "swap"),
  };
}
