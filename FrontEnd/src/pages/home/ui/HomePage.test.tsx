import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { loadHomeDashboard, type HomeDashboardData } from "./homeDashboard";
import { managerHomeActivityApi, type ManagerHomeActivityItem, type ManagerHomeActivityPage } from "../api/managerHomeActivityApi";
import { HomePage } from "./HomePage";
vi.mock("./homeDashboard", () => ({ loadHomeDashboard: vi.fn() }));
vi.mock("../api/managerHomeActivityApi", () => ({ managerHomeActivityApi: { list: vi.fn() } }));
const event: ManagerHomeActivityItem = { id: "swap:1", kind: "swap_created", occurredAtUtc: "2026-11-04T10:00:00Z", employeeName: "Personal name", status: "open", requiresApproval: false, targetType: "graph", containerId: 8, graphId: 20 };
const row = { id: "r1", employeeId: 99, containerId: 8, graphId: 20, employee: "Personal name", shopId: 1, shop: "Central", dateLabel: "4 November 2026", fromTime: "08:00", toTime: "16:00", shift: "08:00 – 16:00", hoursText: "8h 0m" };
const dashboard: HomeDashboardData = { todayRows: [row], todayActiveEmployeesCount: 1, hasPartialData: false,
 shops: [{ id: 1, name: "Central" }, { id: 2, name: "North" }], employees: new Map([[99, "Personal name"]]) };
