import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { shopsApi } from "@entities/shops";
import { urlopApi, type UrlopShopSummary } from "@entities/urlop";
import { queryKeys } from "@shared/api/queryKeys";
import { setLanguage } from "@shared/i18n";
import { ManagerUrlopListPage } from "./ManagerUrlopListPage";

const shops = [
  { id: 17, name: "F35", address: "Krakowska 1" },
  { id: 23, name: "Central", address: "Main Street", description: "East wing" },
  { id: 31, name: "No requests", address: "" },
];
beforeEach(() => {
  setLanguage("en");
  vi.spyOn(shopsApi, "list").mockResolvedValue(shops);
  vi.spyOn(urlopApi, "managerShopSummaries").mockResolvedValue([
    { shopId: 23, approvedRequests: 8, pendingRequests: 2 },
    { shopId: 17, approvedRequests: 1, pendingRequests: 3 },
  ]);
});
afterEach(() => vi.restoreAllMocks());

function mount() {
  window.history.replaceState({}, "", "/urlop");
  const client = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  const view = render(<QueryClientProvider client={client}><BrowserRouter><ManagerUrlopListPage /></BrowserRouter></QueryClientProvider>);
  return { ...view, client };
}
const card = (name: string) => screen.getByRole("button", { name: `Open vacation management for ${name}` });

test("list shows real shops and request counts with no detail controls or quota", async () => {
  mount();
  await screen.findByText("Total: 3");
  expect(screen.getByRole("heading", { name: "Urlop" })).toBeInTheDocument();
  expect(screen.queryByRole("heading", { name: "Choose a shop" })).toBeNull();
  expect(screen.queryByText("View approved vacations and manage requests for each shop.")).toBeNull();
  expect(screen.getByRole("button", { name: "Back", exact: true })).toBeInTheDocument();
  expect(screen.getByRole("textbox", { name: "Search vacation shop" })).toBeInTheDocument();
  await waitFor(() => expect(card("F35")).toHaveTextContent("Approved vacations"));
  expect(within(card("F35")).getByText("1", { exact: true })).toBeInTheDocument();
  expect(within(card("Central")).getByText("8", { exact: true })).toBeInTheDocument();
  expect(within(card("No requests")).getAllByText("0", { exact: true })).toHaveLength(2);
  expect(card("F35")).toHaveTextContent("Pending vacation requests: 3");
  expect(card("F35").querySelectorAll("button")).toHaveLength(0);
  expect(card("F35").querySelector('[aria-hidden="true"]')).toBeInTheDocument();
  expect(screen.queryByLabelText("Month")).toBeNull();
  expect(screen.queryByLabelText("Employee")).toBeNull();
  expect(screen.queryByRole("button", { name: "Open requests" })).toBeNull();
  expect(screen.queryByRole("progressbar")).toBeNull();
  expect(urlopApi.managerShopSummaries).toHaveBeenCalledTimes(1);
});

test("search matches name, address and description without case sensitivity, and clears", async () => {
  mount(); await screen.findByText("F35", { exact: true });
  const search = screen.getByRole("textbox", { name: "Search vacation shop" });
  for (const term of ["f35", "KRAKOWSKA"]) {
    fireEvent.change(search, { target: { value: term } });
    await screen.findByText("Total: 1");
    expect(card("F35")).toBeInTheDocument();
    expect(screen.queryByText("Central", { exact: true })).toBeNull();
  }
  fireEvent.change(search, { target: { value: "EAST WING" } });
  await screen.findByText("Central", { exact: true });
  fireEvent.change(search, { target: { value: "missing" } });
  await screen.findByText("Nothing found");
  expect(screen.getByText('No shop matches "missing".')).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Clear Search" }));
  await screen.findByText("Total: 3");
  expect(card("F35")).toBeInTheDocument();
});

test.each(["click", "Enter", " "])("%s activation uses the actual shop ID", async activation => {
  mount(); await screen.findByText("F35", { exact: true });
  const button = card("F35");
  if (activation === "click") fireEvent.click(button);
  else { button.focus(); await userEvent.keyboard(activation === "Enter" ? "{Enter}" : " "); }
  await waitFor(() => expect(window.location.pathname).toBe(`/urlop/${shops[0].id}`));
});

test("loading counters remain placeholders until a successful response", async () => {
  let finish!: (value: UrlopShopSummary[]) => void;
  vi.mocked(urlopApi.managerShopSummaries).mockReturnValue(new Promise(resolve => { finish = resolve; }));
  mount(); await screen.findByText("F35", { exact: true });
  expect(screen.getByRole("status")).toHaveTextContent("Loading vacation counters...");
  expect(within(card("F35")).getAllByText("—", { exact: true })).toHaveLength(2);
  expect(within(card("F35")).queryByText("0", { exact: true })).toBeNull();
  await act(async () => finish([]));
  await waitFor(() => expect(within(card("F35")).getAllByText("0", { exact: true })).toHaveLength(2));
});

test("failed counters preserve navigation and placeholders; retry restores zero counts", async () => {
  vi.mocked(urlopApi.managerShopSummaries).mockRejectedValue(new Error("offline"));
  mount(); await screen.findByText("Vacation counters unavailable.");
  expect(within(card("F35")).getAllByText("—", { exact: true })).toHaveLength(2);
  expect(within(card("F35")).queryByText("0", { exact: true })).toBeNull();
  vi.mocked(urlopApi.managerShopSummaries).mockResolvedValue([]);
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  await waitFor(() => expect(within(card("F35")).getAllByText("0", { exact: true })).toHaveLength(2));
  fireEvent.click(card("F35"));
  expect(window.location.pathname).toBe(`/urlop/${shops[0].id}`);
});

test("summary failure does not prevent opening a card", async () => {
  vi.mocked(urlopApi.managerShopSummaries).mockRejectedValue(new Error("offline"));
  mount(); await screen.findByText("Vacation counters unavailable.");
  fireEvent.click(card("F35"));
  expect(window.location.pathname).toBe(`/urlop/${shops[0].id}`);
});

test("empty shops, failed shops, and loading shops remain distinct", async () => {
  vi.mocked(shopsApi.list).mockResolvedValue([]);
  const first = mount(); await screen.findByText("No shops yet"); first.unmount();
  vi.mocked(shopsApi.list).mockRejectedValue(new Error("offline"));
  const second = mount(); await screen.findByText("Could not load shops.");
  expect(screen.queryByText("No shops yet")).toBeNull(); second.unmount();
  vi.mocked(shopsApi.list).mockReturnValue(new Promise(() => {}));
  mount(); expect(screen.getByText("Loading shops...")).toBeInTheDocument();
});

test("existing urlop invalidation refreshes list counters", async () => {
  const { client } = mount(); await screen.findByText("F35", { exact: true });
  await waitFor(() => expect(within(card("F35")).getByText("1", { exact: true })).toBeInTheDocument());
  vi.mocked(urlopApi.managerShopSummaries).mockResolvedValue([{ shopId: 17, approvedRequests: 2, pendingRequests: 2 }]);
  act(() => { client.invalidateQueries({ queryKey: queryKeys.urlop.all }); });
  await waitFor(() => expect(card("F35")).toHaveTextContent("Pending vacation requests: 2"));
  expect(within(card("F35")).getAllByText("2", { exact: true })).toHaveLength(2);
});
