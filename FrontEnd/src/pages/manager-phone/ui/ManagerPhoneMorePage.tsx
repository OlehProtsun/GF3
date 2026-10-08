import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { ManagerWorkspaceModeSwitch } from "@features/manager-workspace-mode/ui/ManagerWorkspaceModeSwitch";
import { t } from "@shared/i18n";
import { ManagerPhonePage } from "./ManagerPhonePage";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhoneMorePage() {
  const { session, logout } = useAuth();
  return <ManagerPhonePage>
    <section className={styles.card}><h2>{session?.displayName}</h2><p>@{session?.userName} · {t("Manager")}</p><p>{t("Read only")}</p></section>
    <NavLink className={styles.tile} to="/shop">{t("Shops")} →</NavLink>
    <ManagerWorkspaceModeSwitch targetMode="pc" />
    <button className={styles.retry} onClick={() => void logout()}>{t("Log out")}</button>
  </ManagerPhonePage>;
}
