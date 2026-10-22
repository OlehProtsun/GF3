import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider, useAuth } from "@app/providers/AuthProvider";
import { QueryProvider } from "@app/providers/QueryProvider";
import { getAuthAccessToken, setAuthAccessToken } from "@shared/api/httpClient";
import { EmployeeAccountPage } from "./EmployeeAccountPage";
import { LoginPage } from "@pages/login/ui/LoginPage";

beforeEnglishTest(() => setTestLanguage("en"));

vi.mock("@entities/regulations", () => ({ useMyRegulationHistoryQuery: () => ({ data: [] }) }));
vi.mock("@entities/regulations/ui/RegulationHistoryCard", () => ({ RegulationHistoryCard: () => <section aria-label="Regulation history" /> }));
vi.mock("@shared/i18n/LanguageSelector", () => ({ LanguageSelector: () => <button type="button" aria-label="Application language">English</button> }));

const session = { role: "employee", userName: "worker", displayName: "Worker", employeeId: 1, managerId: null };
const fetchMock = vi.fn();
let resetStatus = 204;
let saveStatus = 200;
let sendStatus = 200;
let storedEmail: string | null = "worker@example.com";

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
  saveStatus = 200;
  sendStatus = 200;
  storedEmail = "worker@example.com";
  fetchMock.mockReset().mockImplementation(async (url: string, options?: RequestInit) => {
    if (url.endsWith("auth/session")) return Response.json(session);
    if (url.endsWith("employee-push/config")) return Response.json({ enabled: false, publicKey: null });
    if (url.endsWith("password/send-code")) return sendStatus === 200 ? Response.json({ deliveryHint: "w***@example.com" }) : Response.json({ detail: "Could not send code." }, { status: sendStatus });
    if (url.endsWith("employee-profile/me") && options?.method === "PUT") {
      if (saveStatus !== 200) return Response.json({ detail: "Invalid recovery email." }, { status: saveStatus });
      const values = JSON.parse(options.body as string);
      storedEmail = values.recoveryEmail;
      return Response.json({ employeeId: 1, username: "worker", displayName: "Worker", ...values });
    }
    if (url.endsWith("employee-profile/me")) return Response.json({
      employeeId: 1, username: "worker", displayName: "Worker", recoveryEmail: storedEmail, phone: null,
    });
    if (url.endsWith("password/confirm")) return resetStatus === 204
      ? new Response(null, { status: 204 })
      : Response.json({ detail: "Invalid verification code." }, { status: resetStatus });
    if (url.endsWith("auth/logout")) return new Response(null, { status: 204 });
    if (url.endsWith("auth/login")) return Response.json({ session, accessToken: "new-token" });
    throw new Error(`Unexpected request: ${url}`);
  });
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => { cleanup(); vi.unstubAllGlobals(); setAuthAccessToken(null); });

async function changePassword() {
  render(<BrowserRouter><AuthProvider><QueryProvider><Workspace /></QueryProvider></AuthProvider></BrowserRouter>);
  fireEvent.click(await screen.findByRole("button", { name: "Change password" }));
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
    "/api/auth/session", "/api/employee-profile/me", "/api/employee-push/config", "/api/employee-profile/me/password/confirm",
  ]);
  expect(JSON.parse(fetchMock.mock.calls[3][1].body)).toEqual({ code: "123456", newPassword: "654321" });
  fireEvent.change(screen.getByLabelText("Username"), { target: { value: "worker" } });
  fireEvent.change(screen.getByLabelText("Password"), { target: { value: "654321" } });
  fireEvent.click(screen.getByRole("button", { name: "Sign in" }));
  await screen.findByRole("button", { name: "Edit account details" });
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

async function openProfile() {
  render(<BrowserRouter><AuthProvider><QueryProvider><Workspace /></QueryProvider></AuthProvider></BrowserRouter>);
  await screen.findByRole("heading", { name: "Account details" });
}
const writes = () => fetchMock.mock.calls.filter(([, options]) => options?.method && options.method !== "GET");

