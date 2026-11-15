import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import type { ShiftSwap } from "@entities/shift-swaps";
import { setLanguage } from "@shared/i18n";
import { ContainerGraphSwapLogPanel } from "./ContainerGraphSwapLogPanel";

const accepted: ShiftSwap = {
  id: 1, scheduleId: 10, scheduleSlotId: 100, scheduleName: "Downtown November",
  containerName: "Downtown", shopName: "Central", year: 2026, month: 11, dayOfMonth: 14,
  fromTime: "08:00:00", toTime: "16:00:00", fromEmployeeName: "Ada Giver",
  acceptedByEmployeeName: "Pat Receiver", status: "accepted", visibility: "public",
  createdAtUtc: "2026-11-10T15:00:00Z", acceptedAtUtc: "2026-11-11T15:00:00Z", shiftHours: 8,
  currentEmployeeHoursBefore: 0, currentEmployeeHoursAfter: 8, currentEmployeeWorkDaysBefore: 0,
  currentEmployeeWorkDaysAfter: 1, currentEmployeeFreeDaysBefore: 0, currentEmployeeFreeDaysAfter: 0,
  fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0, isManagerCreated: false,
  isCreatedByCurrentEmployee: false, isScheduleLocked: false, canAccept: false, canCancel: false,
  allowPartialAcceptance: false, canAcceptFull: false, eligibleClaimWindows: [],
};
const open: ShiftSwap = { ...accepted, id: 2, status: "open", fromEmployeeName: "Open manager offer", isManagerCreated: true, manualColumnId: 4 };
const props = {
  acceptedSwaps: [accepted, open], isLoading: false, highlightColor: "#BBF7D0",
  highlightSettingDisabled: false, highlightError: null, onOpenHighlightColorPicker: vi.fn(), onViewComparison: vi.fn(),
};

beforeEach(() => { setLanguage("en"); vi.clearAllMocks(); });

test("filters accepted swaps by employee, shift date and schedule and preserves comparison and color actions", async () => {
  const user = userEvent.setup();
  render(<ContainerGraphSwapLogPanel {...props} />);
  const search = screen.getByRole("searchbox");
  const comparison = screen.getByRole("button", { name: "View swap comparison for Ada Giver" });
  expect(screen.getByText("14 Nov 08:00-16:00 (8h) - Central")).toBeVisible();
  expect(screen.queryByText("Gives to")).toBeNull();
  expect(screen.queryByText("Open manager offer")).toBeNull();
  for (const term of ["Pat Receiver", "2026-11-14", "Downtown November"]) {
    await user.clear(search);
    await user.type(search, term);
    expect(comparison).toBeVisible();
  }
  await user.click(comparison);
  expect(props.onViewComparison).toHaveBeenCalledWith(accepted);
  await user.click(screen.getByRole("button", { name: "Accepted swap highlight color" }));
  expect(props.onOpenHighlightColorPicker).toHaveBeenCalledOnce();
  await user.clear(search);
  await user.type(search, "Open manager offer");
  expect(screen.getByText("No swaps match this search.")).toBeVisible();
  expect(comparison).not.toBeInTheDocument();
});

test("exchange history shows the correct recipient, date and time on each side", async () => {
  const exchange: ShiftSwap = { ...accepted, requestKind: "exchange", scheduleName: "F27", shopName: " f27 ",
    returnScheduleId: 35, returnScheduleName: "F35", returnShopName: "West",
    returnYear: 2026, returnMonth: 12, returnDayOfMonth: 3, returnFromTime: "12:15", returnToTime: "18:45", returnShiftHours: 6.5,
  };
  const user = userEvent.setup();
  render(<ContainerGraphSwapLogPanel {...props} acceptedSwaps={[exchange]} />);
  await user.type(screen.getByRole("searchbox"), "F35");
  const card = within(screen.getByRole("article"));
  const source = within(card.getByRole("group", { name: "Ada Giver" }));
  const returned = within(card.getByRole("group", { name: "Pat Receiver" }));
  expect(source.getByText("Gives to").parentElement).toHaveTextContent("Gives to Pat Receiver");
  expect(returned.getByText("Gives to").parentElement).toHaveTextContent("Gives to Ada Giver");
  for (const [panel, details] of [
    [source, ["F27", "14 Nov 2026", "08:00–16:00 (8h)"]],
    [returned, ["F35 · West", "03 Dec 2026", "12:15–18:45 (6.5h)"]],
  ] as const) {
    for (const text of details) expect(panel.getByText(text, { exact: true })).toBeVisible();
  }
  expect(card.queryByText(/F27\s*·\s*f27/i)).toBeNull();
  expect(card.getByText("Accepted", { exact: true })).toBeVisible();
  expect(card.getByText("When was accepted:", { exact: true })).toBeVisible();
  await user.click(card.getByRole("button", { name: "View swap comparison for Ada Giver" }));
  expect(props.onViewComparison).toHaveBeenCalledWith(exchange);
});

test("an incomplete exchange leaves the historical return fields unavailable", () => {
  render(<ContainerGraphSwapLogPanel {...props} acceptedSwaps={[{ ...accepted, requestKind: "exchange" }]} />);
  const returned = within(screen.getByRole("group", { name: "Pat Receiver" }));
  expect(returned.getAllByText("Not available", { exact: true })).toHaveLength(3);
  expect(returned.getByText("Gives to").parentElement).toHaveTextContent("Gives to Ada Giver");
  expect(returned.queryByText(/Downtown November/)).toBeNull();
});

test("renders loading, empty history and highlight errors", () => {
  const { rerender } = render(<ContainerGraphSwapLogPanel {...props} isLoading />);
  expect(screen.getByText("Loading swap log...")).toBeVisible();
  rerender(<ContainerGraphSwapLogPanel {...props} acceptedSwaps={[open]} highlightSettingDisabled highlightError="Color unavailable" />);
  expect(screen.getByText("No accepted swaps yet.")).toBeVisible();
  expect(screen.getByText("Color unavailable")).toBeVisible();
  expect(screen.getByRole("button", { name: "Accepted swap highlight color" })).toBeDisabled();
});
