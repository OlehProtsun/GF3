import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ManagerPhoneContainersPage } from "./ManagerPhoneContainersPage";
import { ManagerPhoneContainerDetailPage } from "./ManagerPhoneContainerDetailPage";
import { ManagerPhoneGraphPage } from "./ManagerPhoneGraphPage";
import { ManagerPhoneAvailabilityDetailPage } from "./ManagerPhoneAvailabilityDetailPage";
import { ManagerPhoneEmployeesPage } from "./ManagerPhoneEmployeesPage";
import { EmployeeProfileCard } from "@entities/employees/ui/EmployeeProfileCard";
import { ShopProfileCard } from "@entities/shops/ui/ShopProfileCard";
import { ContainerGraphProfileWorkspace } from "@entities/containers";
import { AvailabilityGroupProfileCard } from "@entities/availability-groups/ui";
import { ManagerPhonePage } from "./ManagerPhonePage";
import { ManagerPhoneEmployeeDetailPage } from "./ManagerPhoneEmployeeDetailPage";
import { ManagerPhoneShopDetailPage } from "./ManagerPhoneShopDetailPage";
import { parsePhoneId } from "./parsePhoneId";
const container = { id: 1, name: "All manager container", note: "Manager records" };
const employee = { id: 2, firstName: "Other", lastName: "Worker", hasLoginAccount: true, isOnline: true, username: "other", email: "other@example.com", phone: "123" };
const graph = { id: 3, containerId: 1, shopId: 4, name: "Private schedule", year: 2026, month: 4, publicationStatus: "private" as const, peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
const group = { id: 5, name: "Private dispo", year: 2026, month: 4, publicationStatus: "private" as const };
const shop = { id: 4, name: "Central", address: "Main Street" };
let mode: "data" | "empty" | "error" = "data";
let calls: Array<{ path: string; method: string }> = [];
beforeEach(() => {
 mode = "data"; calls = [];
 vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
  const path = new URL(String(input), "http://localhost").pathname; calls.push({ path, method: init?.method ?? "GET" });
  if (mode === "error") return Response.json({ detail: "offline" }, { status: 503 });
  if (mode === "empty") return Response.json([]);
  let data: unknown = [];
  if (path === "/api/containers") data = [container];
  else if (path === "/api/containers/1") data = container;
  else if (path === "/api/containers/1/graphs") data = [graph, { ...graph, id: 6, name: "Public schedule", publicationStatus: "public" }];
  else if (path === "/api/containers/1/graphs/3") data = graph;
  else if (path.endsWith("/employees") && path.includes("graphs")) data = [{ id: 7, scheduleId: 3, employeeId: 2, displayOrder: 1 }];
  else if (path.endsWith("/slots") && path.includes("graphs")) data = [{ id: 8, scheduleId: 3, employeeId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "16:00", status: "Working" }];
  else if (path === "/api/employees/2") data = employee;
  else if (path === "/api/shops/4") data = shop;
  else if (path === "/api/employees") data = [employee];
  else if (path === "/api/shops") data = [shop];
  else if (path === "/api/availability-groups") data = [group];
  else if (path === "/api/availability-groups/5") data = group;
  else if (path === "/api/availability-groups/5/items") data = [{ memberId: 7, employeeId: 2, displayOrder: 1, dayId: 8, dayOfMonth: 1, kind: "Available", intervalStr: null }];
  return Response.json(data);
 });
});
afterEach(() => { cleanup(); vi.restoreAllMocks(); });
function mount(element: React.ReactNode, path = "/") { window.history.replaceState({}, "", path); return render(<QueryClientProvider client={new QueryClient()}><BrowserRouter>{element}</BrowserRouter></QueryClientProvider>); }
test("container lists manager data, supports search and empty state without Add", async () => {
 mount(<ManagerPhoneContainersPage />); await screen.findByText(container.name); expect(screen.queryByRole("button", { name: "Add New" })).not.toBeInTheDocument();
 fireEvent.change(screen.getByRole("searchbox"), { target: { value: "missing" } }); await screen.findByText("Nothing found");
});
test("container detail includes private and public graphs and full manager worker counts", async () => {
 mount(<ManagerPhoneContainerDetailPage />, "/container/1"); await screen.findByText("Private schedule"); expect(screen.getByText("Public schedule")).toBeInTheDocument(); expect(await screen.findByText("Other Worker")).toBeInTheDocument();
 expect(calls.every(call => call.method === "GET")).toBe(true);
});
test("graph uses manager matrix and summaries with no write or export actions", async () => {
 mount(<ManagerPhoneGraphPage />, "/container/1/graphs/3"); await screen.findByText("Schedule Summary"); expect(screen.getAllByText("Other Worker").length).toBeGreaterThan(0);
 expect(screen.queryByRole("button", { name: /Edit|Delete|Publish|Export/ })).not.toBeInTheDocument(); expect(calls.every(call => call.method === "GET")).toBe(true);
});
test("availability preserves read-only day codes and manager columns", async () => {
 mount(<ManagerPhoneAvailabilityDetailPage />, "/availability/5"); await screen.findByText("Private dispo"); expect(screen.getAllByText("Other Worker").length).toBeGreaterThan(0);
 expect(screen.queryByRole("button", { name: /Edit|Delete|Publish/ })).not.toBeInTheDocument(); expect(calls.every(call => call.method === "GET")).toBe(true);
});
test("employee list shows other manager-visible workers without Add or Kick", async () => {
 mount(<ManagerPhoneEmployeesPage />, "/employee"); await screen.findByText("Other Worker"); expect(screen.queryByRole("button", { name: /Add|Kick/ })).not.toBeInTheDocument();
});
test("network failure exposes retry; empty lists have a non-mutating state", async () => {
 mode = "error"; mount(<ManagerPhoneContainersPage />); await screen.findByRole("button", { name: "Retry" }); mode = "empty"; fireEvent.click(screen.getByRole("button", { name: "Retry" })); await screen.findByText("No containers yet"); expect(screen.queryByRole("button", { name: "Add New" })).not.toBeInTheDocument();
});
test.each(["0", "-1", "1.5", "9007199254740992", "wrong"])("invalid ID %s sends no record request", async id => {
 mount(<ManagerPhoneContainerDetailPage />, `/container/${id}`); expect(screen.getByText("Invalid record ID.")).toBeInTheDocument(); await waitFor(() => expect(calls.length).toBeGreaterThan(0)); expect(calls.some(call => call.path.startsWith("/api/containers/"))).toBe(false);
});
test("safe integer ID parsing", () => { expect(parsePhoneId("1")).toBe(1); expect(parsePhoneId()).toBeNull(); });
test("default PC cards retain actions while phone hides all management controls", () => {
 const noop = () => {};
 const view = mount(<EmployeeProfileCard employee={employee} isLoading={false} hasLoadError={false} isDeleting={false} isKicking={false} onEditEmployee={noop} onDeleteEmployee={noop} onKickEmployee={noop} />);
 expect(screen.getByRole("button", { name: "Edit Employee" })).toBeInTheDocument(); expect(screen.getByRole("button", { name: "Kick Employee" })).toBeInTheDocument();
 view.unmount();
 mount(<><EmployeeProfileCard employee={employee} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} isKicking={false} onEditEmployee={noop} onDeleteEmployee={noop} onKickEmployee={noop} />
 <ShopProfileCard shop={shop} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEditShop={noop} onDeleteShop={noop} />
 <ContainerGraphProfileWorkspace graph={graph} graphEmployees={[]} slots={[]} cellStyles={[]} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEdit={noop} onDelete={noop} />
 <AvailabilityGroupProfileCard group={group} columns={[]} cellMap={{}} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEdit={noop} onDelete={noop} /></>);
 expect(screen.queryByRole("button", { name: /Edit|Delete|Kick|Publish/ })).not.toBeInTheDocument(); expect(screen.queryByText(/Open edit/)).not.toBeInTheDocument();
});

