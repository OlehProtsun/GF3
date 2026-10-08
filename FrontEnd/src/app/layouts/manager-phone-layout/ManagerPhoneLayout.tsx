import type { PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { HomeIcon, ContainerIcon, AvailabilityIcon, EmployeeIcon, SettingsIcon } from "@shared/ui/icons";
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
  const { pathname } = useLocation();
  const title = pathname.startsWith("/shop") ? "Shops" : tabs.find(tab => tab.to !== "/" && pathname.startsWith(tab.to))?.label ?? "Home";
  return <div className={styles.layout} data-manager-phone>
    <div className={styles.shell}>
      <header className={styles.topBar}><div><small>GF3 · {t("Manager")}</small><h1>{t(title)}</h1></div><span className={styles.badge}>{t("Read only")}</span></header>
      <main className={styles.content}>{children}</main>
      <nav className={styles.nav} aria-label={t("Manager navigation")}>
        {tabs.map(({ label, to, icon: Icon }) => {
          const active = to === "/" ? pathname === "/"
            : pathname === to || pathname.startsWith(`${to}/`) || (to === "/more" && pathname.startsWith("/shop"));
          return <NavLink key={to} to={to} aria-current={active ? "page" : undefined}
            className={`${styles.tab} ${active ? styles.active : ""}`}>
            <Icon size={22} /><span>{t(label)}</span>
          </NavLink>;
        })}
      </nav>
    </div>
  </div>;
}
