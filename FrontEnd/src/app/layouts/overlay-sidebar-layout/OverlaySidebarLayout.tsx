import { useState } from "react";
import type { ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import styles from "./OverlaySidebarLayout.module.css";
import {
  InfoIcon,
  EmployeeIcon,
  ShopIcon,
  AvailabilityIcon,
  ContainerIcon,
  HomeIcon,
  DatabaseIcon,
  ArrowIcon,
} from "@shared/ui/icons";
import { matchPath } from "@shared/lib/react-router-dom";

type OverlaySidebarLayoutProps = {
  children: ReactNode;
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
  const isContainerWideContent = pathname === "/container";
  const isContainerGraphWideContent =
    Boolean(matchPath(pathname, "/container/:containerId/graphs/new")) ||
    Boolean(matchPath(pathname, "/container/:containerId/graphs/:graphId/edit")) ||
    Boolean(matchPath(pathname, "/container/:containerId/graphs/:graphId"));
  const isWideContent =
    isContainerWideContent ||
    isContainerGraphWideContent ||
    pathname === "/availability/new" ||
    Boolean(matchPath(pathname, "/availability/:availabilityId/edit")) ||
    Boolean(matchPath(pathname, "/availability/:availabilityId"));
  const contentClassName = [
    styles.content,
    isCollapsed ? styles.contentExpanded : styles.contentShifted,
    isWideContent ? styles.contentWide : "",
    isContainerWideContent ? styles.contentWideScrollable : "",
  ].filter(Boolean).join(" ");

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
        <div className={styles.nav}>
          <NavItem
            label="Home"
            to="/"
            icon={
              <span className={`${styles.navIcon} ${styles.navIconHome}`}>
                <HomeIcon size={26} />
              </span>
            }
          />
          <NavItem label="Employee" to="/employee" icon={<EmployeeIcon size={22} />} />
          <NavItem label="Shop" to="/shop" icon={<ShopIcon size={28} style={{ transform: "scaleY(-1)" }} />} />
          <NavItem label="Availability" to="/availability" icon={<AvailabilityIcon size={22} style={{ transform: "scaleY(-1)" }} />} />
          <NavItem label="Container" to="/container" icon={<ContainerIcon size={25} />} />
          <NavItem label="Information" to="/information" icon={<InfoIcon size={26} />} />
        </div>

        <div className={`${styles.section} ${styles.sectionBottom}`}>
          <div className={styles.sectionTitle}>Settings</div>
          <div className={styles.nav}>
            <NavItem label="DataBase" to="/database" icon={<DatabaseIcon size={30} style={{ transform: "scaleY(-1)" }} />} />
          </div>
        </div>

        <div className={styles.sidebarFooter}>
          <button
            type="button"
            className={styles.powerButton}
            onClick={() => setIsCollapsed(true)}
            aria-label="Power Off"
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


