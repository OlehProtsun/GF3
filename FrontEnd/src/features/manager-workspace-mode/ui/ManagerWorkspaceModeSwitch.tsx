import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useIsMutating } from "@tanstack/react-query";
import { useAuth } from "@app/providers/AuthProvider";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { EyeIcon, HomeIcon } from "@shared/ui/icons";
import styles from "./ManagerWorkspaceModeSwitch.module.css";

export function ManagerWorkspaceModeSwitch({ targetMode, compact = false, itemClassName, buttonClassName, labelClassName }: {
  targetMode: "pc" | "phone";
  compact?: boolean;
  itemClassName?: string;
  buttonClassName?: string;
  labelClassName?: string;
}) {
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
  const label = t(targetMode === "phone" ? "Switch to Phone" : "Switch to Desktop");
  return <div className={[compact ? styles.compact : styles.wrap, itemClassName].filter(Boolean).join(" ")}>
    <button className={compact && buttonClassName ? buttonClassName : `${styles.button} ${targetMode === "pc" ? styles.pcButton : ""}`} aria-label={label} aria-busy={pending} disabled={pending || mutations > 0} onClick={() => navigate("/", { replace: true, onProceed: proceed => void exchange(proceed) })}>
      {targetMode === "phone" ? <EyeIcon size={20} /> : <HomeIcon className={styles.homeIcon} size={20} />}
      {!compact && <span>{label}<small>{t(targetMode === "phone" ? "Read only" : "Full access")}</small></span>}
    </button>
    {compact && <div className={labelClassName} aria-hidden="true">{label}</div>}
    {mutations > 0 && <small role="status">{t("Wait for the current save to finish.")}</small>}
    {error && <ErrorBanner dismissible={false}>{t("Mode change failed. Please try again.")}</ErrorBanner>}
  </div>;
}
