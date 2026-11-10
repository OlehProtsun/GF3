import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { OverlaySidebarLayout } from "./OverlaySidebarLayout";

beforeEnglishTest(() => setTestLanguage("en"));

vi.mock("@shared/api/httpClient", async importOriginal => ({ ...await importOriginal<typeof import("@shared/api/httpClient")>(), request: vi.fn(() => Promise.resolve({ totalPending: 0, graphPending: 0, urlopPending: 0 })) }));

const authMock = vi.hoisted(() => ({
  session: { role: "manager" as "manager" | "employee", displayName: "Synthetic manager", userName: "manager", isSystemManager: undefined as boolean | undefined },
  setManagerWorkspaceMode: vi.fn(() => Promise.resolve()),
  logout: vi.fn<() => Promise<void>>(() => Promise.resolve()),
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({
    logout: authMock.logout,
    setManagerWorkspaceMode: authMock.setManagerWorkspaceMode,
    session: authMock.session,
  }),
}));

vi.mock("@features/manager-notepad/ui/ManagerNotepad", () => ({
  ManagerNotepad: () => <div data-testid="manager-notepad" />,
}));

vi.mock("@features/manager-system-news/ui/ManagerSystemNews", () => ({
  ManagerSystemNews: () => <div data-testid="manager-system-news" />,
}));

function renderManagerLayout(pathname = "/", openSidebar = true) {
  window.history.replaceState({}, "", pathname);

  const result = render(
    <QueryClientProvider client={new QueryClient()}><BrowserRouter>
      <OverlaySidebarLayout>
        <div>Manager content</div>
      </OverlaySidebarLayout>
    </BrowserRouter></QueryClientProvider>,
  );
  if (openSidebar && authMock.session.role === "manager") fireEvent.click(screen.getByRole("button", { name: "Open sidebar" }));
  return result;
}

