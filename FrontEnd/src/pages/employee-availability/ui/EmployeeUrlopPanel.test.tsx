import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { calendarMonths, urlopApi, type UrlopEmployeeCalendar, type UrlopRequest } from "@entities/urlop";
import { setLanguage, t } from "@shared/i18n";
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
function choose(name: string, option: string) { fireEvent.click(screen.getByRole("button", { name, exact: true })); fireEvent.click(screen.getByRole("option", { name: option, exact: true })); }

test("12 months, selection and Pending do not render UR until approval", async () => {
  const submit = vi.spyOn(urlopApi, "submit").mockImplementation(async payload => { calendar.months[0].requests = [{ ...request, days: payload.days }]; return request; });
  const client = mount();
  await screen.findByText("Open for requests");
  fireEvent.click(screen.getByRole("button", { name: "Month", exact: true }));
  expect(screen.getAllByRole("option")).toHaveLength(12);
  fireEvent.keyDown(screen.getByRole("listbox"), { key: "Escape" });
  expect(document.querySelector("select")).toBeNull();
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
  choose("Shop", "B");
  expect(screen.getByText("Month closed by manager.")).toBeVisible();
  expect(screen.getByRole("button", { name: "2026-11-03", exact: true })).toBeDisabled();
  choose("Month", "October 2027");
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
  fireEvent.click(screen.getByRole("button", { name: t("Month"), exact: true }));
  expect(screen.getAllByRole("option")).toHaveLength(12);
});

test("loading and fetch failure keep their accessible feedback within the Urlop panel", async () => {
  let reject!: (error: Error) => void;
  vi.mocked(urlopApi.employeeCalendar).mockImplementation(() => new Promise((_, fail) => { reject = fail; }));
  mount();
  const panel = screen.getByRole("region", { name: "Urlop", exact: true });
  expect(panel).toHaveAttribute("aria-busy", "true");
  expect(within(panel).getByRole("status")).toHaveTextContent("Loading");
  await waitFor(() => expect(reject).toBeTypeOf("function"));
  reject(new ApiError({ status: 503, message: "Vacation service unavailable." }));
  expect(await within(panel).findByRole("alert")).toHaveTextContent("Vacation service unavailable.");
  expect(panel).toHaveAttribute("aria-busy", "false");
});


test("custom dropdown keyboard selection, search, Escape and outside dismissal preserve correct values", async () => {
  const user = userEvent.setup(); mount(); await screen.findByText("Open for requests");
  const month = screen.getByRole("button", { name: "Month", exact: true });
  await user.click(month); await user.keyboard("{ArrowUp}");
  expect(screen.getByRole("option", { name: "October 2027", exact: true })).toHaveFocus();
  await user.keyboard("{Enter}"); expect(month).toHaveFocus(); expect(month).toHaveTextContent("October 2027");
  expect(screen.getByRole("button", { name: "2027-10-03", exact: true })).toBeVisible();
  const shop = screen.getByRole("button", { name: "Shop", exact: true });
  await user.click(shop); await user.type(screen.getByRole("textbox", { name: "Search...", exact: true }), "B");
  expect(screen.getAllByRole("option")).toHaveLength(1);
  await user.keyboard("{ArrowDown}{Enter}");
  expect(shop).toHaveTextContent("B"); expect(shop).toHaveFocus();
  expect(screen.getByRole("button", { name: "2027-10-03", exact: true })).toBeDisabled();
  await user.click(month); await user.keyboard("{Escape}");
  expect(screen.queryByRole("listbox")).toBeNull(); expect(month).toHaveFocus();
  await user.click(month); await user.click(screen.getByRole("heading", { name: "Your requests" }));
  expect(screen.queryByRole("listbox")).toBeNull();
});

test("period/shop changes clear only selected days and keep submission payloads concrete", async () => {
  calendar.months.forEach(month => { month.isOpen = true; });
  let finish!: (value: UrlopRequest) => void;
  const submit = vi.spyOn(urlopApi, "submit").mockImplementation(() => new Promise(resolve => { finish = resolve; }));
  mount(); await screen.findByText("Open for requests");
  fireEvent.click(screen.getByRole("button", { name: "2026-11-03", exact: true }));
  choose("Month", "December 2026");
  expect(screen.getByRole("button", { name: "2026-12-03", exact: true })).toHaveAttribute("aria-pressed", "false");
  fireEvent.click(screen.getByRole("button", { name: "2026-12-04", exact: true }));
  choose("Shop", "B");
  const day = screen.getByRole("button", { name: "2026-12-04", exact: true });
  expect(day).toHaveAttribute("aria-pressed", "false"); fireEvent.click(day);
  fireEvent.click(screen.getByRole("button", { name: "Submit request" }));
  await waitFor(() => expect(submit).toHaveBeenCalledWith({ shopId: 2, year: 2026, month: 12, days: [4] }));
  expect(screen.getByRole("button", { name: "Shop", exact: true })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Month", exact: true })).toBeDisabled();
  finish({ ...request, shopId: 2, month: 12, days: [4] });
  await waitFor(() => expect(day).toHaveAttribute("aria-pressed", "false"));
});

test.each([['en', '1–3, 8, 12–13 November 2026'], ['pl', '1–3, 8, 12–13 listopada 2026'], ['uk', '1–3, 8, 12–13 листопада 2026']] as const)("request ranges use the shared %s formatter without mutating days", async (language, expected) => {
  setLanguage(language);
  const days = [13, 2, 1, 12, 3, 8, 2];
  calendar.months[0].requests = [{ ...request, days }, { ...request, requestId: "single", days: [15], status: "rejected" }];
  mount(); await waitFor(() => expect(screen.getAllByRole("article")).toHaveLength(2));
  const cards = screen.getAllByRole("article");
  expect(cards[0]).toHaveTextContent(expected);
  expect(cards[1]).toHaveTextContent(language === "en" ? "15 November 2026" : language === "pl" ? "15 listopada 2026" : "15 листопада 2026");
  expect(days).toEqual([13, 2, 1, 12, 3, 8, 2]);
  expect(cards[0].querySelector('[data-urlop-status="pending"]')).toBeInTheDocument();
  expect(cards[1].querySelector('[data-urlop-status="rejected"]')).toBeInTheDocument();
});
