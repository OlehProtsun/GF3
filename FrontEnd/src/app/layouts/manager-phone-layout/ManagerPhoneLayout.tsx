import { useState, useEffect, useRef, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { BackIcon, HomeIcon, ContainerIcon, AvailabilityIcon, EmployeeIcon, SettingsIcon } from "@shared/ui/icons";
import styles from "./ManagerPhoneLayout.module.css";

const tabs = [
  { label: "Home", to: "/", icon: HomeIcon },
  { label: "Containers", to: "/container", icon: ContainerIcon },
  { label: "Dispo", to: "/availability", icon: AvailabilityIcon },
  { label: "Employees", to: "/employee", icon: EmployeeIcon },
  { label: "More", to: "/more", icon: SettingsIcon },
];
export function ManagerPhoneLayout({ children }: PropsWithChildren) {
  useLanguageRevision();
  const [isTabsCollapsed, setIsTabsCollapsed] = useState(false);
  const openButtonRef = useRef<HTMLButtonElement>(null);
  const navigationRef = useRef<HTMLElement>(null);
  const previousCollapsedRef = useRef(isTabsCollapsed);
  useEffect(() => {
    if (previousCollapsedRef.current === isTabsCollapsed) return;
    previousCollapsedRef.current = isTabsCollapsed;
    if (isTabsCollapsed) openButtonRef.current?.focus({ preventScroll: true });
    else navigationRef.current?.querySelector<HTMLAnchorElement>('[aria-current="page"]')?.focus({ preventScroll: true });
  }, [isTabsCollapsed]);
  const { pathname } = useLocation();
  return <div className={styles.layout} data-manager-phone>
    <button ref={openButtonRef} type="button" aria-label={t("Open navigation")}
      className={`${styles.openButton} ${isTabsCollapsed ? "" : styles.openHidden}`}
      inert={!isTabsCollapsed} onClick={() => setIsTabsCollapsed(false)}><BackIcon size={14} /></button>
    <div className={`${styles.shell} ${isTabsCollapsed ? styles.shellCollapsed : ""}`}>
      <main className={styles.content}>{children}</main>
      <nav ref={navigationRef} inert={isTabsCollapsed} className={`${styles.nav} ${isTabsCollapsed ? styles.navCollapsed : ""}`} aria-label={t("Manager navigation")}>
        {tabs.map(({ label, to, icon: Icon }) => {
          const active = to === "/" ? pathname === "/"
            : pathname === to || pathname.startsWith(`${to}/`) || (to === "/more" && pathname.startsWith("/shop"));
          return <NavLink key={to} to={to} tabIndex={0} aria-label={t(label)} aria-current={active ? "page" : undefined}
            className={`${styles.tab} ${active ? styles.active : ""}`}>
            <Icon size={20} className={to === "/" ? styles.homeIcon : undefined} /><span className={styles.tabLabel} aria-hidden="true">{t(label)}</span>
          </NavLink>;
        })}
        <button type="button" className={styles.collapseButton} aria-label={t("Collapse navigation")}
          onClick={() => setIsTabsCollapsed(true)}><BackIcon size={14} /></button>
      </nav>
    </div>
  </div>;
}
