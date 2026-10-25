import { useEffect, useId, useRef, type PropsWithChildren, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { CloseIcon } from "@shared/ui/icons";
import styles from "./ManagerSettingsPage.module.css";

export function SettingsDialog({ title, description, icon, className, onClose, children }: PropsWithChildren<{
  title: string;
  description?: string;
  icon?: ReactNode;
  className?: string;
  onClose: () => void;
}>) {
  useLanguageRevision();
  const titleId = useId();
  const descriptionId = useId();
  const panel = useRef<HTMLDivElement>(null);
  const close = useRef<HTMLButtonElement>(null);
  const onCloseRef = useRef(onClose);
  useEffect(() => { onCloseRef.current = onClose; }, [onClose]);

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    close.current?.focus();
    const keydown = (event: KeyboardEvent) => {
      if (event.key === "Escape") { event.preventDefault(); onCloseRef.current(); }
      if (event.key !== "Tab") return;
      const focusable = panel.current?.querySelectorAll<HTMLElement>("button:not(:disabled), input:not(:disabled):not([type='radio']), input[type='radio']:checked:not(:disabled), a[href]");
      if (!focusable?.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (!panel.current?.contains(document.activeElement)) { event.preventDefault(); first.focus(); }
      else if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    document.addEventListener("keydown", keydown);
    return () => {
      document.removeEventListener("keydown", keydown);
      document.body.style.overflow = overflow;
      opener?.focus();
    };
  }, []);

  return createPortal(
    <div className={styles.backdrop} onClick={event => { if (event.target === event.currentTarget) onClose(); }}>
      <div ref={panel} className={[styles.dialog, className].filter(Boolean).join(" ")} role="dialog" aria-modal="true" aria-labelledby={titleId} aria-describedby={description ? descriptionId : undefined}>
        <div className={styles.dialogHeading}>
          {icon ? <span className={styles.dialogIcon}>{icon}</span> : null}
          <div className={styles.dialogHeadingCopy}>
            <h2 id={titleId}>{title}</h2>
            {description ? <p id={descriptionId} className={styles.dialogDescription}>{description}</p> : null}
          </div>
          <button ref={close} className={styles.dialogClose} type="button" aria-label={t("Close")} onClick={onClose}><CloseIcon size={19} /></button>
        </div>
        {children}
      </div>
    </div>, document.body,
  );
}
