import { afterEach, expect, test, vi } from "vitest";
import { getPwaLaunchMode, subscribePwaDisplayMode } from "./pwaDisplayMode";

afterEach(() => vi.unstubAllGlobals());

test.each([[false, false, "browser"], [true, false, "pwa"], [false, true, "pwa"]] as const)(
  "classifies the current window with media %s and iOS %s as %s", (matches, standalone, mode) => {
    const query = vi.fn().mockReturnValue({ matches });
    vi.stubGlobal("window", { matchMedia: query, navigator: { standalone } });
    expect(getPwaLaunchMode()).toBe(mode);
    expect(query).toHaveBeenCalledWith("(display-mode: standalone)");
  });

test("uses browser mode without browser APIs", () => {
  vi.stubGlobal("window", undefined);
  expect(getPwaLaunchMode()).toBe("browser");
  subscribePwaDisplayMode(() => {})();
  vi.stubGlobal("window", {});
  expect(getPwaLaunchMode()).toBe("browser");
});

test("removes the same listener when unsubscribed", () => {
  const media = new EventTarget();
  vi.stubGlobal("matchMedia", () => media);
  const listener = vi.fn();
  const unsubscribe = subscribePwaDisplayMode(listener);
  media.dispatchEvent(new Event("change"));
  unsubscribe();
  media.dispatchEvent(new Event("change"));
  expect(listener).toHaveBeenCalledOnce();
});
