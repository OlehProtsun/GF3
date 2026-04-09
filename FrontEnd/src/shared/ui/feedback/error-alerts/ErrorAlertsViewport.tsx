import { useEffect, useState } from "react";
import { CloseIcon, ErrorIcon } from "@shared/ui/icons";
import { dismissErrorAlert, subscribeToErrorAlerts, type ErrorAlert } from "./errorAlerts";
import styles from "./ErrorAlertsViewport.module.css";

export function ErrorAlertsViewport() {
  const [alerts, setAlerts] = useState<ErrorAlert[]>([]);

  useEffect(() => subscribeToErrorAlerts(setAlerts), []);

  if (alerts.length === 0) {
    return null;
  }

  return (
    <div className={styles.viewport} aria-live="assertive" aria-atomic="true">
      {alerts.map((alert) => (
        <div key={alert.id} className={styles.alert} role="alert">
          <div className={styles.iconShell} aria-hidden="true">
            <ErrorIcon size={18} />
          </div>

          <div className={styles.copy}>
            <strong>{alert.title}</strong>
            <span>{alert.message}</span>
          </div>

          <button
            type="button"
            className={styles.closeButton}
            aria-label="Close error alert"
            onClick={() => dismissErrorAlert(alert.id)}
          >
            <CloseIcon size={14} />
          </button>
        </div>
      ))}
    </div>
  );
}
