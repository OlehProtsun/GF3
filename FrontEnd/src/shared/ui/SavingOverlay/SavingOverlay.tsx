import { useEffect, useId, useState } from "react";
import { SaveIcon } from "@shared/ui/icons";
import styles from "./SavingOverlay.module.css";

type SavingOverlayProps = {
  active: boolean;
  delayMs?: number;
  title?: string;
  message?: string;
};

export function SavingOverlay({
  active,
  delayMs = 650,
  title = "Saving changes",
  message = "Please wait while we finish saving this page.",
}: SavingOverlayProps) {
  const titleId = useId();
  const messageId = useId();
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    if (!active) {
      setIsVisible(false);
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setIsVisible(true);
    }, delayMs);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [active, delayMs]);

  if (!isVisible) {
    return null;
  }

  return (
    <div
      className={styles.overlay}
      role="status"
      aria-live="polite"
      aria-labelledby={titleId}
      aria-describedby={messageId}
    >
      <div className={styles.panel}>
        <div className={styles.hero}>
          <div className={styles.spinnerShell} aria-hidden="true">
            <span className={styles.spinnerRing} />
            <span className={styles.spinnerCore}>
              <SaveIcon size={18} />
            </span>
          </div>

          <div className={styles.copy}>
            <h3 id={titleId}>{title}</h3>
            <p id={messageId}>{message}</p>
          </div>
        </div>

        <span className={styles.hint}>You can keep this page open while the save finishes.</span>
      </div>
    </div>
  );
}
