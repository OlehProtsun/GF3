import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { useEffect, useMemo, useState, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import { EmployeeCommunicationDialog } from "@entities/communications/ui";
import { useEmployeeShiftSwapsQuery, type ShiftSwap } from "@entities/shift-swaps";
import { useEmployeeScheduleListQuery, type EmployeeSchedule } from "@entities/employee-schedule";
import { employeeUiStateApi, useEmployeeUiStateQuery } from "@entities/employee-ui-state";
import { useSystemNewsQuery } from "@entities/system-news";
import {
  useEmployeeAvailabilityListQuery,
  type EmployeeAvailabilityGroup,
} from "@entities/employee-availability";
import {
  employeeNotificationReadStateEventName,
  getEmployeeNotificationReadStorageKeys,
  readEmployeeNotificationIdsForAccount,
  writeEmployeeNotificationIdsForAccount,
} from "@shared/lib/employeeNotificationReadState";
import {
  getUnreadEmployeeNotificationTargets,
  type EmployeeNavNotificationTarget,
} from "@pages/employee-notifications/model/notifications";
import { AvailabilityIcon, BackIcon, EmployeeIcon, NoteIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./EmployeeWorkspaceLayout.module.css";

type EmployeeNavItem = {
  to: string;
  label: string;
  mobileLabel?: string;
  description: string;
  icon: React.ReactNode;
  iconClassName?: string;
  notificationTarget?: EmployeeNavNotificationTarget;
};

const employeeNavItems: readonly EmployeeNavItem[] = [
  {
    to: "/",
    get label() { return t("Notifications"); },
    get mobileLabel() { return t("Alerts"); },
    get description() { return t("Live schedule, swap and manager updates."); },
    icon: <NoteIcon size={18} />,
    notificationTarget: "alerts",
  },
  {
    to: "/availability",
    get label() { return t("Availability"); },
    get mobileLabel() { return t("Avail."); },
    get description() { return t("Share available time when your manager opens access."); },
    icon: <AvailabilityIcon size={18} />,
    notificationTarget: "availability",
  },
  {
    to: "/schedule",
    get label() { return t("Schedule"); },
    get mobileLabel() { return t("Shifts"); },
    get description() { return t("See published shifts and ready-made plans."); },
    icon: <ScheduleIcon size={18} />,
    notificationTarget: "schedule",
  },
  {
    to: "/swap",
    get label() { return t("Swap"); },
    get mobileLabel() { return t("Swap"); },
    get description() { return t("Give away shifts or accept open swaps."); },
    icon: <NoteIcon size={18} />,
    notificationTarget: "swap",
  },
  {
    to: "/profile",
    get label() { return t("Profile"); },
    get description() { return t("Review your account and contact details."); },
    icon: <EmployeeIcon size={18} />,
  },
] as const;

const emptySwaps: ShiftSwap[] = [];
const emptySchedules: EmployeeSchedule[] = [];
const emptyAvailabilityGroups: EmployeeAvailabilityGroup[] = [];

export function EmployeeWorkspaceLayout({ children }: PropsWithChildren) {
  useLanguageRevision();
  const [isMobileTabsCollapsed, setIsMobileTabsCollapsed] = useState(false);
  const { session } = useAuth();
  const { notifications } = useRealtime();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const schedulesQuery = useEmployeeScheduleListQuery();
  const availabilityQuery = useEmployeeAvailabilityListQuery();
  const { data: uiState, refetch: refetchUiState } = useEmployeeUiStateQuery(Boolean(session?.employeeId));
  const newsQuery = useSystemNewsQuery(Boolean(session?.employeeId));
  const swaps = swapsQuery.data ?? emptySwaps;
  const schedules = schedulesQuery.data ?? emptySchedules;
  const availabilityGroups = availabilityQuery.data ?? emptyAvailabilityGroups;
  const { pathname } = useLocation();
  const [nowMs, setNowMs] = useState(() => Date.now());
  useEffect(() => {
    const refresh = () => setNowMs(Date.now());
    const timer = window.setInterval(refresh, 30_000);
    window.addEventListener("focus", refresh);
    return () => { window.clearInterval(timer); window.removeEventListener("focus", refresh); };
  }, []);
  const [readNotificationIds, setReadNotificationIds] = useState<Set<string>>(() =>
    readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId),
  );
  const isProfileRoute = pathname === "/profile";
  const isAvailabilityRoute = pathname === "/availability";
  const isScheduleRoute = pathname === "/schedule";
  const isSwapRoute = pathname === "/swap";
  const layoutClassName = [
    styles.layout,
    isProfileRoute ? styles.layoutProfile : "",
    isAvailabilityRoute ? styles.layoutAvailability : "",
    isScheduleRoute ? styles.layoutSchedule : "",
    isSwapRoute ? styles.layoutSwap : "",
  ]
    .filter(Boolean)
    .join(" ");
  const shellClassName = [
    styles.shell,
    isProfileRoute ? styles.shellProfile : "",
    isMobileTabsCollapsed ? styles.shellTabsCollapsed : "",
  ]
    .filter(Boolean)
    .join(" ");
  const topBarClassName = [
    styles.topBar,
    isProfileRoute ? styles.topBarProfile : "",
  ]
    .filter(Boolean)
    .join(" ");
  const mobileTabsClassName = [
    styles.mobileTabs,
    isMobileTabsCollapsed ? styles.mobileTabsCollapsed : styles.mobileTabsExpanded,
  ]
    .filter(Boolean)
    .join(" ");
  const mobileOpenTabClassName = [
    styles.mobileOpenTab,
    isMobileTabsCollapsed ? styles.mobileOpenTabVisible : styles.mobileOpenTabHidden,
  ]
    .filter(Boolean)
    .join(" ");
  const unreadNotificationTargets = useMemo(
    () => getUnreadEmployeeNotificationTargets(
      notifications,
      swaps,
      schedules,
      availabilityGroups,
      readNotificationIds,
      nowMs,
    ),
    [availabilityGroups, notifications, readNotificationIds, schedules, swaps, nowMs],
  );

  useEffect(() => {
    const storageKeys = getEmployeeNotificationReadStorageKeys(session?.userName, session?.employeeId);
    const refreshReadState = () => {
      setReadNotificationIds(new Set([
        ...readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId),
        ...(uiState?.readNotificationIds ?? []),
      ]));
    };

    refreshReadState();

    const handleStorage = (event: StorageEvent) => {
      if (event.key && storageKeys.includes(event.key)) {
        refreshReadState();
      }
    };

    window.addEventListener("storage", handleStorage);
    window.addEventListener(employeeNotificationReadStateEventName, refreshReadState);

    return () => {
      window.removeEventListener("storage", handleStorage);
      window.removeEventListener(employeeNotificationReadStateEventName, refreshReadState);
    };
  }, [session?.employeeId, session?.userName, uiState]);
  useEffect(() => {
    if (!uiState) {
      return;
    }

    const localReadIds = readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId);
    const serverReadIds = new Set(uiState.readNotificationIds ?? []);
    const mergedReadIds = new Set([...localReadIds, ...serverReadIds]);
    setReadNotificationIds(mergedReadIds);
    if ([...serverReadIds].some(id => !localReadIds.has(id))) {
      writeEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId, mergedReadIds);
    }

    const localOnlyIds = [...localReadIds].filter(id => !serverReadIds.has(id));
    if (localOnlyIds.length > 0) {
      void employeeUiStateApi.markNotificationsRead(localOnlyIds)
        .then(() => refetchUiState())
        .catch(() => undefined);
    }
  }, [session?.employeeId, session?.userName, uiState, refetchUiState]);

  const hasUnreadNavigationDot = (item: EmployeeNavItem) =>
    item.notificationTarget ? unreadNotificationTargets[item.notificationTarget] ||
      (item.notificationTarget === "alerts" && (newsQuery.data ?? []).some(message => !message.isRead)) : false;

  return (
    <div className={layoutClassName}>
      <button
        type="button"
        className={mobileOpenTabClassName}
        onClick={() => setIsMobileTabsCollapsed(false)}
        aria-label={t("Open navigation")}
      >
        <span className={[styles.controlIcon, styles.controlIconExpand].join(" ")}>
          <BackIcon size={14} />
        </span>
      </button>

      <div className={shellClassName}>
        <header className={topBarClassName}>
          <nav className={styles.desktopTabs} aria-label={t("Employee sections")}>
            {employeeNavItems.map((item) => {
              const hasUnreadDot = hasUnreadNavigationDot(item);

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  aria-label={hasUnreadDot ? t("{0}, new updates", item.label) : item.label}
                  className={({ isActive }) =>
                    [styles.desktopTab, isActive ? styles.desktopTabActive : ""].filter(Boolean).join(" ")
                  }
                >
                  <span className={[styles.tabIcon, item.iconClassName ?? ""].filter(Boolean).join(" ")}>
                    {item.icon}
                    {hasUnreadDot ? <span className={styles.navUnreadDot} aria-hidden="true" /> : null}
                  </span>
                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </nav>
        </header>

        <main className={styles.content}>{children}</main>
      </div>

      <nav className={mobileTabsClassName} aria-label={t("Employee sections")}>
        {employeeNavItems.map((item) => {
          const hasUnreadDot = hasUnreadNavigationDot(item);

          return (
            <NavLink
              key={item.to}
              to={item.to}
              aria-label={hasUnreadDot ? t("{0}, new updates", item.mobileLabel ?? item.label) : item.mobileLabel ?? item.label}
              className={({ isActive }) =>
                [styles.mobileTab, isActive ? styles.mobileTabActive : ""].filter(Boolean).join(" ")
              }
            >
              <span className={[styles.tabIcon, item.iconClassName ?? ""].filter(Boolean).join(" ")}>
                {item.icon}
                {hasUnreadDot ? <span className={styles.navUnreadDot} aria-hidden="true" /> : null}
              </span>
              <span>{item.mobileLabel ?? item.label}</span>
            </NavLink>
          );
        })}

        <button
          type="button"
          className={styles.mobileToggleButton}
          onClick={() => setIsMobileTabsCollapsed(true)}
          aria-label={t("Collapse navigation")}
        >
          <span className={styles.controlIcon}>
            <BackIcon size={14} />
          </span>
        </button>
      </nav>

      <EmployeeCommunicationDialog employeeId={session?.employeeId ?? null} />
    </div>
  );
}
