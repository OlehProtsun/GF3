import { useEffect, useRef, useState } from "react";
import { t } from "@shared/i18n";
import { getAuthAccessToken } from "@shared/api/httpClient";
import { employeePushApi } from "../api/employeePushApi";
import { configuredPushKey, createPushOperation, employeePushStateChanged, getBrowserSubscription, isPushAssociated, pushSessionGuard, pushSupport, syncPushSubscription } from "../lib/pushSubscription";
import { enableEmployeePush } from "../lib/enablePush";
import { AccountSettingsIcon } from "@shared/ui/icons";
import styles from "./EmployeePushSettings.module.css";

const getPermission = (): NotificationPermission => typeof Notification === "undefined" ? "default" : Notification.permission;

export function EmployeePushSettings({ employeeId, appearance = "default" }: { employeeId: number; appearance?: "default" | "profile" }) {
  const [publicKey, setPublicKey] = useState<string | null>(null);
  const [serverDisabled, setServerDisabled] = useState(false);
  const [enabled, setEnabled] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState(false);
  const operations = useRef<AbortController | null>(null);
  const [revision, setRevision] = useState(0);
  const [permission, setPermission] = useState(getPermission);
  const support = pushSupport();
  const token = getAuthAccessToken();
  useEffect(() => {
    const refresh = () => {
      const currentPermission = getPermission();
      const associated = currentPermission === "granted" && isPushAssociated(employeeId);
      setPermission(currentPermission);
      setEnabled(associated);
      if (associated) {
        setError(false);
        setServerDisabled(false);
      }
    };
    window.addEventListener(employeePushStateChanged, refresh);
    return () => window.removeEventListener(employeePushStateChanged, refresh);
  }, [employeeId]);
  useEffect(() => {
    const refreshPermission = () => {
      if (!busy && getPermission() !== permission) setRevision(value => value + 1);
    };
    window.addEventListener("focus", refreshPermission);
    return () => window.removeEventListener("focus", refreshPermission);
  }, [busy, permission]);
  useEffect(() => {
    const controller = createPushOperation();
    operations.current = controller;
    const isCurrent = pushSessionGuard(controller.signal);
    const currentPermission = getPermission();
    setEnabled(false); setPublicKey(null); setServerDisabled(false); setBusy(true); setError(false);
    setPermission(currentPermission);
    void (async () => {
      try {
        if (!isCurrent() || support !== "supported" || currentPermission === "denied") return;
        if (currentPermission === "granted") {
          if (isPushAssociated(employeeId)) {
            const config = await employeePushApi.getConfig(controller.signal);
            if (!isCurrent()) return;
            if (!configuredPushKey(config)) {
              setServerDisabled(true);
              return;
            }
            const subscription = await getBrowserSubscription();
            if (!isCurrent()) return;
            if (subscription && isPushAssociated(employeeId) && getPermission() === "granted") {
              setEnabled(true);
              return;
            }
          }
          const result = await syncPushSubscription(employeeId, controller.signal, isCurrent);
          if (!isCurrent()) return;
          setEnabled(result === "enabled");
          setServerDisabled(result === "server-disabled");
        } else {
          const config = await employeePushApi.getConfig(controller.signal);
          if (!isCurrent()) return;
          const key = configuredPushKey(config);
          setPublicKey(key);
          setServerDisabled(!key);
        }
      } catch { if (isCurrent()) setError(true); }
      finally { if (isCurrent()) setBusy(false); }
    })();
    return () => controller.abort();
  }, [employeeId, support, token, revision]);

  const enable = () => {
    const signal = operations.current?.signal;
    if (!signal || !publicKey || busy || getPermission() !== "default") return;
    const isCurrent = pushSessionGuard(signal);
    const permissionRequest = enableEmployeePush(employeeId, publicKey, signal);
    setBusy(true); setError(false);
    void (async () => {
      try {
        const result = await permissionRequest;
        if (!isCurrent()) return;
        setPermission(result);
        setEnabled(result === "granted");
      } catch {
        if (isCurrent()) { setPermission(getPermission()); setError(true); setEnabled(false); }
      } finally { if (isCurrent()) setBusy(false); }
    })();
  };
  const canEnable = support === "supported" && permission === "default" && publicKey;
  const message = support === "install-ios" ? t("Install GF on Home Screen to enable notifications on iPhone")
    : support === "unsupported" ? t("Push unavailable on this browser")
    : permission === "denied" ? t("Notifications blocked — change system/browser settings")
    : serverDisabled ? t("Push is not configured on the server")
    : enabled && permission === "granted" ? t("Enabled on this device")
    : busy ? t("Loading...") : null;
  return <section className={[styles.settings, appearance === "profile" ? styles.profile : ""].filter(Boolean).join(" ")} aria-label={t("Push notifications")}>
    <div>{appearance === "profile" ? <div className={styles.profileHeader}><span className={styles.icon}><AccountSettingsIcon name="bell" /></span><div><h2>{t("Push notifications")}</h2><p>{t("Enable notifications to receive important updates.")}</p></div></div> : <strong>{t("Push notifications")}</strong>}{message && <p aria-live="polite">{message}</p>}<small>{t("iPhone and iPad require iOS 16.4 or later, Add to Home Screen, HTTPS and system permission. Delivery may be delayed by the system.")}</small></div>
    {error && <p role="alert">{t("Could not update notifications. Please try again.")}</p>}
    {error && !canEnable && permission !== "denied" && <button type="button" disabled={busy} onClick={() => setRevision(value => value + 1)}>{t("Retry")}</button>}
    {canEnable && <button type="button" disabled={busy} onClick={enable}>{t("Enable notifications")}</button>}
  </section>;
}
