import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { EmployeePushInvitation } from "./EmployeePushInvitation";

const mocks = vi.hoisted(() => ({
  auth: { status: "authenticated", session: { role: "employee", employeeId: 100 } },
  config: vi.fn(), upsert: vi.fn(), support: vi.fn(), subscription: vi.fn(), account: vi.fn(), ensure: vi.fn(), remember: vi.fn(), worker: vi.fn(),
}));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => mocks.auth }));
vi.mock("../api/employeePushApi", () => ({ employeePushApi: { getConfig: mocks.config, upsertSubscription: mocks.upsert } }));
vi.mock("../lib/pushSubscription", () => ({
  employeePushStateChanged: "gf3:employee-push-state-changed", pushSupport: mocks.support, getBrowserSubscription: mocks.subscription,
  pushAccount: mocks.account, registerWorker: mocks.worker, ensureSubscription: mocks.ensure, rememberPushAccount: mocks.remember,
  serializeSubscription: (value: unknown) => value,
}));

function workspace(loading = false, otherModal = false) {
  return <><main data-employee-content>{loading ? <div role="status" aria-label="Loading page">Loading</div> : <h1>Employee page</h1>}</main>
    {otherModal && <dialog open aria-label="Existing modal">Existing dialog</dialog>}<EmployeePushInvitation /></>;
}
beforeEach(() => {
  vi.clearAllMocks(); localStorage.clear(); setLanguage("en");
  mocks.auth.status = "authenticated"; mocks.auth.session.role = "employee"; mocks.auth.session.employeeId++;
  mocks.config.mockResolvedValue({ enabled: true, publicKey: "key" }); mocks.upsert.mockResolvedValue(undefined);
  mocks.support.mockReturnValue("supported"); mocks.subscription.mockResolvedValue(null); mocks.account.mockReturnValue(null);
  mocks.worker.mockResolvedValue({}); mocks.ensure.mockResolvedValue({ endpoint: "device" });
  mocks.remember.mockImplementation(id => { mocks.account.mockReturnValue(id); window.dispatchEvent(new Event("gf3:employee-push-state-changed")); });
  vi.stubGlobal("Notification", { permission: "default", requestPermission: vi.fn().mockResolvedValue("granted") });
});
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); setLanguage("en"); });

