import { getLocale } from "@shared/i18n";
import { t } from "@shared/i18n";
import { useEffect, useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import {
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { useEmployeeScheduleListQuery, type EmployeeSchedule } from "@entities/employee-schedule";
import { employeeUiStateApi, useEmployeeUiStateQuery } from "@entities/employee-ui-state";
import { useMarkAllSystemNewsReadMutation, useMarkSystemNewsReadMutation, useSystemNewsQuery } from "@entities/system-news";
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
  const newsQuery = useSystemNewsQuery();
  const markNewsRead = useMarkSystemNewsReadMutation();
  const markAllNewsRead = useMarkAllSystemNewsReadMutation();
  const { data: uiState, refetch: refetchUiState } = useEmployeeUiStateQuery(Boolean(session?.employeeId));
  const swaps = swapsQuery.data ?? EMPTY_SWAPS;
  const schedules = schedulesQuery.data ?? EMPTY_SCHEDULES;
  const availabilityGroups = availabilityQuery.data ?? EMPTY_AVAILABILITY_GROUPS;
  const [nowMs, setNowMs] = useState(() => Date.now());
  const [syncError, setSyncError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"inbox" | "news">("inbox");
  const [readIds, setReadIds] = useState<Set<string>>(() =>
    readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId),
  );

  useEffect(() => {
    setReadIds(readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId));
  }, [session?.employeeId, session?.userName]);
  useEffect(() => {
    if (!uiState) {
      return;
    }

    const localReadIds = readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId);
    const serverReadIds = new Set(uiState.readNotificationIds ?? []);
    const mergedReadIds = new Set([...localReadIds, ...serverReadIds]);
    setReadIds(mergedReadIds);
    if ([...serverReadIds].some(id => !localReadIds.has(id))) {
      writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, mergedReadIds);
    }

  }, [session?.employeeId, session?.userName, uiState, refetchUiState]);

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
  const newsItems = newsQuery.data ?? [];
  const newsUnreadCount = newsItems.filter(item => !item.isRead).length;
  const activeUnreadCount = activeTab === "inbox" ? unreadCount : newsUnreadCount;
  const activeItemCount = activeTab === "inbox" ? notificationItems.length : newsItems.length;
  const queryError = swapsQuery.error ?? schedulesQuery.error ?? availabilityQuery.error ?? newsQuery.error;
  const queryErrorMessage = queryError ? getErrorMessage(queryError, t("Could not load notifications.")) : null;

  const handleMarkAllRead = () => {
    const nextReadIds = new Set([
      ...readIds,
      ...notificationItems.flatMap(item => item.readIds),
    ]);
    setReadIds(nextReadIds);
    writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, nextReadIds);
    setSyncError(null);
    void employeeUiStateApi.markNotificationsRead(notificationItems.flatMap(item => item.readIds))
      .then(() => refetchUiState())
      .catch(error => setSyncError(getErrorMessage(error, t("Could not sync notification state."))));
  };

  const handleMarkRead = (itemReadIds: string[]) => {
    if (itemReadIds.every(id => readIds.has(id))) {
      return;
    }

    const nextReadIds = new Set(readIds);
    itemReadIds.forEach(id => nextReadIds.add(id));
    setReadIds(nextReadIds);
    writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, nextReadIds);
    setSyncError(null);
    void employeeUiStateApi.markNotificationsRead(itemReadIds)
      .then(() => refetchUiState())
      .catch(error => setSyncError(getErrorMessage(error, t("Could not sync notification state."))));
  };

  const getYoutubeEmbedUrl = (value: string | null) => {
    if (!value) return null;
    try {
      const url = new URL(value);
      const id = url.hostname === "youtu.be" ? url.pathname.slice(1).split("/")[0] :
        url.pathname.startsWith("/shorts/") || url.pathname.startsWith("/embed/") ? url.pathname.split("/")[2] : url.searchParams.get("v");
      return id && /^[\w-]{6,20}$/.test(id) ? `https://www.youtube-nocookie.com/embed/${id}` : null;
    } catch { return null; }
  };

  return (
    <div className={workspaceStyles.page}>
      {queryErrorMessage ? <ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner> : null}
      {syncError ? <ErrorBanner dismissible>{syncError}</ErrorBanner> : null}

      <section className={`${workspaceStyles.panel} ${styles.inboxPanel}`}>
        <div className={styles.sectionHeader}>
          <div className={styles.sectionHeading}>
            <span className={styles.sectionIcon} aria-hidden="true">
              <InboxIcon size={20} />
            </span>
            <div className={styles.headingCopy}>
              <span className={workspaceStyles.panelEyebrow}>{t("Notifications")}</span>
              <div className={styles.tabsSummaryRow}>
                <div className={styles.inboxTabs} role="tablist" aria-label={t("Notification type")}>
                  <button type="button" role="tab" aria-selected={activeTab === "inbox"} className={activeTab === "inbox" ? styles.inboxTabActive : ""} onClick={() => setActiveTab("inbox")}>{t("Inbox")}{unreadCount > 0 ? <span>{unreadCount}</span> : null}</button>
                  <button type="button" role="tab" aria-selected={activeTab === "news"} className={activeTab === "news" ? styles.inboxTabActive : ""} onClick={() => setActiveTab("news")}>{t("News")}{newsUnreadCount > 0 ? <span>{newsUnreadCount}</span> : null}</button>
                </div>
                <span className={`${styles.countBadge} ${activeUnreadCount > 0 ? styles.countBadgeUnread : ""}`}>
                  {activeUnreadCount > 0 ? t("{0} unread", activeUnreadCount) : activeItemCount}
                </span>
              </div>
            </div>
          </div>

          <div className={styles.inboxHeaderActions}>
            {activeUnreadCount > 0 ? (
              <button type="button" className={styles.actionButton} onClick={activeTab === "inbox" ? handleMarkAllRead : () => markAllNewsRead.mutate(undefined)}>
                {t("Mark all read")}</button>
            ) : null}
          </div>
        </div>

        {activeTab === "inbox" && notificationItems.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}>
              <NoteIcon size={20} />
            </span>
            <strong>{t("No notifications yet")}</strong>
            <span>{t("Published schedules, availability and shift updates will appear here.")}</span>
          </div>
        ) : activeTab === "inbox" ? (
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
                      {isUnread ? <span className={styles.unreadDot} aria-label={t("Unread")} /> : null}
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
                      {isUnread ? t("Mark as read") : t("Read")}
                    </button>

                    <NavLink to={item.actionPath} className={styles.notificationAction}>
                      {item.actionLabel}
                    </NavLink>
                  </div>
                </article>
              );
            })}
          </div>
        ) : newsItems.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}><NoteIcon size={20} /></span>
            <strong>{t("No system news yet")}</strong>
            <span>{t("Product updates and announcements will appear here.")}</span>
          </div>
        ) : (
          <div className={styles.notificationList}>
            {newsItems.map(item => {
              const embedUrl = getYoutubeEmbedUrl(item.videoUrl);
              return (
                <article key={item.id} className={[styles.notificationItem, styles.newsItem, !item.isRead ? styles.notificationItemUnread : ""].filter(Boolean).join(" ")}>
                  <span className={styles.notificationIcon}><NoteIcon size={17} /></span>
                  <div className={styles.notificationBody}>
                    <div className={styles.notificationTitleRow}><strong>{item.title}</strong>{!item.isRead ? <span className={styles.unreadDot} aria-label={t("Unread")} /> : null}</div>
                    <p className={styles.newsBody}>{item.body}</p>
                    <span>{new Date(item.createdAtUtc).toLocaleString(getLocale())}</span>
                    {item.imageUrl ? <img className={styles.newsImage} src={item.imageUrl} alt="" /> : null}
                    {embedUrl ? <div className={styles.newsVideo}><iframe src={embedUrl} title={t("{0} video", item.title)} allow="accelerometer; autoplay; encrypted-media; picture-in-picture" allowFullScreen /></div> : null}
                  </div>
                  <div className={styles.notificationActions}>
                    <button type="button" className={styles.markReadButton} onClick={() => markNewsRead.mutate(item.id)} disabled={item.isRead}>{item.isRead ? t("Read") : t("Mark as read")}</button>
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
