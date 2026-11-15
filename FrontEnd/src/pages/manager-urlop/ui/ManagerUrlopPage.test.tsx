import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { shopsApi } from "@entities/shops";
import { employeesApi } from "@entities/employees";
import { urlopApi, type UrlopManagerMonth, type UrlopRequest } from "@entities/urlop";
import { setLanguage } from "@shared/i18n";
import { ApiError } from "@shared/api/httpClient";
import { ManagerUrlopPage } from "./ManagerUrlopPage";

const auth = vi.hoisted(() => ({ mode: "pc" }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => ({ session: { role: "manager", workspaceMode: auth.mode } }) }));
let month: UrlopManagerMonth;
const pending: UrlopRequest = { requestId: "r1", employeeId: 7, employeeName: "Ada Worker", shopId: 1, year: 2026, month: 11, days: [3], status: "pending",
  createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null };
beforeEach(() => {
  setLanguage("en"); auth.mode = "pc";
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
  vi.useFakeTimers({ toFake: ["Date"] }); vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
  month = { shopId: 1, shopName: "A", year: 2026, month: 11, isOpen: false, requests: [structuredClone(pending)], approvedDays: [] };
  vi.spyOn(shopsApi, "byId").mockImplementation(async id => ({ id, name: "A", address: "Real shop address" }));
  vi.spyOn(employeesApi, "list").mockResolvedValue([{ id: 7, firstName: "Ada", lastName: "Worker", hasLoginAccount: false, isOnline: false }]);
  vi.spyOn(urlopApi, "managerMonth").mockImplementation(async (shopId, year, selectedMonth) => ({ ...structuredClone(month), shopId, year, month: selectedMonth }));
});
afterEach(() => { vi.restoreAllMocks(); vi.useRealTimers(); });
function mount(path = "/urlop/1") {
  window.history.replaceState({}, "", path);
  return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: 0 } } })}><BrowserRouter><ManagerUrlopPage /></BrowserRouter></QueryClientProvider>);
}
function choose(name: string, option: string) { fireEvent.click(screen.getByRole("button", { name, exact: true })); fireEvent.click(screen.getByRole("option", { name: option, exact: true })); }
function openAdd() { fireEvent.click(screen.getByRole("button", { name: "Add vacation", exact: true })); }

