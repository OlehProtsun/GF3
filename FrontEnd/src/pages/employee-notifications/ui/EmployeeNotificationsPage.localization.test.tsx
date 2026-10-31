import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { EmployeeNotificationsPage } from "./EmployeeNotificationsPage";
import { setLanguage, t } from "@shared/i18n";
import { readEmployeeNotificationIdsForAccount } from "@shared/lib/employeeNotificationReadState";

const mocks = vi.hoisted(() => ({
  schedules: [] as unknown[],
  empty: [] as unknown[],
  sync: vi.fn(), refetch: vi.fn(),
  ui: { data: { readNotificationIds: [] as string[] }, refetch: vi.fn() },
}));
vi.mock("@app/providers/AuthProvider", () => ({useAuth: () => ({session: {employeeId: 12, userName: "worker"}})}));
vi.mock("@app/providers/PresenceProvider", () => ({useRealtime: () => ({notifications: mocks.empty})}));
vi.mock("@entities/employee-schedule", () => ({useEmployeeScheduleListQuery: () => ({data: mocks.schedules})}));
vi.mock("@entities/shift-swaps", () => ({useEmployeeShiftSwapsQuery: () => ({data: mocks.empty})}));
vi.mock("@entities/employee-availability", () => ({useEmployeeAvailabilityListQuery: () => ({data: mocks.empty})}));
vi.mock("@entities/employee-ui-state", () => ({useEmployeeUiStateQuery: () => mocks.ui, employeeUiStateApi: {markNotificationsRead: (...args: unknown[]) => mocks.sync(...args)}}));
vi.mock("@entities/system-news", () => ({useSystemNewsQuery: () => ({data: mocks.empty}), useMarkSystemNewsReadMutation: () => ({mutate: vi.fn()}), useMarkAllSystemNewsReadMutation: () => ({mutate: vi.fn()})}));
vi.mock("@entities/communications/ui", () => ({EmployeeCommunicationDialog: () => null}));
const now = Date.parse("2026-09-19T12:00:00Z");
const published = new Date(now - 5 * 86_400_000 + 10_000).toISOString();
const id = "schedule-public:21:" + published;
beforeEach(() => {
  vi.useFakeTimers(); vi.setSystemTime(now); localStorage.clear();
  setLanguage("en");
  mocks.sync.mockReset().mockResolvedValue(undefined);
  mocks.ui.data = { readNotificationIds: [] };
  mocks.schedules = [{ id: 21, name: "September", year: 2026, month: 9, publishedAtUtc: published }];
});
afterEach(() => { cleanup(); vi.useRealTimers(); setLanguage("pl"); });
it("updates fixed notification data immediately in both directions without losing read identity", async () => {
  render(<BrowserRouter><EmployeeNotificationsPage /></BrowserRouter>);
  const card = screen.getByText("New schedule published").closest("article");
  expect(screen.getByText(/is now available at your shop/)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Open schedule" })).toHaveAttribute("href", "/schedule");
  await act(async () => fireEvent.click(screen.getByRole("button", { name: "Mark as read" })));
  expect(mocks.sync).toHaveBeenCalledWith([id]);
  expect(readEmployeeNotificationIdsForAccount("worker", 12)).toEqual(new Set([id]));
  act(() => setLanguage("pl"));
  expect(screen.getByText(t("New schedule published")).closest("article")).toBe(card);
  expect(screen.getByText(/Twoim sklepie/)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: t("Open schedule") })).toHaveAttribute("href", "/schedule");
  expect(screen.getByRole("button", { name: t("Read"), exact: true })).toBeDisabled();
  act(() => setLanguage("uk"));
  expect(screen.getByText("Опубліковано новий графік").closest("article")).toBe(card);
  expect(screen.getByText(/вашому магазині/)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Відкрити графік" })).toHaveAttribute("href", "/schedule");
  expect(screen.getByRole("button", { name: "Прочитано", exact: true })).toBeDisabled();
  expect(readEmployeeNotificationIdsForAccount("worker", 12)).toEqual(new Set([id]));
  act(() => setLanguage("en"));
  expect(screen.getByText("New schedule published").closest("article")).toBe(card);
  expect(screen.getByText(/is now available at your shop/)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Open schedule" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Read", exact: true })).toBeDisabled();
  expect(readEmployeeNotificationIdsForAccount("worker", 12)).toEqual(new Set([id]));
  expect(mocks.sync).toHaveBeenCalledTimes(1);
});
