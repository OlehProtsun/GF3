import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "./AuthProvider";
import { getAuthAccessToken, setAuthAccessToken } from "@shared/api/httpClient";

const api = vi.hoisted(() => ({ session: vi.fn(), login: vi.fn(), logout: vi.fn() }));
vi.mock("@entities/auth", () => ({ authApi: api }));
const session = { role: "employee" as const, userName: "worker", displayName: "Worker", employeeId: 1, managerId: null };
function Harness() {
  const auth = useAuth();
  return <><output>{auth.status}</output><button onClick={() => void auth.login({ username: "worker", password: "123456" })}>login</button><button onClick={() => void auth.logout()}>logout</button></>;
}
beforeEach(() => {
  window.localStorage.clear();
  setAuthAccessToken(null);
  api.login.mockResolvedValue({ session, accessToken: "token", expiresAtUtc: "" });
  api.logout.mockResolvedValue(undefined);
  api.session.mockResolvedValue(session);
});
afterEach(() => { cleanup(); vi.restoreAllMocks(); setAuthAccessToken(null); });

describe("authentication lifecycle", () => {
  it("supports login and logout when all storage operations fail", async () => {
    for (const operation of ["getItem", "setItem", "removeItem"] as const) {
      vi.spyOn(Storage.prototype, operation).mockImplementation(() => { throw new Error("storage blocked"); });
    }
    render(<AuthProvider><Harness /></AuthProvider>);
    await screen.findByText("unauthenticated");
    fireEvent.click(screen.getByText("login"));
    await screen.findByText("authenticated");
    expect(getAuthAccessToken()).toBe("token");
    fireEvent.click(screen.getByText("logout"));
    await screen.findByText("unauthenticated");
    expect(getAuthAccessToken()).toBeNull();
  });

  it("clears in-memory credentials when another tab logs out", async () => {
    render(<AuthProvider><Harness /></AuthProvider>);
    await screen.findByText("unauthenticated");
    fireEvent.click(screen.getByText("login"));
    await screen.findByText("authenticated");
    act(() => window.dispatchEvent(new StorageEvent("storage", { key: "gf3.auth.access-token", newValue: null })));
    await screen.findByText("unauthenticated");
    expect(getAuthAccessToken()).toBeNull();
  });

  it("does not restore a stale bootstrap response after logout", async () => {
    let finish!: (value: typeof session) => void;
    window.localStorage.setItem("gf3.auth.access-token", "token");
    api.session.mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }));
    render(<AuthProvider><Harness /></AuthProvider>);
    await waitFor(() => expect(finish).toBeDefined());
    fireEvent.click(screen.getByText("logout"));
    await screen.findByText("unauthenticated");
    await act(async () => { finish(session); });
    expect(screen.getByText("unauthenticated")).toBeInTheDocument();
  });
});