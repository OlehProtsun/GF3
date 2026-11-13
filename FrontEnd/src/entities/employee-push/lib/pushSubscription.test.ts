import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

const api = vi.hoisted(() => ({ getConfig: vi.fn(), upsertSubscription: vi.fn(), deleteSubscription: vi.fn() }));
vi.mock("../api/employeePushApi", () => ({ employeePushApi: api }));
const subscription = { endpoint: "https://fcm.googleapis.com/test", toJSON: () => ({ endpoint: "https://fcm.googleapis.com/test", keys: { p256dh: "key", auth: "auth" } }), unsubscribe: vi.fn() };
const registration = { pushManager: { getSubscription: vi.fn(), subscribe: vi.fn() } };
const serviceWorker = { register: vi.fn(), getRegistration: vi.fn(), ready: Promise.resolve(registration) };

beforeEach(() => {
  vi.resetModules(); vi.clearAllMocks(); localStorage.clear();
  vi.stubGlobal("isSecureContext", true);
  vi.stubGlobal("PushManager", class {});
  vi.stubGlobal("Notification", { permission: "granted", requestPermission: vi.fn() });
  vi.stubGlobal("navigator", { userAgent: "Desktop", platform: "Win32", maxTouchPoints: 0, serviceWorker });
  serviceWorker.register.mockResolvedValue(registration); serviceWorker.getRegistration.mockResolvedValue(registration);
  registration.pushManager.getSubscription.mockResolvedValue(subscription); registration.pushManager.subscribe.mockResolvedValue(subscription);
  subscription.unsubscribe.mockResolvedValue(true); api.deleteSubscription.mockResolvedValue(undefined); api.upsertSubscription.mockResolvedValue(undefined);
  api.getConfig.mockResolvedValue({ enabled: true, publicKey: "AQID" });
});
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers(); });

describe("push browser adapter", () => {
  test("registers the root worker once and converts VAPID bytes", async () => {
    const push = await import("./pushSubscription");
    await push.registerWorker(); await push.registerWorker();
    expect(serviceWorker.register).toHaveBeenCalledExactlyOnceWith("/sw.js", { scope: "/" });
    expect([...push.vapidKeyBytes("AQID-_8")]).toEqual([1, 2, 3, 251, 255]);
  });
  test("subscribes only with granted permission and userVisibleOnly", async () => {
    const push = await import("./pushSubscription"); registration.pushManager.getSubscription.mockResolvedValue(null);
    await push.ensureSubscription("AQID");
    expect(registration.pushManager.subscribe).toHaveBeenCalledWith({ userVisibleOnly: true, applicationServerKey: new Uint8Array([1, 2, 3]) });
    vi.stubGlobal("Notification", { permission: "denied" });
    await expect(push.ensureSubscription("AQID")).rejects.toThrow();
    expect(registration.pushManager.subscribe).toHaveBeenCalledOnce();
  });
  test("requires installed iOS and a secure supported browser", async () => {
    const push = await import("./pushSubscription");
    Object.assign(navigator, { userAgent: "iPhone", standalone: false });
    expect(push.pushSupport()).toBe("install-ios");
    Object.assign(navigator, { standalone: true }); expect(push.pushSupport()).toBe("supported");
    vi.stubGlobal("isSecureContext", false); expect(push.pushSupport()).toBe("unsupported");
  });
  test("syncs only a previously authorized matching account without prompting", async () => {
    const push = await import("./pushSubscription"); const signal = new AbortController().signal;
    push.rememberPushAccount(7); await push.syncPushSubscription(8, signal); expect(api.upsertSubscription).not.toHaveBeenCalled();
    await push.syncPushSubscription(7, signal);
    expect(api.upsertSubscription).toHaveBeenCalledWith(push.serializeSubscription(subscription as unknown as PushSubscription), signal);
    expect(Notification.requestPermission).not.toHaveBeenCalled();
    expect([...Array(localStorage.length)].map((_, i) => localStorage.key(i))).toEqual(["gf3.push.employeeId"]);
  });
  test("disable deletes that device before local unsubscribe, including offline fallback", async () => {
    const push = await import("./pushSubscription"); push.rememberPushAccount(7);
    api.deleteSubscription.mockRejectedValue(new Error("Offline")); await push.disableForDevice();
    expect(api.deleteSubscription).toHaveBeenCalledWith(subscription.endpoint, expect.any(AbortSignal));
    expect(subscription.unsubscribe).toHaveBeenCalledOnce(); expect(push.pushAccount()).toBeNull();
    expect(api.deleteSubscription.mock.invocationCallOrder[0]).toBeLessThan(subscription.unsubscribe.mock.invocationCallOrder[0]);
  });
  test("canceled account sync never writes after losing its session", async () => {
    const push = await import("./pushSubscription"); push.rememberPushAccount(7); const controller = new AbortController(); controller.abort();
    await push.syncPushSubscription(7, controller.signal); expect(api.upsertSubscription).not.toHaveBeenCalled();
  });
  test("an account change during browser lookup prevents old-session mutation and hint removal", async () => {
    const push = await import("./pushSubscription"); push.rememberPushAccount(8);
    await push.disableForDevice(() => false);
    expect(api.deleteSubscription).not.toHaveBeenCalled(); expect(subscription.unsubscribe).not.toHaveBeenCalled();
    expect(push.pushAccount()).toBe(8);
    await push.syncPushSubscription(8, new AbortController().signal, () => false);
    expect(api.upsertSubscription).not.toHaveBeenCalled();
  });
});
