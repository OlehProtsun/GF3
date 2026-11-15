import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { shopsApi } from "@entities/shops";
import { urlopApi, type ApprovedUrlopDay } from "@entities/urlop";
import { queryKeys } from "@shared/api/queryKeys";
import { setLanguage } from "@shared/i18n";
import styles from "@entities/containers/ui/ContainerGraphMatrix.module.css";
import { AvailabilityScheduleMatrix } from "./AvailabilityScheduleMatrix";

let approved: ApprovedUrlopDay[];
beforeEach(() => {
  setLanguage("en");
  approved = [1, 2, 4].map(day => ({ shopId: 1, employeeId: 7, date: `2026-05-0${day}` }));
  vi.spyOn(urlopApi, "approved").mockImplementation(async (from, to) => approved.filter(day => day.date >= from && day.date <= to));
  vi.spyOn(shopsApi, "list").mockResolvedValue([{ id: 1, name: "A", address: "Address" }]);
});
afterEach(() => vi.restoreAllMocks());

test.each(["profile", "edit", "automatic"])("%s uses unavailable cell backgrounds for approved vacation, preserving holidays and raw values", async mode => {
  const changed = vi.fn();
  const client = new QueryClient();
  const cellMap = Object.fromEntries([1, 2, 4, 5].flatMap(day => [[`7:${day}`, "+"], [`8:${day}`, "-"]]));
  const props = { year: 2026, month: 5, columns: [{ employeeId: 7, label: "Ada" }, { employeeId: 8, label: "Grace" }], cellMap,
    readOnly: mode === "profile", readOnlyCellKeys: mode === "automatic" ? new Set(["7:1", "7:2", "7:4"]) : undefined, onCellChange: changed };
  const view = render(<QueryClientProvider client={client}><AvailabilityScheduleMatrix {...props} /></QueryClientProvider>);
  await waitFor(() => expect(screen.getAllByText("UR")).toHaveLength(3));
  for (const day of [1, 2, 4]) {
    const badge = screen.getAllByText("UR").find(node => node.closest("tr")?.textContent?.includes(`0${day}.05`))!;
    const vacation = badge.closest("td")!;
    const unavailable = within(vacation.parentElement!).getAllByRole("cell")[1];
    expect(vacation).toHaveClass(styles.emptyCellDanger);
    expect(vacation.classList.contains(styles.emptyCellDangerWeekend)).toBe(day === 2);
    expect(vacation.classList.contains(styles.readonlyEmptyCellDanger)).toBe(mode !== "edit");
    expect(unavailable).toHaveClass(styles.emptyCellDanger);
    expect(badge).toHaveAttribute("title", "UR · A");
  }
  expect(screen.getByTitle("Labour Day")).toHaveAttribute("aria-label", expect.stringContaining("Holiday"));
  expect(screen.queryByRole("textbox", { name: "Ada day 4", exact: true }) !== null).toBe(mode === "edit");
  expect(cellMap["7:4"]).toBe("+"); expect(changed).not.toHaveBeenCalled();
  if (mode === "edit") {
    const input = screen.getByRole("textbox", { name: "Ada day 4", exact: true });
    expect(input).toHaveValue("+");
    fireEvent.change(input, { target: { value: "-" } });
    expect(changed).toHaveBeenCalledWith(7, 4, "-");
  }
  expect(view.container.querySelector("tbody tr:nth-child(5) td")).not.toHaveClass(styles.emptyCellDanger);
  expect(view.container.querySelector("tbody tr:nth-child(5) td:nth-of-type(2)")).toHaveClass(styles.emptyCellDanger);
  view.rerender(<QueryClientProvider client={client}><AvailabilityScheduleMatrix {...props} month={6} /></QueryClientProvider>);
  await waitFor(() => expect(urlopApi.approved).toHaveBeenLastCalledWith("2026-06-01", "2026-06-30", undefined, expect.any(AbortSignal)));
  await waitFor(() => expect(screen.queryByText("UR")).toBeNull());
  view.rerender(<QueryClientProvider client={client}><AvailabilityScheduleMatrix {...props} /></QueryClientProvider>);
  await waitFor(() => expect(screen.getAllByText("UR")).toHaveLength(3));
  approved = [];
  act(() => client.invalidateQueries({ queryKey: queryKeys.urlop.all }));
  await waitFor(() => expect(screen.queryByText("UR")).toBeNull());
  expect(view.container.querySelector("tbody tr:nth-child(4) td")).not.toHaveClass(styles.emptyCellDanger);
  if (mode === "edit") expect(screen.getByRole("textbox", { name: "Ada day 4", exact: true })).toHaveValue("+");
  else expect(view.container.querySelector("tbody tr:nth-child(4) td")).toHaveTextContent("+");
});
