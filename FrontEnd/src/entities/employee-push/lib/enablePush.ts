import { getAuthAccessToken } from "@shared/api/httpClient";
import { employeePushApi } from "../api/employeePushApi";
import { ensureSubscription, rememberPushAccount, serializeSubscription } from "./pushSubscription";

export async function enableEmployeePush(employeeId: number, publicKey: string, signal: AbortSignal): Promise<NotificationPermission> {
  const token = getAuthAccessToken();
  const checkSession = () => {
    if (signal.aborted || getAuthAccessToken() !== token) throw new DOMException("Session changed", "AbortError");
  };
  checkSession();
  // This runs synchronously in the Enable click, before any network or worker await.
  const permission = await (Notification.permission === "granted" ? Promise.resolve("granted" as const) : Notification.requestPermission());
  checkSession();
  if (permission !== "granted") return permission;
  const subscription = await ensureSubscription(publicKey);
  checkSession();
  await employeePushApi.upsertSubscription(serializeSubscription(subscription), signal);
  checkSession();
  rememberPushAccount(employeeId);
  return permission;
}
