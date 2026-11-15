import { getAuthAccessToken } from "@shared/api/httpClient";
import { employeePushApi, type BrowserPushSubscription, type PushConfig } from "../api/employeePushApi";

const accountKey = "gf3.push.employeeId";
export const employeePushStateChanged = "gf3:employee-push-state-changed";
let workerPromise: Promise<ServiceWorkerRegistration> | undefined;
let subscribePromise: Promise<PushSubscription> | undefined;
let unsubscribePromise: Promise<boolean> | undefined;
let deviceSession = new AbortController();
let pendingLogout: { token: string | null } | undefined;
let association: { employeeId: number; token: string | null } | undefined;

export function createPushOperation() {
  const controller = new AbortController();
  const deviceSignal = deviceSession.signal;
  const abort = () => controller.abort();
  deviceSignal.addEventListener("abort", abort, { once: true });
  controller.signal.addEventListener("abort", () => deviceSignal.removeEventListener("abort", abort), { once: true });
  return controller;
}

export function pushSessionGuard(signal: AbortSignal, isCurrent = () => true) {
  const token = getAuthAccessToken();
  const deviceSignal = deviceSession.signal;
  return () => token !== null && !signal.aborted && !deviceSignal.aborted && pendingLogout?.token !== token &&
    getAuthAccessToken() === token && isCurrent();
}

export function pushSupport(): "supported" | "install-ios" | "unsupported" {
  const ios = /iPad|iPhone|iPod/.test(navigator.userAgent) || navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1;
  const standalone = (navigator as Navigator & { standalone?: boolean }).standalone === true || window.matchMedia?.("(display-mode: standalone)").matches;
  if (ios && !standalone) return "install-ios";
  return window.isSecureContext && "serviceWorker" in navigator && "PushManager" in window && "Notification" in window ? "supported" : "unsupported";
}

export function pushAccount(): number | null {
  try { const id = Number(localStorage.getItem(accountKey)); return Number.isSafeInteger(id) && id > 0 ? id : null; }
  catch { return null; }
}
export function rememberPushAccount(employeeId: number) {
  association = { employeeId, token: getAuthAccessToken() };
  try { localStorage.setItem(accountKey, String(employeeId)); } catch { /* Storage is optional. */ }
  window.dispatchEvent(new Event(employeePushStateChanged));
}
export function isPushAssociated(employeeId: number) {
  return association?.employeeId === employeeId && association.token === getAuthAccessToken();
}
function forgetPushAccount() {
  association = undefined;
  try { localStorage.removeItem(accountKey); } catch { /* Storage is optional. */ }
  window.dispatchEvent(new Event(employeePushStateChanged));
}

export function registerWorker() {
  workerPromise ??= navigator.serviceWorker.register("/sw.js", { scope: "/" })
    .then(() => navigator.serviceWorker.ready)
    .catch(error => { workerPromise = undefined; throw error; });
  return workerPromise;
}

export async function getBrowserSubscription() {
  if (subscribePromise) await subscribePromise.catch(() => null);
  const registration = await navigator.serviceWorker.getRegistration("/sw.js");
  return registration?.pushManager ? registration.pushManager.getSubscription() : null;
}

export function vapidKeyBytes(value: string): Uint8Array<ArrayBuffer> {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  return Uint8Array.from(atob(base64), char => char.charCodeAt(0));
}

export function configuredPushKey(config: PushConfig) {
  if (!config.enabled || !config.publicKey) return null;
  try {
    const bytes = vapidKeyBytes(config.publicKey);
    return bytes.length === 65 && bytes[0] === 4 ? config.publicKey : null;
  } catch {
    return null;
  }
}

export function serializeSubscription(subscription: PushSubscription): BrowserPushSubscription {
  const json = subscription.toJSON();
  if (!json.endpoint || !json.keys?.p256dh || !json.keys?.auth) throw new Error("Invalid browser subscription");
  return { endpoint: json.endpoint, keys: { p256dh: json.keys.p256dh, auth: json.keys.auth } };
}

export async function ensureSubscription(publicVapidKey: string, isCurrent = () => true) {
  const checkPermission = () => {
    if (!isCurrent()) throw new DOMException("Session changed", "AbortError");
    if (pushSupport() !== "supported" || Notification.permission !== "granted") throw new Error("Push permission unavailable");
  };
  checkPermission();
  const registration = await registerWorker();
  if (unsubscribePromise) await bounded(unsubscribePromise, 1000);
  checkPermission();
  const current = await registration.pushManager.getSubscription();
  checkPermission();
  if (current) return current;
  subscribePromise ??= registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: vapidKeyBytes(publicVapidKey) })
    .finally(() => { subscribePromise = undefined; });
  return subscribePromise;
}

export async function syncPushSubscription(employeeId: number, signal: AbortSignal, isCurrent = () => true) {
  const sessionIsCurrent = pushSessionGuard(signal, isCurrent);
  const canSync = () => sessionIsCurrent() && pushSupport() === "supported" && Notification.permission === "granted";
  if (!canSync()) return;
  try {
    const config = await employeePushApi.getConfig(signal);
    if (!canSync()) return;
    const publicKey = configuredPushKey(config);
    if (!publicKey) {
      if (isPushAssociated(employeeId)) forgetPushAccount();
      return "server-disabled";
    }
    const subscription = await ensureSubscription(publicKey, canSync);
    if (!canSync()) return;
    await employeePushApi.upsertSubscription(serializeSubscription(subscription), signal);
    if (!canSync()) return;
    rememberPushAccount(employeeId);
    return "enabled";
  } catch (error) {
    if (!canSync()) return;
    if (isPushAssociated(employeeId)) forgetPushAccount();
    throw error;
  }
}

async function bounded<T>(operation: Promise<T>, milliseconds: number): Promise<T> {
  let timer = 0;
  try { return await Promise.race([operation, new Promise<never>((_, reject) => { timer = window.setTimeout(() => reject(new Error("Push operation timed out")), milliseconds); })]); }
  finally { window.clearTimeout(timer); }
}

export async function disableForDevice(isCurrent = () => true) {
  if (!isCurrent()) return;
  const logout = { token: getAuthAccessToken() };
  pendingLogout = logout;
  const previousDeviceSession = deviceSession;
  deviceSession = new AbortController();
  previousDeviceSession.abort();
  try {
    if (!("serviceWorker" in navigator)) return;
    const subscription = await bounded(getBrowserSubscription(), 1000);
    if (!subscription || !isCurrent()) return;
    const controller = new AbortController();
    const timer = window.setTimeout(() => controller.abort(), 1500);
    try { await bounded(employeePushApi.deleteSubscription(subscription.endpoint, controller.signal), 1500); }
    catch { /* Local unsubscribe must also work offline. */ }
    finally {
      window.clearTimeout(timer);
      controller.abort();
      if (isCurrent()) {
        const operation = subscription.unsubscribe().finally(() => {
          if (unsubscribePromise === operation) unsubscribePromise = undefined;
        });
        unsubscribePromise = operation;
        await bounded(operation, 1000);
      }
    }
  } finally {
    if (isCurrent()) forgetPushAccount();
    if (pendingLogout === logout) pendingLogout = undefined;
  }
}
