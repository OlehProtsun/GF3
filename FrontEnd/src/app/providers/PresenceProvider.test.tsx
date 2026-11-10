import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PresenceProvider, useRealtime } from "./PresenceProvider";
import { queryKeys } from "@shared/api/queryKeys";
import { setAuthAccessToken } from "@shared/api/httpClient";
import { beginGraphSaveTraffic, finishGraphSaveTraffic } from "@shared/api/graphSaveTraffic";

const realtime = vi.hoisted(() => ({ handlers: new Map<string, (update: unknown) => void>(), invocations: vi.fn(), reconnected: null as (() => void) | null }));
const session = { role: "manager", managerId: 3, employeeId: undefined as number | undefined, userName: "chief", workspaceMode: "pc" };
vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ status: "authenticated", session, logout: vi.fn() }),
}));
vi.mock("@microsoft/signalr", () => ({
  HubConnectionState: { Connected: "connected" },
  LogLevel: { Warning: 3, Error: 4 },
  HubConnectionBuilder: class {
    withUrl() { return this; }
    withAutomaticReconnect() { return this; }
    configureLogging() { return this; }
    build() {
      return {
        state: "connected",
        on: (name: string, handler: (update: unknown) => void) => realtime.handlers.set(name, handler),
        off: (name: string) => realtime.handlers.delete(name),
        onreconnected: (handler: () => void) => { realtime.reconnected = handler; },
        start: async () => {},
        stop: async () => {},
        invoke: async (_method: string, locks: unknown) => { realtime.invocations(locks); return []; },
      };
    }
  },
}));

beforeEach(() => { session.role = "manager"; session.employeeId = undefined; session.workspaceMode = "pc"; realtime.handlers.clear(); realtime.invocations.mockClear(); setAuthAccessToken("old-token"); });
afterEach(() => { cleanup(); vi.useRealTimers(); setAuthAccessToken(null); });

function scheduleBurst(graphId: number, reason = "manager-schedule-style-updated") {
  const update = { containerId: 7, graphId, reason, changedAtUtc: "2026-11-09T10:00:00Z" };
  realtime.handlers.get("ScheduleChanged")!(update);
  realtime.handlers.get("ManagerDataChanged")!({ ...update, resourceType: "schedule" });
  realtime.handlers.get("ShiftSwapsChanged")!(update);
}

test("coalesces three streams and distinct graphs into one exact refresh group", async () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  session.role = "employee"; session.employeeId = 1;
  function Notifications() {
    const { notifications } = useRealtime();
    return <output>{notifications.map(item => item.reason).join(",")}</output>;
  }
  const view = render(<QueryClientProvider client={client}><PresenceProvider><Notifications /></PresenceProvider></QueryClientProvider>);
  await waitFor(() => expect(realtime.handlers.has("ScheduleChanged")).toBe(true));
  invalidate.mockClear();
  vi.useFakeTimers();
  act(() => { scheduleBurst(34); scheduleBurst(36, "manager-schedule-published"); });
  expect(screen.getByText("manager-schedule-published")).toBeVisible();
  act(() => vi.advanceTimersByTime(299));
  expect(invalidate.mock.calls.map(([value]) => value.queryKey)).toEqual([queryKeys.home.navBadges(), queryKeys.home.navBadges()]);
  act(() => vi.advanceTimersByTime(1));
  const filters = invalidate.mock.calls.map(([value]) => value);
  expect(filters.filter(value => JSON.stringify(value.queryKey) === JSON.stringify(queryKeys.containers.graphs(7)))).toEqual([
    { queryKey: queryKeys.containers.graphs(7), exact: true },
  ]);
  for (const id of [34, 36]) expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.containers.graphSlots(7, id), exact: true });
  expect(filters.filter(value => JSON.stringify(value.queryKey) === JSON.stringify(queryKeys.home.navBadges()))).toHaveLength(3);
  expect(invalidate).not.toHaveBeenCalledWith({ queryKey: queryKeys.containers.all });
  invalidate.mockClear();
  act(() => scheduleBurst(42));
  invalidate.mockClear();
  view.unmount();
  act(() => vi.advanceTimersByTime(300));
  expect(invalidate).not.toHaveBeenCalled();
});

test("refreshes dock badges for Urlop, non-graph swaps and reconnects", async () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  render(<QueryClientProvider client={client}><PresenceProvider><span>workspace</span></PresenceProvider></QueryClientProvider>);
  await waitFor(() => expect(realtime.handlers.has("ManagerDataChanged")).toBe(true));
  for (const emit of [
    () => realtime.handlers.get("ManagerDataChanged")!({ resourceType: "urlop", reason: "urlop-approved" }),
    () => realtime.handlers.get("ShiftSwapsChanged")!({ reason: "approval-rejected" }),
    () => realtime.reconnected!(),
  ]) {
    invalidate.mockClear(); act(emit);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.navBadges() });
  }
});

