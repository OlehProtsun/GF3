import { useEffect, useId, type MouseEvent, type ReactNode } from "react";
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CloseIcon, WarnIcon } from "@shared/ui/icons";
import styles from "./ConfirmDialog.module.css";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  variant?: "warning" | "confirm";
  onConfirm: () => void;
  onCancel: () => void;
  confirmText?: string;
  cancelText?: string;
  footerSlot?: ReactNode;
  confirmDisabled?: boolean;
  cancelDisabled?: boolean;
};

export function ConfirmDialog({
  open,
  title,
  message,
  variant = "warning",
  onConfirm,
  onCancel,
  confirmText = "Confirm",
  cancelText = "Cancel",
  footerSlot,
  confirmDisabled = false,
  cancelDisabled = false,
}: ConfirmDialogProps) {
  const titleId = useId();
  const messageId = useId();

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !cancelDisabled) {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [cancelDisabled, onCancel, open]);

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget || cancelDisabled) {
      return;
    }

    onCancel();
  };
  const isConfirmVariant = variant === "confirm";

  return (
    <div
      className={`${styles.overlay} ${open ? styles.overlayOpen : styles.overlayClosing}`}
      role={open ? "dialog" : undefined}
      aria-modal={open ? true : undefined}
      aria-hidden={open ? undefined : true}
      aria-labelledby={open ? titleId : undefined}
      aria-describedby={open ? messageId : undefined}
      onMouseDown={handleOverlayMouseDown}
    >
      <div
        className={[
          styles.dialog,
          open ? styles.dialogOpen : styles.dialogClosing,
          isConfirmVariant ? styles.dialogConfirm : styles.dialogWarning,
        ]
          .filter(Boolean)
          .join(" ")}
      >
        <div className={styles.hero}>
          <div className={styles.iconShell} aria-hidden="true">
            {isConfirmVariant ? <CheckIcon size={22} /> : <WarnIcon size={22} />}
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
            icon={isConfirmVariant ? <CheckIcon size={16} /> : <WarnIcon size={16} />}
            customColor={isConfirmVariant ? "#2563eb" : "#dc2626"}
            customBorderColor={isConfirmVariant ? "#2563eb" : "#dc2626"}
            onClick={onConfirm}
            disabled={confirmDisabled}
          />
        </div>
      </div>
    </div>
  );
}
