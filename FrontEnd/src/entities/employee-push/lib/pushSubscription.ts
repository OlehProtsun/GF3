import { employeePushApi, type BrowserPushSubscription } from "../api/employeePushApi";

const accountKey = "gf3.push.employeeId";
let workerPromise: Promise<ServiceWorkerRegistration> | undefined;

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
  try { localStorage.setItem(accountKey, String(employeeId)); } catch { /* Storage is optional. */ }
}
function forgetPushAccount() {
  try { localStorage.removeItem(accountKey); } catch { /* Storage is optional. */ }
}

export function registerWorker() {
  workerPromise ??= navigator.serviceWorker.register("/sw.js", { scope: "/" })
    .then(() => navigator.serviceWorker.ready)
    .catch(error => { workerPromise = undefined; throw error; });
  return workerPromise;
}

export async function getBrowserSubscription() {
  const registration = await navigator.serviceWorker.getRegistration("/sw.js");
  return registration?.pushManager ? registration.pushManager.getSubscription() : null;
}

export function vapidKeyBytes(value: string): Uint8Array<ArrayBuffer> {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  return Uint8Array.from(atob(base64), char => char.charCodeAt(0));
}

export function serializeSubscription(subscription: PushSubscription): BrowserPushSubscription {
  const json = subscription.toJSON();
  if (!json.endpoint || !json.keys?.p256dh || !json.keys?.auth) throw new Error("Invalid browser subscription");
  return { endpoint: json.endpoint, keys: { p256dh: json.keys.p256dh, auth: json.keys.auth } };
}

export async function ensureSubscription(publicVapidKey: string) {
  if (pushSupport() !== "supported" || Notification.permission !== "granted") throw new Error("Push permission unavailable");
  const registration = await registerWorker();
  const current = await registration.pushManager.getSubscription();
  return current ?? registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: vapidKeyBytes(publicVapidKey) });
}

export async function syncPushSubscription(employeeId: number, signal: AbortSignal, isCurrent = () => true) {
  if (!isCurrent() || signal.aborted || pushSupport() !== "supported" || Notification.permission !== "granted" || pushAccount() !== employeeId) return;
  const config = await employeePushApi.getConfig(signal);
  if (!config.enabled || signal.aborted || !isCurrent()) return;
  const subscription = await getBrowserSubscription();
  if (!subscription || signal.aborted || !isCurrent() || pushAccount() !== employeeId) return;
  await employeePushApi.upsertSubscription(serializeSubscription(subscription), signal);
}

async function bounded<T>(operation: Promise<T>, milliseconds: number): Promise<T> {
  let timer = 0;
  try { return await Promise.race([operation, new Promise<never>((_, reject) => { timer = window.setTimeout(() => reject(new Error("Push operation timed out")), milliseconds); })]); }
  finally { window.clearTimeout(timer); }
}

export async function disableForDevice(isCurrent = () => true) {
  try {
    if (!("serviceWorker" in navigator)) return;
    const subscription = await bounded(getBrowserSubscription(), 1000);
    if (!subscription || !isCurrent()) return;
    const controller = new AbortController();
    const timer = window.setTimeout(() => controller.abort(), 1500);
    try { await bounded(employeePushApi.deleteSubscription(subscription.endpoint, controller.signal), 1500); }
    catch { /* Local unsubscribe must also work offline. */ }
    finally { window.clearTimeout(timer); controller.abort(); if (isCurrent()) await bounded(subscription.unsubscribe(), 1000); }
  } finally { if (isCurrent()) forgetPushAccount(); }
}