test("defers save-related events and keeps confirmed cache data on gate exit", async () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  render(<QueryClientProvider client={client}><PresenceProvider><span>workspace</span></PresenceProvider></QueryClientProvider>);
  await waitFor(() => expect(invalidate).toHaveBeenCalled());
  invalidate.mockClear();
  beginGraphSaveTraffic(client, 7);
  act(() => scheduleBurst(34));
  expect(invalidate.mock.calls.map(([value]) => value.queryKey)).toEqual([queryKeys.home.navBadges()]);
  const key = queryKeys.containers.graphById(7, 34);
  client.setQueryData(key, "confirmed");
  await finishGraphSaveTraffic(client, 7, []);
  expect(invalidate).toHaveBeenCalledWith({ queryKey: key, exact: true, refetchType: "none" });
  expect(client.getQueryState(key)).toEqual({ data: "confirmed", updatedAt: 0 });
});

test("keeps workflow logs and manual-offer notifications current without refetching during a save", async () => {
  session.role = "employee";
  session.employeeId = 1;
  function Notifications() {
    const { notifications } = useRealtime();
    return <output>{notifications.map(item => item.reason).join(",")}</output>;
  }
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  client.setQueryData(queryKeys.workflowLogs.list(), []);
  render(<QueryClientProvider client={client}><PresenceProvider><Notifications /></PresenceProvider></QueryClientProvider>);
  await waitFor(() => expect(realtime.handlers.has("WorkflowLogCreated")).toBe(true));
  beginGraphSaveTraffic(client, 7);
  const entry = { id: 5, occurredAtUtc: "2026-11-09T10:00:00Z", actorRole: "manager", actorName: "Manager", action: "Edited schedule" };
  act(() => {
    realtime.handlers.get("WorkflowLogCreated")!(entry);
    realtime.handlers.get("ShiftSwapsChanged")!({ containerId: 7, graphId: 34, shiftSwapId: 10,
      reason: "manager-manual-shift-offer-created", changedAtUtc: entry.occurredAtUtc });
  });
  expect(client.getQueryState(queryKeys.workflowLogs.list()).data).toEqual([entry]);
  expect(screen.getByText("manager-manual-shift-offer-created")).toBeVisible();
  expect(invalidate.mock.calls.map(([value]) => value.queryKey)).toEqual([queryKeys.home.navBadges()]);
  await finishGraphSaveTraffic(client, 7, []);
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.all, exact: false, refetchType: "none" });
});

test("defers own profile realtime refresh until replacement credentials arrive, while preserving other updates", async () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  render(<QueryClientProvider client={client}><PresenceProvider><span>workspace</span></PresenceProvider></QueryClientProvider>);
  await waitFor(() => expect(invalidate).toHaveBeenCalled());
  invalidate.mockClear();
  client.setQueryData(queryKeys.managerProfile.updating(), true);
  const update = { resourceType: "manager-profile", resourceId: "3", reason: "manager-profile-updated" };
  act(() => realtime.handlers.get("ManagerDataChanged")!(update));
  expect(invalidate).not.toHaveBeenCalled();
  act(() => realtime.handlers.get("ManagerDataChanged")!({ ...update, resourceId: "4" }));
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.managerProfile.list() });
  invalidate.mockClear();
  client.setQueryData(queryKeys.managerProfile.updating(), false);
  act(() => realtime.handlers.get("ManagerDataChanged")!(update));
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.managerProfile.me() });
});
test.each(["phone", "choose"])("manager %s never starts SignalR", mode => {
 session.workspaceMode = mode;
 render(<QueryClientProvider client={new QueryClient()}><PresenceProvider><span>workspace</span></PresenceProvider></QueryClientProvider>);
 expect(realtime.handlers.size).toBe(0);
});
test("leaving PC releases the connection, phone setters are inert and old lock targets are not republished", async () => {
  function Locks() { const { setManagerEditLocks } = useRealtime(); return <button onClick={() => void setManagerEditLocks([{ resourceType: "employee", resourceId: "2" }])}>lock</button>; }
  const client = new QueryClient();
  const element = () => <QueryClientProvider client={client}><PresenceProvider><Locks /></PresenceProvider></QueryClientProvider>;
  const view = render(element()); await waitFor(() => expect(realtime.handlers.size).toBeGreaterThan(0));
  fireEvent.click(screen.getByText("lock")); await waitFor(() => expect(realtime.invocations).toHaveBeenCalledWith([{ resourceType: "employee", resourceId: "2" }]));
  session.workspaceMode = "phone"; view.rerender(element()); await waitFor(() => expect(realtime.handlers.size).toBe(0)); realtime.invocations.mockClear();
  fireEvent.click(screen.getByText("lock")); expect(realtime.invocations).not.toHaveBeenCalled();
  session.workspaceMode = "pc"; view.rerender(element()); await waitFor(() => expect(realtime.invocations).toHaveBeenCalled());
  expect(realtime.invocations.mock.calls.every(([locks]) => locks.length === 0)).toBe(true);
});
