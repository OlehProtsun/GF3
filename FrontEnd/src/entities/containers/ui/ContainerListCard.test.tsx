import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { setLanguage } from "@shared/i18n";
import type { Container, Graph } from "../model/types";
import { ContainerListCard } from "./ContainerListCard";

const { listGraphs } = vi.hoisted(() => ({ listGraphs: vi.fn() }));
vi.mock("../api/containersApi", () => ({ containersApi: { listGraphs } }));

const containers: Container[] = [{ id: 1, name: "Container A", note: null }, { id: 2, name: "Container B", note: "Instructions" }];
const graph: Graph = {
  id: 11, containerId: 1, shopId: 1, name: "Actual schedule A", year: 2026, month: 10,
  publicationStatus: "private", peoplePerShift: 2, shift1Time: "08:00", shift2Time: "16:00",
  maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 2, maxFullPerMonth: 4,
};
function setup(items = containers, searchQuery = "", cache: Record<number, Graph[]> = {}) {
  const client = new QueryClient();
  Object.entries(cache).forEach(([id, graphs]) => client.setQueryData(["containers", Number(id), "graphs"], graphs));
  const onContainerOpen = vi.fn();
  const onGraphOpen = vi.fn();
  const onClearSearch = vi.fn();
  const view = render(
    <QueryClientProvider client={client}>
      <ContainerListCard containers={items} isLoading={false} searchQuery={searchQuery} onClearSearch={onClearSearch} onContainerOpen={onContainerOpen} onGraphOpen={onGraphOpen} />
    </QueryClientProvider>,
  );
  return { ...view, client, onContainerOpen, onGraphOpen, onClearSearch };
}
function tile(name = "Container A") {
  return screen.getByRole("button", { name: `Open ${name} profile` }).closest("article")!;
}
function preview(element = tile()) {
  const toggle = within(element).getByRole("button", { name: "Schedules" });
  return document.getElementById(toggle.getAttribute("aria-controls")!)!;
}
function enter(element: HTMLElement) { fireEvent.pointerEnter(element, { pointerType: "mouse" }); }
function leave(element: HTMLElement) { fireEvent.pointerLeave(element, { pointerType: "mouse" }); }

beforeEach(() => {
  localStorage.clear();
  setLanguage("en");
  listGraphs.mockReset();
});
afterEach(cleanup);

