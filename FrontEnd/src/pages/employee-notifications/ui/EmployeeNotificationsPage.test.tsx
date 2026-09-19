import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { EmployeeNotificationsPage } from "./EmployeeNotificationsPage";
import { EmployeeWorkspaceLayout } from "@app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout";
import { writeEmployeeNotificationIdsForAccount } from "@shared/lib/employeeNotificationReadState";

const mocks = vi.hoisted(() => ({
  schedules: [] as unknown[],
  sync: vi.fn(), refetch: vi.fn(),
  ui: { data: { readNotificationIds: [] as string[] }, refetch: vi.fn() },
}));
vi.mock("@app/providers/AuthProvider", () => ({useAuth: () => ({session: {employeeId: 12, userName: "worker"}})}));
vi.mock("@app/providers/PresenceProvider", () => ({useRealtime: () => ({notifications: []})}));
vi.mock("@entities/employee-schedule", () => ({useEmployeeScheduleListQuery: () => ({data: mocks.schedules})}));
vi.mock("@entities/shift-swaps", () => ({useEmployeeShiftSwapsQuery: () => ({data: []})}));
vi.mock("@entities/employee-availability", () => ({useEmployeeAvailabilityListQuery: () => ({data: []})}));
vi.mock("@entities/employee-ui-state", () => ({useEmployeeUiStateQuery: () => mocks.ui, employeeUiStateApi: {markNotificationsRead: (...args: unknown[]) => mocks.sync(...args)}}));
vi.mock("@entities/system-news", () => ({useSystemNewsQuery: () => ({data: []}), useMarkSystemNewsReadMutation: () => ({mutate: vi.fn()}), useMarkAllSystemNewsReadMutation: () => ({mutate: vi.fn()})}));
vi.mock("@entities/communications/ui", () => ({EmployeeCommunicationDialog: () => null}));
const now = Date.parse("2026-09-19T12:00:00Z");
const published = new Date(now - 5 * 86_400_000 + 10_000).toISOString();
const id = "schedule-public:21:" + published;
beforeEach(() => {
  vi.useFakeTimers(); vi.setSystemTime(now); localStorage.clear();
  mocks.sync.mockReset().mockResolvedValue(undefined);
  mocks.ui.data = {readNotificationIds: []};
  mocks.schedules = [{id: 21, name: "September", year: 2026, month: 9, publishedAtUtc: published}];
});
afterEach(() => {cleanup(); vi.useRealTimers();});
const show = () => render(<BrowserRouter><EmployeeWorkspaceLayout><EmployeeNotificationsPage /></EmployeeWorkspaceLayout></BrowserRouter>);
it("expires the card and navigation indicators without reloading", () => {
  show();
  expect(screen.getByText("New schedule published")).toBeInTheDocument();
  expect(screen.getByRole("link", {name: "Notifications, new updates"})).toBeInTheDocument();
  act(() => vi.advanceTimersByTime(30_000));
  expect(screen.queryByText("New schedule published")).not.toBeInTheDocument();
  expect(screen.queryByRole("link", {name: "Notifications, new updates"})).not.toBeInTheDocument();
  expect(screen.getByText("No notifications yet")).toBeInTheDocument();
});
it("marks a card read and clears navigation indicators", async () => {
  show();
  await act(async () => fireEvent.click(screen.getByRole("button", {name: "Mark as read"})));
  expect(mocks.sync).toHaveBeenCalledWith([id]);
  expect(screen.getByRole("button", {name: "Read"})).toBeDisabled();
  expect(screen.queryByRole("link", {name: "Notifications, new updates"})).not.toBeInTheDocument();
});
it("updates the inbox after read state changes in another surface", () => {
  show();
  act(() => writeEmployeeNotificationIdsForAccount("worker", 12, new Set([id])));
  expect(screen.getByRole("button", {name: "Read"})).toBeDisabled();
});
it("shows synchronization failures instead of silently losing server state", async () => {
  mocks.sync.mockRejectedValue(new Error("offline"));
  show();
  await act(async () => fireEvent.click(screen.getByRole("button", {name: "Mark all read"})));
  expect(screen.getByText("offline")).toBeInTheDocument();
});

it("honors server read state even when browser storage is blocked", () => {
  mocks.ui.data = {readNotificationIds: [id]};
  const spy = vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {throw new Error("blocked");});
  try {
    show();
    expect(screen.getByRole("button", {name: "Read"})).toBeDisabled();
    expect(screen.queryByRole("link", {name: "Notifications, new updates"})).not.toBeInTheDocument();
  } finally {spy.mockRestore();}
});
