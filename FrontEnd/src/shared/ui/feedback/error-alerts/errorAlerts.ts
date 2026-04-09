import { getErrorMessage } from "@shared/api/httpClient";

export type ErrorAlert = {
  id: number;
  title: string;
  message: string;
};

type ErrorAlertListener = (alerts: ErrorAlert[]) => void;

const listeners = new Set<ErrorAlertListener>();
const recentAlertTimestamps = new Map<string, number>();
let alerts: ErrorAlert[] = [];
let nextAlertId = 1;

function emitAlerts() {
  listeners.forEach((listener) => listener(alerts));
}

function shouldPublishAlert(message: string): boolean {
  const normalizedMessage = message.trim().toLowerCase();
  const now = Date.now();
  const recentTimestamp = recentAlertTimestamps.get(normalizedMessage);

  if (recentTimestamp && now - recentTimestamp < 1800) {
    return false;
  }

  recentAlertTimestamps.set(normalizedMessage, now);

  recentAlertTimestamps.forEach((timestamp, key) => {
    if (now - timestamp > 30_000) {
      recentAlertTimestamps.delete(key);
    }
  });

  return true;
}

export function subscribeToErrorAlerts(listener: ErrorAlertListener): () => void {
  listeners.add(listener);
  listener(alerts);

  return () => {
    listeners.delete(listener);
  };
}

export function dismissErrorAlert(alertId: number) {
  alerts = alerts.filter((alert) => alert.id !== alertId);
  emitAlerts();
}

export function pushErrorAlert(input: { title?: string; message: string }) {
  const message = input.message.trim();
  if (!message || !shouldPublishAlert(message)) {
    return;
  }

  const alert: ErrorAlert = {
    id: nextAlertId++,
    title: input.title?.trim() || "Request Error",
    message,
  };

  alerts = [...alerts, alert].slice(-4);
  emitAlerts();

  window.setTimeout(() => {
    dismissErrorAlert(alert.id);
  }, 6500);
}

export function pushErrorAlertFromError(error: unknown, fallbackMessage = "Something went wrong while processing the request.") {
  pushErrorAlert({
    message: getErrorMessage(error, fallbackMessage),
  });
}
