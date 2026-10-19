import { useEffect, useRef, useState } from "react";
import { t } from "@shared/i18n";
import { employeePushApi, type PushConfig } from "../api/employeePushApi";
import { disableForDevice, ensureSubscription, getBrowserSubscription, pushAccount, pushSupport, registerWorker, rememberPushAccount, serializeSubscription } from "../lib/pushSubscription";
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
    const controller = new AbortController();
    operations.current = controller;
    setEnabled(false); setBusy(true); setError(false);
    void (async () => {
      try {
        const settings = await employeePushApi.getConfig(controller.signal);
        if (controller.signal.aborted) return;
        setConfig(settings);
        if (support === "supported" && settings.enabled) {
          await registerWorker();
          const subscription = await getBrowserSubscription();
          if (!controller.signal.aborted) setEnabled(Boolean(subscription && pushAccount() === employeeId && Notification.permission === "granted"));
        }
      } catch { if (!controller.signal.aborted) setError(true); }
      finally { if (!controller.signal.aborted) setBusy(false); }
    })();
    return () => controller.abort();
  }, [employeeId, support, revision]);

  const enable = () => {
    const signal = operations.current.signal;
    // Start the permission request directly in the click's user gesture.
    const permissionRequest = Notification.permission === "granted" ? Promise.resolve("granted" as const) : Notification.requestPermission();
    setBusy(true); setError(false);
    void (async () => {
      try {
        const result = await permissionRequest;
        if (signal.aborted) return;
        setPermission(result);
        if (result !== "granted" || !config?.publicKey) return;
        const subscription = await ensureSubscription(config.publicKey);
        if (signal.aborted) return;
        await employeePushApi.upsertSubscription(serializeSubscription(subscription), signal);
        if (signal.aborted) return;
        rememberPushAccount(employeeId);
        setEnabled(true);
      } catch { setError(true); setEnabled(false); }
      finally { setBusy(false); }
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
    {support === "supported" && config?.enabled && permission !== "denied" && <button type="button" disabled={busy} onClick={enabled ? () => void disable() : enable}>{t(enabled ? "Disable notifications" : "Enable notifications")}</button>}
  </section>;
}
