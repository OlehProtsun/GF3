import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { EmployeePushSettings } from "./EmployeePushSettings";

beforeEnglishTest(() => setTestLanguage("en"));

const mocks = vi.hoisted(() => ({ getConfig: vi.fn(), upsertSubscription: vi.fn(), support: vi.fn(), getSubscription: vi.fn(), account: vi.fn(), ensure: vi.fn(), remember: vi.fn(), disable: vi.fn() }));
vi.mock("../api/employeePushApi", () => ({ employeePushApi: { getConfig: mocks.getConfig, upsertSubscription: mocks.upsertSubscription } }));
vi.mock("../lib/pushSubscription", () => ({ pushSupport: mocks.support, getBrowserSubscription: mocks.getSubscription, pushAccount: mocks.account,
  employeePushStateChanged: "gf3:employee-push-state-changed",
  registerWorker: () => Promise.resolve({}), ensureSubscription: mocks.ensure, rememberPushAccount: mocks.remember,
  serializeSubscription: (value: unknown) => value, disableForDevice: mocks.disable }));
beforeEach(() => {
  vi.clearAllMocks();
  mocks.getConfig.mockResolvedValue({ enabled: true, publicKey: "key" }); mocks.upsertSubscription.mockResolvedValue(undefined);
  mocks.support.mockReturnValue("supported"); mocks.getSubscription.mockResolvedValue(null); mocks.account.mockReturnValue(null);
  mocks.ensure.mockResolvedValue({ endpoint: "test" }); mocks.disable.mockResolvedValue(undefined);
  vi.stubGlobal("Notification", { permission: "default", requestPermission: vi.fn().mockResolvedValue("granted") });
});
afterEach(() => vi.unstubAllGlobals());

describe("push Profile settings", () => {
  test.each(["default", "profile"] as const)("%s requests permission only on click and enables only after server acknowledgement", async appearance => {
    render(<EmployeePushSettings employeeId={7} appearance={appearance} />);
    const button = await screen.findByRole("button", { name: "Enable notifications" }); await waitFor(() => expect(button).toBeEnabled());
    expect(Notification.requestPermission).not.toHaveBeenCalled();
    let acknowledge!: () => void; mocks.upsertSubscription.mockReturnValue(new Promise<void>(resolve => { acknowledge = resolve; }));
    fireEvent.click(button); expect(Notification.requestPermission).toHaveBeenCalledOnce();
    await waitFor(() => expect(mocks.upsertSubscription).toHaveBeenCalled());
    expect(screen.queryByText("Enabled on this device")).not.toBeInTheDocument();
    acknowledge(); expect(await screen.findByText("Enabled on this device")).toBeVisible();
    expect(mocks.remember).toHaveBeenCalledWith(7);
    fireEvent.click(screen.getByRole("button", { name: "Disable notifications" }));
    await waitFor(() => expect(mocks.disable).toHaveBeenCalledOnce());
  });
  test.each(["default", "profile"] as const)("%s server rejection is retryable and never claims enabled", async appearance => {
    mocks.upsertSubscription.mockRejectedValue(new Error("Offline"));
    render(<EmployeePushSettings employeeId={7} appearance={appearance} />);
    const button = await screen.findByRole("button", { name: "Enable notifications" }); await waitFor(() => expect(button).toBeEnabled()); fireEvent.click(button);
    expect(await screen.findByRole("alert")).toHaveTextContent("Could not update notifications");
    expect(screen.queryByText("Enabled on this device")).not.toBeInTheDocument(); expect(button).toBeEnabled();
  });
  test.each([["unsupported", "Push unavailable on this browser"], ["install-ios", "Install GF on Home Screen to enable notifications on iPhone"]])("explains %s without permission prompts", async (support, message) => {
    mocks.support.mockReturnValue(support); render(<EmployeePushSettings employeeId={7} />);
    expect(screen.getByText(message)).toBeVisible(); expect(Notification.requestPermission).not.toHaveBeenCalled();
    await waitFor(() => expect(mocks.getConfig).toHaveBeenCalled());
    expect(screen.queryByRole("button", { name: "Enable notifications" })).not.toBeInTheDocument();
  });
  test("shows OS denial and does not prompt again", async () => {
    vi.stubGlobal("Notification", { permission: "denied", requestPermission: vi.fn() }); render(<EmployeePushSettings employeeId={7} />);
    expect(await screen.findByText("Notifications blocked — change system/browser settings")).toBeVisible();
    expect(Notification.requestPermission).not.toHaveBeenCalled();
  });
  test("shows server-disabled state", async () => {
    mocks.getConfig.mockResolvedValue({ enabled: false, publicKey: null }); render(<EmployeePushSettings employeeId={7} />);
    expect(await screen.findByText("Push is not configured on the server")).toBeVisible();
  });
  test("still allows current-device cleanup when server push is disabled", async () => {
    mocks.getConfig.mockResolvedValue({ enabled: false, publicKey: null }); mocks.getSubscription.mockResolvedValue({}); mocks.account.mockReturnValue(7);
    render(<EmployeePushSettings employeeId={7} />);
    fireEvent.click(await screen.findByRole("button", { name: "Disable notifications" }));
    await waitFor(() => expect(mocks.disable).toHaveBeenCalledOnce());
  });
  test("refreshes Profile after push is enabled in the welcome dialog", async () => {
    render(<EmployeePushSettings employeeId={7} />);
    await screen.findByRole("button", { name: "Enable notifications" });
    mocks.getSubscription.mockResolvedValue({}); mocks.account.mockReturnValue(7);
    fireEvent(window, new Event("gf3:employee-push-state-changed"));
    expect(await screen.findByRole("button", { name: "Disable notifications" })).toBeEnabled();
    expect(screen.getByText("Enabled on this device")).toBeVisible();
  });
});

test("profile header and config retry retain existing supported actions", async () => {
  mocks.getConfig.mockRejectedValueOnce(new Error("Offline"));
  render(<EmployeePushSettings employeeId={7} appearance="profile" />);
  expect(screen.getByRole("heading", { name: "Push notifications" })).toBeVisible();
  expect(screen.getByText("Enable notifications to receive important updates.")).toBeVisible();
  expect(await screen.findByRole("alert")).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  const enable = await screen.findByRole("button", { name: "Enable notifications" });
  await waitFor(() => expect(enable).toBeEnabled());
  expect(Notification.requestPermission).not.toHaveBeenCalled();
});
