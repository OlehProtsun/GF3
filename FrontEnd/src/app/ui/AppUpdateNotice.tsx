import { useEffect, useRef, useState } from "react";
import { fetchLatestBuildId, isNewBuild } from "@shared/lib/appUpdate";
import { t } from "@shared/i18n";
import styles from "./AppUpdateNotice.module.css";

export function AppUpdateNotice() {
  const [updateAvailable, setUpdateAvailable] = useState(false);
  const inFlight = useRef(false);

  useEffect(() => {
    if (!import.meta.env.PROD || updateAvailable) return;
    const controller = new AbortController();
    let stopped = false;
    const check = async () => {
      if (stopped || document.visibilityState !== "visible" || inFlight.current) return;
      inFlight.current = true;
      try {
        const buildId = await fetchLatestBuildId(controller.signal);
        if (!controller.signal.aborted && isNewBuild(__GF3_BUILD_ID__, buildId)) {
          stopped = true;
          setUpdateAvailable(true);
        }
      } finally {
        if (!controller.signal.aborted) inFlight.current = false;
      }
    };
    void check();
    const interval = window.setInterval(() => { void check(); }, 60_000);
    const onResume = () => { void check(); };
    document.addEventListener("visibilitychange", onResume);
    window.addEventListener("focus", onResume);
    return () => {
      stopped = true;
      controller.abort();
      inFlight.current = false;
      window.clearInterval(interval);
      document.removeEventListener("visibilitychange", onResume);
      window.removeEventListener("focus", onResume);
    };
  }, [updateAvailable]);

  if (!updateAvailable) return null;
  return <aside className={styles.notice} aria-label={t("New version available. Save your changes before updating.")}>
    <span role="status">{t("New version available. Save your changes before updating.")}</span>
    <button type="button" onClick={() => window.location.reload()}>{t("Update now")}</button>
  </aside>;
}