test("PC opens months and approves pending requests", async () => {
  const set = vi.spyOn(urlopApi, "setMonth").mockImplementation(async body => { month.isOpen = body.isOpen; return body; });
  const approve = vi.spyOn(urlopApi, "approve").mockImplementation(async () => { month.requests[0].status = "approved"; month.approvedDays = [{ shopId: 1, employeeId: 7, date: "2026-11-03", employeeName: "Ada Worker" }]; return month.requests[0]; });
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  fireEvent.click(screen.getByRole("button", { name: "Open requests" }));
  await screen.findByText("Open for requests"); expect(set).toHaveBeenCalledWith({ shopId: 1, year: 2026, month: 11, isOpen: true });
  fireEvent.click(screen.getByRole("button", { name: "Close requests" }));
  await screen.findByText("Month closed by manager."); expect(set).toHaveBeenLastCalledWith({ shopId: 1, year: 2026, month: 11, isOpen: false });
  fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  await screen.findByRole("button", { name: "2026-11-03 UR: Ada Worker" });
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  await screen.findByText("Approved"); expect(approve).toHaveBeenCalledWith("r1");
  expect(screen.getByRole("button", { name: "2026-11-03 UR: Ada Worker" })).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Reject", exact: true })).toBeNull();
});
test("manager creates, replaces all dates and confirms deletion in closed month", async () => {
  month.requests = [];
  const create = vi.spyOn(urlopApi, "create").mockImplementation(async body => { const value = { ...pending, ...body, status: "approved" as const, createdByManagerId: 1 }; month.requests = [value]; return value; });
  const replace = vi.spyOn(urlopApi, "replace").mockImplementation(async (_, days) => { month.requests[0].days = days; return month.requests[0]; });
  const remove = vi.spyOn(urlopApi, "remove").mockImplementation(async () => { month.requests = []; });
  mount(); await screen.findByText("No pending vacation requests.");
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  openAdd();
  fireEvent.click(within(screen.getByRole("dialog", { name: /Add vacation|Edit dates/ })).getByRole("button", { name: "2026-11-04", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Create approved vacation", exact: true }));
  await screen.findByText("Approved"); expect(create).toHaveBeenCalledWith({ shopId: 1, employeeId: 7, year: 2026, month: 11, days: [4] });
  fireEvent.click(screen.getByRole("button", { name: "Edit dates" }));
  fireEvent.click(within(screen.getByRole("dialog", { name: /Add vacation|Edit dates/ })).getByRole("button", { name: "2026-11-04", exact: true }));
  fireEvent.click(within(screen.getByRole("dialog", { name: /Add vacation|Edit dates/ })).getByRole("button", { name: "2026-11-05", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Save", exact: true }));
  await waitFor(() => expect(replace).toHaveBeenCalledWith("r1", [5]));
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Edit dates" })).toBeNull());
  fireEvent.click(screen.getByRole("button", { name: "Delete", exact: true }));
  expect(remove).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("dialog", { name: "Delete vacation request?" }).querySelector("button:last-child")!);
  await waitFor(() => expect(remove).toHaveBeenCalledWith("r1"));
  await screen.findByText("No vacation requests.");
});
test("409 shows conflicting dates, refreshes, and does not silently retry", async () => {
  const approve = vi.spyOn(urlopApi, "approve").mockRejectedValue(new ApiError({ status: 409, message: "Conflict with scheduled shift.", details: { code: "urlop_shift_conflict", conflictingDates: ["2026-11-03"], scheduleIds: [9] } }));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" }); fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  await screen.findByText("Vacation cannot be approved because the employee has a scheduled shift.");
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenCalledTimes(2));
  expect(approve).toHaveBeenCalledTimes(1); expect(screen.getByRole("alert")).toHaveTextContent("3 November 2026");
});
test("phone is read only even when page is mounted directly", async () => {
  auth.mode = "phone"; const approve = vi.spyOn(urlopApi, "approve");
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  for (const name of ["Approve", "Reject", "Delete", "Edit dates", "Open requests"]) expect(screen.getByRole("button", { name, exact: true })).toBeDisabled();
  expect(approve).not.toHaveBeenCalled();
});

test("route ID controls month and create payloads with no first-shop fallback, and Back opens list", async () => {
  month = { ...month, shopId: 2, shopName: "Second shop", requests: [] };
  const set = vi.spyOn(urlopApi, "setMonth").mockImplementation(async body => { month.isOpen = body.isOpen; return body; });
  const create = vi.spyOn(urlopApi, "create").mockImplementation(async body => ({ ...pending, ...body, status: "approved" }));
  mount("/urlop/2"); await screen.findByRole("heading", { name: "Second shop" });
  expect(urlopApi.managerMonth).toHaveBeenCalledWith(2, 2026, 11, expect.any(AbortSignal));
  expect(screen.queryByRole("combobox", { name: "Shop" })).toBeNull();
  fireEvent.click(screen.getByRole("button", { name: "Open requests" }));
  await waitFor(() => expect(set).toHaveBeenCalledWith({ shopId: 2, year: 2026, month: 11, isOpen: true }));
  openAdd();
  fireEvent.click(within(screen.getByRole("dialog", { name: /Add vacation|Edit dates/ })).getByRole("button", { name: "2026-11-04", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Create approved vacation", exact: true }));
  await waitFor(() => expect(create).toHaveBeenCalledWith({ shopId: 2, employeeId: 7, year: 2026, month: 11, days: [4] }));
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Add vacation" })).toBeNull());
  fireEvent.click(screen.getByRole("button", { name: "Back", exact: true }));
  expect(window.location.pathname).toBe("/urlop");
});

test.each(["-1", "0", "1.5", "9007199254740992", "invalid"])("invalid shop ID %s disables detail query and writes", async id => {
  const create = vi.spyOn(urlopApi, "create");
  mount(`/urlop/${id}`);
  expect(await screen.findByText("Shop not found.")).toBeInTheDocument();
  expect(urlopApi.managerMonth).not.toHaveBeenCalled();
  expect(create).not.toHaveBeenCalled();
  expect(screen.queryByLabelText("Month")).toBeNull();
  fireEvent.click(screen.getByRole("button", { name: "Back", exact: true }));
  expect(window.location.pathname).toBe("/urlop");
});

test("nonexistent shop shows the API error without redirecting or falling back", async () => {
  vi.mocked(urlopApi.managerMonth).mockRejectedValue(new ApiError({ status: 404, message: "Shop not found." }));
  mount("/urlop/999"); await screen.findByText("Shop not found.");
  expect(urlopApi.managerMonth).toHaveBeenCalledWith(999, 2026, 11, expect.any(AbortSignal));
  expect(window.location.pathname).toBe("/urlop/999");
  expect(screen.queryByRole("button", { name: "Create approved vacation", exact: true })).toBeNull();
});

test("dirty Back navigation asks for confirmation and cancellation retains selected dates", async () => {
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  openAdd();
  const day = within(screen.getByRole("dialog", { name: /Add vacation|Edit dates/ })).getByRole("button", { name: "2026-11-04", exact: true });
  fireEvent.click(day);
  fireEvent.click(screen.getByRole("button", { name: "Back", exact: true }));
  await screen.findByRole("dialog", { name: "Leave without saving?" });
  expect(window.location.pathname).toBe("/urlop/1");
  fireEvent.click(screen.getByRole("button", { name: "Stay here" }));
  expect(day).toHaveAttribute("aria-pressed", "true");
  expect(window.location.pathname).toBe("/urlop/1");
  fireEvent.click(screen.getByRole("button", { name: "Back", exact: true }));
  fireEvent.click(await screen.findByRole("button", { name: "Leave page" }));
  expect(window.location.pathname).toBe("/urlop");
});

test("manager can still reject a pending request", async () => {
  const reject = vi.spyOn(urlopApi, "reject").mockImplementation(async () => { month.requests[0].status = "rejected"; return month.requests[0]; });
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  fireEvent.click(screen.getByRole("button", { name: "Reject", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  await screen.findByText("Rejected");
  expect(reject).toHaveBeenCalledWith("r1");
});

test("overview has no employee selector; clean Cancel restores focus", async () => {
  mount(); await screen.findByRole("heading", { name: "Monthly vacation schedule" });
  expect(screen.queryByRole("combobox", { name: "Employee" })).toBeNull();
  expect(await screen.findByText("Real shop address")).toBeInTheDocument();
  const add = screen.getByRole("button", { name: "Add vacation" }); add.focus(); openAdd();
  expect(screen.getByRole("button", { name: "Employee", exact: true })).toHaveFocus();
  fireEvent.click(within(screen.getByRole("dialog", { name: "Add vacation" })).getByRole("button", { name: "Cancel" }));
  expect(screen.queryByRole("dialog")).toBeNull(); expect(add).toHaveFocus();
});
test("month/year controls and arrows query the chosen period across year boundaries", async () => {
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  choose("Month", "December");
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenLastCalledWith(1, 2026, 12, expect.any(AbortSignal)));
  fireEvent.click(screen.getByRole("button", { name: "Next month" }));
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenLastCalledWith(1, 2027, 1, expect.any(AbortSignal)));
  fireEvent.click(screen.getByRole("button", { name: "Previous month" }));
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenLastCalledWith(1, 2026, 12, expect.any(AbortSignal)));
  choose("Year", "2031");
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenLastCalledWith(1, 2031, 12, expect.any(AbortSignal)));
  fireEvent.click(screen.getByRole("button", { name: "Year", exact: true }));
  expect(screen.getAllByRole("option")).toHaveLength(11);
});
test("failed modal create preserves selection, exact conflicts, and does not retry", async () => {
  const create = vi.spyOn(urlopApi, "create").mockRejectedValue(new ApiError({ status: 409, message: "Conflict with scheduled shift.", details: { code: "urlop_shift_conflict", conflictingDates: ["2026-11-04"], scheduleIds: [9] } }));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" }); openAdd();
  const editor = screen.getByRole("dialog", { name: "Add vacation" });
  const day = within(editor).getByRole("button", { name: "2026-11-04" }); fireEvent.click(day);
  fireEvent.click(within(editor).getByRole("button", { name: "Create approved vacation" }));
  await within(editor).findByRole("alert");
  expect(within(editor).getByRole("alert")).toHaveTextContent("4 November 2026");
  expect(day).toHaveAttribute("aria-pressed", "true"); expect(create).toHaveBeenCalledTimes(1);
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenCalledTimes(2));
  fireEvent.keyDown(editor, { key: "Escape" });
  expect(screen.getByRole("dialog", { name: "Leave without saving?" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Stay here" })); expect(day).toHaveAttribute("aria-pressed", "true");
});
test("dirty month navigation prompts and only queries after confirmation", async () => {
  mount(); await screen.findByText("Ada Worker", { selector: "strong" }); openAdd();
  fireEvent.click(within(screen.getByRole("dialog", { name: "Add vacation" })).getByRole("button", { name: "2026-11-04" }));
  choose("Month", "December");
  await screen.findByRole("dialog", { name: "Leave without saving?" });
  expect(urlopApi.managerMonth).toHaveBeenCalledTimes(1);
  fireEvent.click(screen.getByRole("button", { name: "Leave page" }));
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenLastCalledWith(1, 2026, 12, expect.any(AbortSignal)));
  expect(screen.queryByRole("dialog", { name: "Add vacation" })).toBeNull();
});

test("persistent dashboard excludes stale data during rapid queries and keeps filters", async () => {
  let resolveDecember!: (value: UrlopManagerMonth) => void;
  let resolveJanuary!: (value: UrlopManagerMonth) => void;
  vi.mocked(urlopApi.managerMonth).mockImplementation(async (_, year, selectedMonth) => {
    if (selectedMonth === 12) return new Promise(resolve => { resolveDecember = resolve; });
    if (year === 2027) return new Promise(resolve => { resolveJanuary = resolve; });
    return structuredClone(month);
  });
  const { container } = mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  const calendar = screen.getByRole("region", { name: "Vacation calendar" });
  const panel = screen.getByRole("region", { name: "Vacation requests" });
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Next month" }));
  expect(screen.getByRole("region", { name: "Vacation calendar" })).toBe(calendar);
  expect(calendar).toHaveAttribute("aria-busy", "true");
  expect(screen.queryByText("Ada Worker", { selector: "strong" })).toBeNull();
  expect(screen.getByRole("button", { name: "Add vacation" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Next month" }));
  await waitFor(() => expect(resolveJanuary).toBeTypeOf("function"));
  resolveJanuary({ ...month, year: 2027, month: 1, requests: [{ ...pending, year: 2027, month: 1, employeeName: "January only" }] });
  await screen.findByText("January only", { selector: "strong" });
  resolveDecember({ ...month, month: 12, requests: [{ ...pending, month: 12, employeeName: "December stale" }] });
  await waitFor(() => expect(calendar).toHaveAttribute("aria-busy", "false"));
  expect(screen.queryByText("December stale")).toBeNull();
  expect(screen.getByRole("region", { name: "Vacation requests" })).toBe(panel);
  expect(screen.getByRole("button", { name: "All", exact: true })).toHaveAttribute("aria-pressed", "true");
  expect(container.querySelector("select")).toBeNull();
  expect(window.location.pathname).toBe("/urlop/1");
  expect(document.documentElement).toHaveClass("page-scrollbar-hidden");
});

test.each([null, {}, { conflictingDates: "bad" }, { conflictingDates: [null, 42, "bad", "2026-02-30"] }])("malformed shift details %j show safe advice", async payload => {
  vi.spyOn(urlopApi, "approve").mockRejectedValue(new ApiError({ status: 409, message: "Grafik: 42", details: { ...payload, code: "urlop_shift_conflict", scheduleIds: [42] } }));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  const alert = await screen.findByRole("alert");
  expect(alert).toHaveTextContent("Check the employee's work schedule and try again.");
  expect(alert).not.toHaveTextContent("42");
  expect(screen.getByRole("article")).toHaveTextContent("Pending");
});

test("non-shift failure retains its actual reason", async () => {
  vi.spyOn(urlopApi, "approve").mockRejectedValue(new ApiError({ status: 409, message: "Vacation already requested for these dates.", details: { code: "urlop_day_taken" } }));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Vacation already requested for these dates.");
});

test("employee popup Escape closes only popup; dirty employee change cancellation retains selection", async () => {
  vi.mocked(employeesApi.list).mockResolvedValue([
    { id: 7, firstName: "Ada", lastName: "Worker", hasLoginAccount: false, isOnline: false },
    { id: 8, firstName: "Other", lastName: "Worker", hasLoginAccount: false, isOnline: false },
  ]);
  mount(); await screen.findByText("Ada Worker", { selector: "strong" }); openAdd();
  const editor = screen.getByRole("dialog", { name: "Add vacation" });
  const employee = within(editor).getByRole("button", { name: "Employee", exact: true });
  fireEvent.click(employee);
  const search = screen.getByRole("textbox", { name: "Search..." });
  expect(search).toHaveFocus();
  fireEvent.keyDown(search, { key: "Escape" });
  expect(screen.queryByRole("listbox")).toBeNull(); expect(employee).toHaveFocus();
  expect(editor).toBeInTheDocument();
  const day = within(editor).getByRole("button", { name: "2026-11-04" }); fireEvent.click(day);
  choose("Employee", "Other Worker");
  fireEvent.click(screen.getByRole("button", { name: "Stay here" }));
  expect(employee).toHaveTextContent("Ada Worker"); expect(day).toHaveAttribute("aria-pressed", "true");
  choose("Employee", "Other Worker");
  fireEvent.click(screen.getByRole("button", { name: "Leave page" }));
  expect(employee).toHaveTextContent("Other Worker"); expect(day).toHaveAttribute("aria-pressed", "false");
});

test("approved replacement conflict retains original dates and editor draft", async () => {
  month.requests = [{ ...pending, status: "approved" }];
  const replace = vi.spyOn(urlopApi, "replace").mockRejectedValue(new ApiError({ status: 409, message: "Grafik: 42", details: { code: "urlop_shift_conflict", conflictingDates: ["2026-11-04", "2026-11-05"], scheduleIds: [42] } }));
  mount(); await screen.findByText("No pending vacation requests.");
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Edit dates" }));
  const editor = screen.getByRole("dialog", { name: "Edit dates" });
  const day = within(editor).getByRole("button", { name: "2026-11-04" }); fireEvent.click(day);
  fireEvent.click(within(editor).getByRole("button", { name: "Save", exact: true }));
  const alert = await within(editor).findByRole("alert");
  expect(alert).toHaveTextContent("4–5 November 2026"); expect(alert).not.toHaveTextContent("42");
  expect(day).toHaveAttribute("aria-pressed", "true"); expect(month.requests[0].days).toEqual([3]);
  expect(replace).toHaveBeenCalledTimes(1);
});

test("toolbar keyboard options, Escape and live language updates preserve the draft", async () => {
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  const month = screen.getByRole("button", { name: "Month", exact: true }); month.focus(); fireEvent.click(month);
  fireEvent.keyDown(month, { key: "ArrowDown" });
  expect(screen.getByRole("option", { name: "January", exact: true })).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: "Escape" });
  expect(screen.queryByRole("listbox")).toBeNull(); expect(month).toHaveFocus();
  openAdd();
  const day = within(screen.getByRole("dialog", { name: "Add vacation" })).getByRole("button", { name: "2026-11-04" }); fireEvent.click(day);
  act(() => setLanguage("pl"));
  expect(screen.getByRole("button", { name: "Miesiąc", exact: true })).toHaveTextContent("listopad");
  expect(day).toHaveAttribute("aria-pressed", "true");
  act(() => setLanguage("uk"));
  expect(screen.getByRole("button", { name: "Місяць", exact: true })).toHaveTextContent("листопад");
  expect(day).toHaveAttribute("aria-pressed", "true");
});

test("no employees disables creation; archived edited employee stays visible and locked", async () => {
  vi.mocked(employeesApi.list).mockResolvedValue([]);
  month.requests = [{ ...pending, status: "approved" }];
  mount(); await screen.findByText("No pending vacation requests.");
  expect(screen.getByRole("button", { name: "Add vacation" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Edit dates" }));
  const employee = screen.getByRole("button", { name: "Employee", exact: true });
  expect(employee).toBeDisabled(); expect(employee).toHaveTextContent("Ada Worker");
});

test.each([["0001-01-01T10:00:00Z", "Previous month", "1", "11"], ["9999-12-01T10:00:00Z", "Next month", "9989", "9999"]])("year boundary %s keeps arrows and eleven options bounded", async (date, disabledArrow, first, last) => {
  vi.setSystemTime(new Date(date));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  expect(screen.getByRole("button", { name: disabledArrow })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Year", exact: true }));
  const options = screen.getAllByRole("option");
  expect(options).toHaveLength(11); expect(options[0]).toHaveTextContent(first); expect(options[10]).toHaveTextContent(last);
});
