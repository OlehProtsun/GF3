import { act, cleanup, render, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PresenceProvider } from "./PresenceProvider";
import { queryKeys } from "@shared/api/queryKeys";
import { setAuthAccessToken } from "@shared/api/httpClient";

const realtime = vi.hoisted(() => ({ handlers: new Map<string, (update: unknown) => void>() }));
const session = { role: "manager", managerId: 3, userName: "chief" };
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
        invoke: async () => [],
      };
    }
  },
}));

beforeEach(() => { realtime.handlers.clear(); setAuthAccessToken("old-token"); });
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
