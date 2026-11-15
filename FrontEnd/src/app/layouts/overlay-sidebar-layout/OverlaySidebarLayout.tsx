import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { useState } from "react";
import type { ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { ManagerDockNav } from "./ManagerDockNav";
import styles from "./OverlaySidebarLayout.module.css";
import {
  EmployeeIcon,
  HomeIcon,
  LogoutIcon,
  ArrowIcon,
} from "@shared/ui/icons";
import { matchPath } from "@shared/lib/react-router-dom";
import { ManagerNotepad } from "@features/manager-notepad/ui/ManagerNotepad";
import { ManagerSystemNews } from "@features/manager-system-news/ui/ManagerSystemNews";

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
  useLanguageRevision();
  const { logout, session } = useAuth();
  const [isCollapsed, setIsCollapsed] = useState(() => session?.role === "manager");
  const { pathname } = useLocation();
  const isManager = session?.role === "manager";
  const isSystemManager = isManager && session?.isSystemManager === true;
  const accountPath = isManager ? "/manager-profile" : "/profile";
  const isContainerWideContent = pathname === "/container";
  const isUrlopListContent = pathname === "/urlop";
  const isUrlopDetailContent = Boolean(matchPath(pathname, "/urlop/:shopId"));
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
    isUrlopDetailContent ||
    isUrlopListContent ||
    isHomeWideContent ||
    isContainerWideContent ||
    isDatabaseWideContent ||
    isContainerGraphWideContent ||
    pathname === "/availability/new" ||
    Boolean(matchPath(pathname, "/availability/:availabilityId/edit")) ||
    Boolean(matchPath(pathname, "/availability/:availabilityId"));
  const isWideScrollableContent =
    isUrlopDetailContent ||
    isUrlopListContent ||
    isHomeWideContent ||
    isContainerWideContent ||
    isDatabaseWideContent ||
    isContainerGraphProfileContent;
  const isContainerGraphHeaderAligned = isContainerGraphEditorContent;
  const contentClassName = [
    styles.content,
    isManager ? styles.contentManagerSidebar : isCollapsed ? styles.contentExpanded : styles.contentShifted,
    isWideContent ? styles.contentWide : "",
    isWideScrollableContent ? styles.contentWideScrollable : "",
    isContainerGraphHeaderAligned ? styles.contentWideHeaderAligned : "",
    isManager ? styles.contentManager : "",
  ].filter(Boolean).join(" ");
  const mainNavItems: NavItemDefinition[] = [
        {
          label: t("Home"),
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
      {isManager ? <ManagerDockNav isSystemManager={isSystemManager} accountPath={accountPath} displayName={session?.displayName} userName={session?.userName}
        isCollapsed={isCollapsed} onToggleCollapse={() => setIsCollapsed(current => !current)} /> : <><button
        type="button"
        className={`${styles.openTab} ${
          isCollapsed ? styles.openTabVisible : styles.openTabHidden
        }`}
        onClick={() => setIsCollapsed(false)}
        aria-label={t("Open sidebar")}
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

        </div>

        <div className={styles.sidebarFooter}>
          <div className={styles.navItem}>
            <NavLink
              to={accountPath}
              className={({ isActive }) => `${styles.navButton} ${isActive ? styles.navButtonActive : ""}`}
              aria-label={isManager ? t("Open manager profile") : t("Open profile")}
              title={`${session?.displayName ?? t("Signed in")} (@${session?.userName ?? "account"})`}
            >
              <EmployeeIcon size={22} />
            </NavLink>
            <div className={styles.navLabel} aria-hidden="true">
              {isManager ? t("Manager") : t("Profile")}
            </div>
          </div>

          {!isManager ? (
            <div className={styles.navItem}>
              <button
                type="button"
                className={styles.navButton}
                onClick={() => {
                  void logout();
                }}
                aria-label={t("Log out")}
              >
                <LogoutIcon size={22} />
              </button>
              <div className={styles.navLabel} aria-hidden="true">
                {t("Log out")}</div>
            </div>
          ) : null}

          <button
            type="button"
            className={styles.powerButton}
            onClick={() => setIsCollapsed(true)}
            aria-label={t("Collapse sidebar")}
          >
            <span className={styles.navIcon}>
              <ArrowIcon size={20} className={styles.arrowLeft} style={{ transform: "scaleY(-1)translateX(2px) translateY(2px)" }} />
            </span>
          </button>
        </div>
      </aside></>}
      <main className={contentClassName}>
        <div className="container">{children}</div>
        {pathname === accountPath && <nav className={styles.legalLinks} aria-label={t("Legal documents")}>
            <a href="/legal/index.html">{t("Legal documents")}</a>
            <a href="/legal/regulamin.html">{t("Terms")}</a>
            <a href="/legal/polityka-prywatnosci.html">{t("Privacy policy")}</a>
          </nav>}
      </main>

      <div
        className={`${styles.backdrop} ${
          isCollapsed ? styles.backdropHidden : styles.backdropVisible
        }`}
        onClick={() => setIsCollapsed(true)}
        aria-hidden="true"
      />

      {isManager ? <><ManagerSystemNews /><ManagerNotepad /></> : null}

    </div>
  );
}


