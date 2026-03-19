import { useEffect, useId, useState, type MouseEvent, type ReactNode } from "react";
import { IosButton } from "@shared/ui/components/IosButton";
import { CloseIcon, WarnIcon } from "@shared/ui/icons";
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
  confirmDisabled?: boolean;
  cancelDisabled?: boolean;
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
  confirmDisabled = false,
  cancelDisabled = false,
}: ConfirmDialogProps) {
  const [isMounted, setIsMounted] = useState(open);
  const titleId = useId();
  const messageId = useId();

  useEffect(() => {
    if (open) {
      setIsMounted(true);
      return;
    }

    const timeoutId = window.setTimeout(() => setIsMounted(false), EXIT_ANIMATION_MS);
    return () => window.clearTimeout(timeoutId);
  }, [open]);

  useEffect(() => {
    if (!isMounted) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !cancelDisabled) {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [cancelDisabled, isMounted, onCancel]);

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget || cancelDisabled) {
      return;
    }

    onCancel();
  };

  if (!isMounted) return null;

  return (
    <div
      className={`${styles.overlay} ${open ? styles.overlayOpen : styles.overlayClosing}`}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={messageId}
      onMouseDown={handleOverlayMouseDown}
    >
      <div className={`${styles.dialog} ${open ? styles.dialogOpen : styles.dialogClosing}`}>
        <div className={styles.hero}>
          <div className={styles.iconShell} aria-hidden="true">
            <WarnIcon size={22} />
          </div>

          <div className={styles.copy}>
            <h3 id={titleId}>{title}</h3>
            <p id={messageId}>{message}</p>
          </div>
        </div>

        <div className={styles.footer}>
          {footerSlot ? <div className={styles.footerSlot}>{footerSlot}</div> : null}

          <IosButton
            label={cancelText}
            icon={<CloseIcon size={16} />}
            variant="secondary"
            customColor="#e5e7eb"
            customBorderColor="#d1d5db"
            onClick={onCancel}
            disabled={cancelDisabled}
          />

          <IosButton
            label={confirmText}
            icon={<WarnIcon size={16} />}
            customColor="#dc2626"
            customBorderColor="#dc2626"
            onClick={onConfirm}
            disabled={confirmDisabled}
          />
        </div>
      </div>
    </div>
  );
}
