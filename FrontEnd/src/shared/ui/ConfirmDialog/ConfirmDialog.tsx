import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import styles from "./ConfirmDialog.module.css";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmText?: string;
  cancelText?: string;
  footerSlot?: ReactNode;
};

const EXIT_ANIMATION_MS = 220;

export function ConfirmDialog({
  open,
  title,
  message,
  onConfirm,
  onCancel,
  confirmText = "Confirm",
  cancelText = "Cancel",
  footerSlot,
}: ConfirmDialogProps) {
  const [isMounted, setIsMounted] = useState(open);

  useEffect(() => {
    if (open) {
      setIsMounted(true);
      return;
    }

    const timeoutId = window.setTimeout(() => setIsMounted(false), EXIT_ANIMATION_MS);
    return () => window.clearTimeout(timeoutId);
  }, [open]);

  if (!isMounted) return null;

  return (
    <div
      className={`${styles.overlay} ${open ? styles.overlayOpen : styles.overlayClosing}`}
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div className={`${styles.dialog} ${open ? styles.dialogOpen : styles.dialogClosing}`}>
        <h3>{title}</h3>
        <p>{message}</p>
        <div className={styles.footer}>
          {footerSlot}
          <button type="button" onClick={onCancel}>
            {cancelText}
          </button>
          <button type="button" onClick={onConfirm}>
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );
}
