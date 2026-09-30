import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider, useAuth } from "@app/providers/AuthProvider";
import { QueryProvider } from "@app/providers/QueryProvider";
import { getAuthAccessToken, setAuthAccessToken } from "@shared/api/httpClient";
import { EmployeeAccountPage } from "./EmployeeAccountPage";
import { LoginPage } from "@pages/login/ui/LoginPage";

vi.mock("@entities/regulations", () => ({ useMyRegulationHistoryQuery: () => ({ data: [] }) }));
vi.mock("@entities/regulations/ui/RegulationHistoryCard", () => ({ RegulationHistoryCard: () => null }));
vi.mock("@shared/i18n/LanguageSelector", () => ({ LanguageSelector: () => null }));

const session = { role: "employee", userName: "worker", displayName: "Worker", employeeId: 1, managerId: null };
const fetchMock = vi.fn();
let resetStatus = 204;

function Workspace() {
  const { status } = useAuth();
  if (status === "loading") return null;
  return status === "authenticated" ? <EmployeeAccountPage /> : <LoginPage />;
}

beforeEach(() => {
  window.localStorage.clear();
  window.localStorage.setItem("gf3.auth.access-token", "old-token");
  setAuthAccessToken(null);
  resetStatus = 204;
  fetchMock.mockReset().mockImplementation(async (url: string) => {
    if (url.endsWith("auth/session")) return Response.json(session);
    if (url.endsWith("employee-profile/me")) return Response.json({
      employeeId: 1, username: "worker", displayName: "Worker", recoveryEmail: "worker@example.com", phone: null,
    });
    if (url.endsWith("password/confirm")) return resetStatus === 204
      ? new Response(null, { status: 204 })
      : Response.json({ detail: "Invalid verification code." }, { status: resetStatus });
    if (url.endsWith("auth/login")) return Response.json({ session, accessToken: "new-token" });
    throw new Error(`Unexpected request: ${url}`);
  });
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => { cleanup(); vi.unstubAllGlobals(); setAuthAccessToken(null); });

async function changePassword() {
  render(<BrowserRouter><AuthProvider><QueryProvider><Workspace /></QueryProvider></AuthProvider></BrowserRouter>);
  fireEvent.click(await screen.findByRole("button", { name: "Edit" }));
  fireEvent.change(screen.getByLabelText("Verification code"), { target: { value: "123456" } });
  fireEvent.change(screen.getByLabelText("New password"), { target: { value: "654321" } });
  fireEvent.click(screen.getByRole("button", { name: "Change password" }));
}

test("successful reset clears the revoked session, shows success at login and allows a new session", async () => {
  await changePassword();
  expect(await screen.findByRole("status")).toHaveTextContent("Password changed successfully. Sign in with your new password.");
  expect(getAuthAccessToken()).toBeNull();
  expect(window.localStorage.getItem("gf3.auth.access-token")).toBeNull();
  expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
    "/api/auth/session", "/api/employee-profile/me", "/api/employee-profile/me/password/confirm",
  ]);
  expect(JSON.parse(fetchMock.mock.calls[2][1].body)).toEqual({ code: "123456", newPassword: "654321" });
  fireEvent.change(screen.getByLabelText("Username"), { target: { value: "worker" } });
  fireEvent.change(screen.getByLabelText("Password"), { target: { value: "654321" } });
  fireEvent.click(screen.getByRole("button", { name: "Sign in" }));
  await screen.findByRole("button", { name: "Edit" });
  expect(getAuthAccessToken()).toBe("new-token");
  expect(screen.queryByRole("status")).not.toBeInTheDocument();
});

test("an invalid reset code displays the validation error and preserves the session", async () => {
  resetStatus = 400;
  await changePassword();
  await waitFor(() => expect(screen.getByText("Invalid verification code.")).toBeInTheDocument());
  expect(getAuthAccessToken()).toBe("old-token");
  expect(window.localStorage.getItem("gf3.auth.access-token")).toBe("old-token");
  expect(screen.queryByRole("status")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Change password" })).toBeEnabled();
});

test("an expired session during reset returns to login without claiming success", async () => {
  resetStatus = 401;
  await changePassword();
  await screen.findByRole("button", { name: "Sign in" });
  expect(getAuthAccessToken()).toBeNull();
  expect(window.localStorage.getItem("gf3.auth.access-token")).toBeNull();
  expect(screen.queryByRole("status")).not.toBeInTheDocument();
});
