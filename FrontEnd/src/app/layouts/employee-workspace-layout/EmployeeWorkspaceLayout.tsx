import { useEffect, useMemo, useState, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import { EmployeeCommunicationDialog } from "@entities/communications/ui";
import { useEmployeeShiftSwapsQuery, type ShiftSwap } from "@entities/shift-swaps";
import { useEmployeeScheduleListQuery, type EmployeeSchedule } from "@entities/employee-schedule";
import {
  useEmployeeAvailabilityListQuery,
  type EmployeeAvailabilityGroup,
} from "@entities/employee-availability";
import {
  employeeNotificationReadStateEventName,
  getEmployeeNotificationReadStorageKeys,
  readEmployeeNotificationIdsForAccount,
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
    label: "Notifications",
    mobileLabel: "Alerts",
    description: "Live schedule, swap and manager updates.",
    icon: <NoteIcon size={18} />,
    notificationTarget: "alerts",
  },
  {
    to: "/availability",
    label: "Availability",
    mobileLabel: "Avail.",
    description: "Share available time when your manager opens access.",
    icon: <AvailabilityIcon size={18} />,
    notificationTarget: "availability",
  },
  {
    to: "/schedule",
    label: "Schedule",
    mobileLabel: "Shifts",
    description: "See published shifts and ready-made plans.",
    icon: <ScheduleIcon size={18} />,
    notificationTarget: "schedule",
  },
  {
    to: "/swap",
    label: "Swap",
    mobileLabel: "Swap",
    description: "Give away shifts or accept open swaps.",
    icon: <NoteIcon size={18} />,
    notificationTarget: "swap",
  },
  {
    to: "/profile",
    label: "Profile",
    description: "Review your account and contact details.",
    icon: <EmployeeIcon size={18} />,
  },
] as const;

const emptySwaps: ShiftSwap[] = [];
const emptySchedules: EmployeeSchedule[] = [];
const emptyAvailabilityGroups: EmployeeAvailabilityGroup[] = [];

export function EmployeeWorkspaceLayout({ children }: PropsWithChildren) {
  const [isMobileTabsCollapsed, setIsMobileTabsCollapsed] = useState(false);
  const { session } = useAuth();
  const { notifications } = useRealtime();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const schedulesQuery = useEmployeeScheduleListQuery();
  const availabilityQuery = useEmployeeAvailabilityListQuery();
  const swaps = swapsQuery.data ?? emptySwaps;
  const schedules = schedulesQuery.data ?? emptySchedules;
  const availabilityGroups = availabilityQuery.data ?? emptyAvailabilityGroups;
  const { pathname } = useLocation();
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
    isSwapRoute ? styles.layoutSchedule : "",
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
    ),
    [availabilityGroups, notifications, readNotificationIds, schedules, swaps],
  );

  useEffect(() => {
    const storageKeys = getEmployeeNotificationReadStorageKeys(session?.userName, session?.employeeId);
    const refreshReadState = () => {
      setReadNotificationIds(readEmployeeNotificationIdsForAccount(session?.userName, session?.employeeId));
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
  }, [session?.employeeId, session?.userName]);

  const hasUnreadNavigationDot = (item: EmployeeNavItem) =>
    item.notificationTarget ? unreadNotificationTargets[item.notificationTarget] : false;

  return (
    <div className={layoutClassName}>
      <button
        type="button"
        className={mobileOpenTabClassName}
        onClick={() => setIsMobileTabsCollapsed(false)}
        aria-label="Open navigation"
      >
        <span className={[styles.controlIcon, styles.controlIconExpand].join(" ")}>
          <BackIcon size={14} />
        </span>
      </button>

      <div className={shellClassName}>
        <header className={topBarClassName}>
          <nav className={styles.desktopTabs} aria-label="Employee sections">
            {employeeNavItems.map((item) => {
              const hasUnreadDot = hasUnreadNavigationDot(item);

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  aria-label={hasUnreadDot ? `${item.label}, new updates` : item.label}
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

      <nav className={mobileTabsClassName} aria-label="Employee sections">
        {employeeNavItems.map((item) => {
          const hasUnreadDot = hasUnreadNavigationDot(item);

          return (
            <NavLink
              key={item.to}
              to={item.to}
              aria-label={hasUnreadDot ? `${item.mobileLabel ?? item.label}, new updates` : item.mobileLabel ?? item.label}
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
          aria-label="Collapse navigation"
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
