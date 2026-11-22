import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, expect, test, vi } from "vitest";
import type { AuthSession } from "@entities/auth/model/types";
import type { EmployeeLaunchUsage } from "@entities/employee-launches/model/types";
import { setLanguage } from "@shared/i18n";
import { InformationPage } from "./InformationPage";

const mocks = vi.hoisted(() => ({ auth: vi.fn(), usage: vi.fn() }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: mocks.auth }));
vi.mock("@entities/employee-launches/api/employeeLaunchesApi", () => ({ employeeLaunchesApi: { list: mocks.usage } }));
vi.mock("@entities/workflow-logs/api/workflowLogsApi", () => ({ workflowLogsApi: {
  list: async () => [], settings: async () => ({ isEnabled: true, audience: "all" }),
} }));

const manager: AuthSession = { role: "manager", userName: "system", displayName: "System", isSystemManager: true, workspaceMode: "pc" };
const noAccount: EmployeeLaunchUsage = { employeeId: 1, employeeName: "Ada Worker", username: null, hasLoginAccount: false, isOnline: false,
  lastLoginAtUtc: null, lastSeenAtUtc: null, lastBrowserLaunchAtUtc: null, lastPwaLaunchAtUtc: null, lastLaunchMode: null, lastLaunchAtUtc: null };

function mount() {
  render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><InformationPage /></BrowserRouter></QueryClientProvider>);
}

beforeEach(() => {
  setLanguage("en");
  mocks.auth.mockReturnValue({ status: "authenticated", session: manager });
  mocks.usage.mockReset();
});

test("system PC shows presence, launch history and a searchable account list", async () => {
  const timestamp = "2026-11-22T10:00:00Z";
  mocks.usage.mockResolvedValue([noAccount, { ...noAccount, employeeId: 2, employeeName: "Ben Worker", username: "ben-login",
    isOnline: true, hasLoginAccount: true, lastBrowserLaunchAtUtc: timestamp, lastPwaLaunchAtUtc: timestamp, lastLaunchAtUtc: timestamp, lastLaunchMode: "pwa" }]);
  mount();
  const section = screen.getByText("Employee usage").closest("details")!;
  expect(await within(section).findByText("No account")).toBeVisible();
  expect(within(section).getByText("No data")).toBeVisible();
  expect(within(section).getByText("Online")).toBeVisible();
  expect(within(section).getByText("PWA", { exact: true })).toBeVisible();
  expect(within(section).getAllByText(new Intl.DateTimeFormat("en-GB", { dateStyle: "short", timeStyle: "short" }).format(new Date(timestamp)))).toHaveLength(2);
  await userEvent.type(within(section).getByRole("searchbox"), "ben-login");
  expect(within(section).queryByText("Ada Worker")).toBeNull();
  expect(within(section).getByText("Ben Worker")).toBeVisible();
});

test.each([{ ...manager, isSystemManager: false }, { ...manager, workspaceMode: "phone" }, { ...manager, role: "employee" }])(
  "usage stays hidden for an ineligible session", session => {
    mocks.auth.mockReturnValue({ status: "authenticated", session });
    mount();
    expect(screen.queryByText("Employee usage")).toBeNull();
    expect(mocks.usage).not.toHaveBeenCalled();
  },
);

test("a usage failure leaves the workflow logs visible", async () => {
  mocks.usage.mockRejectedValue(new Error("offline"));
  mount();
  expect(await screen.findByText("Could not load employee usage.")).toBeVisible();
  expect(screen.getByText("Workflow logs", { exact: true })).toBeVisible();
});
