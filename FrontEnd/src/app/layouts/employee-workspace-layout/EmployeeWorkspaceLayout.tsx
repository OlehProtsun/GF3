import { useState, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { AvailabilityIcon, BackIcon, EmployeeIcon, HomeIcon, NoteIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./EmployeeWorkspaceLayout.module.css";

type EmployeeNavItem = {
  to: string;
  label: string;
  mobileLabel?: string;
  description: string;
  icon: React.ReactNode;
  iconClassName?: string;
};

const employeeNavItems: readonly EmployeeNavItem[] = [
  {
    to: "/",
    label: "Home",
    description: "Your personal desk for daily updates.",
    icon: <HomeIcon size={19} />,
    iconClassName: styles.homeIconFlipped,
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
  const { pathname } = useLocation();
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
            {employeeNavItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  [styles.desktopTab, isActive ? styles.desktopTabActive : ""].filter(Boolean).join(" ")
                }
              >
                <span className={[styles.tabIcon, item.iconClassName ?? ""].filter(Boolean).join(" ")}>{item.icon}</span>
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </header>

        <main className={styles.content}>{children}</main>
      </div>

      <nav className={mobileTabsClassName} aria-label="Employee sections">
        {employeeNavItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              [styles.mobileTab, isActive ? styles.mobileTabActive : ""].filter(Boolean).join(" ")
            }
          >
            <span className={[styles.tabIcon, item.iconClassName ?? ""].filter(Boolean).join(" ")}>{item.icon}</span>
            <span>{item.mobileLabel ?? item.label}</span>
          </NavLink>
        ))}

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
