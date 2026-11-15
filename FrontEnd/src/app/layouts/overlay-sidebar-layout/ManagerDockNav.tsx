import { useEffect, useRef, useState, type ReactElement, type ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { managerHomeActivityApi, type ManagerNavBadges } from "@pages/home/api/managerHomeActivityApi";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { ArrowIcon, AvailabilityIcon, ContainerIcon, DatabaseIcon, EmployeeIcon, InfoIcon, NoteIcon, ScheduleIcon, SettingsIcon, ShopIcon } from "@shared/ui/icons";
import { useManagerDockMotion } from "./useManagerDockMotion";
import styles from "./ManagerDockNav.module.css";
import sidebarStyles from "./OverlaySidebarLayout.module.css";

// Kept beside the dock so nested-route behavior can be verified independently.
// eslint-disable-next-line react-refresh/only-export-components
export function isManagerDockRouteActive(pathname: string, to: string): boolean {
  return to === "/" || to === "/manager-profile" ? pathname === to : pathname === to || pathname.startsWith(to + "/");
}

type Item = { to: string; label: string; tone: string; icon: ReactNode; count?: number };
type Overlay = { to: string; y: number; left: number };
const countValue = (value: number) => Number.isFinite(value) ? Math.max(0, Math.floor(value)) : 0;

export function ManagerDockNav({ isSystemManager, accountPath, displayName, userName, isCollapsed, onToggleCollapse }: {
  isSystemManager: boolean; accountPath: string; displayName?: string | null; userName?: string | null;
  isCollapsed: boolean; onToggleCollapse: () => void;
}): ReactElement {
  useLanguageRevision();
  const { pathname } = useLocation();
  const navRef = useRef<HTMLElement>(null);
  const motion = useManagerDockMotion(navRef);
  const { reset } = motion;
  const client = useQueryClient();
  const { data } = useQuery({ queryKey: queryKeys.home.navBadges(), queryFn: ({ signal }) => managerHomeActivityApi.navBadges(signal), staleTime: 30_000 });
  const baseline = useRef<ManagerNavBadges | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const [tooltip, setTooltip] = useState<(Overlay & { pathname: string }) | null>(null);
  const [toast, setToast] = useState<(Overlay & { delta: number; id: number }) | null>(null);
  const position = (to: string): Overlay => {
    const anchor = navRef.current?.querySelector<HTMLElement>(`[data-manager-dock-anchor][href="${to}"]`);
    const box = anchor?.getBoundingClientRect();
    const rail = navRef.current?.getBoundingClientRect();
    return { to, y: Math.max(36, Math.min(window.innerHeight - 36, box ? box.top + box.height / 2 : 64)), left: (rail?.right ?? 100) + 12 };
  };
  useEffect(() => { reset(); setTooltip(null); }, [pathname, isSystemManager, isCollapsed, reset]);
  useEffect(() => {
    if (!data) return;
    const next = { totalPending: countValue(data.totalPending), graphPending: countValue(data.graphPending), urlopPending: countValue(data.urlopPending) };
    const previous = baseline.current;
    baseline.current = next;
    if (!previous || next.totalPending <= previous.totalPending) return;
    const graphRose = next.graphPending > previous.graphPending;
    const urlopRose = next.urlopPending > previous.urlopPending;
    const to = graphRose && !urlopRose ? "/container" : urlopRose && !graphRose ? "/urlop" : "/";
    setTooltip(current => current?.to === to ? null : current);
    setToast({ ...position(to), delta: next.totalPending - previous.totalPending, id: Date.now() });
    clearTimeout(timer.current);
    timer.current = setTimeout(() => setToast(null), 3500);
  }, [data]);
  useEffect(() => () => clearTimeout(timer.current), []);
  useEffect(() => {
    const refresh = () => {
      if (document.visibilityState === "visible" && Date.now() - client.getQueryState(queryKeys.home.navBadges()).updatedAt > 30_000) {
        client.invalidateQueries({ queryKey: queryKeys.home.navBadges(), exact: true });
      }
    };
    window.addEventListener("focus", refresh);
    document.addEventListener("visibilitychange", refresh);
    return () => { window.removeEventListener("focus", refresh); document.removeEventListener("visibilitychange", refresh); };
  }, [client]);
  useEffect(() => {
    const hide = () => setTooltip(null);
    const reposition = () => {
      hide();
      setToast(current => current ? { ...current, ...position(current.to) } : null);
    };
    window.addEventListener("resize", reposition);
    window.addEventListener("blur", hide);
    const nav = navRef.current;
    nav?.addEventListener("scroll", reposition, true);
    const observer = new ResizeObserver(reposition);
    if (nav) observer.observe(nav);
    return () => { observer.disconnect(); window.removeEventListener("resize", reposition); window.removeEventListener("blur", hide); nav?.removeEventListener("scroll", reposition, true); };
  }, []);
  const items: Item[] = [
    { to: "/", label: t("Home"), tone: "home", icon: <img src="/gf-favicon.svg" alt="" />, count: data && countValue(data.totalPending) },
    { to: "/employee", label: t("Employee"), tone: "employee", icon: <EmployeeIcon size={24} /> },
    { to: "/shop", label: t("Shop"), tone: "shop", icon: <ShopIcon size={26} style={{ transform: "scaleY(-1)" }} /> },
    { to: "/availability", label: t("Availability"), tone: "availability", icon: <AvailabilityIcon size={24} /> },
    { to: "/urlop", label: t("Urlop"), tone: "urlop", icon: <ScheduleIcon size={24} />, count: data && countValue(data.urlopPending) },
    { to: "/container", label: t("Container"), tone: "container", icon: <ContainerIcon size={25} />, count: data && countValue(data.graphPending) },
    ...(isSystemManager ? [{ to: "/information", label: t("Information"), tone: "information", icon: <InfoIcon size={26} /> }] : []),
    { to: "/communications", label: t("Message"), tone: "message", icon: <NoteIcon size={24} /> },
    { to: "/settings", label: t("Settings"), tone: "settings", icon: <SettingsIcon size={24} /> },
    ...(isSystemManager ? [{ to: "/database", label: t("DataBase"), tone: "database", icon: <DatabaseIcon size={26} /> }] : []),
  ];
  const profile: Item = { to: accountPath, label: t("Open manager profile"), tone: "profile", icon: <EmployeeIcon size={24} /> };
  const showTooltip = (to: string) => setTooltip({ ...position(to), pathname });
  const renderItem = (item: Item) => {
    const active = isManagerDockRouteActive(pathname, item.to);
    return <div key={item.to} className={styles.navItem}>
      <NavLink to={item.to} aria-label={item.label} aria-current={active ? "page" : undefined}
      title={item === profile ? `${displayName ?? t("Signed in")} (@${userName ?? "account"})` : undefined}
      data-manager-dock-anchor="" data-tone={item.tone} className={`${styles.dockLink} ${active ? styles.dockLinkActive : ""}`}
      onPointerEnter={event => { if (event.pointerType !== "touch") showTooltip(item.to); }} onPointerLeave={() => setTooltip(null)}
      onFocus={() => showTooltip(item.to)} onBlur={() => setTooltip(null)}>
      <span className={styles.dockFace} data-manager-dock-face="" aria-hidden="true">
        <span className={styles.dockFaceInner}><span className={styles.dockIcon}>{item.icon}</span></span>
        {item.count != null && item.count > 0 && <span key={item.count} className={styles.badge} data-manager-dock-badge="">{item.count >= 100 ? "99+" : item.count}</span>}
      </span>
      </NavLink>
      <div className={styles.navLabel} aria-hidden="true"><span>{item === profile ? t("Manager") : item.label}</span></div>
    </div>;
  };
  const tooltipItem = [...items, profile].find(item => item.to === tooltip?.to);
  return <>
    {isCollapsed && <button type="button" className={`${sidebarStyles.openTab} ${sidebarStyles.openTabVisible} ${styles.reopenButton}`}
      onClick={onToggleCollapse} aria-label={t("Open sidebar")} aria-expanded="false" aria-controls="manager-sidebar-navigation">
      <ArrowIcon size={20} className={sidebarStyles.arrowDown} />
    </button>}
    <nav ref={navRef} id="manager-sidebar-navigation" aria-label={t("Manager navigation")} data-collapsed={isCollapsed} aria-hidden={isCollapsed} inert={isCollapsed}
      className={styles.rail} onPointerEnter={motion.onPointerMove} onPointerMove={motion.onPointerMove} onPointerLeave={motion.onPointerLeave}>
      <div className={styles.navScroll}><div className={styles.navGroup}>{items.map(item => <div key={item.to}>
        {item.to === "/database" && <div className={styles.divider} />}{renderItem(item)}
      </div>)}</div></div>
      <div className={styles.profileFooter}>{renderItem(profile)}
        <div className={styles.collapseControl} aria-hidden={isCollapsed} inert={isCollapsed}><div>
          <button type="button" className={`${sidebarStyles.powerButton} ${styles.collapseButton}`} onPointerEnter={reset}
            onClick={onToggleCollapse} aria-label={t("Collapse sidebar")} aria-expanded={!isCollapsed} aria-controls="manager-sidebar-navigation">
            <ArrowIcon size={20} className={sidebarStyles.arrowLeft} />
          </button>
        </div></div>
      </div>
      <span className={styles.srOnly} role="status">{data ? t("{0} pending approval requests", countValue(data.totalPending)) : ""}</span>
    </nav>
    {!isCollapsed && tooltip && tooltip.pathname === pathname && tooltipItem && (!toast || (toast.to !== tooltip.to && Math.abs(toast.y - tooltip.y) > 72)) && <div role="tooltip" className={styles.tooltip} style={{ left: tooltip.left, top: tooltip.y, maxWidth: `calc(100vw - ${tooltip.left + 8}px)` }}>
      {tooltipItem.label}{(tooltipItem.count ?? 0) > 0 && <span>{t("{0} pending", tooltipItem.count)}</span>}
    </div>}
    {!isCollapsed && toast && <div key={toast.id} className={styles.toast} aria-live="polite" data-anchor={toast.to} style={{ left: toast.left, top: toast.y, maxWidth: `calc(100vw - ${toast.left + 8}px)` }}>{t("New approval requests: {0}", toast.delta)}</div>}
  </>;
}
