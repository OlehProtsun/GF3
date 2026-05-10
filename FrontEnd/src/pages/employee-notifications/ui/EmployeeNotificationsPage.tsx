import { useEffect, useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime, type EmployeeRealtimeNotification } from "@app/providers/PresenceProvider";
import {
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import {
  getEmployeeOpenShiftNotificationId,
  getEmployeeNotificationReadStorageKey,
  readEmployeeNotificationIds,
  writeEmployeeNotificationIds,
} from "@shared/lib/employeeNotificationReadState";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { NoteIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeNotificationsPage.module.css";

type NotificationTone = "swap";

const EMPTY_SWAPS: ShiftSwap[] = [];

type EmployeeNotificationItem = {
  id: string;
  title: string;
  body: string;
  meta: string;
  tone: NotificationTone;
  occurredAtUtc?: string | null;
  actionPath: string;
  actionLabel: string;
};

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

function formatSwapDay(swap: Pick<ShiftSwap, "year" | "month" | "dayOfMonth">) {
  return dayFormatter.format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
}

function formatNotificationTime(value?: string | null) {
  if (!value) {
    return "Current";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Just now" : notificationTimeFormatter.format(date);
}

function isOpenShiftPostedNotification(event: EmployeeRealtimeNotification) {
  return event.kind === "shiftSwap" && event.reason === "manager-manual-shift-offer-created";
}

function buildOpenShiftLiveNotification(event: EmployeeRealtimeNotification, swap: ShiftSwap | null): EmployeeNotificationItem {
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

function buildOpenShiftSnapshotNotification(swap: ShiftSwap): EmployeeNotificationItem {
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

function getNotificationSortValue(item: EmployeeNotificationItem) {
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

export function EmployeeNotificationsPage() {
  const { session } = useAuth();
  const { notifications: realtimeNotifications } = useRealtime();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const swaps = swapsQuery.data ?? EMPTY_SWAPS;
  const storageKey = getEmployeeNotificationReadStorageKey(session?.userName);
  const [readIds, setReadIds] = useState<Set<string>>(() => readEmployeeNotificationIds(storageKey));

  useEffect(() => {
    setReadIds(readEmployeeNotificationIds(storageKey));
  }, [storageKey]);

  const notificationItems = useMemo(() => {
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
      if (isOpenShiftPostedNotification(event)) {
        const relatedScheduleId = event.scheduleId ?? event.graphId ?? null;
        const swap = event.shiftSwapId != null
          ? openShiftById.get(event.shiftSwapId) ?? null
          : relatedScheduleId !== null
            ? latestOpenShiftByScheduleId.get(relatedScheduleId) ?? null
            : null;
        items.push(buildOpenShiftLiveNotification(event, swap));
      }
    });

    openShifts.forEach(swap => {
      items.push(buildOpenShiftSnapshotNotification(swap));
    });

    return [...new Map(items.map(item => [item.id, item])).values()]
      .sort((left, right) => getNotificationSortValue(right) - getNotificationSortValue(left))
      .slice(0, 80);
  }, [realtimeNotifications, swaps]);

  const unreadCount = notificationItems.reduce((count, item) => count + (readIds.has(item.id) ? 0 : 1), 0);
  const queryError = swapsQuery.error;
  const queryErrorMessage = queryError ? getErrorMessage(queryError, "Could not load notifications.") : null;

  const handleMarkAllRead = () => {
    const nextReadIds = new Set([...readIds, ...notificationItems.map(item => item.id)]);
    setReadIds(nextReadIds);
    writeEmployeeNotificationIds(storageKey, nextReadIds);
  };

  const handleMarkRead = (itemId: string) => {
    if (readIds.has(itemId)) {
      return;
    }

    const nextReadIds = new Set(readIds);
    nextReadIds.add(itemId);
    setReadIds(nextReadIds);
    writeEmployeeNotificationIds(storageKey, nextReadIds);
  };

  return (
    <div className={workspaceStyles.page}>
      {queryErrorMessage ? <ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner> : null}

      <section className={`${workspaceStyles.panel} ${styles.inboxPanel}`}>
        <div className={styles.sectionHeader}>
          <div>
            <span className={workspaceStyles.panelEyebrow}>Inbox</span>
            <h2 className={workspaceStyles.panelTitle}>Inbox</h2>
          </div>

          <div className={styles.inboxHeaderActions}>
            <span className={`${styles.countBadge} ${unreadCount > 0 ? styles.countBadgeUnread : ""}`}>
              {unreadCount > 0 ? `${unreadCount} unread` : notificationItems.length}
            </span>

            {unreadCount > 0 ? (
              <button type="button" className={styles.actionButton} onClick={handleMarkAllRead}>
                Mark all read
              </button>
            ) : null}
          </div>
        </div>

        {notificationItems.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}>
              <NoteIcon size={20} />
            </span>
            <strong>No notifications yet</strong>
            <span>Open shift updates will appear here.</span>
          </div>
        ) : (
          <div className={styles.notificationList}>
            {notificationItems.map(item => {
              const isUnread = !readIds.has(item.id);
              const itemClassName = [
                styles.notificationItem,
                styles[`notificationItem_${item.tone}`],
                isUnread ? styles.notificationItemUnread : "",
              ].filter(Boolean).join(" ");

              return (
                <article key={item.id} className={itemClassName}>
                  <span className={styles.notificationIcon}>
                    <NoteIcon size={17} />
                  </span>

                  <div className={styles.notificationBody}>
                    <div className={styles.notificationTitleRow}>
                      <strong>{item.title}</strong>
                      {isUnread ? <span className={styles.unreadDot} aria-label="Unread" /> : null}
                    </div>
                    <p>{item.body}</p>
                    <span>{item.meta}</span>
                  </div>

                  <div className={styles.notificationActions}>
                    <button
                      type="button"
                      className={styles.markReadButton}
                      onClick={() => handleMarkRead(item.id)}
                      disabled={!isUnread}
                    >
                      {isUnread ? "Mark as read" : "Read"}
                    </button>

                    <NavLink to={item.actionPath} className={styles.notificationAction}>
                      {item.actionLabel}
                    </NavLink>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