test("account editing is separate, mutually exclusive and cancellable without writes", async () => {
  await openProfile();
  const account = screen.getByRole("region", { name: "Account details" });
  expect(within(account).getByRole("button", { name: "Application language" })).toBeVisible();
  expect(within(account).getByText("Recovery email")).toBeVisible();
  expect(within(account).getByText("Phone")).toBeVisible();
  expect(screen.getByRole("region", { name: "Regulation history" })).toBeVisible();
  expect(within(account).getByText("Login")).toBeVisible();
  expect(within(account).getByText("Display name")).toBeVisible();
  expect(screen.getByRole("region", { name: "Push notifications" })).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Edit account details" }));
  expect(screen.queryByLabelText("Verification code")).not.toBeInTheDocument();
  expect(screen.queryByLabelText("New password")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Change password" })).toBeDisabled();
  expect(within(account).getByRole("button", { name: "Save profile" })).toBeDisabled();
  fireEvent.change(screen.getByLabelText(/Recovery email/), { target: { value: "discard@example.com" } });
  fireEvent.click(within(account).getByRole("button", { name: "Cancel" }));
  expect(screen.getByText("worker@example.com")).toBeVisible();
  expect(screen.getByRole("button", { name: "Change password" })).toBeEnabled();
  expect(writes()).toEqual([]);
});

test("password editor contains only password controls and cancel clears inputs", async () => {
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Change password" }));
  const security = screen.getByRole("region", { name: "Password & security" });
  expect(within(security).getByLabelText("Verification code")).toBeVisible();
  expect(screen.queryByLabelText(/Recovery email/)).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Save profile" })).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Edit account details" })).toBeDisabled();
  fireEvent.change(screen.getByLabelText("Verification code"), { target: { value: "123456" } });
  fireEvent.change(screen.getByLabelText("New password"), { target: { value: "12345" } });
  expect(within(security).getByRole("button", { name: "Change password" })).toBeDisabled();
  fireEvent.click(within(security).getByRole("button", { name: "Cancel" }));
  fireEvent.click(screen.getByRole("button", { name: "Change password" }));
  expect(screen.getByLabelText("Verification code")).toHaveValue("");
  expect(screen.getByLabelText("New password")).toHaveValue("");
  expect(writes()).toEqual([]);
});

test("send code never saves contacts and disables logout while pending", async () => {
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Change password" }));
  fireEvent.click(screen.getByRole("button", { name: "Send code" }));
  expect(screen.getByRole("button", { name: "Log out" })).toBeDisabled();
  await screen.findByText("Code sent to w***@example.com.");
  expect(writes().map(([url]) => url)).toEqual(["/api/employee-profile/me/password/send-code"]);
});

test("missing saved email requires contact save before password initiation", async () => {
  storedEmail = null;
  await openProfile();
  expect(screen.getByText("Recovery email required")).toBeVisible();
  expect(screen.getByRole("button", { name: "Change password" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Edit account details" }));
  fireEvent.change(screen.getByLabelText(/Recovery email/), { target: { value: "new@example.com" } });
  fireEvent.change(screen.getByLabelText(/Phone/), { target: { value: "+48123456789" } });
  expect(screen.getByRole("button", { name: "Change password" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Save profile" }));
  await screen.findByText("Profile updated.");
  expect(screen.getByText("new@example.com")).toBeVisible();
  expect(screen.getByRole("button", { name: "Change password" })).toBeEnabled();
  expect(writes()).toHaveLength(1);
  expect(JSON.parse(writes()[0][1].body)).toEqual({ recoveryEmail: "new@example.com", phone: "+48123456789" });
});

test("failed contact save retains editor and draft", async () => {
  saveStatus = 400;
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Edit account details" }));
  fireEvent.change(screen.getByLabelText(/Recovery email/), { target: { value: "invalid" } });
  fireEvent.click(screen.getByRole("button", { name: "Save profile" }));
  await screen.findByText("Invalid recovery email.");
  expect(screen.getByLabelText(/Recovery email/)).toHaveValue("invalid");
  expect(screen.getByRole("button", { name: "Change password" })).toBeDisabled();
  expect(writes()).toHaveLength(1);
});

test("failed send code preserves password editor, permits retry and never saves profile", async () => {
  sendStatus = 500;
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Change password" }));
  fireEvent.click(screen.getByRole("button", { name: "Send code" }));
  await screen.findByText("Could not send code.");
  expect(screen.getByLabelText("Verification code")).toBeVisible();
  expect(screen.queryByText(/Last code sent to/)).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Send code" })).toBeEnabled();
  expect(writes().every(([url]) => url.endsWith("password/send-code"))).toBe(true);
});

test("normalized contact values do not enable redundant saving", async () => {
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Edit account details" }));
  fireEvent.change(screen.getByLabelText(/Recovery email/), { target: { value: " worker@example.com " } });
  fireEvent.change(screen.getByLabelText(/Phone/), { target: { value: "  " } });
  expect(screen.getByRole("button", { name: "Save profile" })).toBeDisabled();
});

test("logout invokes the existing auth endpoint and clears the session", async () => {
  await openProfile();
  fireEvent.click(screen.getByRole("button", { name: "Log out" }));
  await screen.findByRole("button", { name: "Sign in" });
  expect(getAuthAccessToken()).toBeNull();
  expect(writes().map(([url]) => url)).toContain("/api/auth/logout");
});
