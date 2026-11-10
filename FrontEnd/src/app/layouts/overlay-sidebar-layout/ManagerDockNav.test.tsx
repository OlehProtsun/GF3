import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { useState } from "react";
import { gsap } from "gsap";
import { request } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { setLanguage } from "@shared/i18n";
import { ManagerDockNav, isManagerDockRouteActive } from "./ManagerDockNav";

vi.mock("@shared/api/httpClient", async importOriginal => ({ ...await importOriginal<typeof import("@shared/api/httpClient")>(), request: vi.fn() }));
const mockedRequest = vi.mocked(request);
const counts = (n: number) => ({ totalPending: n, graphPending: n, urlopPending: n });
function mount() {
  const client = new QueryClient();
  function Navigation() {
    const [isCollapsed, setIsCollapsed] = useState(false);
    return <ManagerDockNav isSystemManager accountPath="/manager-profile" isCollapsed={isCollapsed} onToggleCollapse={() => setIsCollapsed(current => !current)} />;
  }
  const result = render(<QueryClientProvider client={client}><BrowserRouter><Navigation /></BrowserRouter></QueryClientProvider>);
  return { ...result, client };
}
beforeEach(() => {
  setLanguage("en"); window.history.replaceState({}, "", "/"); mockedRequest.mockReset();
  mockedRequest.mockResolvedValue(counts(0));
  vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() })));
  vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockReturnValue({ x: 14, y: 40, left: 14, top: 40, right: 118, bottom: 90, width: 104, height: 50, toJSON() {} });
});
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

