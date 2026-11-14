import { employeePushApi } from "../api/employeePushApi";
import { ensureSubscription, pushSessionGuard, rememberPushAccount, serializeSubscription } from "./pushSubscription";

export async function enableEmployeePush(employeeId: number, publicKey: string, signal: AbortSignal): Promise<NotificationPermission> {
  const isCurrent = pushSessionGuard(signal);
  const checkSession = () => {
    if (!isCurrent()) throw new DOMException("Session changed", "AbortError");
  };
  checkSession();
  // This runs synchronously in the Enable click, before any network or worker await.
  const permission = await (Notification.permission === "default" ? Notification.requestPermission() : Notification.permission);
  checkSession();
  if (permission !== "granted") return permission;
  const subscription = await ensureSubscription(publicKey, isCurrent);
  checkSession();
  if (Notification.permission !== "granted") return Notification.permission;
  await employeePushApi.upsertSubscription(serializeSubscription(subscription), signal);
  checkSession();
  if (Notification.permission !== "granted") return Notification.permission;
  rememberPushAccount(employeeId);
  return permission;
}
