import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { AuthProvider, useAuth } from "./AuthProvider";
import { getAuthAccessToken, setAuthAccessToken } from "@shared/api/httpClient";
import type { AuthLoginResult, AuthSession } from "@entities/auth";
const api = vi.hoisted(() => ({ session: vi.fn(), login: vi.fn(), logout: vi.fn(), changeManagerWorkspaceMode: vi.fn() }));
vi.mock("@entities/auth", () => ({ authApi: api }));
const manager: AuthSession = { role: "manager", userName: "boss", displayName: "Boss", managerId: 1, workspaceMode: "choose" };
const result = (mode: AuthSession["workspaceMode"] = "phone"): AuthLoginResult => ({ accessToken: "new-token", expiresAtUtc: "", session: { ...manager, workspaceMode: mode } });
function Harness() {
  const auth = useAuth();
  return <><output>{auth.status}:{auth.session?.workspaceMode ?? "none"}</output>
    <button onClick={() => void auth.login({ username: "boss", password: "123456" })}>login</button>
    <button onClick={() => void auth.logout()}>logout</button>
    <button onClick={() => void auth.refreshSession()}>refresh</button>
    <button onClick={() => void auth.setManagerWorkspaceMode("phone").catch(() => {})}>phone</button></>;
}
beforeEach(() => { vi.clearAllMocks(); localStorage.clear(); setAuthAccessToken(null); api.session.mockResolvedValue(manager); api.login.mockResolvedValue({ ...result("choose"), accessToken: "old-token" }); api.logout.mockResolvedValue(undefined); });
afterEach(() => { cleanup(); setAuthAccessToken(null); });
async function login() { render(<AuthProvider><Harness /></AuthProvider>); await screen.findByText("unauthenticated:none"); fireEvent.click(screen.getByText("login")); await screen.findByText("authenticated:choose"); }
test("exchange replaces both credentials and mode only on success; concurrent exchange is refused", async () => {
  let finish!: (value: AuthLoginResult) => void;
  api.changeManagerWorkspaceMode.mockImplementation(() => new Promise(resolve => { finish = resolve; }));
  await login(); fireEvent.click(screen.getByText("phone")); fireEvent.click(screen.getByText("phone"));
  expect(api.changeManagerWorkspaceMode).toHaveBeenCalledTimes(1); expect(getAuthAccessToken()).toBe("old-token"); expect(screen.getByText("authenticated:choose")).toBeInTheDocument();
  await act(async () => finish(result())); await screen.findByText("authenticated:phone");
  expect(getAuthAccessToken()).toBe("new-token"); expect(localStorage.getItem("gf3.auth.access-token")).toBe("new-token"); expect(localStorage.length).toBe(1);
});
test("failed exchange retains token and session and permits retry", async () => {
  api.changeManagerWorkspaceMode.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(result()); await login();
  fireEvent.click(screen.getByText("phone")); await waitFor(() => expect(api.changeManagerWorkspaceMode).toHaveBeenCalledTimes(1));
  expect(getAuthAccessToken()).toBe("old-token"); expect(screen.getByText("authenticated:choose")).toBeInTheDocument();
  fireEvent.click(screen.getByText("phone")); await screen.findByText("authenticated:phone");
});
test("refresh restores signed mode and employee exchange is never sent", async () => {
  localStorage.setItem("gf3.auth.access-token", "phone-token"); api.session.mockResolvedValue({ ...manager, workspaceMode: "phone" });
  render(<AuthProvider><Harness /></AuthProvider>); await screen.findByText("authenticated:phone"); expect(getAuthAccessToken()).toBe("phone-token");
  api.session.mockResolvedValue({ role: "employee", userName: "worker", displayName: "Worker", employeeId: 1 }); fireEvent.click(screen.getByText("refresh")); await screen.findByText("authenticated:none");
  fireEvent.click(screen.getByText("phone")); expect(api.changeManagerWorkspaceMode).not.toHaveBeenCalled();
});
test("late successful exchange cannot restore logged-out session", async () => {
  let finish!: (value: AuthLoginResult) => void; api.changeManagerWorkspaceMode.mockImplementation(() => new Promise(resolve => { finish = resolve; }));
  await login(); fireEvent.click(screen.getByText("phone")); fireEvent.click(screen.getByText("logout")); await screen.findByText("unauthenticated:none");
  await act(async () => finish(result())); expect(getAuthAccessToken()).toBeNull(); expect(screen.getByText("unauthenticated:none")).toBeInTheDocument();
});