describe("employee push welcome", () => {
  test("waits for auth, config and page readiness without requesting native permission", async () => {
    mocks.auth.status = "loading";
    const page = render(workspace(true));
    expect(mocks.config).not.toHaveBeenCalled();
    let resolveConfig!: (value: object) => void;
    mocks.config.mockReturnValue(new Promise(resolve => { resolveConfig = resolve; }));
    mocks.auth.status = "authenticated"; page.rerender(workspace(true));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    await act(async () => resolveConfig({ enabled: true, publicKey: "key" }));
    await waitFor(() => expect(mocks.subscription).toHaveBeenCalled());
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    page.rerender(workspace());
    expect(await screen.findByRole("dialog", { name: "Enable notifications" })).toBeVisible();
    expect(Notification.requestPermission).not.toHaveBeenCalled();
  });

  test("registers only on explicit Enable and closes after server acknowledgement", async () => {
    let acknowledge!: () => void; mocks.upsert.mockReturnValue(new Promise<void>(resolve => { acknowledge = resolve; }));
    render(workspace()); await screen.findByRole("dialog", { name: "Enable notifications" });
    expect(Notification.requestPermission).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Enable notifications" }));
    expect(Notification.requestPermission).toHaveBeenCalledOnce();
    await waitFor(() => expect(mocks.upsert).toHaveBeenCalledWith({ endpoint: "device" }, expect.any(AbortSignal)));
    expect(mocks.remember).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Enable notifications" })).toBeDisabled();
    await act(async () => acknowledge());
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(mocks.remember).toHaveBeenCalledWith(mocks.auth.session.employeeId);
  });

  test("Not now persists a seven-day employee/device cooldown across navigation and remount", async () => {
    const page = render(workspace()); await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("button", { name: "Not now" }));
    const key = `gf3.push.invitation.${mocks.auth.session.employeeId}`;
    const until = Number(localStorage.getItem(key)); expect(until - Date.now()).toBeGreaterThan(6 * 86400000);
    page.rerender(workspace()); expect(screen.queryByRole("dialog")).not.toBeInTheDocument(); page.unmount();
    const next = render(workspace()); await act(async () => {});
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument(); expect(mocks.config).toHaveBeenCalledOnce(); next.unmount();
    vi.spyOn(Date, "now").mockReturnValue(until + 1);
    render(workspace()); expect(await screen.findByRole("dialog")).toBeVisible();
    expect(Notification.requestPermission).not.toHaveBeenCalled();
  });

  test.each(["manager", "unauthenticated", "denied", "unsupported", "install-ios", "server-disabled", "already-enabled"])("never invites in %s state", async state => {
    if (state === "manager") mocks.auth.session.role = "manager";
    if (state === "unauthenticated") mocks.auth.status = "unauthenticated";
    if (state === "denied") vi.stubGlobal("Notification", { permission: "denied", requestPermission: vi.fn() });
    if (state === "unsupported" || state === "install-ios") mocks.support.mockReturnValue(state);
    if (state === "server-disabled") mocks.config.mockResolvedValue({ enabled: false, publicKey: null });
    if (state === "already-enabled") {
      vi.stubGlobal("Notification", { permission: "granted", requestPermission: vi.fn() });
      mocks.subscription.mockResolvedValue({}); mocks.account.mockReturnValue(mocks.auth.session.employeeId);
    }
    render(workspace()); await act(async () => {});
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument(); expect(Notification.requestPermission).not.toHaveBeenCalled();
    if (["manager", "unauthenticated", "denied", "unsupported", "install-ios"].includes(state)) expect(mocks.config).not.toHaveBeenCalled();
  });

  test("ignores stale configuration after switching accounts", async () => {
    let resolveOld!: (value: object) => void; mocks.config.mockReturnValueOnce(new Promise(resolve => { resolveOld = resolve; }));
    const page = render(workspace());
    mocks.auth.session.employeeId++; mocks.config.mockResolvedValue({ enabled: false, publicKey: null }); page.rerender(workspace());
    await act(async () => resolveOld({ enabled: true, publicKey: "old-key" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument(); expect(mocks.ensure).not.toHaveBeenCalled();
  });

  test("a native permission result arriving after logout cannot register a device", async () => {
    let resolvePermission!: (value: NotificationPermission) => void;
    vi.stubGlobal("Notification", { permission: "default", requestPermission: vi.fn().mockReturnValue(new Promise(resolve => { resolvePermission = resolve; })) });
    const page = render(workspace()); await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("button", { name: "Enable notifications" }));
    mocks.auth.status = "unauthenticated"; page.rerender(workspace());
    await act(async () => resolvePermission("granted"));
    expect(mocks.upsert).not.toHaveBeenCalled(); expect(mocks.remember).not.toHaveBeenCalled();
  });

  test("defers to existing modals and restores viewport listeners and scroll lock", async () => {
    const viewport = new EventTarget(); Object.assign(viewport, { height: 500, offsetTop: 30 });
    const remove = vi.spyOn(viewport, "removeEventListener"); vi.stubGlobal("visualViewport", viewport);
    const originalOverflow = document.documentElement.style.overflow;
    const page = render(workspace(false, true)); await waitFor(() => expect(mocks.subscription).toHaveBeenCalled());
    expect(screen.queryByRole("dialog", { name: "Enable notifications" })).not.toBeInTheDocument();
    page.rerender(workspace()); const dialog = await screen.findByRole("dialog", { name: "Enable notifications" });
    expect(dialog.parentElement).toBe(document.body); expect(dialog.style.getPropertyValue("--push-visual-height")).toBe("500px");
    expect(dialog.style.getPropertyValue("--push-visual-top")).toBe("30px");
    fireEvent.click(screen.getByRole("button", { name: "Not now" }));
    expect(document.documentElement.style.overflow).toBe(originalOverflow);
    expect(remove).toHaveBeenCalledWith("resize", expect.any(Function)); expect(remove).toHaveBeenCalledWith("scroll", expect.any(Function));
  });

  test("registration failure remains retryable without claiming enabled", async () => {
    mocks.upsert.mockRejectedValue(new Error("Offline")); render(workspace()); await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("button", { name: "Enable notifications" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Could not update notifications");
    expect(screen.getByRole("button", { name: "Enable notifications" })).toBeEnabled(); expect(mocks.remember).not.toHaveBeenCalled();
  });

  test("renders the requested Polish invitation copy", async () => {
    setLanguage("pl"); render(workspace());
    expect(await screen.findByRole("dialog", { name: "Włącz powiadomienia" })).toHaveTextContent("Otrzymuj informacje o nowych zmianach, grafikach i zaakceptowanych zamianach, nawet gdy aplikacja jest zamknięta.");
    expect(screen.getByRole("button", { name: "Nie teraz" })).toBeVisible();
  });
});
