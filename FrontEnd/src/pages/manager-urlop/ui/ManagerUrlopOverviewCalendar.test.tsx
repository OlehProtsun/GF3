import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import type { ApprovedUrlopDay } from "@entities/urlop";
import { ManagerUrlopOverviewCalendar } from "./ManagerUrlopOverviewCalendar";

beforeEach(() => setLanguage("en"));
const entry = (employeeId: number, date = "2026-10-01"): ApprovedUrlopDay => ({ shopId: 1, employeeId, employeeName: `Person ${employeeId}`, date });
test("October 2026 has Monday-first offsets and 31 read-only dates", () => {
  const { container } = render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[]} />);
  expect(screen.getAllByRole("button")).toHaveLength(31);
  const grid = container.querySelector('[class*="grid"]')!;
  expect(grid.children[10]).toHaveAccessibleName("2026-10-01");
  expect(grid.children[40]).toHaveAccessibleName("2026-10-31");
  expect(grid.children).toHaveLength(49);
  expect(screen.getByText("0 employees on vacation")).toBeInTheDocument();
  expect(screen.queryByRole("combobox")).toBeNull();
  expect(screen.getByRole("button", { name: "2026-10-01" })).not.toHaveAttribute("aria-pressed");
});
test("all employees, per-day deduplication, stable colors and unique monthly count", () => {
  render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[entry(7), entry(8), entry(7), entry(7, "2026-10-02"), entry(7, "2026-10-03"), entry(9, "2026-11-01")]} />);
  const day = screen.getByRole("button", { name: "2026-10-01 UR: Person 7, Person 8" });
  expect(within(day).getAllByTitle("Person 7")).toHaveLength(1);
  expect(within(day).getByTitle("Person 8")).toBeInTheDocument();
  expect(screen.getByText("2 employees on vacation")).toBeInTheDocument();
  expect(screen.queryByText("Person 9")).toBeNull();
  expect(screen.getAllByTitle("Person 7").map(element => element.className)).toEqual(Array(3).fill(within(day).getByTitle("Person 7").className));
});
test("one employee on three dates counts once", () => {
  render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[entry(1), entry(1, "2026-10-02"), entry(1, "2026-10-03")]} />);
  expect(screen.getByText("1 employees on vacation")).toBeInTheDocument();
});
test("overflow details expose every name, close with Close/Escape and reset across months", () => {
  const { rerender } = render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[1,2,3,4].map(id => entry(id))} />);
  const day = screen.getByRole("button", { name: "2026-10-01 UR: Person 1, Person 2, Person 3, Person 4" });
  expect(within(day).getByLabelText("+2 more")).toBeInTheDocument();
  expect(within(day).queryByText("Person 3")).toBeNull();
  fireEvent.click(day);
  const details = screen.getByRole("region", { name: "Approved vacations on 2026-10-01" });
  for (const id of [1,2,3,4]) expect(within(details).getByText(`Person ${id}`)).toBeInTheDocument();
  fireEvent.click(within(details).getByRole("button", { name: "Close" }));
  expect(screen.queryByRole("region", { name: /^Approved vacations on/ })).toBeNull();
  fireEvent.click(day); fireEvent.keyDown(day, { key: "Escape" });
  expect(screen.queryByRole("region", { name: /^Approved vacations on/ })).toBeNull();
  fireEvent.click(day);
  rerender(<ManagerUrlopOverviewCalendar year={2026} month={11} approvedDays={[]} />);
  expect(screen.queryByRole("region", { name: /^Approved vacations on/ })).toBeNull();
  rerender(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[]} />);
  expect(screen.queryByRole("region", { name: /^Approved vacations on/ })).toBeNull();
});
test.each([[2026,12,31],[2027,1,31],[2028,2,29],[2027,2,28]])("month boundary %i/%i has %i days", (year, month, days) => {
  render(<ManagerUrlopOverviewCalendar year={year} month={month} approvedDays={[]} />);
  expect(screen.getAllByRole("button")).toHaveLength(days);
});
test.each(["pl", "uk"] as const)("month and weekday labels follow %s", language => {
  setLanguage(language);
  render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[]} />);
  expect(screen.getByRole("heading").textContent).toBe(new Intl.DateTimeFormat(language === "pl" ? "pl-PL" : "uk-UA", { month: "long", year: "numeric" }).format(new Date(2026,9,1)));
});


test("today is highlighted and neighbor-month placeholders show muted dates", () => {
  vi.useFakeTimers({ toFake: ["Date"] });
  vi.setSystemTime(new Date("2026-10-15T10:00:00Z"));
  try {
    const { container } = render(<ManagerUrlopOverviewCalendar year={2026} month={10} approvedDays={[]} />);
    expect(screen.getByRole("button", { name: "2026-10-15" })).toHaveAttribute("aria-current", "date");
    expect(screen.getByRole("button", { name: "2026-10-14" })).not.toHaveAttribute("aria-current");
    const grid = container.querySelector('[class*="grid"]')!;
    expect(grid.children[7]).toHaveTextContent("28");
    expect(grid.children[8]).toHaveTextContent("29");
    expect(grid.children[9]).toHaveTextContent("30");
    expect(grid.children[41]).toHaveTextContent("1");
  } finally { vi.useRealTimers(); }
});

test.each([[2026, 2], [2028, 2], [2026, 8], [2026, 11]])("six weeks stay mounted for %i/%i and loading disables dates", (year, month) => {
  const { container, rerender } = render(<ManagerUrlopOverviewCalendar year={year} month={month} approvedDays={[]} />);
  const calendar = screen.getByRole("region", { name: "Vacation calendar" });
  expect(container.querySelector('[class*="grid"]')!.children).toHaveLength(49);
  rerender(<ManagerUrlopOverviewCalendar year={year} month={month === 12 ? 1 : month + 1} approvedDays={[]} loading />);
  expect(screen.getByRole("region", { name: "Vacation calendar" })).toBe(calendar);
  expect(calendar).toHaveAttribute("aria-busy", "true");
  expect(screen.getByRole("status")).toHaveTextContent("Loading");
  for (const button of screen.getAllByRole("button")) expect(button).toBeDisabled();
  expect(container.querySelector('[class*="grid"]')!.children).toHaveLength(49);
});
