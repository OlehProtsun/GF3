import { useEffect, useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import {
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { useEmployeeScheduleListQuery, type EmployeeSchedule } from "@entities/employee-schedule";
import {
  useEmployeeAvailabilityListQuery,
  type EmployeeAvailabilityGroup,
} from "@entities/employee-availability";
import { getErrorMessage } from "@shared/api/httpClient";
import {
  readEmployeeNotificationIdsForAccount,
  writeEmployeeNotificationIdsForAccount,
} from "@shared/lib/employeeNotificationReadState";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { InboxIcon, NoteIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import { buildEmployeeNotificationItems, formatEmployeeNotificationExpiry, isEmployeeNotificationRead } from "../model/notifications";
import styles from "./EmployeeNotificationsPage.module.css";

const EMPTY_SWAPS: ShiftSwap[] = [];
const EMPTY_SCHEDULES: EmployeeSchedule[] = [];
const EMPTY_AVAILABILITY_GROUPS: EmployeeAvailabilityGroup[] = [];

export function EmployeeNotificationsPage() {
  const { session } = useAuth();
  const { notifications: realtimeNotifications } = useRealtime();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const schedulesQuery = useEmployeeScheduleListQuery();
  const availabilityQuery = useEmployeeAvailabilityListQuery();
  const swaps = swapsQuery.data ?? EMPTY_SWAPS;
  const schedules = schedulesQuery.data ?? EMPTY_SCHEDULES;
  const availabilityGroups = availabilityQuery.data ?? EMPTY_AVAILABILITY_GROUPS;
  const [nowMs, setNowMs] = useState(() => Date.now());
  const [readIds, setReadIds] = useState<Set<string>>(() =>
    readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId),
  );

  useEffect(() => {
    setReadIds(readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId));
  }, [session?.employeeId, session?.userName]);

  useEffect(() => {
    const intervalId = window.setInterval(() => setNowMs(Date.now()), 30_000);
    return () => window.clearInterval(intervalId);
  }, []);

  const notificationItems = useMemo(
    () => buildEmployeeNotificationItems(realtimeNotifications, swaps, schedules, availabilityGroups, nowMs),
    [availabilityGroups, nowMs, realtimeNotifications, schedules, swaps],
  );

  const unreadCount = notificationItems.reduce(
    (count, item) => count + (isEmployeeNotificationRead(item, readIds) ? 0 : 1),
    0,
  );
  const queryError = swapsQuery.error ?? schedulesQuery.error ?? availabilityQuery.error;
  const queryErrorMessage = queryError ? getErrorMessage(queryError, "Could not load notifications.") : null;

  const handleMarkAllRead = () => {
    const nextReadIds = new Set([
      ...readIds,
      ...notificationItems.flatMap(item => item.readIds),
    ]);
    setReadIds(nextReadIds);
    writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, nextReadIds);
  };

  const handleMarkRead = (itemReadIds: string[]) => {
    if (itemReadIds.every(id => readIds.has(id))) {
      return;
    }

    const nextReadIds = new Set(readIds);
    itemReadIds.forEach(id => nextReadIds.add(id));
    setReadIds(nextReadIds);
    writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, nextReadIds);
  };

  return (
    <div className={workspaceStyles.page}>
      {queryErrorMessage ? <ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner> : null}

      <section className={`${workspaceStyles.panel} ${styles.inboxPanel}`}>
        <div className={styles.sectionHeader}>
          <div className={styles.sectionHeading}>
            <span className={styles.sectionIcon} aria-hidden="true">
              <InboxIcon size={20} />
            </span>
            <div>
              <span className={workspaceStyles.panelEyebrow}>Notifications</span>
              <h2 className={workspaceStyles.panelTitle}>Inbox</h2>
            </div>
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
            <span>Published schedules, availability and shift updates will appear here.</span>
          </div>
        ) : (
          <div className={styles.notificationList}>
            {notificationItems.map(item => {
              const isUnread = !isEmployeeNotificationRead(item, readIds);
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
                    <span className={styles.notificationExpiry}>
                      {formatEmployeeNotificationExpiry(item.occurredAtUtc, nowMs)}
                    </span>
                  </div>

                  <div className={styles.notificationActions}>
                    <button
                      type="button"
                      className={styles.markReadButton}
                      onClick={() => handleMarkRead(item.readIds)}
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
