import { useEffect, useMemo, useState, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useRealtime } from "@app/providers/PresenceProvider";
import { useEmployeeShiftSwapsQuery } from "@entities/shift-swaps";
import {
  employeeNotificationReadStateEventName,
  getEmployeeOpenShiftNotificationId,
  getEmployeeNotificationReadStorageKey,
  readEmployeeNotificationIds,
} from "@shared/lib/employeeNotificationReadState";
import { AvailabilityIcon, BackIcon, EmployeeIcon, NoteIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./EmployeeWorkspaceLayout.module.css";

type EmployeeNavNotificationTarget = "alerts" | "swap";

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
  },
  {
    to: "/schedule",
    label: "Schedule",
    mobileLabel: "Shifts",
    description: "See published shifts and ready-made plans.",
    icon: <ScheduleIcon size={18} />,
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

export function EmployeeWorkspaceLayout({ children }: PropsWithChildren) {
  const [isMobileTabsCollapsed, setIsMobileTabsCollapsed] = useState(false);
  const { session } = useAuth();
  const { notifications } = useRealtime();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const swaps = swapsQuery.data ?? [];
  const { pathname } = useLocation();
  const storageKey = getEmployeeNotificationReadStorageKey(session?.userName);
  const [readNotificationIds, setReadNotificationIds] = useState<Set<string>>(() =>
    readEmployeeNotificationIds(storageKey),
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
  const unreadNotificationTargets = useMemo(() => {
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

    notifications.forEach((notification) => {
      if (readNotificationIds.has(notification.id)) {
        return;
      }

      if (notification.kind === "shiftSwap" && notification.reason === "manager-manual-shift-offer-created") {
        targets.alerts = true;
        targets.swap = true;
      }
    });

    return targets;
  }, [notifications, readNotificationIds, swaps]);

  useEffect(() => {
    const refreshReadState = () => {
      setReadNotificationIds(readEmployeeNotificationIds(storageKey));
    };

    refreshReadState();

    const handleStorage = (event: StorageEvent) => {
      if (event.key === storageKey) {
        refreshReadState();
      }
    };

    window.addEventListener("storage", handleStorage);
    window.addEventListener(employeeNotificationReadStateEventName, refreshReadState);

    return () => {
      window.removeEventListener("storage", handleStorage);
      window.removeEventListener(employeeNotificationReadStateEventName, refreshReadState);
    };
  }, [storageKey]);

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
    </div>
  );
}
