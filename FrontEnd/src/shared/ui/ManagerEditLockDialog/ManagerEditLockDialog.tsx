import { t } from "@shared/i18n";
import { useId } from "react";
import { IosButton } from "@shared/ui/components/IosButton";
import { BackIcon, WarnIcon } from "@shared/ui/icons";
import styles from "./ManagerEditLockDialog.module.css";

type ManagerEditLockDialogProps = {
  open: boolean;
  title?: string;
  message: string;
  actionText?: string;
  onClose?: () => void;
};

export function ManagerEditLockDialog({
  open,
  title = t("Editing is locked"),
  message,
  actionText = t("Go back"),
  onClose,
}: ManagerEditLockDialogProps) {
  const titleId = useId();
  const messageId = useId();

  if (!open) {
    return null;
  }

  return (
    <section
      className={styles.panel}
      role="alert"
      aria-labelledby={titleId}
      aria-describedby={messageId}
    >
      <div className={styles.hero}>
        <div className={styles.iconShell} aria-hidden="true">
          <WarnIcon size={24} />
        </div>

        <div className={styles.copy}>
          <h3 id={titleId}>{title}</h3>
          <p id={messageId}>{message}</p>
        </div>
      </div>

      {onClose ? (
        <div className={styles.footer}>
          <IosButton
            label={actionText}
            icon={<BackIcon size={16} />}
            customColor="#dc2626"
            customBorderColor="#dc2626"
            onClick={onClose}
          />
        </div>
      ) : null}
    </section>
  );
}
