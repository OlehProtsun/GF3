import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PresenceProvider, useRealtime } from "./PresenceProvider";
import { queryKeys } from "@shared/api/queryKeys";
import { setAuthAccessToken } from "@shared/api/httpClient";

const realtime = vi.hoisted(() => ({ handlers: new Map<string, (update: unknown) => void>(), invocations: vi.fn() }));
const session = { role: "manager", managerId: 3, userName: "chief", workspaceMode: "pc" };
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
        onreconnected: vi.fn(),
        start: async () => {},
        stop: async () => {},
        invoke: async (_method: string, locks: unknown) => { realtime.invocations(locks); return []; },
      };
    }
  },
}));

beforeEach(() => { session.workspaceMode = "pc"; realtime.handlers.clear(); realtime.invocations.mockClear(); setAuthAccessToken("old-token"); });
afterEach(() => { cleanup(); setAuthAccessToken(null); });

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
