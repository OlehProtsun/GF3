import { render, screen, waitFor } from "@testing-library/react";
import type { PropsWithChildren } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthSession } from "@entities/auth/model/types";
import { AppRouter } from "./AppRouter";

const auth = vi.hoisted(() => ({
  status: "authenticated" as "authenticated" | "unauthenticated" | "loading",
  session: null as AuthSession | null,
  protectedMount: vi.fn(),
}));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => auth }));
vi.mock("@app/layouts/overlay-sidebar-layout", () => ({ OverlaySidebarLayout: ({ children }: PropsWithChildren) => <>{children}</> }));
vi.mock("@app/layouts/employee-workspace-layout", () => ({ EmployeeWorkspaceLayout: ({ children }: PropsWithChildren) => <>{children}</> }));
vi.mock("./PageTransition", () => ({ PageTransition: ({ children }: PropsWithChildren) => <>{children}</> }));
vi.mock("@entities/regulations/ui/RegulationAcceptanceGate", () => ({ RegulationAcceptanceGate: () => null }));
vi.mock("@pages/home", () => ({ HomePage: () => { return <div>HomePage</div>; } }));
vi.mock("@pages/employee-notifications", () => ({ EmployeeNotificationsPage: () => { return <div>EmployeeNotificationsPage</div>; } }));
vi.mock("@pages/employee-availability", () => ({ EmployeeAvailabilityPage: () => { return <div>EmployeeAvailabilityPage</div>; } }));
vi.mock("@pages/employee-schedule", () => ({ EmployeeSchedulePage: () => { return <div>EmployeeSchedulePage</div>; } }));
vi.mock("@pages/employee-swap", () => ({ EmployeeSwapPage: () => { return <div>EmployeeSwapPage</div>; } }));
vi.mock("@pages/employee-account", () => ({ EmployeeAccountPage: () => { return <div>EmployeeAccountPage</div>; } }));
vi.mock("@pages/login", () => ({ LoginPage: () => { return <div>LoginPage</div>; } }));
vi.mock("@pages/password-recovery", () => ({ PasswordRecoveryPage: () => { return <div>PasswordRecoveryPage</div>; } }));
vi.mock("@pages/shop-list", () => ({ ShopListPage: () => { return <div>ShopListPage</div>; } }));
vi.mock("@pages/shop-profile", () => ({ ShopProfilePage: () => { return <div>ShopProfilePage</div>; } }));
vi.mock("@pages/shop-edit", () => ({ ShopEditPage: () => { return <div>ShopEditPage</div>; } }));
vi.mock("@pages/availability", () => ({ AvailabilityPage: () => { return <div>AvailabilityPage</div>; } }));
vi.mock("@pages/availability-edit", () => ({ AvailabilityEditPage: () => { return <div>AvailabilityEditPage</div>; } }));
vi.mock("@pages/availability-profile", () => ({ AvailabilityProfilePage: () => { return <div>AvailabilityProfilePage</div>; } }));
vi.mock("@pages/container", () => ({ ContainerPage: () => { return <div>ContainerPage</div>; } }));
vi.mock("@pages/container-graph-profile", () => ({ ContainerGraphProfilePage: () => { return <div>ContainerGraphProfilePage</div>; } }));
vi.mock("@pages/container-graph-edit", () => ({ ContainerGraphEditPage: () => { return <div>ContainerGraphEditPage</div>; } }));
vi.mock("@pages/information", () => ({ InformationPage: () => { auth.protectedMount(); return <div>InformationPage</div>; } }));
vi.mock("@pages/communications", () => ({ CommunicationsPage: () => { return <div>CommunicationsPage</div>; } }));
vi.mock("@pages/database", () => ({ DataBasePage: () => { auth.protectedMount(); return <div>DataBasePage</div>; } }));
vi.mock("@pages/manager-account", () => ({ ManagerAccountPage: () => { return <div>ManagerAccountPage</div>; } }));
vi.mock("@pages/employee-list", () => ({ EmployeeListPage: () => { return <div>EmployeeListPage</div>; } }));
vi.mock("@pages/employee-profile", () => ({ EmployeeProfilePage: () => { return <div>EmployeeProfilePage</div>; } }));
vi.mock("@pages/employee-edit", () => ({ EmployeeEditPage: () => { return <div>EmployeeEditPage</div>; } }));

