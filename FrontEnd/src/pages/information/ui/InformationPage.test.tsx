import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, expect, test, vi } from "vitest";
import type { WorkflowLog } from "@entities/workflow-logs";
import { setLanguage } from "@shared/i18n";
import { InformationPage } from "./InformationPage";

const mocks = vi.hoisted(() => ({ list: vi.fn(), settings: vi.fn() }));
vi.mock("@entities/workflow-logs/api/workflowLogsApi", () => ({ workflowLogsApi: {
  list: mocks.list, settings: mocks.settings,
} }));

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
