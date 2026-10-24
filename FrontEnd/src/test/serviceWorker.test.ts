// @vitest-environment node
import { readFileSync } from "node:fs";
import { runInNewContext } from "node:vm";
import { describe, expect, test, vi } from "vitest";

function worker() {
  const handlers: Record<string, (event: unknown) => void> = {};
  const showNotification = vi.fn().mockResolvedValue(undefined);
  const clients = { matchAll: vi.fn().mockResolvedValue([]), openWindow: vi.fn().mockResolvedValue(undefined) };
  runInNewContext(readFileSync(new URL("../../public/sw.js", import.meta.url), "utf8"), {
    URL, TextEncoder, self: { addEventListener: (name: string, handler: (event: unknown) => void) => { handlers[name] = handler; },
      registration: { showNotification }, location: { origin: "https://gf.example" }, clients }
  });
  const send = async (name: string, event: object) => {
    let completion = Promise.resolve();
    handlers[name]({ ...event, waitUntil: (promise: Promise<void>) => { completion = promise; } });
    await completion;
  };
  return { handlers, showNotification, clients, send };
}

describe("push service worker", () => {
  test("shows a tagged notification without a fetch listener", async () => {
    const sw = worker();
    const payload = { notificationId: "swap-public:12", title: "Shift", body: "Available", url: "/swap?swapId=12", tone: "swap" };
    await sw.send("push", { data: { text: () => JSON.stringify(payload), json: () => payload } });
    expect(sw.showNotification).toHaveBeenCalledWith("Shift", expect.objectContaining({ tag: "swap-public:12", renotify: false, data: { url: "/swap?swapId=12" } }));
    expect(Object.keys(sw.handlers).sort()).toEqual(["notificationclick", "push"]);
  });
  test.each(["invalid json", JSON.stringify({ notificationId: "x", title: "Evil", body: "x", url: "https://evil.example" }), JSON.stringify({ notificationId: "x", title: "Evil", body: "x", url: "/swap?swapId=9007199254740992" })])("falls back safely for %s", async data => {
    const sw = worker(); await sw.send("push", { data: { text: () => data, json: () => JSON.parse(data) } });
    expect(sw.showNotification).toHaveBeenCalledWith("GF", expect.objectContaining({ data: { url: "/notifications" } }));
  });
  test.each([true, false])("click reuses a same-origin window when available: %s", async existing => {
    const sw = worker(); const focus = vi.fn(); const navigate = vi.fn().mockResolvedValue({ focus }); const close = vi.fn();
    sw.clients.matchAll.mockResolvedValue(existing ? [{ url: "https://gf.example/profile", navigate, frameType: "top-level" }] : [{ url: "https://evil.example", navigate }]);
    await sw.send("notificationclick", { notification: { close, data: { url: "/swap?swapId=12" } } });
    expect(close).toHaveBeenCalledOnce();
    if (existing) { expect(navigate).toHaveBeenCalledWith("https://gf.example/swap?swapId=12"); expect(focus).toHaveBeenCalledOnce(); }
    else expect(sw.clients.openWindow).toHaveBeenCalledWith("https://gf.example/swap?swapId=12");
  });
});
