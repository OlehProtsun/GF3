import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import { EmployeeWorkspaceLayout } from "./EmployeeWorkspaceLayout";

const state = vi.hoisted(() => ({
  empty: { data: [] }, ui: { data: { readNotificationIds: [] }, refetch: vi.fn() },
}));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => ({ session: { role: "employee", employeeId: 1, userName: "synthetic" } }) }));
vi.mock("@app/providers/PresenceProvider", () => ({ useRealtime: () => ({ notifications: [] }) }));
vi.mock("@entities/shift-swaps", () => ({ useEmployeeShiftSwapsQuery: () => state.empty }));
vi.mock("@entities/employee-schedule", () => ({ useEmployeeScheduleListQuery: () => state.empty }));
vi.mock("@entities/employee-availability", () => ({ useEmployeeAvailabilityListQuery: () => state.empty }));
vi.mock("@entities/system-news", () => ({ useSystemNewsQuery: () => state.empty }));
vi.mock("@entities/employee-ui-state", () => ({ useEmployeeUiStateQuery: () => state.ui, employeeUiStateApi: { markNotificationsRead: vi.fn() } }));
vi.mock("@entities/communications/ui", () => ({ EmployeeCommunicationDialog: () => null }));
vi.mock("./useEmployeeMotion", () => ({ useEmployeeMotion: () => {} }));

beforeEach(() => { localStorage.clear(); window.history.replaceState({}, "", "/schedule"); });
it("keeps legal links accessible when mobile navigation is collapsed", () => {
  window.history.replaceState({}, "", "/profile");
  render(<BrowserRouter><EmployeeWorkspaceLayout><div>Schedule</div></EmployeeWorkspaceLayout></BrowserRouter>);
  fireEvent.click(screen.getByRole("button", { name: "Collapse navigation" }));
  for (const [name, href] of [["Legal documents", "/legal/index.html"], ["Terms", "/legal/regulamin.html"], ["Privacy policy", "/legal/polityka-prywatnosci.html"]]) {
    const link = screen.getByRole("link", { name });
    expect(link).toHaveAttribute("href", href);
    expect(link.closest('[inert]')).toBeNull();
    expect(link).toBeVisible();
    link.focus();
    expect(link).toHaveFocus();
  }
});


it("Swap follows the visual viewport and releases its listeners on exit", () => {
  window.history.replaceState({}, "", "/swap");
  const viewport = new EventTarget(); Object.assign(viewport, { height: 640, offsetTop: 0 });
  const remove = vi.spyOn(viewport, "removeEventListener"); vi.stubGlobal("visualViewport", viewport);
  try {
    const page = render(<BrowserRouter><EmployeeWorkspaceLayout><div>Swap content</div></EmployeeWorkspaceLayout></BrowserRouter>);
    const layout = screen.getByText("Swap content").closest("main")!.parentElement!.parentElement!;
    expect(layout.style.getPropertyValue("--employee-visual-height")).toBe("640px");
    Object.assign(viewport, { height: 480, offsetTop: 25 }); viewport.dispatchEvent(new Event("resize"));
    expect(layout.style.getPropertyValue("--employee-visual-height")).toBe("480px");
    expect(layout.style.getPropertyValue("--employee-visual-top")).toBe("25px");
    page.unmount();
    expect(remove).toHaveBeenCalledWith("resize", expect.any(Function));
    expect(remove).toHaveBeenCalledWith("scroll", expect.any(Function));
    expect(layout.style.getPropertyValue("--employee-visual-height")).toBe("");
  } finally { vi.unstubAllGlobals(); }
});