beforeEach(() => {
 vi.resetAllMocks(); setLanguage("en"); window.history.replaceState({}, "", "/");
 vi.mocked(loadHomeDashboard).mockResolvedValue(dashboard);
 vi.mocked(managerHomeActivityApi.list).mockResolvedValue({ items: [event], allActivityCount: 1, requiresApprovalCount: 0, hasMore: false, nextOffset: 1 });
});
afterEach(() => {
 vi.useRealTimers();
 vi.unstubAllGlobals();
});
function mount(phoneMode = false) {
 return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: 0 } } })}><BrowserRouter><HomePage phoneMode={phoneMode} /></BrowserRouter></QueryClientProvider>);
}
test("uses the shared Home header and renders only the two requested cards", async () => {
 const { container } = mount(); await screen.findByText("Personal name", { selector: "strong" });
 expect(screen.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 expect(screen.getByText("Overview of today's schedules and recent application activity.")).toBeVisible();
 expect(screen.getByRole("button", { name: "Collapse header" })).toBeVisible();
 expect(container.querySelectorAll("section")).toHaveLength(2);
 expect(screen.getAllByRole("tab")).toHaveLength(5);
 expect(screen.queryByRole("tab", { name: /Availability/ })).toBeNull();
 expect(screen.getAllByRole("columnheader").map(cell => cell.textContent)).toEqual(["Employee", "Shift", "Shop", "Hours", "Status"]);
 const shopIcon = screen.getByRole("table").querySelector("tbody td:nth-child(3) svg"); expect(shopIcon?.getAttribute("class")).toContain("shopIcon");
 expect(container.querySelector("select")).toBeNull();
});

test("shop filters count distinct employees and remain selected across date navigation", async () => {
 vi.setSystemTime(new Date("2026-11-04T10:00:00Z"));
 vi.mocked(loadHomeDashboard).mockResolvedValue({ ...dashboard, todayActiveEmployeesCount: 2,
  todayRows: [row, { ...row, id: "r2" }, { ...row, id: "r3", employee: "North Worker", employeeId: 2, shopId: 2, shop: "North" }] });
 const user = userEvent.setup();
 mount();
 const shops = within(await screen.findByRole("group", { name: "Filter by shop" }));
 expect(shops.getByRole("button", { name: "All" })).toHaveAttribute("aria-pressed", "true");
 await user.click(await shops.findByRole("button", { name: "Central" }));
 expect(screen.getByText("1 assigned employees")).toBeVisible();
 expect(screen.getByRole("table").querySelectorAll("tbody tr")).toHaveLength(2);
 await user.click(screen.getByRole("button", { name: "Next day" }));
 await waitFor(() => expect(loadHomeDashboard).toHaveBeenLastCalledWith("2026-11-05", expect.anything()));
 expect(await shops.findByRole("button", { name: "Central" })).toHaveAttribute("aria-pressed", "true");
});

test("activity category counts survive filtering and missing names use the employee lookup", async () => {
 vi.mocked(managerHomeActivityApi.list).mockImplementation(async ({ tab }) => ({
  items: tab === "swaps" || tab === "all" ? [
   { ...event, employeeName: "—", employeeId: 99, shopName: "F27", scheduleName: "F27" },
   { ...event, id: "manual:1", employeeName: "—", isManagerCreatedOffer: true },
  ] : [],
  allActivityCount: 15, requiresApprovalCount: 2, categoryCounts: { swaps: 11, vacations: 1, corrections: 1, availability: 2 },
  hasMore: false, nextOffset: 1,
 }));
 mount();
 const feed = within(screen.getByRole("tabpanel"));
 expect(await feed.findByText("Personal name gave up a shift")).toBeVisible();
 expect(feed.getByText("Manager published an open shift")).toBeVisible();
 expect(feed.queryByText("F27 · F27")).toBeNull();
 const swaps = screen.getByRole("tab", { name: /Swaps/ });
 expect(swaps).toHaveTextContent("11");
 await userEvent.click(swaps);
 await waitFor(() => expect(managerHomeActivityApi.list).toHaveBeenLastCalledWith(expect.objectContaining({ tab: "swaps", offset: 0 }), expect.anything()));
 await userEvent.keyboard("{ArrowRight}");
 expect(screen.getByRole("tab", { name: /Vacations/ })).toHaveAttribute("aria-selected", "true");
 expect(await feed.findByText("No activity recorded in this month.")).toBeVisible();
});
test.each(["pl", "uk"] as const)("localizes the restored header in %s", async language => {
 setLanguage(language); mount();
 expect(screen.queryByText("Overview of today's schedules and recent application activity.")).toBeNull();
 expect(screen.getByRole("button", { name: language === "pl" ? "Zwiń nagłówek" : "Згорнути заголовок" })).toBeVisible();
});
test("the entire event links by structured owning IDs regardless of labels", async () => {
 mount(); const panel = screen.getByRole("tabpanel");
 const link = await within(panel).findByRole("link"); expect(link).toHaveAttribute("href", "/container/8/graphs/20");
 fireEvent.click(within(link).getByText("Personal name gave up a shift"));
 await waitFor(() => expect(window.location.pathname).toBe("/container/8/graphs/20"));
});
test("phone Home preserves its placeholder and starts no data requests", () => {
 mount(true); expect(screen.getByRole("heading", { name: "Coming Soon" })).toBeVisible();
 expect(loadHomeDashboard).not.toHaveBeenCalled(); expect(managerHomeActivityApi.list).not.toHaveBeenCalled();
 expect(screen.queryByRole("heading", { name: "Home", exact: true })).toBeNull();
});

test("missing or inconsistent destination metadata remains visible without an incorrect link", async () => {
 vi.mocked(managerHomeActivityApi.list).mockResolvedValue({ items: [{ ...event, graphId: null }, { ...event, id: "bad:2", targetType: "urlop", shopId: 2, year: 2026, month: 11 }], allActivityCount: 2, requiresApprovalCount: 0, hasMore: false, nextOffset: 2 });
 mount(); const panel = screen.getByRole("tabpanel");
 expect(await within(panel).findAllByText("This activity's resource is no longer available.")).toHaveLength(2);
 expect(within(panel).queryByRole("link")).toBeNull();
 expect(panel.querySelectorAll('[aria-disabled="true"]')).toHaveLength(2);
 expect(window.location.pathname).toBe("/");
});

function activityPage(offset: number, count: number, prefix = "Worker"): ManagerHomeActivityPage {
 const nextOffset = Math.min(count, offset + 7);
 return {
  items: Array.from({ length: nextOffset - offset }, (_, index) => ({
   ...event, id: `${prefix}:${offset + index}`, employeeName: `${prefix} ${offset + index + 1}`,
  })),
  allActivityCount: count, requiresApprovalCount: count, hasMore: nextOffset < count, nextOffset,
 };
}

function observeFeed() {
 const observers: Array<{ intersect: () => void; root?: Element | Document | null; disconnect: ReturnType<typeof vi.fn> }> = [];
 vi.stubGlobal("IntersectionObserver", class {
  disconnect = vi.fn();
  constructor(callback: IntersectionObserverCallback, options?: IntersectionObserverInit) {
   observers.push({
    root: options?.root, disconnect: this.disconnect,
    intersect: () => callback([{ isIntersecting: true } as IntersectionObserverEntry], this as unknown as IntersectionObserver),
   });
  }
  observe() {}
 });
 return observers;
}

test("the work date and activity period are independent across month boundaries", async () => {
 vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
 const user = userEvent.setup();
 mount();
 const works = screen.getByRole("heading", { name: "Who Works Today?" }).closest("section")!;
 const activity = screen.getByRole("heading", { name: "Application Activity" }).closest("section")!;
 for (const name of ["Select date", "Previous day", "Today", "Next day"]) {
  expect(within(works).getByRole("button", { name, exact: true })).toBeVisible();
  expect(within(activity).queryByRole("button", { name, exact: true })).toBeNull();
 }
 await user.click(within(works).getByRole("button", { name: "Previous day" }));
 await waitFor(() => expect(loadHomeDashboard).toHaveBeenLastCalledWith("2026-10-31", expect.anything()));
 await waitFor(() => expect(managerHomeActivityApi.list).toHaveBeenLastCalledWith(
  expect.objectContaining({ year: 2026, month: 11, tab: "all", offset: 0 }), expect.anything()));
 await user.click(within(activity).getByRole("button", { name: "Period", exact: true }));
 await user.click(screen.getByRole("option", { name: "February 2027", exact: true }));
 await waitFor(() => expect(managerHomeActivityApi.list).toHaveBeenLastCalledWith(
  expect.objectContaining({ year: 2027, month: 2, tab: "all", offset: 0 }), expect.anything()));
 expect(loadHomeDashboard).toHaveBeenLastCalledWith("2026-10-31", expect.anything());
 await user.click(within(works).getByRole("button", { name: "Next day" }));
 expect(within(works).getByText("Employees scheduled for 1 November 2026")).toBeVisible();
 expect(loadHomeDashboard).toHaveBeenCalledWith("2026-11-01", expect.anything());
 expect(managerHomeActivityApi.list).toHaveBeenLastCalledWith(
  expect.objectContaining({ year: 2027, month: 2, tab: "all", offset: 0 }), expect.anything());
 await user.click(within(works).getByRole("button", { name: "Today", exact: true }));
 expect(screen.getByRole("heading", { name: "Who Works Today?" })).toBeVisible();
});

test("an intersecting feed sentinel reloads successive seven-item pages only once while pending", async () => {
 const observers = observeFeed();
 let finishPage!: (result: ManagerHomeActivityPage) => void;
 vi.mocked(managerHomeActivityApi.list).mockImplementation(({ offset = 0 }) => offset === 7
  ? new Promise(resolve => { finishPage = resolve; })
  : Promise.resolve(activityPage(offset, 17)));
 mount();
 const panel = screen.getByRole("tabpanel");
 await waitFor(() => expect(observers).toHaveLength(1));
 expect(observers[0].root).toBe(panel);
 act(() => { observers[0].intersect(); observers[0].intersect(); });
 expect(screen.getByRole("status")).toHaveTextContent("Loading more activity...");
 expect(managerHomeActivityApi.list.mock.calls.map(([request]) => request.offset)).toEqual([0, 7]);
 expect(screen.queryByRole("button", { name: "Show more" })).toBeNull();
 await act(async () => finishPage(activityPage(7, 17)));
 await waitFor(() => expect(observers).toHaveLength(2));
 expect(observers[0].disconnect).toHaveBeenCalled();
 act(() => observers[1].intersect());
 await waitFor(() => expect(within(panel).getAllByRole("link")).toHaveLength(17));
 expect(managerHomeActivityApi.list.mock.calls.map(([request]) => [request.offset, request.limit])).toEqual([[0, 7], [7, 7], [14, 7]]);
 expect(observers).toHaveLength(2);
});

test("a next-page failure waits for Retry and retains the loaded page", async () => {
 const observers = observeFeed();
 vi.mocked(managerHomeActivityApi.list)
  .mockResolvedValueOnce(activityPage(0, 8))
  .mockRejectedValueOnce(new Error("Unavailable"))
  .mockResolvedValueOnce(activityPage(7, 8));
 mount();
 await waitFor(() => expect(observers).toHaveLength(1));
 act(() => observers[0].intersect());
 const retry = await screen.findByRole("button", { name: "Retry" });
 expect(screen.getByText("Could not load more activity.")).toBeVisible();
 act(() => observers[0].intersect());
 expect(managerHomeActivityApi.list).toHaveBeenCalledTimes(2);
 expect(within(screen.getByRole("tabpanel")).getAllByRole("link")).toHaveLength(7);
 fireEvent.click(retry);
 await waitFor(() => expect(within(screen.getByRole("tabpanel")).getAllByRole("link")).toHaveLength(8));
 expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
});

test("changing tab or month resets the feed and never displays an old in-flight page", async () => {
 vi.setSystemTime(new Date("2026-11-04T10:00:00Z"));
 const observers = observeFeed();
 let finishOldPage!: (result: ManagerHomeActivityPage) => void;
 vi.mocked(managerHomeActivityApi.list).mockImplementation(({ tab, month, offset = 0 }) => {
  if (tab === "all" && month === 11) return offset === 7
   ? new Promise(resolve => { finishOldPage = resolve; })
   : Promise.resolve(activityPage(0, 8, "Old"));
  return Promise.resolve(activityPage(0, 3, `${tab} ${month}`));
 });
 mount();
 const user = userEvent.setup();
 const feed = screen.getByRole("tabpanel");
 await waitFor(() => expect(observers).toHaveLength(1));
 act(() => observers[0].intersect());
 feed.scrollTop = 100;
 await user.click(screen.getByRole("tab", { name: /Requires Approval/ }));
 expect(feed.scrollTop).toBe(0);
 expect(await within(feed).findByText("approvals 11 1 gave up a shift")).toBeVisible();
 await user.click(within(screen.getByRole("group", { name: "Period" })).getByRole("button", { name: "Period", exact: true }));
 await user.click(screen.getByRole("option", { name: "December 2026", exact: true }));
 expect(await within(feed).findByText("approvals 12 1 gave up a shift")).toBeVisible();
 await act(async () => finishOldPage(activityPage(7, 8, "Old")));
 expect(within(feed).queryByText(/Old \d/)).toBeNull();
 expect(within(feed).getAllByRole("link")).toHaveLength(3);
});

test("overflow fades follow scroll and resize while the feed remains keyboard reachable", async () => {
 mount();
 const feed = screen.getByRole("tabpanel");
 await within(feed).findByRole("link");
 let height = 100;
 Object.defineProperties(feed, {
  clientHeight: { get: () => height },
  scrollHeight: { get: () => 300 },
 });
 expect(feed).toHaveAttribute("tabindex", "0");
 expect(feed).not.toHaveAttribute("data-overflow-bottom");
 fireEvent.resize(window);
 expect(feed).toHaveAttribute("data-overflow-bottom", "true");
 expect(feed).not.toHaveAttribute("data-overflow-top");
 feed.scrollTop = 100;
 fireEvent.scroll(feed);
 expect(feed).toHaveAttribute("data-overflow-top", "true");
 expect(feed).toHaveAttribute("data-overflow-bottom", "true");
 feed.scrollTop = 200;
 fireEvent.scroll(feed);
 expect(feed).not.toHaveAttribute("data-overflow-bottom");
 height = 300;
 feed.scrollTop = 0;
 fireEvent.resize(window);
 expect(feed).not.toHaveAttribute("data-overflow-top");
 expect(feed).not.toHaveAttribute("data-overflow-bottom");
});