describe("OverlaySidebarLayout", () => {
  it.each(["/employee/2", "/shop/2", "/availability/2", "/urlop/2", "/container/2/graphs/3"])("activates only the nested parent for %s", path => {
    renderManagerLayout(path);
    const links = screen.getByRole("navigation", { name: "Manager navigation" }).querySelectorAll('[aria-current="page"]');
    expect(links).toHaveLength(1);
    expect(links[0]).toHaveAttribute("href", "/" + path.split("/")[1]);
    expect(screen.getByRole("link", { name: "Home" })).not.toHaveAttribute("aria-current");
  });
  beforeEach(() => {
    authMock.logout.mockClear();
    authMock.setManagerWorkspaceMode.mockClear();
    authMock.session.role = "manager";
    authMock.session.isSystemManager = undefined;
    window.history.replaceState({}, "", "/");
  });

  it("restores expanded manager labels and controls without sidebar session actions", async () => {
    const user = userEvent.setup();
    const { container } = renderManagerLayout();

    const homeLink = screen.getByRole("link", { name: "Home" });
    const managerLink = screen.getByRole("link", { name: "Open manager profile" });
    expect(homeLink.className).toContain("dockLink");
    expect(managerLink.className).toContain("dockLink");
    expect(container.querySelector("main")?.className).toContain("contentManager");
    expect(screen.getAllByRole("button", { name: "Collapse sidebar" })[0]).toHaveAttribute("aria-expanded", "true");
    expect(screen.queryByRole("button", { name: "Open sidebar" })).not.toBeInTheDocument();
    expect(container.querySelector('[class*="backdrop"]')?.className).toContain("backdropVisible");
    expect(screen.getByRole("navigation", { name: "Manager navigation" }).querySelectorAll("a")).toHaveLength(9);
    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Manager")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Switch to Phone" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Log out" })).not.toBeInTheDocument();

    await user.click(managerLink);

    expect(window.location.pathname).toBe("/manager-profile");
    expect(managerLink.className).toContain("dockLinkActive");
    for (const name of ["Legal documents", "Terms", "Privacy policy"]) {
      expect(screen.getByRole("link", { name })).toBeVisible();
    }
  });

  it("starts hidden and toggles the mounted overlay without reserving a compact rail", async () => {
    const user = userEvent.setup(); const { container } = renderManagerLayout("/", false);
    const nav = container.querySelector("#manager-sidebar-navigation")!;
    const toggle = screen.getByRole("button", { name: "Open sidebar" });
    expect(nav).toHaveAttribute("data-collapsed", "true");
    expect(nav).toHaveAttribute("aria-hidden", "true");
    expect(nav).toHaveAttribute("inert");
    expect(screen.queryByRole("link", { name: "Container" })).toBeNull();
    expect(container.querySelector('[class*="backdrop"]')?.className).toContain("backdropHidden");
    await user.click(toggle);
    expect(nav).toHaveAttribute("data-collapsed", "false");
    expect(nav).not.toHaveAttribute("inert");
    expect(screen.getByRole("link", { name: "Container" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Open sidebar" })).toBeNull();
    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));
    expect(nav).toHaveAttribute("aria-hidden", "true");
    expect(container.querySelector("#manager-sidebar-navigation")).toBe(nav);
    await user.click(screen.getByRole("button", { name: "Open sidebar" }));
    await user.click(container.querySelector('[class*="backdrop"]')!);
    expect(nav).toHaveAttribute("data-collapsed", "true");
  });

  it("collapses and reopens the sidebar while keeping the collapsed arrow pointed down", async () => {
    const user = userEvent.setup();
    authMock.session.role = "employee";
    const { container } = renderManagerLayout("/profile");

    await user.click(screen.getAllByRole("button", { name: "Collapse sidebar" })[0]);

    expect(container.querySelector("aside")?.getAttribute("aria-hidden")).toBe("true");
    for (const name of ["Legal documents", "Terms", "Privacy policy"]) {
      const link = screen.getByRole("link", { name });
      expect(link.closest('[aria-hidden="true"]')).toBeNull();
      expect(link).toBeVisible();
    }

    const openButton = screen.getByRole("button", { name: "Open sidebar" });
    const openButtonArrow = openButton.querySelector("svg");

    expect(openButtonArrow?.className.baseVal).toContain("arrowDown");
    expect(openButtonArrow?.className.baseVal).not.toContain("arrowLeft");

    await user.click(openButton);

    expect(container.querySelector("aside")?.getAttribute("aria-hidden")).toBe("false");
  });

  it("keeps the system Settings heading and DataBase restricted", () => {
    authMock.session.isSystemManager = true;
    renderManagerLayout();
    expect(screen.getByRole("link", { name: "Information" })).toHaveAttribute("href", "/information");
    expect(screen.getByRole("link", { name: "DataBase" })).toHaveAttribute("href", "/database");
    expect(screen.getByText("Settings")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Settings" })).toHaveAttribute("href", "/settings");
  });

  it.each([false, undefined])("keeps ordinary manager navigation with privilege %s", async privilege => {
    authMock.session.isSystemManager = privilege;
    const user = userEvent.setup();
    renderManagerLayout();
    expect(screen.queryByRole("link", { name: "Information" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "DataBase" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Settings" })).toHaveAttribute("href", "/settings");
    for (const name of ["Home", "Employee", "Shop", "Availability", "Urlop", "Container", "Message", "Open manager profile"]) {
      expect(screen.getByRole("link", { name })).toBeInTheDocument();
    }
    expect(screen.getByTestId("manager-notepad")).toBeInTheDocument();
    expect(screen.getByTestId("manager-system-news")).toBeInTheDocument();
    await user.click(screen.getByRole("link", { name: "Message" }));
    expect(window.location.pathname).toBe("/communications");
    expect(screen.getByRole("link", { name: "Message" })).toBeInTheDocument();
  });

  it.each([true, false, undefined])("does not grant employee system links with flag %s", privilege => {
    authMock.session.role = "employee";
    authMock.session.isSystemManager = privilege;
    renderManagerLayout("/profile");
    expect(screen.getByRole("link", { name: "Open profile" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Information" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "DataBase" })).not.toBeInTheDocument();
    expect(screen.queryByText("Settings")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-notepad")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-system-news")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Legal documents" })).toHaveAttribute("href", "/legal/index.html");
  });

  it("logs out through the nonmanager fallback action", async () => {
    authMock.session.role = "employee";
    const user = userEvent.setup();
    renderManagerLayout();

    await user.click(screen.getByRole("button", { name: "Log out" }));

    expect(authMock.logout).toHaveBeenCalledTimes(1);
  });
});