function manager(privilege?: boolean): AuthSession {
  return { role: "manager", userName: "manager", displayName: "Synthetic manager", isSystemManager: privilege };
}
const employee: AuthSession = { role: "employee", userName: "manager", displayName: "Synthetic employee", isSystemManager: true };
function mount(path: string) {
  window.history.replaceState({}, "", path);
  return render(<AppRouter />);
}

describe("AppRouter system access", () => {
  beforeEach(() => {
    auth.status = "authenticated";
    auth.session = manager(false);
    auth.protectedMount.mockClear();
    window.history.replaceState({}, "", "/");
  });

  for (const [path, page] of [["/information", "InformationPage"], ["/database", "DataBasePage"]]) {
    it(`allows a system manager at ${path}`, async () => {
      auth.session = manager(true);
      mount(path);
      expect(await screen.findByText(page)).toBeInTheDocument();
      expect(window.location.pathname).toBe(path);
    });
    it.each([false, undefined])(`redirects a non-system manager at ${path} with flag %s`, async privilege => {
      auth.session = manager(privilege);
      const historyLength = window.history.length;
      mount(path);
      expect(await screen.findByText("HomePage")).toBeInTheDocument();
      expect(window.location.pathname).toBe("/");
      expect(window.history.length).toBe(historyLength);
      expect(auth.protectedMount).not.toHaveBeenCalled();
    });
    it(`redirects employees at ${path}`, async () => {
      auth.session = employee;
      mount(path);
      expect(await screen.findByText("EmployeeNotificationsPage")).toBeInTheDocument();
      expect(window.location.pathname).toBe("/");
      expect(auth.protectedMount).not.toHaveBeenCalled();
    });
    it(`redirects anonymous users at ${path}`, async () => {
      auth.session = null;
      auth.status = "unauthenticated";
      mount(path);
      expect(await screen.findByText("LoginPage")).toBeInTheDocument();
      expect(window.location.pathname).toBe("/login");
      expect(auth.protectedMount).not.toHaveBeenCalled();
    });
    it(`waits for restored auth then denies ${path}`, async () => {
      auth.session = null;
      auth.status = "loading";
      const view = mount(path);
      expect(screen.getByRole("status", { name: "Loading page" })).toBeInTheDocument();
      expect(window.location.pathname).toBe(path);
      expect(auth.protectedMount).not.toHaveBeenCalled();
      auth.session = manager(false);
      auth.status = "authenticated";
      view.rerender(<AppRouter />);
      expect(await screen.findByText("HomePage")).toBeInTheDocument();
      expect(auth.protectedMount).not.toHaveBeenCalled();
    });
    it(`clears previous account access at ${path} after logout and restoration`, async () => {
      auth.session = manager(true);
      const view = mount(path);
      await screen.findByText(page);
      auth.session = null;
      auth.status = "unauthenticated";
      view.rerender(<AppRouter />);
      await screen.findByText("LoginPage");
      auth.session = manager(false);
      auth.status = "authenticated";
      view.rerender(<AppRouter />);
      await screen.findByText("HomePage");
      auth.protectedMount.mockClear();
      window.history.pushState({}, "", path);
      window.dispatchEvent(new PopStateEvent("popstate"));
      await waitFor(() => expect(window.location.pathname).toBe("/"));
      expect(auth.protectedMount).not.toHaveBeenCalled();
    });
  }

  it.each([["/communications", "CommunicationsPage"], ["/shop", "ShopListPage"], ["/manager-profile", "ManagerAccountPage"]])("keeps regular manager route %s", async (path, page) => {
    mount(path);
    expect(await screen.findByText(page)).toBeInTheDocument();
    expect(window.location.pathname).toBe(path);
  });
  it.each([["/schedule", "EmployeeSchedulePage"], ["/availability", "EmployeeAvailabilityPage"], ["/profile", "EmployeeAccountPage"]])("keeps employee route %s", async (path, page) => {
    auth.session = employee;
    mount(path);
    expect(await screen.findByText(page)).toBeInTheDocument();
    expect(window.location.pathname).toBe(path);
  });
});
