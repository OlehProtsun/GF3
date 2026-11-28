import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, expect, test, vi } from "vitest";
import type { WorkflowLog } from "@entities/workflow-logs";
import { setLanguage } from "@shared/i18n";
import { InformationPage } from "./InformationPage";

const mocks = vi.hoisted(() => ({ list: vi.fn(), settings: vi.fn(), employees: vi.fn(), managers: vi.fn() }));
vi.mock("@entities/workflow-logs/api/workflowLogsApi", () => ({ workflowLogsApi: {
  list: mocks.list, settings: mocks.settings,
} }));
vi.mock("@entities/employees/api/employeesApi", () => ({ employeesApi: { list: mocks.employees } }));
vi.mock("@features/manager-profile/api/managerProfileApi", () => ({ managerProfileApi: { listManagers: mocks.managers } }));

const event: WorkflowLog = {
  id: 1, actorRole: "employee", actorName: "Ada Worker", actorEmployeeId: 1,
  action: "Opened platform", occurredAtUtc: "2026-11-22T10:00:00Z", launchMode: "pwa",
};

function mount() {
  render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><InformationPage /></BrowserRouter></QueryClientProvider>);
}

beforeEach(() => {
  setLanguage("en");
  mocks.list.mockReset().mockResolvedValue([event]);
  mocks.settings.mockReset().mockResolvedValue({ isEnabled: true, audience: "all" });
  mocks.employees.mockReset().mockResolvedValue([]);
  mocks.managers.mockReset().mockResolvedValue([]);
});

test("shows browser and PWA launch history in the searchable workflow log", async () => {
  mocks.list.mockResolvedValue([event, { ...event, id: 2, actorName: "Ben Worker", launchMode: "browser" }]);
  mount();
  expect(await screen.findByText("Ada Worker")).toBeVisible();
  const rows = screen.getAllByRole("article");
  expect(rows).toHaveLength(2);
  expect(within(rows.find(row => row.textContent?.includes("Ada Worker"))!).getByText("PWA", { exact: true })).toBeVisible();
  expect(within(rows.find(row => row.textContent?.includes("Ben Worker"))!).getByText("Browser", { exact: true })).toBeVisible();
  await userEvent.type(screen.getByRole("searchbox"), "Ben");
  expect(screen.queryByText("Ada Worker")).toBeNull();
  expect(screen.getByText("Ben Worker")).toBeVisible();
});

test.each([
  { action: "Opened platform via PWA", badge: "PWA" },
  { action: "Opened platform via browser", badge: "Browser" },
  { action: "Online", badge: null },
])("uses the matching label for historical event $action", async ({ action, badge }) => {
  mocks.list.mockResolvedValue([{ ...event, action, launchMode: null }]);
  mount();
  const row = await screen.findByRole("article");
  expect(within(row).getByText(badge ? "Opened platform" : "Online", { exact: true })).toBeVisible();
  if (badge) expect(within(row).getByText(badge, { exact: true })).toBeVisible();
  else {
    expect(within(row).queryByText("Browser", { exact: true })).toBeNull();
    expect(within(row).queryByText("PWA", { exact: true })).toBeNull();
  }
});

test("a logging settings failure leaves launch history visible", async () => {
  mocks.settings.mockRejectedValue(new Error());
  mount();
  expect(await screen.findByText("Could not load logging settings.")).toBeVisible();
  expect(screen.getByText("Workflow logs", { exact: true })).toBeVisible();
  expect(screen.getByText("PWA", { exact: true })).toBeVisible();
});

test("keeps presence separate from logs and uses server presence for both roles", async () => {
  mocks.settings.mockResolvedValue({ isEnabled: false, audience: "all" });
  mocks.employees.mockResolvedValue([{ id: 1, firstName: "Zoe", lastName: "Worker", isOnline: true },
    { id: 2, firstName: "Offline", lastName: "Worker", isOnline: false }]);
  mocks.managers.mockResolvedValue([{ id: 1, displayName: "", userName: "ada", isOnline: true },
    { id: 2, displayName: "Current offline", isCurrent: true, isOnline: false }]);
  mount();
  const section = within(screen.getByRole("region", { name: "Online users" }));
  const rows = within(await section.findByRole("table")).getAllByRole("row");
  expect(rows.map(row => row.textContent)).toEqual(["NameRoleStatus", "adaManagerOnline", "Zoe WorkerEmployeeOnline"]);
  expect(section.getByLabelText("2 online users")).toBeVisible();
  expect(await screen.findByText("Logging off")).toBeVisible();
});

test("keeps successful rows while another source loads and reports a failed source with retry", async () => {
  let failManagers: (reason: Error) => void = () => {};
  mocks.employees.mockResolvedValue([{ id: 1, firstName: "Ada", lastName: "Online", isOnline: true }]);
  mocks.managers.mockReturnValueOnce(new Promise((_, reject) => { failManagers = reject; })).mockResolvedValue([]);
  mount();
  const section = within(screen.getByRole("region", { name: "Online users" }));
  expect(await section.findByText("Ada Online")).toBeVisible();
  expect(section.getByText("Loading online users...")).toBeVisible();
  expect(section.queryByLabelText(/\d+ online users/)).toBeNull();
  failManagers(new Error("Manager list failed"));
  expect(await section.findByRole("alert")).toHaveTextContent("Managers · Manager list failed");
  expect(section.getByText("Ada Online")).toBeVisible();
  await userEvent.click(section.getByRole("button", { name: "Retry" }));
  expect(await section.findByLabelText("1 online users")).toBeVisible();
  expect(mocks.employees).toHaveBeenCalledOnce();
});

test("one failed source and one empty source do not imply an empty online population", async () => {
  mocks.employees.mockRejectedValue(new Error("Employee list failed"));
  mount();
  const section = within(screen.getByRole("region", { name: "Online users" }));
  expect(await section.findByRole("alert")).toHaveTextContent("Employee list failed");
  expect(section.queryByText("No users online.")).toBeNull();
  expect(section.queryByLabelText("0 online users")).toBeNull();
});
