import { useState, useEffect, useLayoutEffect, useRef, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { BackIcon, HomeIcon, ContainerIcon, AvailabilityIcon, EmployeeIcon, SettingsIcon } from "@shared/ui/icons";
import { ManagerNotepad } from "@features/manager-notepad/ui/ManagerNotepad";
import { ManagerPhoneNotepadContext } from "./ManagerPhoneNotepadContext";
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
  const shellRef = useRef<HTMLDivElement>(null);
  const previousCollapsedRef = useRef(isTabsCollapsed);
  useEffect(() => {
    if (previousCollapsedRef.current === isTabsCollapsed) return;
    previousCollapsedRef.current = isTabsCollapsed;
    const frame = requestAnimationFrame(() => {
      const target = isTabsCollapsed ? openButtonRef.current
        : navigationRef.current?.querySelector<HTMLAnchorElement>('[aria-current="page"]');
      target?.focus({ preventScroll: true });
    });
    return () => cancelAnimationFrame(frame);
  }, [isTabsCollapsed]);
  const { pathname, search, hash } = useLocation();
  const pageLocation = `${pathname}${search}${hash}`;
  const [notepadLocation, setNotepadLocation] = useState<string | null>(null);
  const notepadLauncher = useRef<HTMLElement | null>(null);
  const isNotepadOpen = notepadLocation === pageLocation;
  if (notepadLocation !== null && !isNotepadOpen) setNotepadLocation(null);

  const openNotepad = (launcher?: HTMLElement) => {
    notepadLauncher.current = launcher ?? (document.activeElement instanceof HTMLElement ? document.activeElement : null);
    setNotepadLocation(pageLocation);
  };
  const closeNotepad = () => {
    setNotepadLocation(null);
    const launcher = notepadLauncher.current;
    requestAnimationFrame(() => {
      if (launcher?.isConnected) launcher.focus({ preventScroll: true });
    });
  };
  const isHome = pathname === "/";
  useLayoutEffect(() => {
    const shell = shellRef.current;
    const dock = isTabsCollapsed ? openButtonRef.current : navigationRef.current;
    if (!isHome || !shell || !dock) return;
    const update = () => {
      const bottom = Number.parseFloat(getComputedStyle(dock).bottom);
      if (Number.isFinite(bottom)) {
        shell.style.setProperty("--home-dock-space", `${dock.offsetHeight + bottom + 12}px`);
      }
    };
    update();
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
    observer?.observe(dock);
    window.addEventListener("resize", update);
    window.visualViewport?.addEventListener("resize", update);
    return () => {
      observer?.disconnect();
      window.removeEventListener("resize", update);
      window.visualViewport?.removeEventListener("resize", update);
      shell.style.removeProperty("--home-dock-space");
    };
  }, [isHome, isTabsCollapsed]);
  return <div className={styles.layout} data-manager-phone data-notepad-open={isNotepadOpen || undefined}>
    <button ref={openButtonRef} type="button" aria-label={t("Open navigation")}
      className={`${styles.openButton} ${isTabsCollapsed ? styles.openVisible : styles.openHidden}`}
      inert={!isTabsCollapsed || isNotepadOpen} onClick={() => setIsTabsCollapsed(false)}>
      <span className={`${styles.controlIcon} ${styles.controlIconExpand}`}><BackIcon size={14} /></span>
    </button>
    <div ref={shellRef} inert={isNotepadOpen} className={`${styles.shell} ${isTabsCollapsed ? styles.shellCollapsed : ""} ${isHome ? styles.shellHome : ""}`}>
      <main className={styles.content}>
        <ManagerPhoneNotepadContext.Provider value={openNotepad}>{children}</ManagerPhoneNotepadContext.Provider>
      </main>
      <nav ref={navigationRef} inert={isTabsCollapsed} className={`${styles.nav} ${isTabsCollapsed ? styles.navCollapsed : styles.navExpanded}`} aria-label={t("Manager navigation")}>
        {tabs.map(({ label, to, icon: Icon }) => {
          const active = to === "/" ? pathname === "/"
            : pathname === to || pathname.startsWith(`${to}/`) || (to === "/more" && pathname.startsWith("/shop"));
          return <NavLink key={to} to={to} tabIndex={0} aria-label={t(label)} aria-current={active ? "page" : undefined}
            className={`${styles.tab} ${active ? styles.active : ""}`}>
            <span className={styles.tabIcon}><Icon size={18} className={to === "/" ? styles.homeIcon : undefined} /></span>
            <span className={styles.tabLabel} aria-hidden="true">{t(label)}</span>
          </NavLink>;
        })}
        <button type="button" className={styles.collapseButton} aria-label={t("Collapse navigation")}
          onClick={() => setIsTabsCollapsed(true)}><span className={styles.controlIcon}><BackIcon size={14} /></span></button>
      </nav>
    </div>
    {isNotepadOpen && <ManagerNotepad onClosePhone={closeNotepad} />}
  </div>;
}
