import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { EmployeePwaInstallDialog } from "./EmployeePwaInstallDialog";

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ status: "authenticated", session: { role: "employee" } }),
}));

afterEach(() => {
  vi.unstubAllGlobals();
  sessionStorage.clear();
});

test("browser offers the internal install page and closes on standalone launch", () => {
  setLanguage("en");
  const media = Object.assign(new EventTarget(), { matches: false });
  vi.stubGlobal("matchMedia", () => media);
  render(<BrowserRouter><EmployeePwaInstallDialog /></BrowserRouter>);
  expect(screen.getByRole("dialog", { name: "Add GF to your home screen" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Install" })).toBeInTheDocument();
  act(() => { media.matches = true; media.dispatchEvent(new Event("change")); });
  expect(screen.queryByRole("dialog")).toBeNull();
});

test("Not now dismisses the dialog across remounts in the same session", () => {
  setLanguage("en");
  const { unmount } = render(<BrowserRouter><EmployeePwaInstallDialog /></BrowserRouter>);
  fireEvent.click(screen.getByRole("button", { name: "Not now" }));
  expect(screen.queryByRole("dialog")).toBeNull();
  unmount();
  render(<BrowserRouter><EmployeePwaInstallDialog /></BrowserRouter>);
  expect(screen.queryByRole("dialog")).toBeNull();
});

test("iOS Home Screen launch hides the dialog", () => {
  vi.stubGlobal("navigator", { standalone: true });
  render(<BrowserRouter><EmployeePwaInstallDialog /></BrowserRouter>);
  expect(screen.queryByRole("dialog")).toBeNull();
});

test("an open employee announcement takes priority and the install invite resumes after it closes", async () => {
  setLanguage("en");
  const { rerender } = render(<BrowserRouter>
    <div role="dialog" aria-modal="true" aria-label="Employee announcement" />
    <EmployeePwaInstallDialog />
  </BrowserRouter>);
  expect(screen.getByRole("dialog", { name: "Employee announcement" })).toBeVisible();
  expect(screen.queryByRole("dialog", { name: "Add GF to your home screen" })).toBeNull();
  rerender(<BrowserRouter><EmployeePwaInstallDialog /></BrowserRouter>);
  await waitFor(() => expect(screen.getByRole("dialog", { name: "Add GF to your home screen" })).toBeVisible());
});
