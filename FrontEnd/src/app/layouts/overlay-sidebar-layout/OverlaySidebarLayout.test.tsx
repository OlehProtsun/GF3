import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { OverlaySidebarLayout } from "./OverlaySidebarLayout";

const authMock = vi.hoisted(() => ({
  logout: vi.fn<() => Promise<void>>(() => Promise.resolve()),
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({
    logout: authMock.logout,
    session: {
      role: "manager",
      displayName: "OlehProtsun",
      userName: "manager",
    },
  }),
}));

function renderManagerLayout(pathname = "/") {
  window.history.replaceState({}, "", pathname);

  return render(
    <BrowserRouter>
      <OverlaySidebarLayout>
        <div>Manager content</div>
      </OverlaySidebarLayout>
    </BrowserRouter>,
  );
}

describe("OverlaySidebarLayout", () => {
  beforeEach(() => {
    authMock.logout.mockClear();
    window.history.replaceState({}, "", "/");
  });

  it("renders manager footer actions with the same nav button contract as top nav items", async () => {
    const user = userEvent.setup();
    renderManagerLayout();

    const homeLink = screen.getByRole("link", { name: "Home" });
    const managerLink = screen.getByRole("link", { name: "Open manager profile" });
    const logoutButton = screen.getByRole("button", { name: "Log out" });

    expect(homeLink.className).toContain("navButton");
    expect(managerLink.className).toContain("navButton");
    expect(logoutButton.className).toContain("navButton");
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

    const openButton = screen.getByRole("button", { name: "Open sidebar" });
    const openButtonArrow = openButton.querySelector("svg");

    expect(openButtonArrow?.className.baseVal).toContain("arrowDown");
    expect(openButtonArrow?.className.baseVal).not.toContain("arrowLeft");

    await user.click(openButton);

    expect(container.querySelector("aside")?.getAttribute("aria-hidden")).toBe("false");
  });

  it("logs out through the shared nav-style action button", async () => {
    const user = userEvent.setup();
    renderManagerLayout();

    await user.click(screen.getByRole("button", { name: "Log out" }));

    expect(authMock.logout).toHaveBeenCalledTimes(1);
  });
});
