import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { calendarMonths, urlopApi, type UrlopEmployeeCalendar, type UrlopRequest } from "@entities/urlop";
import { setLanguage } from "@shared/i18n";
import { ApiError } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { EmployeeUrlopPanel } from "./EmployeeUrlopPanel";

let calendar: UrlopEmployeeCalendar;
const request: UrlopRequest = { requestId: "r1", shopId: 1, employeeId: 7, employeeName: "Ada Worker", year: 2026, month: 11,
  days: [3], status: "pending", createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null };
beforeEach(() => {
  setLanguage("en"); vi.useFakeTimers({ toFake: ["Date"] }); vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
  calendar = { shops: [{ id: 1, name: "A" }, { id: 2, name: "B" }], months: [1, 2].flatMap(shopId => calendarMonths(2026, 11).map(m => ({ ...m, shopId, isOpen: shopId === 1, requests: [], approved: [] }))) };
  vi.spyOn(urlopApi, "employeeCalendar").mockImplementation(async () => structuredClone(calendar));
});
afterEach(() => { vi.restoreAllMocks(); vi.useRealTimers(); });
function mount() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  render(<QueryClientProvider client={client}><EmployeeUrlopPanel /></QueryClientProvider>); return client;
}
test("12 months, selection and Pending do not render UR until approval", async () => {
  const submit = vi.spyOn(urlopApi, "submit").mockImplementation(async payload => { calendar.months[0].requests = [{ ...request, days: payload.days }]; return request; });
  const client = mount();
  await screen.findByText("Open for requests");
  expect(within(screen.getByLabelText("Month")).getAllByRole("option")).toHaveLength(12);
  fireEvent.click(screen.getByRole("button", { name: "2026-11-03", exact: true }));
  expect(screen.getByRole("button", { name: "2026-11-03", exact: true })).toHaveAttribute("aria-pressed", "true");
  fireEvent.click(screen.getByRole("button", { name: "Clear selection" }));
  expect(screen.getByRole("button", { name: "Submit request" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "2026-11-03", exact: true }));
  fireEvent.click(screen.getByRole("button", { name: "Submit request" }));
  await waitFor(() => expect(submit).toHaveBeenCalledWith({ shopId: 1, year: 2026, month: 11, days: [3] }));
  await screen.findAllByText("Pending"); expect(screen.queryByText("UR")).toBeNull();
  calendar.months[0].requests[0].status = "approved";
  calendar.months[0].approved = [{ shopId: 1, employeeId: 7, employeeName: "Ada Worker", date: "2026-11-03" }];
  act(() => client.invalidateQueries({ queryKey: queryKeys.urlop.all }));
  await screen.findByRole("button", { name: "2026-11-03 UR: Ada Worker" });
  expect(screen.getAllByText("UR")).toHaveLength(2);
});
test("closed months remain visible and shop switching scopes submissions", async () => {
  mount(); await screen.findByText("Open for requests");
  fireEvent.change(screen.getByLabelText("Shop"), { target: { value: "2" } });
  expect(screen.getByText("Month closed by manager.")).toBeVisible();
  expect(screen.getByRole("button", { name: "2026-11-03", exact: true })).toBeDisabled();
  fireEvent.change(screen.getByLabelText("Month"), { target: { value: "11" } });
  expect(screen.getByRole("button", { name: "2027-10-03", exact: true })).toBeVisible();
  expect(screen.getByRole("button", { name: "Submit request" })).toBeDisabled();
});
test("409 refreshes and requires explicit retry", async () => {
  const submit = vi.spyOn(urlopApi, "submit").mockRejectedValue(new ApiError({ status: 409, message: "Month closed by manager." }));
  mount(); await screen.findByText("Open for requests");
  fireEvent.click(screen.getByRole("button", { name: "2026-11-03", exact: true }));
  calendar.months[0].isOpen = false;
  fireEvent.click(screen.getByRole("button", { name: "Submit request" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Submit request" })).toBeDisabled());
  expect(submit).toHaveBeenCalledTimes(1);
  await waitFor(() => expect(urlopApi.employeeCalendar).toHaveBeenCalledTimes(2));
});
test.each(["pl", "uk"] as const)("empty state is localized in %s", async language => {
  setLanguage(language); calendar = { shops: [], months: [] }; mount();
  expect(await screen.findByText(language === "pl" ? "Brak sklepów dostępnych do składania wniosków urlopowych." : "Немає доступних магазинів для подачі заявок на відпустку.")).toBeVisible();
  expect(within(screen.getByRole("combobox")).getAllByRole("option")).toHaveLength(12);
});
