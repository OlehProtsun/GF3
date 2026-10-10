import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { OverlaySidebarLayout } from "./OverlaySidebarLayout";

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

function renderManagerLayout(pathname = "/") {
  window.history.replaceState({}, "", pathname);

  return render(
    <QueryClientProvider client={new QueryClient()}><BrowserRouter>
      <OverlaySidebarLayout>
        <div>Manager content</div>
      </OverlaySidebarLayout>
    </BrowserRouter></QueryClientProvider>,
  );
}

describe("OverlaySidebarLayout", () => {
  beforeEach(() => {
    authMock.logout.mockClear();
    authMock.setManagerWorkspaceMode.mockClear();
    authMock.session.role = "manager";
    authMock.session.isSystemManager = undefined;
    window.history.replaceState({}, "", "/");
  });

  it("renders manager footer actions with the same nav button contract as top nav items", async () => {
    const user = userEvent.setup();
    const { container } = renderManagerLayout();

    const homeLink = screen.getByRole("link", { name: "Home" });
    const managerLink = screen.getByRole("link", { name: "Open manager profile" });
    const logoutButton = screen.getByRole("button", { name: "Log out" });

    expect(homeLink.className).toContain("navButton");
    const modeButton = screen.getByRole("button", { name: "Switch to Phone", exact: true });
    expect(modeButton.className).toBe(homeLink.className.split(" ")[0]);
    expect(modeButton.textContent).toBe("");
    expect(modeButton.querySelector("svg")).toBeInTheDocument();
    expect(screen.getByText("Switch to Phone").className).toContain("navLabel");
    expect(screen.getByText("Switch to Phone").previousElementSibling).toBe(modeButton);
    expect(managerLink.className).toContain("navButton");
    expect(logoutButton.className).toContain("navButton");
    expect(container.querySelector("main")?.className).toContain("contentManager");
    expect(screen.getByText("Manager")).toBeInTheDocument();
    expect(screen.getByText("Log out")).toBeInTheDocument();

    await user.click(managerLink);

    expect(window.location.pathname).toBe("/manager-profile");
    expect(managerLink.className).toContain("navButtonActive");
  });

  it("collapses and reopens the sidebar while keeping the collapsed arrow pointed down", async () => {
    const user = userEvent.setup();
    const { container } = renderManagerLayout();

    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));

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

  it("shows system tools and Settings only for a system manager", () => {
    authMock.session.isSystemManager = true;
    renderManagerLayout();
    expect(screen.getByRole("link", { name: "Information" })).toHaveAttribute("href", "/information");
    expect(screen.getByRole("link", { name: "DataBase" })).toHaveAttribute("href", "/database");
    expect(screen.getByText("Settings")).toBeInTheDocument();
  });

  it.each([false, undefined])("keeps ordinary manager navigation with privilege %s", async privilege => {
    authMock.session.isSystemManager = privilege;
    const user = userEvent.setup();
    renderManagerLayout();
    expect(screen.queryByRole("link", { name: "Information" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "DataBase" })).not.toBeInTheDocument();
    expect(screen.queryByText("Settings")).not.toBeInTheDocument();
    for (const name of ["Home", "Employee", "Shop", "Availability", "Container", "Message", "Open manager profile"]) {
      expect(screen.getByRole("link", { name })).toBeInTheDocument();
    }
    expect(screen.getByTestId("manager-notepad")).toBeInTheDocument();
    expect(screen.getByTestId("manager-system-news")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));
    await user.click(screen.getByRole("button", { name: "Open sidebar" }));
    expect(screen.getByRole("link", { name: "Message" })).toBeInTheDocument();
  });

  it.each([true, false, undefined])("does not grant employee system links with flag %s", privilege => {
    authMock.session.role = "employee";
    authMock.session.isSystemManager = privilege;
    renderManagerLayout();
    expect(screen.getByRole("link", { name: "Open profile" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Information" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "DataBase" })).not.toBeInTheDocument();
    expect(screen.queryByText("Settings")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-notepad")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-system-news")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Legal documents" })).toHaveAttribute("href", "/legal/index.html");
  });

  it("logs out through the shared nav-style action button", async () => {
    const user = userEvent.setup();
    renderManagerLayout();

    await user.click(screen.getByRole("button", { name: "Log out" }));

    expect(authMock.logout).toHaveBeenCalledTimes(1);
  });
});
