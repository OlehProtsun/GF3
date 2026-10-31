import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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
  vi.useFakeTimers({ toFake: ["Date"] }); vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
  month = { shopId: 1, shopName: "A", year: 2026, month: 11, isOpen: false, requests: [structuredClone(pending)], approvedDays: [] };
  vi.spyOn(shopsApi, "list").mockResolvedValue([{ id: 1, name: "A", address: "Address" }]);
  vi.spyOn(employeesApi, "list").mockResolvedValue([{ id: 7, firstName: "Ada", lastName: "Worker", hasLoginAccount: false, isOnline: false }]);
  vi.spyOn(urlopApi, "managerMonth").mockImplementation(async () => structuredClone(month));
});
afterEach(() => { vi.restoreAllMocks(); vi.useRealTimers(); });
function mount() {
  window.history.replaceState({}, "", "/urlop");
  return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: 0 } } })}><BrowserRouter><ManagerUrlopPage /></BrowserRouter></QueryClientProvider>);
}
test("PC opens months and approves pending requests", async () => {
  const set = vi.spyOn(urlopApi, "setMonth").mockImplementation(async body => { month.isOpen = body.isOpen; return body; });
  const approve = vi.spyOn(urlopApi, "approve").mockImplementation(async () => { month.requests[0].status = "approved"; month.approvedDays = [{ shopId: 1, employeeId: 7, date: "2026-11-03", employeeName: "Ada Worker" }]; return month.requests[0]; });
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  fireEvent.click(screen.getByRole("button", { name: "Open requests" }));
  await screen.findByText("Open for requests"); expect(set).toHaveBeenCalledWith({ shopId: 1, year: 2026, month: 11, isOpen: true });
  fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  await screen.findByText("Approved"); expect(approve).toHaveBeenCalledWith("r1");
  expect(screen.getByRole("button", { name: "2026-11-03 UR: Ada Worker" })).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Reject", exact: true })).toBeNull();
});
test("manager creates, replaces all dates and confirms deletion in closed month", async () => {
  month.requests = [];
  const create = vi.spyOn(urlopApi, "create").mockImplementation(async body => { const value = { ...pending, ...body, status: "approved" as const, createdByManagerId: 1 }; month.requests = [value]; return value; });
  const replace = vi.spyOn(urlopApi, "replace").mockImplementation(async (_, days) => { month.requests[0].days = days; return month.requests[0]; });
  const remove = vi.spyOn(urlopApi, "remove").mockImplementation(async () => { month.requests = []; });
  mount(); await screen.findByText("No vacation requests.");
  fireEvent.click(screen.getByRole("button", { name: "2026-11-04", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Create approved vacation", exact: true }));
  await screen.findByText("Approved"); expect(create).toHaveBeenCalledWith({ shopId: 1, employeeId: 7, year: 2026, month: 11, days: [4] });
  fireEvent.click(screen.getByRole("button", { name: "Edit dates" }));
  fireEvent.click(screen.getByRole("button", { name: "2026-11-04", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "2026-11-05", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Save", exact: true }));
  await waitFor(() => expect(replace).toHaveBeenCalledWith("r1", [5]));
  await screen.findByRole("button", { name: "Create approved vacation", exact: true });
  fireEvent.click(screen.getByRole("button", { name: "Delete", exact: true }));
  expect(remove).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("dialog", { name: "Delete vacation request?" }).querySelector("button:last-child")!);
  await waitFor(() => expect(remove).toHaveBeenCalledWith("r1"));
  await screen.findByText("No vacation requests.");
});
test("409 shows conflicting dates, refreshes, and does not silently retry", async () => {
  const approve = vi.spyOn(urlopApi, "approve").mockRejectedValue(new ApiError({ status: 409, message: "Conflict with scheduled shift.", details: { conflictingDates: ["2026-11-03"], scheduleIds: [9] } }));
  mount(); await screen.findByText("Ada Worker", { selector: "strong" }); fireEvent.click(screen.getByRole("button", { name: "Approve", exact: true }));
  await screen.findByText(/Conflict with scheduled shift/);
  await waitFor(() => expect(urlopApi.managerMonth).toHaveBeenCalledTimes(2));
  expect(approve).toHaveBeenCalledTimes(1); expect(screen.getByRole("alert")).toHaveTextContent("2026-11-03");
});
test("phone is read only even when page is mounted directly", async () => {
  auth.mode = "phone"; const approve = vi.spyOn(urlopApi, "approve");
  mount(); await screen.findByText("Ada Worker", { selector: "strong" });
  for (const name of ["Approve", "Reject", "Delete", "Edit dates", "Open requests"]) expect(screen.getByRole("button", { name, exact: true })).toBeDisabled();
  expect(approve).not.toHaveBeenCalled();
});
