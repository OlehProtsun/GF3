import { useId, useLayoutEffect, useRef, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { getPwaLaunchMode, subscribePwaDisplayMode } from "@shared/lib/pwaDisplayMode";
import styles from "./EmployeePwaInstallDialog.module.css";

const dismissalKey = "gf3.pwa.install.dismissed";
let dismissedInMemory = false;

function wasDismissed() {
  try {
    return dismissedInMemory || sessionStorage.getItem(dismissalKey) === "1";
  } catch {
    return dismissedInMemory;
  }
}

export function EmployeePwaInstallDialog() {
  const { status, session } = useAuth();
  const mode = useSyncExternalStore(subscribePwaDisplayMode, getPwaLaunchMode);
  const [dismissed, setDismissed] = useState(wasDismissed);
  if (dismissed || status !== "authenticated" || session?.role !== "employee" || mode !== "browser") return null;

  const dismiss = () => {
    try { sessionStorage.setItem(dismissalKey, "1"); } catch { dismissedInMemory = true; }
    setDismissed(true);
  };

  return <InstallDialog onDismiss={dismiss} />;
}

function InstallDialog({ onDismiss }: { onDismiss: () => void }) {
  useLanguageRevision();
  const navigate = useNavigate();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();

  useLayoutEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    const syncDialog = () => {
      const anotherModal = [...document.querySelectorAll('dialog[open], [role="dialog"][aria-modal="true"]')]
        .some(element => element !== dialog && !element.closest('[hidden], [inert], [aria-hidden="true"]'));
      if (anotherModal) {
        if (dialog.open) dialog.close();
      } else if (!dialog.open) {
        dialog.showModal();
      }
    };

    const observer = new MutationObserver(syncDialog);
    observer.observe(document.body, {
      subtree: true,
      childList: true,
      attributes: true,
      attributeFilter: ["open", "aria-modal", "aria-hidden", "hidden", "inert"],
    });
    syncDialog();
    return () => {
      observer.disconnect();
      if (dialog.open) dialog.close();
    };
  }, []);

  return createPortal(
    <dialog
      ref={dialogRef}
      className={styles.dialog}
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onCancel={event => { event.preventDefault(); onDismiss(); }}
    >
      <button type="button" className={styles.close} aria-label={t("Close")} onClick={onDismiss}>
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
          <path d="m5 5 14 14M19 5 5 19" />
        </svg>
      </button>
      <div className={styles.illustration} aria-hidden="true">
        <div className={styles.accents}><span /><span /><span /></div>
        <div className={styles.phone}><img src="/icons/gf-icon-192.png" alt="" /></div>
      </div>
      <h2 id={titleId} className={styles.title}>{t("Add GF to your home screen")}</h2>
      <p id={descriptionId} className={styles.description}>{t("Install the app for faster access and a better experience.")}</p>
      <div className={styles.actions}>
        <button type="button" className={styles.primary} onClick={() => {
          onDismiss();
          navigate("/install?topic=getting-started");
        }}>
          <svg aria-hidden="true" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 2v13m-5-5 5 5 5-5M3 17v4h18v-4" />
          </svg>
          {t("Install")}
        </button>
        <button type="button" className={styles.secondary} onClick={onDismiss}>{t("Not now")}</button>
      </div>
    </dialog>,
    document.body,
  );
}
