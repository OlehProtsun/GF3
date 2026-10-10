import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { HomeIcon, EyeIcon, LogoutIcon } from "@shared/ui/icons";
import styles from "./ManagerWorkspaceModePicker.module.css";

export function ManagerWorkspaceModePicker() {
  const { session, setManagerWorkspaceMode, logout } = useAuth();
  const navigate = useNavigate();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(false);
  const busy = useRef(false);
  if (session?.role !== "manager" || session.workspaceMode !== "choose") return null;
  const choose = async (mode: "pc" | "phone") => {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError(false);
    try {
      await setManagerWorkspaceMode(mode);
      navigate("/", { replace: true });
    } catch {
      setError(true);
    } finally {
      busy.current = false;
      setPending(false);
    }
  };
  return <main className={styles.page}>
    <section className={styles.card} aria-labelledby="workspace-title" aria-busy={pending}>
      <img src="/gf-favicon.svg" alt="" className={styles.logo} />
      <h1 id="workspace-title">{t("Choose workspace")}</h1>
      <p>{t("How would you like to continue?")}</p>
      <div className={styles.choices}>
        <button disabled={pending} onClick={() => void choose("pc")}><HomeIcon className={styles.homeIcon} size={28} /><strong>{t("PC")}</strong><span>{t("Full access")}</span></button>
        <button disabled={pending} onClick={() => void choose("phone")}><EyeIcon size={28} /><strong>{t("Phone")}</strong><span>{t("Read only")}</span></button>
      </div>
      <p>{t("You can view all manager records but cannot edit.")}</p>
      {error && <ErrorBanner dismissible={false}>{t("Mode change failed. Please try again.")}</ErrorBanner>}
      <button className={styles.logout} disabled={pending} onClick={() => void logout()}><LogoutIcon size={18} />{t("Log out")}</button>
    </section>
  </main>;
}
