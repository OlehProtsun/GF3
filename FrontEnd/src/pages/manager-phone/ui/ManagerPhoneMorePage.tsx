import { LogoutIcon } from "@shared/ui/icons";
import { useAuth } from "@app/providers/AuthProvider";
import { ManagerWorkspaceModeSwitch } from "@features/manager-workspace-mode/ui/ManagerWorkspaceModeSwitch";
import { t } from "@shared/i18n";
import { ManagerPhonePage } from "./ManagerPhonePage";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhoneMorePage() {
  const { session, logout } = useAuth();
  return <ManagerPhonePage>
    <section className={`${styles.card} ${styles.account}`}><h2>{session?.displayName}</h2><p>@{session?.userName} · {t("Manager")}</p><p>{t("Read only")}</p></section>
    <ManagerWorkspaceModeSwitch targetMode="pc" />
    <button className={styles.logout} onClick={() => void logout()}><LogoutIcon size={18} />{t("Log out")}</button>
  </ManagerPhonePage>;
}
