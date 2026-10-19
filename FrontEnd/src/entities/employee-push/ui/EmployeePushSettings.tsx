import { useEffect, useRef, useState } from "react";
import { t } from "@shared/i18n";
import { employeePushApi, type PushConfig } from "../api/employeePushApi";
import { disableForDevice, employeePushStateChanged, getBrowserSubscription, pushAccount, pushSupport, registerWorker } from "../lib/pushSubscription";
import { enableEmployeePush } from "../lib/enablePush";
import styles from "./EmployeePushSettings.module.css";

export function EmployeePushSettings({ employeeId }: { employeeId: number }) {
  const [config, setConfig] = useState<PushConfig | null>(null);
  const [enabled, setEnabled] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState(false);
  const operations = useRef(new AbortController());
  const [revision, setRevision] = useState(0);
  const [permission, setPermission] = useState<NotificationPermission>(typeof Notification === "undefined" ? "default" : Notification.permission);
  const support = pushSupport();
  useEffect(() => {
    const refresh = () => setRevision(value => value + 1);
    window.addEventListener(employeePushStateChanged, refresh);
    return () => window.removeEventListener(employeePushStateChanged, refresh);
  }, []);
  useEffect(() => {
    const controller = new AbortController();
    operations.current = controller;
    setEnabled(false); setConfig(null); setBusy(true); setError(false);
    setPermission(typeof Notification === "undefined" ? "default" : Notification.permission);
    void (async () => {
      try {
        const settings = await employeePushApi.getConfig(controller.signal);
        if (controller.signal.aborted) return;
        setConfig(settings);
        if (support === "supported") {
          if (settings.enabled) await registerWorker();
          const subscription = await getBrowserSubscription();
          if (!controller.signal.aborted) setEnabled(Boolean(subscription && pushAccount() === employeeId));
        }
      } catch { if (!controller.signal.aborted) setError(true); }
      finally { if (!controller.signal.aborted) setBusy(false); }
    })();
    return () => controller.abort();
  }, [employeeId, support, revision]);

  const enable = () => {
    const signal = operations.current.signal;
    if (!config?.publicKey || busy) return;
    const permissionRequest = enableEmployeePush(employeeId, config.publicKey, signal);
    setBusy(true); setError(false);
    void (async () => {
      try {
        const result = await permissionRequest;
        if (signal.aborted) return;
        setPermission(result);
        setEnabled(result === "granted");
      } catch { if (!signal.aborted) { setError(true); setEnabled(false); } }
      finally { if (!signal.aborted) setBusy(false); }
    })();
  };
  const disable = async () => {
    setBusy(true); setError(false);
    try { await disableForDevice(); setEnabled(false); }
    catch { setError(true); setEnabled(false); }
    finally { setBusy(false); }
  };
  const message = support === "install-ios" ? t("Install GF on Home Screen to enable notifications on iPhone")
    : support === "unsupported" ? t("Push unavailable on this browser")
    : permission === "denied" ? t("Notifications blocked — change system/browser settings")
    : config && !config.enabled ? t("Push is not configured on the server")
    : enabled ? t("Enabled on this device") : t("Enable notifications");
  return <section className={styles.settings} aria-label={t("Push notifications")}>
    <div><strong>{t("Push notifications")}</strong><p>{message}</p><small>{t("iPhone and iPad require iOS 16.4 or later, Add to Home Screen, HTTPS and system permission. Delivery may be delayed by the system.")}</small></div>
    {error && <p role="alert">{t("Could not update notifications. Please try again.")}</p>}
    {error && !config && <button type="button" disabled={busy} onClick={() => setRevision(value => value + 1)}>{t("Retry")}</button>}
    {(enabled || support === "supported" && config?.enabled && permission !== "denied") && <button type="button" disabled={busy} onClick={enabled ? () => void disable() : enable}>{t(enabled ? "Disable notifications" : "Enable notifications")}</button>}
  </section>;
}
