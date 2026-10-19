import { createPortal } from "react-dom";
import { useEffect, useId, useRef, useState } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { employeePushApi } from "../api/employeePushApi";
import { enableEmployeePush } from "../lib/enablePush";
import { employeePushStateChanged, getBrowserSubscription, pushAccount, pushSupport, registerWorker } from "../lib/pushSubscription";
import styles from "./EmployeePushInvitation.module.css";

const cooldownMs = 7 * 24 * 60 * 60 * 1000;
const shownUntil = new Map<number, number>();
const dismissalKey = (id: number) => `gf3.push.invitation.${id}`;
function isDismissed(id: number) {
  let until = shownUntil.get(id) ?? 0;
  try { until = Math.max(until, Number(localStorage.getItem(dismissalKey(id))) || 0); } catch { /* Session fallback still applies. */ }
  return until > Date.now();
}

export function EmployeePushInvitation() {
  useLanguageRevision();
  const { status, session } = useAuth();
  const employeeId = status === "authenticated" && session?.role === "employee" && session.employeeId && session.employeeId > 0 ? session.employeeId : null;
  return employeeId ? <EmployeeInvitation key={employeeId} employeeId={employeeId} /> : null;
}

function EmployeeInvitation({ employeeId }: { employeeId: number }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const operations = useRef<AbortController | null>(null);
  const [publicKey, setPublicKey] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  const [revision, setRevision] = useState(0);
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const refresh = () => { setPublicKey(null); setRevision(value => value + 1); };
    window.addEventListener(employeePushStateChanged, refresh);
    return () => window.removeEventListener(employeePushStateChanged, refresh);
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    operations.current = controller;
    if (isDismissed(employeeId) || pushSupport() !== "supported" || Notification.permission === "denied") return () => controller.abort();
    void (async () => {
      try {
        const config = await employeePushApi.getConfig(controller.signal);
        if (controller.signal.aborted || !config.enabled || !config.publicKey) return;
        await registerWorker();
        const subscription = await getBrowserSubscription();
        if (controller.signal.aborted || Notification.permission === "denied" ||
            subscription && pushAccount() === employeeId && Notification.permission === "granted") return;
        setPublicKey(config.publicKey);
      } catch { /* Optional invitation must never block the employee workspace. */ }
    })();
    return () => controller.abort();
  }, [employeeId, revision]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!publicKey || !dialog) return;
    let previousFocus: HTMLElement | null = null;
    let previousOverflow: string | null = null;
    const viewport = window.visualViewport;
    const updateViewport = () => {
      dialog.style.setProperty("--push-visual-height", `${viewport?.height ?? window.innerHeight}px`);
      dialog.style.setProperty("--push-visual-top", `${viewport?.offsetTop ?? 0}px`);
    };
    const showWhenReady = () => {
      const content = document.querySelector("[data-employee-content]");
      const loading = content && [...content.querySelectorAll('[role="status"]')].some(element => element.getAttribute("aria-label") === t("Loading page"));
      if (!content?.childElementCount || loading || document.querySelector('dialog[open], [role="dialog"][aria-modal="true"]') ||
          Notification.permission === "denied" || operations.current?.signal.aborted || isDismissed(employeeId)) return;
      previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
      previousOverflow = document.documentElement.style.overflow;
      document.documentElement.style.overflow = "hidden";
      updateViewport();
      dialog.showModal();
      shownUntil.set(employeeId, Date.now() + cooldownMs);
      observer.disconnect();
    };
    const observer = new MutationObserver(showWhenReady);
    observer.observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ["open", "aria-modal"] });
    const frame = requestAnimationFrame(showWhenReady);
    viewport?.addEventListener("resize", updateViewport);
    viewport?.addEventListener("scroll", updateViewport);
    window.addEventListener("resize", updateViewport);
    return () => {
      observer.disconnect(); cancelAnimationFrame(frame);
      viewport?.removeEventListener("resize", updateViewport);
      viewport?.removeEventListener("scroll", updateViewport);
      window.removeEventListener("resize", updateViewport);
      dialog.close();
      if (previousOverflow !== null) document.documentElement.style.overflow = previousOverflow;
      if (previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    };
  }, [publicKey, employeeId]);

  const dismiss = () => {
    if (busy) return;
    const until = Date.now() + cooldownMs;
    shownUntil.set(employeeId, until);
    try { localStorage.setItem(dismissalKey(employeeId), String(until)); } catch { /* Session fallback still applies. */ }
    setPublicKey(null);
  };
  const enable = () => {
    if (busy || !publicKey || !operations.current) return;
    const signal = operations.current.signal;
    const permissionRequest = enableEmployeePush(employeeId, publicKey, signal);
    setBusy(true); setError(false);
    void permissionRequest.then(permission => {
      if (signal.aborted) return;
      setBusy(false);
      if (permission === "granted" || permission === "denied") setPublicKey(null);
      else {
        try { localStorage.setItem(dismissalKey(employeeId), String(Date.now() + cooldownMs)); } catch { /* Session fallback still applies. */ }
        setPublicKey(null);
      }
    }).catch(() => { if (!signal.aborted) { setBusy(false); setError(true); } });
  };
  return createPortal(<dialog ref={dialogRef} className={styles.dialog} aria-labelledby={titleId} aria-describedby={descriptionId}
    onCancel={event => { event.preventDefault(); dismiss(); }} onClick={event => { if (event.target === event.currentTarget) dismiss(); }}>
    <span className={styles.bell} aria-hidden="true"><svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4M10 2h4" /></svg></span>
    <h2 id={titleId}>{t("Enable notifications")}</h2>
    <p id={descriptionId}>{t("Get updates about new shifts, schedules and accepted swaps, even when the app is closed.")}</p>
    {error && <p role="alert">{t("Could not update notifications. Please try again.")}</p>}
    <button type="button" className={styles.primary} disabled={busy} onClick={enable}>{t("Enable notifications")}</button>
    <button type="button" className={styles.secondary} disabled={busy} onClick={dismiss}>{t("Not now")}</button>
  </dialog>, document.body);
}
