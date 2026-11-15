import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { setLanguage } from "@shared/i18n";
import type { ShiftSwap } from "@entities/shift-swaps";
import { beforeEach, expect, test, vi } from "vitest";
import { ShiftSwapHistoryDialog } from "./ShiftSwapHistoryDialog";
import matrixStyles from "./ContainerGraphMatrix.module.css";

const accepted: ShiftSwap = {
  id: 1, scheduleId: 27, scheduleSlotId: 100, scheduleName: "F27", containerName: "November",
  shopName: "Central", year: 2026, month: 11, dayOfMonth: 14, fromTime: "08:00", toTime: "16:00",
  fromEmployeeId: 1, fromEmployeeName: "Alice", acceptedByEmployeeId: 2, acceptedByEmployeeName: "Bob",
  status: "accepted", visibility: "public", createdAtUtc: "2026-11-10T12:00:00Z", shiftHours: 8,
  currentEmployeeHoursBefore: 0, currentEmployeeHoursAfter: 8,
  currentEmployeeWorkDaysBefore: 0, currentEmployeeWorkDaysAfter: 1,
  currentEmployeeFreeDaysBefore: 0, currentEmployeeFreeDaysAfter: 0, fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0,
  isManagerCreated: false, isCreatedByCurrentEmployee: false, isScheduleLocked: false, canAccept: false, canCancel: false,
  allowPartialAcceptance: false, canAcceptFull: false, eligibleClaimWindows: [],
};
const row = (employeeId: number, employeeName: string, dayValues: Record<number, string> = {}) =>
  ({ employeeId, employeeName, dayValues, kind: "employee" as const });
const source = {
  beforeSnapshot: { rows: [row(1, "Alice", { 14: "08:00 - 16:00" }), row(2, "Bob"), row(3, "Source Only")] },
  afterSnapshot: { rows: [row(1, "Alice"), row(2, "Bob", { 14: "08:00 - 16:00" }), row(3, "Source Only")] },
};
const crossSchedule: ShiftSwap = {
  ...accepted, ...source, requestKind: "exchange", returnScheduleId: 35, returnScheduleName: "F35", returnShopName: "West",
  returnYear: 2026, returnMonth: 12, returnDayOfMonth: 3, returnFromTime: "12:15", returnToTime: "18:45",
  returnBeforeSnapshot: { rows: [row(1, "Alice"), row(2, "Bob", { 3: "12:15 - 18:45" }), row(4, "Return Only")] },
  returnAfterSnapshot: { rows: [row(1, "Alice", { 3: "12:15 - 18:45" }), row(2, "Bob"), row(4, "Return Only")] },
};

beforeEach(() => setLanguage("en"));

function selectedCells(table: HTMLElement) {
  return Array.from(table.querySelectorAll(`.${matrixStyles.selectedCell}`), cell => cell.getAttribute("data-cell-key")).sort();
}

test("keeps the Before and After matrices for a regular giveaway", () => {
  render(<ShiftSwapHistoryDialog open swap={{ ...accepted, ...source, requestKind: "give" }} onCancel={vi.fn()} />);
  expect(screen.queryByRole("group", { name: "Comparison schedule" })).toBeNull();
  const [before, after] = screen.getAllByRole("table");
  expect(selectedCells(before)).toEqual(["1:14"]);
  expect(selectedCells(after)).toEqual(["2:14"]);
});

test("a same-schedule exchange highlights both transfers in the original matrix pair", () => {
  render(<ShiftSwapHistoryDialog open onCancel={vi.fn()} swap={{
    ...accepted, requestKind: "exchange", returnScheduleId: 27, returnYear: 2026, returnMonth: 11, returnDayOfMonth: 18,
    returnFromTime: "12:15", returnToTime: "18:45",
    beforeSnapshot: { rows: [row(1, "Alice", { 14: "08:00 - 16:00" }), row(2, "Bob", { 18: "12:15 - 18:45" })] },
    afterSnapshot: { rows: [row(1, "Alice", { 18: "12:15 - 18:45" }), row(2, "Bob", { 14: "08:00 - 16:00" })] },
  }} />);
  expect(screen.queryByRole("group", { name: "Comparison schedule" })).toBeNull();
  const tables = screen.getAllByRole("table");
  expect(tables).toHaveLength(2);
  expect(selectedCells(tables[0])).toEqual(["1:14", "2:18"]);
  expect(selectedCells(tables[1])).toEqual(["1:18", "2:14"]);
});

test("cross-schedule selections keep snapshots, periods and each transfer direction separate", async () => {
  const user = userEvent.setup();
  const onCancel = vi.fn();
  render(<ShiftSwapHistoryDialog open swap={crossSchedule} onCancel={onCancel} />);
  const switcher = screen.getByRole("group", { name: "Comparison schedule" });
  const sourceButton = within(switcher).getByRole("button", { name: /Source schedule.*F27/ });
  const returnButton = within(switcher).getByRole("button", { name: /Return schedule.*F35/ });
  expect(screen.getByText("Exchange between different schedules")).toBeVisible();
  for (const text of ["Alice", "14 Nov 2026", "08:00–16:00"]) expect(within(sourceButton).getByText(text)).toBeVisible();
  for (const text of ["Bob", "03 Dec 2026", "12:15–18:45"]) expect(within(returnButton).getByText(text)).toBeVisible();
  expect(sourceButton).toHaveAttribute("aria-pressed", "true");
  expect(screen.getByRole("button", { name: "Close swap comparison" })).toHaveFocus();
  let tables = screen.getAllByRole("table");
  expect(tables).toHaveLength(2);
  expect(within(tables[0]).getByRole("columnheader", { name: "Source Only" })).toBeVisible();
  expect(selectedCells(tables[0])).toEqual(["1:14"]);
  expect(selectedCells(tables[1])).toEqual(["2:14"]);

  await user.tab();
  expect(sourceButton).toHaveFocus();
  await user.tab();
  expect(returnButton).toHaveFocus();
  await user.keyboard("{Enter}");
  expect(returnButton).toHaveAttribute("aria-pressed", "true");
  tables = screen.getAllByRole("table");
  expect(tables).toHaveLength(2);
  expect(within(tables[0]).getByRole("columnheader", { name: "Return Only" })).toBeVisible();
  expect(within(tables[0]).queryByRole("columnheader", { name: "Source Only" })).toBeNull();
  expect(selectedCells(tables[0])).toEqual(["2:3"]);
  expect(selectedCells(tables[1])).toEqual(["1:3"]);
  await user.click(sourceButton);
  expect(selectedCells(screen.getAllByRole("table")[0])).toEqual(["1:14"]);
  await user.keyboard("{Escape}");
  expect(onCancel).toHaveBeenCalledOnce();
});

test("missing return snapshots never reuse source data", async () => {
  render(<ShiftSwapHistoryDialog open onCancel={vi.fn()} swap={{
    ...crossSchedule, returnBeforeSnapshot: null, returnAfterSnapshot: null,
  }} />);
  await userEvent.setup().click(screen.getByRole("button", { name: /Return schedule.*F35/ }));
  expect(screen.getByText("No comparison data is available for this schedule.")).toBeVisible();
  expect(screen.queryByRole("table")).toBeNull();
});
