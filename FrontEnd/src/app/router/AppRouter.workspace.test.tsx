import { render, screen } from "@testing-library/react";
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
vi.mock("@entities/regulations/ui/RegulationAcceptanceGate", () => ({ RegulationAcceptanceGate: () => <div>LegalGate</div> }));
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

vi.mock("@app/layouts/manager-phone-layout", () => ({ ManagerPhoneLayout: ({ children }: PropsWithChildren) => <div data-testid="phone-shell">{children}</div> }));
vi.mock("@features/manager-workspace-mode/ui/ManagerWorkspaceModePicker", () => ({ ManagerWorkspaceModePicker: () => <div>ModePicker</div> }));
vi.mock("@pages/manager-phone", () => ({
  ManagerPhoneHomePage: () => <div>PhoneHome</div>,
  ManagerPhoneContainersPage: () => <div>PhoneContainers</div>,
  ManagerPhoneContainerDetailPage: () => <div>PhoneContainerDetail</div>,
  ManagerPhoneGraphPage: () => <div>PhoneGraph</div>,
  ManagerPhoneAvailabilityPage: () => <div>PhoneAvailability</div>,
  ManagerPhoneAvailabilityDetailPage: () => <div>PhoneAvailabilityDetail</div>,
  ManagerPhoneEmployeesPage: () => <div>PhoneEmployees</div>,
  ManagerPhoneEmployeeDetailPage: () => <div>PhoneEmployeeDetail</div>,
  ManagerPhoneShopsPage: () => <div>PhoneShops</div>,
  ManagerPhoneShopDetailPage: () => <div>PhoneShopDetail</div>,
  ManagerPhoneMorePage: () => <div>PhoneMore</div>,
}));
describe("workspace routing", () => {
  beforeEach(() => { auth.status = "authenticated"; auth.session = { ...manager(true), workspaceMode: "phone" }; auth.protectedMount.mockClear(); });
  it.each([["/", "Home"], ["/container/1", "ContainerDetail"], ["/container/1/graphs/2", "Graph"], ["/availability/1", "AvailabilityDetail"], ["/employee/1", "EmployeeDetail"], ["/shop/1", "ShopDetail"]])("phone direct route %s mounts only read-only page", async (path, page) => {
    mount(path); expect(await screen.findByText(`Phone${page}`)).toBeInTheDocument(); expect(screen.getByTestId("phone-shell")).toBeInTheDocument(); expect(screen.getByText("LegalGate")).toBeInTheDocument();
  });
  it.each(["/employee/new", "/container/1/graphs/2/edit", "/database", "/information", "/manager-profile"])('phone refuses desktop URL %s', async path => {
    mount(path); await screen.findByText("PhoneHome"); expect(window.location.pathname).toBe("/"); expect(auth.protectedMount).not.toHaveBeenCalled();
  });
  it("choose shows picker with legal gate irrespective of business URL", async () => {
    auth.session = { ...manager(true), workspaceMode: "choose" }; mount("/employee/1/edit"); await screen.findByText("ModePicker"); expect(screen.queryByTestId("phone-shell")).not.toBeInTheDocument(); expect(screen.getByText("LegalGate")).toBeInTheDocument();
  });
  it.each(["pc", undefined] as const)("PC and legacy keep original editor", async mode => {
    auth.session = { ...manager(true), workspaceMode: mode }; mount("/employee/1/edit"); await screen.findByText("EmployeeEditPage"); expect(screen.queryByTestId("phone-shell")).not.toBeInTheDocument();
  });
  it("employee routing remains unchanged", async () => { auth.session = employee; mount("/schedule"); await screen.findByText("EmployeeSchedulePage"); expect(screen.queryByText("ModePicker")).not.toBeInTheDocument(); });
});
