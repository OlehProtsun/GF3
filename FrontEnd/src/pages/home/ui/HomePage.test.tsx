import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { loadHomeDashboard } from "./homeDashboard";
import { managerHomeActivityApi, type ManagerHomeActivityItem } from "../api/managerHomeActivityApi";
import { HomePage } from "./HomePage";
vi.mock("./homeDashboard", () => ({ loadHomeDashboard: vi.fn() }));
vi.mock("../api/managerHomeActivityApi", () => ({ managerHomeActivityApi: { list: vi.fn() } }));
const event: ManagerHomeActivityItem = { id: "swap:1", kind: "swap_created", occurredAtUtc: "2026-11-04T10:00:00Z", employeeName: "Personal name", status: "open", requiresApproval: false, targetType: "graph", containerId: 8, graphId: 20 };
beforeEach(() => {
 vi.resetAllMocks(); setLanguage("en"); window.history.replaceState({}, "", "/");
 vi.mocked(loadHomeDashboard).mockResolvedValue({ todayRows: [{ id: "r1", employeeId: 99, containerId: 8, graphId: 20, employee: "Personal name", shop: "Shop", dateLabel: "4 November 2026", fromTime: "08:00", toTime: "16:00", shift: "08:00 – 16:00", hoursText: "8h 0m" }], todayActiveEmployeesCount: 1, hasPartialData: false });
 vi.mocked(managerHomeActivityApi.list).mockResolvedValue({ items: [event], allActivityCount: 1, requiresApprovalCount: 0, hasMore: false, nextOffset: 1 });
});
function mount(phoneMode = false) {
 return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: 0 } } })}><BrowserRouter><HomePage phoneMode={phoneMode} /></BrowserRouter></QueryClientProvider>);
}
test("uses the shared Home header and retains the two dashboard cards and table fields", async () => {
 const { container } = mount(); await screen.findByText("Personal name", { selector: "strong" });
 expect(screen.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 expect(screen.getByText("Overview of today's schedules and recent application activity.")).toBeVisible();
 expect(screen.getByRole("button", { name: "Collapse header" })).toBeVisible();
 expect(container.querySelectorAll("section")).toHaveLength(2);
 expect(screen.getAllByRole("columnheader").map(cell => cell.textContent)).toEqual(["Employee", "Shift", "Shop", "Hours", "Status"]);
 const shopIcon = screen.getByRole("table").querySelector("tbody td:nth-child(3) svg"); expect(shopIcon?.getAttribute("class")).toContain("shopIcon");
 expect(container.querySelector("select")).toBeNull();
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