describe("ManagerDockNav", () => {
  it("toggles the existing navigation without remounting or fetching badges again", async () => {
    mockedRequest.mockResolvedValue(counts(3));
    const user = userEvent.setup(); mount();
    await screen.findByText("3 pending approval requests");
    const nav = screen.getByRole("navigation");
    expect(nav).toHaveAttribute("data-collapsed", "false");
    expect(screen.getByText("Manager")).toBeInTheDocument();
    await user.click(screen.getAllByRole("button", { name: "Collapse sidebar" })[0]);
    expect(nav).toHaveAttribute("data-collapsed", "true");
    expect(screen.getByRole("button", { name: "Open sidebar" })).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("link", { name: "Home" })).toBeNull();
    expect(nav.querySelector("[data-manager-dock-badge]")).toHaveTextContent("3");
    await user.click(screen.getByRole("button", { name: "Open sidebar" }));
    expect(nav).toHaveAttribute("data-collapsed", "false");
    expect(mockedRequest).toHaveBeenCalledTimes(1);
    expect(screen.queryByText(/New approval requests/)).toBeNull();
  });
  it.each([0, 1, 9, 99, 100])("renders accurate category counters for %s", async n => {
    mockedRequest.mockResolvedValue(counts(n)); mount();
    await waitFor(() => expect(screen.getByRole("status")).toHaveTextContent(`${n} pending approval requests`));
    for (const label of ["Home", "Container", "Urlop"]) {
      const badge = screen.getByRole("link", { name: label }).querySelector("[data-manager-dock-badge]");
      if (n === 0) expect(badge).toBeNull(); else expect(badge).toHaveTextContent(n >= 100 ? "99+" : String(n));
    }
    for (const label of ["Message", "Employee", "Shop", "Availability", "Information", "Settings", "DataBase"]) {
      expect(screen.getByRole("link", { name: label }).querySelector("[data-manager-dock-badge]")).toBeNull();
    }
    expect(screen.queryByText(/New approval requests/)).not.toBeInTheDocument();
    expect(mockedRequest).toHaveBeenCalledWith("manager-home/nav-badges", expect.objectContaining({ signal: expect.any(AbortSignal) }));
  });

  it("restarts proximity motion on repeated pointer entry and after scrolling", async () => {
    vi.stubGlobal("matchMedia", vi.fn((query: string) => ({ matches: query.includes("hover: hover"), addEventListener: vi.fn(), removeEventListener: vi.fn() })));
    mount();
    await screen.findByText("0 pending approval requests");
    const nav = screen.getByRole("navigation");
    const face = screen.getByRole("link", { name: "Home" }).querySelector<HTMLElement>("[data-manager-dock-face]")!;
    const enter = () => fireEvent(nav, Object.assign(new MouseEvent("pointerover", { bubbles: true, clientX: 50, clientY: 65, relatedTarget: document.body }), { pointerType: "mouse" }));
    const leave = () => fireEvent(nav, Object.assign(new MouseEvent("pointerout", { bubbles: true, relatedTarget: document.body }), { pointerType: "mouse" }));
    for (let i = 0; i < 3; i++) {
      enter();
      await waitFor(() => expect(Number(gsap.getProperty(face, "scaleX"))).toBeGreaterThan(1.1));
      fireEvent.scroll(nav.querySelector('[class*="navScroll"]')!);
      await waitFor(() => expect(Number(gsap.getProperty(face, "scaleX"))).toBeGreaterThan(1.1));
      leave();
      expect(Number(gsap.getProperty(face, "scaleX"))).toBe(1);
    }
  });

  it("refreshes via the custom cache, toasts only successful increases, and retains stale counts on errors", async () => {
    mockedRequest.mockResolvedValue({ totalPending: 3, graphPending: 2, urlopPending: 1 });
    const { client } = mount();
    await screen.findByText("3 pending approval requests");
    expect(screen.queryByText(/New approval requests/)).toBeNull();
    mockedRequest.mockResolvedValue({ totalPending: 5, graphPending: 4, urlopPending: 1 });
    act(() => client.invalidateQueries({ queryKey: queryKeys.home.navBadges() }));
    const toast = await screen.findByText("New approval requests: 2");
    expect(screen.getAllByText(/New approval requests/)).toHaveLength(1);
    expect(toast).toHaveAttribute("data-anchor", "/container");
    mockedRequest.mockResolvedValue({ totalPending: 2, graphPending: 1, urlopPending: 1 });
    act(() => client.invalidateQueries({ queryKey: queryKeys.home.navBadges() }));
    await screen.findByText("2 pending approval requests");
    mockedRequest.mockRejectedValue(new Error("offline"));
    act(() => client.invalidateQueries({ queryKey: queryKeys.home.navBadges() }));
    await waitFor(() => expect(mockedRequest).toHaveBeenCalledTimes(4));
    expect(screen.getByRole("status")).toHaveTextContent("2 pending approval requests");
    expect(screen.queryByText("New approval requests: 3")).toBeNull();
    expect(mockedRequest.mock.calls.every(([path]) => path === "manager-home/nav-badges")).toBe(true);
  });

  it("replaces delta pills, dismisses them after 3500ms, and cleans up timers", async () => {
    const { client, unmount } = mount(); await screen.findByText("0 pending approval requests");
    vi.useFakeTimers();
    act(() => client.setQueryData(queryKeys.home.navBadges(), { totalPending: 1, graphPending: 0, urlopPending: 1 }));
    expect(screen.getByText("New approval requests: 1")).toHaveAttribute("data-anchor", "/urlop");
    act(() => client.setQueryData(queryKeys.home.navBadges(), { totalPending: 4, graphPending: 2, urlopPending: 2 }));
    expect(screen.getByText("New approval requests: 3")).toHaveAttribute("data-anchor", "/");
    act(() => vi.advanceTimersByTime(3500)); expect(screen.queryByText(/New approval requests/)).toBeNull();
    act(() => client.setQueryData(queryKeys.home.navBadges(), counts(6)));
    unmount(); act(() => vi.advanceTimersByTime(4000));
    expect(screen.queryByText(/New approval requests/)).toBeNull();
  });

  it("shows fixed sibling tooltips with real pending counts and supports keyboard navigation", async () => {
    mockedRequest.mockResolvedValue(counts(3)); const user = userEvent.setup(); mount();
    await screen.findByText("3 pending approval requests");
    const home = screen.getByRole("link", { name: "Home" });
    fireEvent.focus(home);
    const tooltip = screen.getByRole("tooltip");
    expect(tooltip).toHaveTextContent("Home3 pending");
    expect(tooltip.closest("nav")).toBeNull(); expect(tooltip.style.left).toBe("130px");
    fireEvent.blur(home); expect(screen.queryByRole("tooltip")).toBeNull();
    fireEvent.focus(screen.getByRole("link", { name: "Message" }));
    expect(within(screen.getByRole("tooltip")).queryByText(/pending/)).toBeNull();
    const container = screen.getByRole("link", { name: "Container" });
    container.focus(); await user.keyboard("{Enter}");
    expect(window.location.pathname).toBe("/container"); expect(screen.queryByRole("tooltip")).toBeNull();
    expect(container).toHaveAttribute("aria-current", "page");
  });

  it("remains navigable with reduced motion and missing server data", async () => {
    mockedRequest.mockRejectedValue(new Error("offline"));
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true, addEventListener: vi.fn(), removeEventListener: vi.fn() })));
    const { container } = mount();
    fireEvent.pointerMove(screen.getByRole("navigation"), { pointerType: "mouse", clientX: 50, clientY: 50 });
    expect(container.querySelector("[data-manager-dock-badge]")).toBeNull();
    await userEvent.click(screen.getByRole("link", { name: "Urlop" }));
    expect(window.location.pathname).toBe("/urlop");
  });

  it.each([["/", "/", true], ["/employee/2", "/", false], ["/container/2/graphs/3", "/container", true], ["/urlop/2", "/urlop", true], ["/urlop-other", "/urlop", false], ["/manager-profile", "/manager-profile", true], ["/manager-profile/edit", "/manager-profile", false]])("route %s for %s is active %s", (path, to, active) => {
    expect(isManagerDockRouteActive(String(path), String(to))).toBe(active);
  });
});
