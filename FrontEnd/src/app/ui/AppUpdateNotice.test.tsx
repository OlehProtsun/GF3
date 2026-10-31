import { StrictMode } from "react";
import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { AppUpdateNotice } from "./AppUpdateNotice";

const metadata = (buildId = __GF3_BUILD_ID__) => new Response(JSON.stringify({ buildId }), { headers: { "Content-Type": "application/json" } });
const flush = async () => { await act(async () => { await Promise.resolve(); }); };
beforeEach(() => {
  vi.useFakeTimers();
  vi.stubEnv("PROD", true);
  vi.stubGlobal("fetch", vi.fn().mockImplementation(async () => metadata()));
  setLanguage("en");
});
afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); vi.unstubAllEnvs(); setLanguage("en"); });

it("checks immediately, every minute and on resume, stopping after a mismatch", async () => {
  render(<AppUpdateNotice />); await flush();
  expect(fetch).toHaveBeenCalledTimes(1);
  await act(async () => { vi.advanceTimersByTime(60_000); });
  expect(fetch).toHaveBeenCalledTimes(2);
  fireEvent.focus(window); await flush();
  fireEvent(document, new Event("visibilitychange")); await flush();
  expect(fetch).toHaveBeenCalledTimes(4);
  vi.mocked(fetch).mockResolvedValueOnce(metadata("different"));
  fireEvent.focus(window); await flush();
  expect(screen.getByRole("button", { name: "Update now" })).toBeInTheDocument();
  await act(async () => { vi.advanceTimersByTime(120_000); });
  expect(fetch).toHaveBeenCalledTimes(5);
});

it("guards in-flight requests and aborts on unmount", async () => {
  vi.mocked(fetch).mockImplementation(() => new Promise(() => {}));
  const view = render(<AppUpdateNotice />);
  fireEvent.focus(window);
  await act(async () => { vi.advanceTimersByTime(120_000); });
  expect(fetch).toHaveBeenCalledTimes(1);
  const signal = vi.mocked(fetch).mock.calls[0][1]?.signal;
  view.unmount();
  expect(signal?.aborted).toBe(true);
  fireEvent.focus(window);
  expect(fetch).toHaveBeenCalledTimes(1);
});

it("handles StrictMode, localizes and leaves auth storage untouched", async () => {
  setLanguage("pl");
  localStorage.setItem("gf3.auth.access-token", "existing-session");
  vi.mocked(fetch).mockImplementation(async () => metadata("different"));
  render(<StrictMode><AppUpdateNotice /></StrictMode>); await flush();
  expect(screen.getAllByRole("button", { name: "Aktualizuj teraz" })).toHaveLength(1);
  expect(screen.getByRole("status")).toHaveTextContent("Dostępna jest nowa wersja. Zapisz zmiany przed aktualizacją.");
  expect(localStorage.getItem("gf3.auth.access-token")).toBe("existing-session");
});

it("does not poll in development", () => {
  vi.stubEnv("PROD", false);
  render(<AppUpdateNotice />);
  expect(fetch).not.toHaveBeenCalled();
});

it("reloads only on explicit click and never clears stored authentication", async () => {
  const reload = vi.fn();
  const remove = vi.spyOn(Storage.prototype, "removeItem");
  const clear = vi.spyOn(Storage.prototype, "clear");
  const browser = window;
  vi.stubGlobal("window", new Proxy(browser, {
    get(target, key) {
      return key === "location" ? { reload } : Reflect.get(target, key, target);
    },
  }));
  vi.mocked(fetch).mockImplementation(async () => metadata("changed"));
  render(<AppUpdateNotice />); await flush();
  expect(reload).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Update now" }));
  expect(reload).toHaveBeenCalledTimes(1);
  expect(remove).not.toHaveBeenCalled(); expect(clear).not.toHaveBeenCalled();
  remove.mockRestore(); clear.mockRestore();
});

it("recovers from offline and skips hidden checks", async () => {
  vi.mocked(fetch).mockRejectedValueOnce(new Error("offline"));
  render(<AppUpdateNotice />); await flush();
  expect(screen.queryByRole("status")).toBeNull();
  const hidden = vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
  fireEvent.focus(window); await flush();
  expect(fetch).toHaveBeenCalledTimes(1);
  hidden.mockRestore();
  vi.mocked(fetch).mockResolvedValueOnce(metadata("new"));
  fireEvent(document, new Event("visibilitychange")); await flush();
  expect(screen.getByRole("status")).toBeInTheDocument();
});