describe("decorative container folders", () => {
  test("preserves container information and unlabeled status", () => {
    setup([...containers, { id: 3, name: "Blank", note: "   " }]);
    expect(screen.getByText("ID 1")).toBeInTheDocument();
    expect(screen.getByText("Instructions")).toBeInTheDocument();
    expect(screen.getAllByText("Schedule workspace for container planning and statistics.")).toHaveLength(2);
    expect(screen.getAllByText("No note yet")).toHaveLength(2);
    expect(screen.getByText("Note available")).toBeInTheDocument();
    expect(screen.queryByText("State")).not.toBeInTheDocument();
    expect(listGraphs).not.toHaveBeenCalled();
  });

  test("reuses only the first three cached schedules, scoped to their own container", () => {
    const graphs = Array.from({ length: 15 }, (_, index) => ({ ...graph, id: index + 10, name: `Schedule ${index}` }));
    setup(containers, "", { 1: graphs, 2: [{ ...graph, containerId: 2, name: "Schedule B" }] });
    enter(tile());
    expect(preview().children).toHaveLength(3);
    expect(within(preview()).getByText("Schedule 0")).toBeInTheDocument();
    expect(within(preview()).queryByText("Schedule 3")).toBeNull();
    expect(within(preview(tile("Container B"))).getByText("Schedule B")).toBeInTheDocument();
    expect(within(preview(tile("Container B"))).queryByText("Schedule 0")).toBeNull();
    leave(tile());
    enter(tile("Container B"));
    expect(listGraphs).not.toHaveBeenCalled();
  });

  test("uses blank papers without fetching and observes cache updates including zero schedules", () => {
    const { client } = setup();
    enter(tile());
    expect(preview().children).toHaveLength(3);
    expect(preview().textContent).toBe("");
    act(() => client.setQueryData(["containers", 1, "graphs"], []));
    expect(preview().children).toHaveLength(0);
    act(() => client.setQueryData(["containers", 1, "graphs"], [graph, { ...graph, id: 12, name: "Second" }]));
    expect(preview().children).toHaveLength(2);
    expect(within(preview()).getByText("Second")).toBeInTheDocument();
    expect(listGraphs).not.toHaveBeenCalled();
  });

  test("decorations have no actions or accessibility focus targets", () => {
    const { onContainerOpen, onGraphOpen } = setup(containers, "", { 1: [graph] });
    enter(tile());
    const decoration = preview();
    expect(decoration).toHaveAttribute("aria-hidden", "true");
    expect(decoration).toHaveAttribute("inert");
    expect(decoration.querySelector("button, a, input, [tabindex], [role=button]")).toBeNull();
    expect(screen.queryByRole("button", { name: /Open schedule/ })).toBeNull();
    fireEvent.click(decoration.firstElementChild!);
    expect(onGraphOpen).not.toHaveBeenCalled();
    expect(onContainerOpen).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Open Container A profile" }));
    expect(onContainerOpen).toHaveBeenCalledExactlyOnceWith(1);
  });

  test("retains papers for closing motion and keeps the touch toggle functional", () => {
    setup(containers, "", { 1: [graph] });
    const a = tile();
    const toggle = within(a).getByRole("button", { name: "Schedules" });
    const paper = preview().firstElementChild;
    fireEvent.pointerEnter(a, { pointerType: "touch" });
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    fireEvent.click(toggle);
    fireEvent.pointerLeave(a, { pointerType: "touch" });
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    fireEvent.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    expect(paper).toBeInTheDocument();
    enter(a);
    expect(preview().firstElementChild).toBe(paper);
    leave(a);
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    expect(listGraphs).not.toHaveBeenCalled();
  });

  test("keyboard navigation skips decorative papers and Escape closes the reveal", async () => {
    const user = userEvent.setup();
    setup(containers, "", { 1: [graph] });
    await user.tab();
    const toggle = within(tile()).getByRole("button", { name: "Schedules" });
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    await user.tab();
    expect(screen.getByRole("button", { name: "Pin Container A" })).toHaveFocus();
    await user.tab();
    expect(toggle).toHaveFocus();
    await user.keyboard("{Escape}");
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    await user.keyboard("{Enter}");
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    await user.tab();
    expect(screen.getByRole("button", { name: "Open Container B profile" })).toHaveFocus();
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    expect(listGraphs).not.toHaveBeenCalled();
  });

  test("pinning persists and never navigates", () => {
    const { onContainerOpen, onGraphOpen, unmount } = setup();
    fireEvent.click(screen.getByRole("button", { name: "Pin Container B" }));
    expect(screen.getByRole("button", { name: "Unpin Container B" })).toHaveAttribute("aria-pressed", "true");
    expect(localStorage.getItem("containers:list:pinned")).toBe('["2"]');
    expect(screen.getAllByRole("article")[0]).toBe(tile("Container B"));
    expect(onContainerOpen).not.toHaveBeenCalled();
    expect(onGraphOpen).not.toHaveBeenCalled();
    unmount();
    setup();
    expect(screen.getAllByRole("article")[0]).toBe(tile("Container B"));
  });

  test("preserves search, empty, loading and error list states", async () => {
    const user = userEvent.setup();
    const view = setup([], "missing");
    expect(screen.getByText("Nothing found")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Clear Search" }));
    expect(view.onClearSearch).toHaveBeenCalledOnce();
    view.unmount();
    const empty = setup([]);
    expect(screen.getByText("No containers yet")).toBeVisible();
    empty.unmount();
    const props = { containers: [], searchQuery: "", onClearSearch: vi.fn(), onContainerOpen: vi.fn(), onGraphOpen: vi.fn() };
    const loading = render(<ContainerListCard {...props} isLoading />);
    expect(screen.getByText("Loading containers...")).toBeVisible();
    loading.rerender(<ContainerListCard {...props} isLoading={false} error={new Error("offline")} />);
    expect(screen.getByText("Could not load containers.")).toBeVisible();
  });

  test("long names retain their accessible profile name without nested buttons", () => {
    const longName = "Very long container name ".repeat(20);
    const { container } = setup([{ id: 1, name: longName }], "", { 1: [{ ...graph, name: "Long schedule name ".repeat(20) }] });
    expect(screen.getByRole("button", { name: `Open ${longName.trim()} profile` })).toBeInTheDocument();
    expect(screen.getByTitle(longName.trim())).toBeInTheDocument();
    expect(container.querySelector("button button, button a")).toBeNull();
  });
});
