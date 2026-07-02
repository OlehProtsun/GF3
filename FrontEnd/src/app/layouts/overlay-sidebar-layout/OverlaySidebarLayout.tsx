import { useState } from "react";
import type { ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import styles from "./OverlaySidebarLayout.module.css";
import {
  InfoIcon,
  EmployeeIcon,
  ShopIcon,
  AvailabilityIcon,
  ContainerIcon,
  HomeIcon,
  DatabaseIcon,
  LogoutIcon,
  ArrowIcon,
  NoteIcon,
} from "@shared/ui/icons";
import { matchPath } from "@shared/lib/react-router-dom";

type OverlaySidebarLayoutProps = {
  children: ReactNode;
};

type NavItemDefinition = {
  label: string;
  to: string;
  icon: React.ReactNode;
};

function NavItem({
  label,
  icon,
  to,
}: {
  label: string;
  icon: React.ReactNode;
  to: string;
}) {
  return (
    <div className={styles.navItem}>
      <NavLink
        to={to}
        className={({ isActive }) => `${styles.navButton} ${isActive ? styles.navButtonActive : ""}`}
        aria-label={label}
      >
        {icon}
      </NavLink>

      <div className={styles.navLabel} aria-hidden="true">
        {label}
      </div>
    </div>
  );
}

export function OverlaySidebarLayout({ children }: OverlaySidebarLayoutProps) {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const { pathname } = useLocation();
  const { logout, session } = useAuth();
  const isManager = session?.role === "manager";
  const accountPath = isManager ? "/manager-profile" : "/profile";
  const isContainerWideContent = pathname === "/container";
  const isHomeWideContent = pathname === "/";
  const isDatabaseWideContent = pathname === "/database";
  const isContainerGraphEditorContent =
    Boolean(matchPath(pathname, "/container/:containerId/graphs/new")) ||
    Boolean(matchPath(pathname, "/container/:containerId/graphs/:graphId/edit"));
  const isContainerGraphProfileContent =
    !isContainerGraphEditorContent &&
    Boolean(matchPath(pathname, "/container/:containerId/graphs/:graphId"));
  const isContainerGraphWideContent = isContainerGraphProfileContent || isContainerGraphEditorContent;
  const isWideContent =
    isHomeWideContent ||
    isContainerWideContent ||
    isDatabaseWideContent ||
    isContainerGraphWideContent ||
    pathname === "/availability/new" ||
    Boolean(matchPath(pathname, "/availability/:availabilityId/edit")) ||
    Boolean(matchPath(pathname, "/availability/:availabilityId"));
  const isWideScrollableContent =
    isHomeWideContent ||
    isContainerWideContent ||
    isDatabaseWideContent ||
    isContainerGraphProfileContent;
  const isContainerGraphHeaderAligned = isContainerGraphEditorContent;
  const contentClassName = [
    styles.content,
    isCollapsed ? styles.contentExpanded : styles.contentShifted,
    isWideContent ? styles.contentWide : "",
    isWideScrollableContent ? styles.contentWideScrollable : "",
    isContainerGraphHeaderAligned ? styles.contentWideHeaderAligned : "",
  ].filter(Boolean).join(" ");
  const mainNavItems: NavItemDefinition[] = isManager
    ? [
        {
          label: "Home",
          to: "/",
          icon: <img className={styles.navLogo} src="/gf-favicon.svg" alt="" aria-hidden="true" />,
        },
        { label: "Employee", to: "/employee", icon: <EmployeeIcon size={22} /> },
        { label: "Shop", to: "/shop", icon: <ShopIcon size={28} style={{ transform: "scaleY(-1)" }} /> },
        { label: "Availability", to: "/availability", icon: <AvailabilityIcon size={22} /> },
        { label: "Container", to: "/container", icon: <ContainerIcon size={25} /> },
        { label: "Information", to: "/information", icon: <InfoIcon size={26} /> },
        { label: "Message", to: "/communications", icon: <NoteIcon size={24} /> },
      ]
    : [
        {
          label: "Home",
          to: "/",
          icon: (
            <span className={`${styles.navIcon} ${styles.navIconHome}`}>
              <HomeIcon size={26} />
            </span>
          ),
        },
      ];

  return (
    <div className={styles.layout}>
      <button
        type="button"
        className={`${styles.openTab} ${
          isCollapsed ? styles.openTabVisible : styles.openTabHidden
        }`}
        onClick={() => setIsCollapsed(false)}
        aria-label="Open sidebar"
      >
        <span className={styles.navIcon}>
          <ArrowIcon size={20} className={styles.arrowDown} />
        </span>
      </button>

      <aside
        className={`${styles.sidebar} ${
          isCollapsed ? styles.collapsed : styles.expanded
        }`}
        aria-hidden={isCollapsed}
      >
        <div className={styles.sidebarScroll}>
          <div className={styles.nav}>
            {mainNavItems.map(item => (
              <NavItem key={item.to} label={item.label} to={item.to} icon={item.icon} />
            ))}
          </div>

          {isManager ? (
            <div className={styles.section}>
              <div className={styles.sectionTitle}>Settings</div>
              <div className={styles.nav}>
                <NavItem label="DataBase" to="/database" icon={<DatabaseIcon size={30} />} />
              </div>
            </div>
          ) : null}
        </div>

        <div className={styles.sidebarFooter}>
          <div className={styles.navItem}>
            <NavLink
              to={accountPath}
              className={({ isActive }) => `${styles.navButton} ${isActive ? styles.navButtonActive : ""}`}
              aria-label={isManager ? "Open manager profile" : "Open profile"}
              title={`${session?.displayName ?? "Signed in"} (@${session?.userName ?? "account"})`}
            >
              <EmployeeIcon size={22} />
            </NavLink>
            <div className={styles.navLabel} aria-hidden="true">
              {isManager ? "Manager" : "Profile"}
            </div>
          </div>

          <div className={styles.navItem}>
            <button
              type="button"
              className={styles.navButton}
              onClick={() => {
                void logout();
              }}
              aria-label="Log out"
            >
              <LogoutIcon size={22} />
            </button>
            <div className={styles.navLabel} aria-hidden="true">
              Log out
            </div>
          </div>

          <button
            type="button"
            className={styles.powerButton}
            onClick={() => setIsCollapsed(true)}
            aria-label="Collapse sidebar"
          >
            <span className={styles.navIcon}>
              <ArrowIcon size={20} className={styles.arrowLeft} style={{ transform: "scaleY(-1)translateX(2px) translateY(2px)" }} />
            </span>
          </button>
        </div>
      </aside>
      <main className={contentClassName}>
        <div className="container">{children}</div>
      </main>

      <div
        className={`${styles.backdrop} ${
          isCollapsed ? styles.backdropHidden : styles.backdropVisible
        }`}
        onClick={() => setIsCollapsed(true)}
        aria-hidden="true"
      />

    </div>
  );
}


