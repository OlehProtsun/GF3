import { within, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { HomePage } from "@pages/home";
import { ManagerPhoneHomePage } from "./ManagerPhoneHomePage";
import { ManagerPhoneAvailabilityPage } from "./ManagerPhoneAvailabilityPage";
import { ManagerPhoneMorePage } from "./ManagerPhoneMorePage";
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
const auth = vi.hoisted(() => ({ logout: vi.fn(), setManagerWorkspaceMode: vi.fn() }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => ({ ...auth, session: { displayName: "Real Manager", userName: "manager" } }) }));
let mode: "data" | "empty" | "error" = "data";
let calls: Array<{ path: string; method: string }> = [];
beforeEach(() => {
 mode = "data"; calls = []; localStorage.clear(); auth.logout.mockReset(); auth.setManagerWorkspaceMode.mockReset();
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
function mount(element: React.ReactNode, path = "/", client = new QueryClient()) { window.history.replaceState({}, "", path); return render(<QueryClientProvider client={client}><BrowserRouter>{element}</BrowserRouter></QueryClientProvider>); }
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


test("phone Home shows Coming Soon and sends no dashboard requests", () => {
 const view = mount(<ManagerPhoneHomePage />);
 expect(screen.getByRole("heading", { name: "Coming Soon" })).toBeInTheDocument(); expect(view.container.querySelector("a")).toBeNull(); expect(calls).toEqual([]);
 view.unmount(); mount(<HomePage />);
 expect(screen.getByRole("heading", { name: "Coming Soon" })).toBeInTheDocument(); expect(calls).toEqual([]);
});

test("Back and controlled search share one toolbar with exact destination and clear", () => {
 const change = vi.fn();
 const view = mount(<ManagerPhonePage backTo="/container/17" query="worker" onQueryChange={change} />);
 const back = screen.getByRole("link", { name: "Back" });
 expect(back).toHaveAttribute("href", "/container/17");
 expect(back.parentElement).toContainElement(screen.getByRole("searchbox", { name: "Search" }));
 fireEvent.change(screen.getByRole("searchbox"), { target: { value: "new" } }); expect(change).toHaveBeenCalledWith("new");
 fireEvent.click(screen.getByRole("button", { name: "Clear Search" })); expect(change).toHaveBeenCalledWith("");
 view.rerender(<QueryClientProvider client={new QueryClient()}><BrowserRouter><ManagerPhonePage query="" onQueryChange={change} /></BrowserRouter></QueryClientProvider>);
 expect(screen.queryByRole("button", { name: "Clear Search" })).not.toBeInTheDocument();
});

test("containers descend within pinned and unpinned groups without mutating cached data", async () => {
 const data = Object.freeze([container, { ...container, id: 9, name: "Container nine" }, { ...container, id: 4, name: "Container four" }, { ...container, id: 7, name: "Container seven" }]);
 localStorage.setItem("containers:list:pinned", JSON.stringify(["1", "4"]));
 const client = new QueryClient();
 const setData = client.setQueryData.bind(client);
 const cacheWrites = vi.spyOn(client, "setQueryData").mockImplementation((key, value) => {
  if (Array.isArray(value)) Object.freeze(value);
  setData(key, value);
 });
 const original = vi.mocked(fetch).getMockImplementation()!;
 vi.mocked(fetch).mockImplementation(async (input, init) => new URL(String(input), "http://localhost").pathname === "/api/containers" ? Response.json(data) : original(input, init));
 mount(<ManagerPhoneContainersPage />, "/container", client); await screen.findByText("Container nine");
 const names = () => screen.getAllByText(/^(All manager container|Container nine|Container four|Container seven)$/).map(node => node.textContent);
 expect(names()).toEqual(["Container four", "All manager container", "Container nine", "Container seven"]);
 fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Container" } }); expect(names()).toEqual(["Container four", "All manager container", "Container nine", "Container seven"]);
 expect(data.map(item => item.id)).toEqual([1, 9, 4, 7]);
 const key = cacheWrites.mock.calls.find(([key]) => key[0] === "containers" && key[1] === "list")![0];
 expect(client.getQueryState<typeof data>(key).data!.map(item => item.id)).toEqual([1, 9, 4, 7]);
});

test("statistics retain all employee, total and per-shop values in cards", async () => {
 const original = vi.mocked(fetch).getMockImplementation()!;
 vi.mocked(fetch).mockImplementation(async (input, init) => {
  const path = new URL(String(input), "http://localhost").pathname;
  if (path === "/api/shops") return Response.json([shop, { ...shop, id: 10, name: "Second shop" }]);
  if (path === "/api/containers/1/graphs") return Response.json([graph, { ...graph, id: 6, shopId: 10 }]);
  return original(input, init);
 });
 const view = mount(<ManagerPhoneContainerDetailPage />, "/container/1"); await screen.findByText("Other Worker");
 const heading = screen.getByRole("heading", { name: "Statistics" }); const stats = heading.closest("section")!;
 expect(within(stats).queryByRole("table")).not.toBeInTheDocument();
 const cards = within(stats).getAllByRole("article"); expect(cards).toHaveLength(2);
 expect(within(cards[0]).getByRole("heading")).toHaveTextContent("Other Worker"); expect(within(cards[1]).getByRole("heading")).toHaveTextContent("TOTAL");
 for (const card of cards) {
  expect(within(card).getByText("Work Days")).toBeInTheDocument(); expect(within(card).getByText("Free Days")).toBeInTheDocument(); expect(within(card).getByText("Hours")).toBeInTheDocument();
  expect(Array.from(card.querySelectorAll("dl:first-of-type dd")).slice(0, 3).map(node => node.textContent)).toEqual(["1", "29", "16"]);
  const details = card.querySelector("details")!; expect(details.open).toBe(false); await userEvent.click(details.querySelector("summary")!); expect(details.open).toBe(true);
  expect(within(details).getByText("Central")).toBeInTheDocument(); expect(within(details).getByText("Second shop")).toBeInTheDocument(); expect(within(details).getAllByText("8")).toHaveLength(2);
 }
 expect(view.container.querySelectorAll("article")).toHaveLength(2);
});

test.each(["items", "nested"])("availability uses %s base data despite optional hint failures", async source => {
 const original = vi.mocked(fetch).getMockImplementation()!;
 vi.mocked(fetch).mockImplementation(async (input, init) => {
  const path = new URL(String(input), "http://localhost").pathname;
  if (path.includes("transfer")) return Response.json({}, { status: 503 });
  if (source === "nested") {
   if (path.endsWith("/items")) return Response.json({}, { status: 503 });
   if (path.endsWith("/members")) return Response.json([{ id: 7, employeeId: 2, displayOrder: 1 }]);
   if (path.endsWith("/slots")) return Response.json([{ id: 8, availabilityGroupMemberId: 7, dayOfMonth: 1, kind: "Available", intervalStr: null }]);
  }
  return original(input, init);
 });
 const view = mount(<ManagerPhoneAvailabilityDetailPage />, "/availability/5");
 await screen.findByText("Availability Schedule"); expect(screen.getByText("Other Worker")).toBeInTheDocument();
 expect(screen.getByText("Availability Profile")).toBeInTheDocument();
 expect(view.container.querySelector("[data-phone-availability-information]")).toBeVisible();
 expect(view.container.querySelector("details")).toBeNull();
 expect(screen.queryByRole("button", { name: /Collapse Availability Information|Expand Availability Information/ })).not.toBeInTheDocument();
 expect(screen.getByText("Private dispo")).toBeInTheDocument(); expect(view.container.querySelector("[data-phone-matrix-scroll]")).toBeInTheDocument();
 expect(view.container.querySelector("tbody tr td:nth-child(2)")).toHaveTextContent("+");
 expect(screen.queryByRole("button", { name: /Edit|Delete/ })).not.toBeInTheDocument();
});

test("unresolved mandatory availability data shows retry instead of fabricated grid", async () => {
 const original = vi.mocked(fetch).getMockImplementation()!;
 vi.mocked(fetch).mockImplementation(async (input, init) => /\/(items|members|slots)$/.test(new URL(String(input), "http://localhost").pathname) ? Response.json(null) : original(input, init));
 const view = mount(<ManagerPhoneAvailabilityDetailPage />, "/availability/5"); await screen.findByRole("button", { name: "Retry" });
 expect(view.container.querySelector("[data-phone-matrix-scroll]")).not.toBeInTheDocument();
});

test("PC availability retains expanded desktop info and management controls", () => {
 const view = mount(<AvailabilityGroupProfileCard group={group} columns={[]} cellMap={{}} isLoading={false} hasLoadError={false} isDeleting={false} onEdit={() => {}} onDelete={() => {}} />);
 expect(view.container.querySelector("details")).not.toBeInTheDocument(); expect(screen.getByText("Private dispo")).toBeInTheDocument(); expect(screen.getByRole("button", { name: /Edit/ })).toBeInTheDocument();
});

test.each([ManagerPhoneAvailabilityPage, ManagerPhoneEmployeesPage])("lists retain search and clear", async Component => {
 mount(<Component />); await screen.findByText(Component === ManagerPhoneAvailabilityPage ? "Private dispo" : "Other Worker");
 const input = screen.getByRole("searchbox"); fireEvent.change(input, { target: { value: "missing" } });
 expect(input).toHaveValue("missing"); await screen.findByText("Nothing found"); fireEvent.click(screen.getAllByRole("button", { name: "Clear Search" })[0]); expect(input).toHaveValue("");
});

test("More shows account, PC switch and functional logout without Shops shortcut", () => {
 mount(<ManagerPhoneMorePage />, "/more"); expect(screen.getByText("Real Manager")).toBeInTheDocument();
 expect(screen.queryByRole("link", { name: /Shops/ })).not.toBeInTheDocument(); expect(screen.getByRole("button", { name: /Switch to Desktop/ })).toBeInTheDocument();
 fireEvent.click(screen.getByRole("button", { name: "Log out" })); expect(auth.logout).toHaveBeenCalledOnce();
});


test("phone schedule summary keeps metrics and every split shift in compact searchable cards", async () => {
 const props = { graph, graphEmployees: [{ id: 7, scheduleId: 3, employeeId: 2, displayOrder: 1 }],
  slots: [{ id: 8, scheduleId: 3, employeeId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:00", status: "Working" },
   { id: 9, scheduleId: 3, employeeId: 2, dayOfMonth: 1, slotNo: 2, fromTime: "13:00", toTime: "16:00", status: "Working" }],
  employeesById: new Map([[2, employee]]), cellStyles: [], isLoading: false, hasLoadError: false, isDeleting: false, onEdit: () => {}, onDelete: () => {} };
 const view = mount(<ContainerGraphProfileWorkspace {...props} compactSize showManagementActions={false} />);
 const summary = view.container.querySelector("[data-phone-schedule-summary]")!;
 expect(within(summary as HTMLElement).queryByRole("table")).not.toBeInTheDocument();
 expect(within(summary as HTMLElement).getByRole("heading", { name: "Other Worker" })).toBeInTheDocument();
 const metrics = summary.querySelector("dl")!; expect(Array.from(metrics.querySelectorAll("dd")).map(node => node.textContent)).toEqual(["1", "29", "7"]);
 await userEvent.click(summary.querySelector("summary")!);
 for (const value of ["08:00", "12:00", "13:00", "16:00", "4", "3"]) expect(within(summary as HTMLElement).getByText(value)).toBeInTheDocument();
 expect(summary.querySelectorAll("dl > div > dt")).toHaveLength(33);
 const search = screen.getByRole("searchbox", { name: "Search schedule summary by employee name or surname" });
 fireEvent.change(search, { target: { value: "missing" } }); expect(screen.getByRole("status")).toHaveTextContent('No employees found');
 fireEvent.change(search, { target: { value: "worker" } }); expect(view.container.querySelector("[data-phone-schedule-summary] > details")).toBeInTheDocument();
 view.unmount();
 const pc = mount(<ContainerGraphProfileWorkspace {...props} />);
 expect(pc.container.querySelector("[data-phone-schedule-summary]")).not.toBeInTheDocument(); expect(pc.container.querySelectorAll("table")[1].querySelectorAll("thead th").length).toBeGreaterThan(30);
});


test.each(["public", "private"] as const)("phone availability keeps %s status, visibility dates and every detail", status => {
 const datedGroup = { ...group, publicationStatus: status, visibleFromUtc: "2026-04-01T08:00:00Z", visibleToUtc: "2026-04-30T18:00:00Z" };
 const view = mount(<AvailabilityGroupProfileCard group={datedGroup} columns={[{ employeeId: 2, memberId: 7, label: "Other Worker" }]} cellMap={{ "2:1": "+" }} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEdit={() => {}} onDelete={() => {}} />);
 const information = view.container.querySelector<HTMLElement>("[data-phone-availability-information]")!;
 expect(information).toBeVisible();
 for (const label of ["Month", "Year", "Employees", "Status", "Visible", "From", "To", "ID 5", "2026"]) expect(within(information).getByText(label)).toBeInTheDocument();
 expect(within(information).getByText(status === "public" ? "Public" : "Private")).toBeInTheDocument();
 expect(within(information).getByText("1")).toBeInTheDocument();
 expect(within(information).getByText(/Apr 1/)).toBeInTheDocument();
 expect(within(information).getByText(/Apr 30/)).toBeInTheDocument();
 expect(view.container.querySelector("details")).toBeNull();
 expect(screen.queryByRole("button", { name: /Edit|Delete/ })).not.toBeInTheDocument();
});

test("phone availability retains missing visibility fallback", () => {
 const view = mount(<AvailabilityGroupProfileCard group={group} columns={[]} cellMap={{}} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEdit={() => {}} onDelete={() => {}} />);
 expect(screen.getByText("Not configured")).toBeInTheDocument();
 expect(view.container.querySelector("[data-phone-availability-information]")).toHaveTextContent("Employees0");
});
