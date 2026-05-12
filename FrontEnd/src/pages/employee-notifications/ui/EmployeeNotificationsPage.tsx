import { useEffect, useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import {
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import {
  getEmployeeNotificationReadStorageKey,
  readEmployeeNotificationIds,
  writeEmployeeNotificationIds,
} from "@shared/lib/employeeNotificationReadState";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { NoteIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import { buildEmployeeNotificationItems } from "../model/notifications";
import styles from "./EmployeeNotificationsPage.module.css";

const EMPTY_SWAPS: ShiftSwap[] = [];

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

  const notificationItems = useMemo(
    () => buildEmployeeNotificationItems(realtimeNotifications, swaps),
    [realtimeNotifications, swaps],
  );

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
