import type { ShiftSwap } from "@entities/shift-swaps";
import {
  getEmployeeOpenShiftNotificationId,
} from "@shared/lib/employeeNotificationReadState";

export type NotificationTone = "swap";

export type EmployeeRealtimeNotificationLike = {
  id: string;
  kind: "schedule" | "shiftSwap";
  reason: string;
  occurredAtUtc: string;
  containerId?: number | null;
  graphId?: number | null;
  scheduleId?: number | null;
  shiftSwapId?: number | null;
};

export type EmployeeNotificationItem = {
  id: string;
  title: string;
  body: string;
  meta: string;
  tone: NotificationTone;
  occurredAtUtc?: string | null;
  actionPath: string;
  actionLabel: string;
};

export type EmployeeNavNotificationTarget = "alerts" | "swap";

const dayFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const notificationTimeFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
});

export function formatSwapDay(swap: Pick<ShiftSwap, "year" | "month" | "dayOfMonth">) {
  return dayFormatter.format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
}

export function formatNotificationTime(value?: string | null) {
  if (!value) {
    return "Current";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Just now" : notificationTimeFormatter.format(date);
}

export function isOpenShiftPostedNotification(event: EmployeeRealtimeNotificationLike) {
  return event.kind === "shiftSwap" && event.reason === "manager-manual-shift-offer-created";
}

export function buildOpenShiftLiveNotification(
  event: EmployeeRealtimeNotificationLike,
  swap: ShiftSwap | null,
): EmployeeNotificationItem {
  const notificationIdSource = swap?.id ?? event.shiftSwapId ?? event.scheduleId ?? event.graphId ?? null;
  return {
    id: getEmployeeOpenShiftNotificationId(notificationIdSource),
    title: "Open shift posted",
    body: swap
      ? `Open shift for ${swap.scheduleName}: ${formatSwapDay(swap)} ${swap.fromTime} - ${swap.toTime}.`
      : "A new open shift is available.",
    meta: formatNotificationTime(event.occurredAtUtc),
    tone: "swap",
    occurredAtUtc: event.occurredAtUtc,
    actionPath: "/swap",
    actionLabel: "Open swap",
  };
}

export function buildOpenShiftSnapshotNotification(swap: ShiftSwap): EmployeeNotificationItem {
  return {
    id: getEmployeeOpenShiftNotificationId(swap.id),
    title: "Open shift posted",
    body: `Open shift for ${swap.scheduleName}: ${formatSwapDay(swap)} ${swap.fromTime} - ${swap.toTime}.`,
    meta: `${swap.shopName || "Shop"} / ${swap.containerName || "Container"}`,
    tone: "swap",
    occurredAtUtc: swap.createdAtUtc,
    actionPath: "/swap",
    actionLabel: "Open swap",
  };
}

export function getNotificationSortValue(item: Pick<EmployeeNotificationItem, "occurredAtUtc">) {
  if (!item.occurredAtUtc) {
    return 0;
  }

  const parsed = new Date(item.occurredAtUtc).getTime();
  return Number.isNaN(parsed) ? 0 : parsed;
}

function getSwapSortValue(swap: Pick<ShiftSwap, "createdAtUtc">) {
  const parsed = new Date(swap.createdAtUtc).getTime();
  return Number.isNaN(parsed) ? 0 : parsed;
}

export function buildEmployeeNotificationItems(
  realtimeNotifications: EmployeeRealtimeNotificationLike[],
  swaps: ShiftSwap[],
) {
  const openShifts = swaps.filter(swap => swap.isManagerCreated && swap.status === "open");
  const openShiftById = new Map(openShifts.map(swap => [swap.id, swap]));
  const latestOpenShiftByScheduleId = new Map<number, ShiftSwap>();

  openShifts.forEach(swap => {
    const current = latestOpenShiftByScheduleId.get(swap.scheduleId);
    if (!current || getSwapSortValue(swap) > getSwapSortValue(current)) {
      latestOpenShiftByScheduleId.set(swap.scheduleId, swap);
    }
  });

  const items: EmployeeNotificationItem[] = [];

  realtimeNotifications.forEach(event => {
    if (!isOpenShiftPostedNotification(event)) {
      return;
    }

    const relatedScheduleId = event.scheduleId ?? event.graphId ?? null;
    const swap = event.shiftSwapId != null
      ? openShiftById.get(event.shiftSwapId) ?? null
      : relatedScheduleId !== null
        ? latestOpenShiftByScheduleId.get(relatedScheduleId) ?? null
        : null;
    items.push(buildOpenShiftLiveNotification(event, swap));
  });

  openShifts.forEach(swap => {
    items.push(buildOpenShiftSnapshotNotification(swap));
  });

  return [...new Map(items.map(item => [item.id, item])).values()]
    .sort((left, right) => getNotificationSortValue(right) - getNotificationSortValue(left))
    .slice(0, 80);
}

export function getUnreadEmployeeNotificationTargets(
  notifications: EmployeeRealtimeNotificationLike[],
  swaps: ShiftSwap[],
  readNotificationIds: Set<string>,
): Record<EmployeeNavNotificationTarget, boolean> {
  const targets: Record<EmployeeNavNotificationTarget, boolean> = {
    alerts: false,
    swap: false,
  };

  if (swaps.some(swap =>
    swap.isManagerCreated &&
    swap.status === "open" &&
    !readNotificationIds.has(getEmployeeOpenShiftNotificationId(swap.id)),
  )) {
    targets.alerts = true;
    targets.swap = true;
  }

  notifications.forEach(notification => {
    if (readNotificationIds.has(notification.id)) {
      return;
    }

    if (isOpenShiftPostedNotification(notification)) {
      targets.alerts = true;
      targets.swap = true;
    }
  });

  return targets;
}