test("loading stays non-interactive and 404 is a not-found state", async () => {
 let finish!: (response: Response) => void;
 vi.mocked(globalThis.fetch).mockImplementation(() => new Promise(resolve => { finish = resolve; }));
 const view = mount(<ManagerPhoneContainersPage />); await screen.findByText("Loading..."); expect(screen.queryByRole("button", { name: "Add New" })).not.toBeInTheDocument();
 finish(Response.json([])); await screen.findByText("No containers yet"); view.unmount();
 vi.mocked(globalThis.fetch).mockImplementation(async input => String(input).includes("/containers/") ? Response.json({ detail: "Not found" }, { status: 404 }) : Response.json([]));
 mount(<ManagerPhoneContainerDetailPage />, "/container/999"); await screen.findByText("Record not found."); expect(screen.getByRole("link", { name: /Back/ })).toHaveAttribute("href", "/container");
});


test("root wrapper and manager lists omit Back by default", async () => {
 const view = mount(<ManagerPhonePage>Home</ManagerPhonePage>);
 expect(screen.queryByRole("link", { name: /Back/ })).not.toBeInTheDocument();
 view.unmount();
 mount(<ManagerPhoneContainersPage />, "/container"); await screen.findByText(container.name);
 expect(screen.queryByRole("link", { name: /Back/ })).not.toBeInTheDocument();
});

test.each([
 ["graph", <ManagerPhoneGraphPage />, "/container/1/graphs/3", "/container/1"],
 ["availability", <ManagerPhoneAvailabilityDetailPage />, "/availability/5", "/availability"],
 ["employee", <ManagerPhoneEmployeeDetailPage />, "/employee/2", "/employee"],
 ["shop", <ManagerPhoneShopDetailPage />, "/shop/4", "/shop"],
] as const)("%s detail preserves parent Back and GET-only access", async (_name, element, path, parent) => {
 mount(element, path);
 expect(screen.getByRole("link", { name: /Back/ })).toHaveAttribute("href", parent);
 await waitFor(() => expect(screen.queryByText("Loading...")).not.toBeInTheDocument());
 expect(calls.every(call => call.method === "GET")).toBe(true);
 expect(screen.queryByRole("button", { name: /Edit|Delete|Publish|Export|Kick/ })).not.toBeInTheDocument();
});
