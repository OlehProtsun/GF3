import { act, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { EmployeePwaInstallBanner } from "./EmployeePwaInstallBanner";

afterEach(() => vi.unstubAllGlobals());

test("browser offers the internal install page without a dismiss action", () => {
  setLanguage("en");
  const media = Object.assign(new EventTarget(), { matches: false });
  vi.stubGlobal("matchMedia", () => media);
  render(<BrowserRouter><EmployeePwaInstallBanner /></BrowserRouter>);
  expect(screen.getByRole("link", { name: "Download" })).toHaveAttribute("href", "/install");
  expect(screen.queryByRole("button")).toBeNull();
  act(() => { media.matches = true; media.dispatchEvent(new Event("change")); });
  expect(screen.queryByRole("complementary")).toBeNull();
});

test("iOS Home Screen launch hides the banner", () => {
  vi.stubGlobal("navigator", { standalone: true });
  render(<BrowserRouter><EmployeePwaInstallBanner /></BrowserRouter>);
  expect(screen.queryByRole("complementary")).toBeNull();
});
