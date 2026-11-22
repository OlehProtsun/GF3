import { act, fireEvent, render, screen } from "@testing-library/react";
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
