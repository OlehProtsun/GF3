import { readFileSync } from "node:fs";
import { afterEach, expect, test, vi } from "vitest";
import { finishPwaSplash } from "./pwaSplash";

const html = readFileSync("index.html", "utf8");

afterEach(() => {
  document.documentElement.classList.remove("gf3-pwa-standalone");
  document.getElementById("gf3-startup-splash")?.remove();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function mountSplash(standalone: boolean) {
  document.documentElement.classList.toggle("gf3-pwa-standalone", standalone);
  const splash = document.createElement("div");
  splash.id = "gf3-startup-splash";
  splash.style.cssText = "display: grid; opacity: 1; transition-property: opacity; transition-duration: 180ms";
  document.body.append(splash);
  return splash;
}

test("HTML provides the PWA-only logo, background and motion before React", () => {
  const entry = new DOMParser().parseFromString(html, "text/html");
  const splash = entry.getElementById("gf3-startup-splash");
  expect(splash?.nextElementSibling).not.toBeNull();
  expect(splash?.compareDocumentPosition(entry.getElementById("root")!)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
  expect(splash?.querySelector("img")?.getAttribute("src")).toBe("/icons/gf-icon-512.png");
  expect(entry.head.querySelector("script")?.textContent).toContain("(display-mode: standalone)");
  expect(entry.head.querySelector("style")?.textContent).toContain("#gf3-startup-splash { display: none; }");
  expect(entry.head.querySelector("style")?.textContent).toContain("prefers-reduced-motion: reduce");
});

test("finishes once on the overlay's actual opacity transition", () => {
  const splash = mountSplash(true);
  const timers = vi.spyOn(window, "setTimeout");
  finishPwaSplash();
  finishPwaSplash();
  expect(splash).toHaveClass("gf3-splash-exiting");
  expect(splash.style.pointerEvents).toBe("none");
  splash.dispatchEvent(Object.assign(new Event("transitionend"), { propertyName: "opacity" }));
  expect(splash).not.toBeInTheDocument();
  expect(timers).not.toHaveBeenCalled();
});

test("removes a browser splash, reduced-motion splash or missing node immediately", () => {
  finishPwaSplash();
  const browser = mountSplash(false);
  finishPwaSplash();
  expect(browser).not.toBeInTheDocument();
  const standalone = mountSplash(true);
  vi.stubGlobal("matchMedia", () => ({ matches: true }));
  finishPwaSplash();
  expect(standalone).not.toBeInTheDocument();
});
