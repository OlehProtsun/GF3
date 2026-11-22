import { useSyncExternalStore } from "react";
import { NavLink } from "react-router-dom";
import { t } from "@shared/i18n";
import { getPwaLaunchMode, subscribePwaDisplayMode } from "@shared/lib/pwaDisplayMode";
import styles from "./EmployeePwaInstallBanner.module.css";

export function EmployeePwaInstallBanner() {
  const mode = useSyncExternalStore(subscribePwaDisplayMode, getPwaLaunchMode);
  if (mode === "pwa") return null;

  return <aside className={styles.banner}>
    <div>
      <strong>{t("GF is available as an app")}</strong>
      <p>{t("Open GF from your home screen.")}</p>
    </div>
    <NavLink to="/install">{t("Download")}</NavLink>
  </aside>;
}
