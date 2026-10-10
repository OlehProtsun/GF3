import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useIsMutating } from "@tanstack/react-query";
import { useAuth } from "@app/providers/AuthProvider";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { EyeIcon, HomeIcon } from "@shared/ui/icons";
import styles from "./ManagerWorkspaceModeSwitch.module.css";

export function ManagerWorkspaceModeSwitch({ targetMode, compact = false }: { targetMode: "pc" | "phone"; compact?: boolean }) {
  const { setManagerWorkspaceMode } = useAuth();
  const navigate = useNavigate();
  const mutations = useIsMutating();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(false);
  const busy = useRef(false);
  const exchange = async (proceed: () => void) => {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError(false);
    try {
      await setManagerWorkspaceMode(targetMode);
      proceed();
    } catch { setError(true); }
    finally { busy.current = false; setPending(false); }
  };
  return <div className={compact ? styles.compact : styles.wrap}>
    <button className={`${styles.button} ${targetMode === "pc" ? styles.pcButton : ""}`} disabled={pending || mutations > 0} onClick={() => navigate("/", { replace: true, onProceed: proceed => void exchange(proceed) })}>
      {targetMode === "phone" ? <EyeIcon size={20} /> : <HomeIcon className={styles.homeIcon} size={20} />}
      <span>{t(targetMode === "phone" ? "Switch to Phone" : "Switch to PC")}<small>{t(targetMode === "phone" ? "Read only" : "Full access")}</small></span>
    </button>
    {mutations > 0 && <small role="status">{t("Wait for the current save to finish.")}</small>}
    {error && <ErrorBanner dismissible={false}>{t("Mode change failed. Please try again.")}</ErrorBanner>}
  </div>;
}
