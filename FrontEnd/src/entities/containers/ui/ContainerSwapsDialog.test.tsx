import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import type { ShiftSwap } from "@entities/shift-swaps";
import { setLanguage } from "@shared/i18n";
import { ContainerSwapsDialog } from "./ContainerSwapsDialog";

const base: ShiftSwap = {
  id: 1, scheduleId: 27, scheduleSlotId: 2701, scheduleName: "F27", containerName: "North",
  shopName: "F27", year: 2026, month: 10, dayOfMonth: 4, fromTime: "09:00", toTime: "21:00",
  fromEmployeeId: 1, fromEmployeeName: "Judyta Noga", targetEmployeeId: 2, targetEmployeeName: "Oleh Protsun",
  visibility: "public", status: "accepted", createdAtUtc: "2026-09-20T10:00:00Z",
  acceptedAtUtc: "2026-09-21T10:00:00Z", shiftHours: 12, currentEmployeeHoursBefore: 0,
  currentEmployeeHoursAfter: 12, currentEmployeeWorkDaysBefore: 0, currentEmployeeWorkDaysAfter: 1,
  currentEmployeeFreeDaysBefore: 0, currentEmployeeFreeDaysAfter: 0, fromEmployeeHoursBefore: 12,
  fromEmployeeHoursAfter: 0, isManagerCreated: false, isCreatedByCurrentEmployee: false, isScheduleLocked: false,
  canAccept: false, canCancel: false, allowPartialAcceptance: false, canAcceptFull: false, eligibleClaimWindows: [],
};
const exchange: ShiftSwap = {
  ...base, id: 2, requestKind: "exchange", scheduleName: "F27", acceptedByEmployeeName: "Oleh Protsun",
  returnScheduleId: 35, returnScheduleName: "F35", returnShopName: "West", returnYear: 2026,
  returnMonth: 10, returnDayOfMonth: 6, returnFromTime: "09:00", returnToTime: "22:00", returnShiftHours: 13,
};
const open: ShiftSwap = { ...base, id: 3, status: "open", acceptedByEmployeeName: null, canCancel: true };
const callbacks = {
  onRetry: vi.fn(), onClose: vi.fn(), onView: vi.fn(), onCancel: vi.fn(), onDelete: vi.fn(),
};
const props = { open: true, swaps: [base, exchange, open], isLoading: false, isError: false, actionError: null,
  isActionPending: false, ...callbacks };

beforeEach(() => { setLanguage("en"); vi.clearAllMocks(); });

test("shows container totals, filters by tabs and searches exchange metadata", async () => {
  const user = userEvent.setup();
  render(<ContainerSwapsDialog {...props} />);
  expect(screen.getByRole("dialog", { name: "Container Swaps" })).toBeVisible();
  expect(screen.getByText("Total swaps").parentElement).toHaveTextContent("3");
  expect(screen.getByRole("tab", { name: "Exchange 1" })).toBeVisible();
  await user.click(screen.getByRole("tab", { name: "Exchange 1" }));
  expect(screen.getAllByRole("article")).toHaveLength(1);
  const exchangeCard = screen.getByRole("article");
  expect(within(exchangeCard).getByRole("group", { name: "Judyta Noga" })).toHaveTextContent("F27");
  expect(within(exchangeCard).getByRole("group", { name: "Oleh Protsun" })).toHaveTextContent("F35");
  await user.type(screen.getByRole("searchbox"), "F35");
  expect(screen.getAllByRole("article")).toHaveLength(1);
  await user.clear(screen.getByRole("searchbox"));
  await user.click(screen.getByRole("tab", { name: "Open 1" }));
  expect(screen.getAllByRole("article")).toHaveLength(1);
  expect(screen.getByText("Giveaway to Oleh Protsun")).toBeVisible();
});

test("keeps view comparison, cancel, delete and close actions wired to handlers", async () => {
  const user = userEvent.setup();
  render(<ContainerSwapsDialog {...props} />);
  await user.click(screen.getAllByRole("button", { name: "View" })[0]);
  expect(callbacks.onView).toHaveBeenCalledWith(base);
  await user.click(screen.getByRole("tab", { name: "Open 1" }));
  await user.click(screen.getByRole("button", { name: "Cancel" }));
  expect(callbacks.onCancel).toHaveBeenCalledWith(open);
  await user.click(screen.getByRole("button", { name: "Delete" }));
  expect(callbacks.onDelete).toHaveBeenCalledWith(open);
  await user.click(screen.getByRole("button", { name: "Close" }));
  expect(callbacks.onClose).toHaveBeenCalledOnce();
});

test("supports loading, retry and empty results", async () => {
  const user = userEvent.setup();
  const { rerender } = render(<ContainerSwapsDialog {...props} isLoading />);
  expect(screen.getByText("Loading swaps...")).toBeVisible();
  rerender(<ContainerSwapsDialog {...props} swaps={[]} isError />);
  await user.click(screen.getByRole("button", { name: "Retry" }));
  expect(callbacks.onRetry).toHaveBeenCalledOnce();
  rerender(<ContainerSwapsDialog {...props} swaps={[]} />);
  expect(screen.getByText("No swaps in this container.")).toBeVisible();
});

test("locks background scrolling and closes on Escape", async () => {
  const user = userEvent.setup();
  const { rerender } = render(<ContainerSwapsDialog {...props} />);
  expect(document.body.style.overflow).toBe("hidden");
  await user.keyboard("{Escape}");
  expect(callbacks.onClose).toHaveBeenCalledOnce();
  rerender(<ContainerSwapsDialog {...props} open={false} />);
  expect(document.body.style.overflow).toBe("");
});
