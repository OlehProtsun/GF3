import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { useRef, useState } from "react";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwapOfferPreview, ShiftSwapReturnCandidate, SwapWizardRequest } from "@entities/shift-swaps";
import { SwapCreateWizard } from "./SwapCreateWizard";
import { SwapShiftPickerDialog } from "./SwapShiftPickerDialog";

const mocks = vi.hoisted(() => ({ candidates: vi.fn(), preview: vi.fn() }));
vi.mock("@entities/shift-swaps", async original => ({
  ...await original<typeof import("@entities/shift-swaps")>(),
  useShiftSwapReturnCandidatesQuery: (params: unknown) => mocks.candidates(params),
  useShiftSwapPreviewQuery: (payload: unknown) => mocks.preview(payload),
}));
const source: ShiftSwapReturnCandidate = { scheduleId: 10, scheduleName: "F27", shopName: "Shop", year: 2026, month: 10, dayOfMonth: 1,
  slotId: 100, fromTime: "09:00", toTime: "16:30", durationMinutes: 450, canSelect: true };
const candidate: ShiftSwapReturnCandidate = { ...source, scheduleId: 20, scheduleName: "F35", slotId: 200, dayOfMonth: 3,
  fromTime: "08:00", toTime: "18:00", durationMinutes: 600, suggestedFromTime: "09:00", suggestedToTime: "16:30",
  eligibleWindows: [{ fromTime: "09:00", toTime: "16:30" }, { fromTime: "09:15", toTime: "16:45" }] };
const schedule: EmployeeSchedule = { id: 10, containerId: 1, containerName: "Main", shopId: 1, shopName: "Shop", name: "F27", year: 2026, month: 10,
  publicationStatus: "public", allowSwap: true, slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 1, status: "ASSIGNED", fromTime: "09:00", toTime: "16:30" }] };
const employees = [{ id: 2, firstName: "Target", lastName: "Worker", displayName: "Target Worker" }, { id: 3, firstName: "Other", lastName: "Worker", displayName: "Other Worker" }];

beforeEach(() => {
  setLanguage("en");
  Object.defineProperties(HTMLDialogElement.prototype, {
    showModal: { configurable: true, value(this: HTMLDialogElement) { this.open = true; } },
    close: { configurable: true, value(this: HTMLDialogElement) { this.open = false; } },
  });
  mocks.candidates.mockImplementation(() => ({ data: { items: [candidate, { ...candidate, slotId: 201, canSelect: false, unavailableReason: "Not enough hours." }], hasMore: true }, isFetching: false }));
  mocks.preview.mockImplementation((payload: SwapWizardRequest | null) => ({ data: payload ? {
    requestKind: payload.requestKind, targetEmployeeId: payload.targetEmployeeId, targetEmployeeName: payload.targetEmployeeId ? "Target Worker" : "Everyone",
    source: { ...source, fromTime: payload.fromTime, toTime: payload.toTime }, return: payload.requestKind === "exchange" ? { ...candidate, fromTime: payload.returnFromTime, toTime: payload.returnToTime } : null,
    isValid: true, projectedMonths: [{ year: 2026, month: 10, hoursBefore: 7.5, hoursAfter: payload.requestKind === "exchange" ? 7.5 : 0, workDaysBefore: 1, workDaysAfter: 1, freeDaysBefore: 30, freeDaysAfter: 30 }],
  } satisfies ShiftSwapOfferPreview : undefined, isFetching: false }));
});
function setup() {
  const onSubmit = vi.fn(); const onClose = vi.fn();
  const props = { schedules: [schedule], employees, employeeId: 1, swaps: [], busy: false, onSubmit, onClose };
  const client = new QueryClient();
  const result = render(<QueryClientProvider client={client}><SwapCreateWizard {...props} /></QueryClientProvider>);
  return { ...result, onSubmit, onClose, props, client };
}

test("keyboard users can choose another row when the saved return becomes unavailable", async () => {
  const unavailable = { ...candidate, canSelect: false, unavailableReason: "Shift unavailable." };
  const available = { ...candidate, slotId: 202, dayOfMonth: 4 };
  render(<SwapShiftPickerDialog shifts={[unavailable, available]} selection={{ shift: candidate, period: { fromTime: "09:00", toTime: "16:30" } }}
    returnMode durationMinutes={450} onBack={() => {}} onConfirm={() => {}} />);
  screen.getByRole("heading", { name: "Choose their shift" }).focus();
  await userEvent.setup().tab();
  expect(screen.getByRole("radio", { name: /04.10.2026/ })).toHaveFocus();
});

test("restores the explicit launcher even when it was not focused before opening", async () => {
  function FocusHarness() {
    const launcher = useRef<HTMLButtonElement>(null);
    const [open, setOpen] = useState(true);
    return <><button ref={launcher}>Launch swap</button>{open && <SwapCreateWizard schedules={[schedule]} employees={employees}
      employeeId={1} swaps={[]} busy={false} onSubmit={() => {}} onClose={() => setOpen(false)} returnFocusRef={launcher} />}</>;
  }
  render(<QueryClientProvider client={new QueryClient()}><FocusHarness /></QueryClientProvider>);
  await userEvent.setup().click(screen.getByRole("button", { name: "Close" }));
  expect(screen.getByRole("button", { name: "Launch swap" })).toHaveFocus();
});
async function chooseSource(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("button", { name: /F27/ }));
  await user.click(screen.getByRole("radio", { name: /09:00.*16:30/ }));
  await user.click(screen.getByRole("button", { name: "Confirm shift" }));
  await user.click(screen.getByRole("button", { name: "Continue" }));
}

test("public Give has three steps, authoritative review and submits once", async () => {
  const user = userEvent.setup(); const result = setup();
  expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
  await chooseSource(user);
  expect(screen.queryByRole("group", { name: "Employees" })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByText("After sending, eligible employees will be able to view and accept your offer.")).toBeInTheDocument();
  expect(screen.getByText("7.5 → 0")).toBeInTheDocument();
  expect(screen.queryByLabelText("Reciprocal exchange")).not.toBeInTheDocument();
  expect(screen.queryByRole("region", { name: "Employee shift (they give)" })).not.toBeInTheDocument();
  await user.dblClick(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledTimes(1);
  expect(result.onSubmit).toHaveBeenCalledWith({ requestKind: "give", scheduleId: 10, scheduleSlotId: 100, fromTime: "09:00", toTime: "16:30", targetEmployeeId: null });
});

test("specific Exchange selects an exact custom return window in a single modal", async () => {
  const user = userEvent.setup(); const result = setup(); await chooseSource(user);
  await user.click(screen.getByRole("button", { name: "Specific employee" }));
  expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
  await user.type(screen.getByRole("searchbox"), "Target");
  expect(screen.queryByRole("button", { name: /Other Worker/ })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: /Target Worker/ }));
  await user.click(screen.getByRole("radio", { name: "Swap shifts" }));
  expect(screen.getByRole("button", { name: "Save selection" })).toBeDisabled();
  await user.click(screen.getByRole("button", { name: "Choose their shift" }));
  expect(screen.getAllByRole("dialog")).toHaveLength(1);
  const disabled = screen.getByRole("radio", { name: /Not enough hours/ }); expect(disabled).toBeDisabled();
  await user.click(screen.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/, exact: false }));
  expect(screen.getByRole("button", { name: "Full shift" })).toBeDisabled();
  await user.click(screen.getByRole("button", { name: "Increase From" }));
  expect(screen.getByDisplayValue("16:45")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Confirm selection" }));
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByText("Employee shift (they give)")).toBeInTheDocument();
  expect(screen.getByLabelText("Reciprocal exchange")).toBeInTheDocument();
  expect(screen.getByText("7.5 → 7.5")).toBeInTheDocument();
  expect(result.onSubmit).not.toHaveBeenCalled();
  await user.click(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledWith(expect.objectContaining({ requestKind: "exchange", targetEmployeeId: 2, returnScheduleId: 20, returnScheduleSlotId: 200, returnFromTime: "09:15", returnToTime: "16:45" }));
});

test("Escape restores the parent heading and Close invokes dismissal", async () => {
  const user = userEvent.setup(); const result = setup();
  await user.click(screen.getByRole("button", { name: /F27/ }));
  fireEvent(screen.getByRole("dialog"), new Event("cancel", { bubbles: true, cancelable: true }));
  expect(screen.getByRole("heading", { name: "Choose a schedule" })).toHaveFocus();
  await user.click(screen.getByRole("button", { name: "Close" })); expect(result.onClose).toHaveBeenCalledOnce();
});

test("pending and failed submission preserve the draft and allow an explicit retry", async () => {
  const user = userEvent.setup(); const result = setup(); await chooseSource(user);
  await user.click(screen.getByRole("button", { name: "Continue" }));
  await user.click(screen.getByRole("button", { name: "Send request" }));
  result.rerender(<QueryClientProvider client={result.client}><SwapCreateWizard {...result.props} busy /></QueryClientProvider>);
  expect(screen.getByRole("button", { name: "Sending..." })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Close" })).toBeDisabled();
  result.rerender(<QueryClientProvider client={result.client}><SwapCreateWizard {...result.props} error="Temporary failure" /></QueryClientProvider>);
  expect(screen.getByText("Temporary failure")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledTimes(2);
  expect(result.onSubmit.mock.calls[0][0]).toEqual(result.onSubmit.mock.calls[1][0]);
});

test("Polish labels and keyboard radio selection remain accessible", async () => {
  const user = userEvent.setup(); setLanguage("pl"); setup();
  expect(screen.getByRole("heading", { name: "Wybierz grafik" })).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: /F27/ }));
  const radio = screen.getByRole("radio", { name: /09:00.*16:30/ });
  radio.focus(); await user.keyboard("{ArrowDown}");
  expect(radio).toHaveAttribute("aria-checked", "true");
  await user.click(screen.getByRole("button", { name: "Potwierdź zmianę" }));
  expect(screen.getByText("Cała zmiana")).toBeInTheDocument();
});

test("failed preview blocks submission and preserves choices for editing", async () => {
  const user = userEvent.setup(); setup(); await chooseSource(user);
  mocks.preview.mockReturnValue({ error: new Error("Return shift unavailable."), isFetching: false });
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByRole("button", { name: "Send request" })).toBeDisabled();
  await user.click(screen.getByRole("button", { name: "Edit recipient" }));
  expect(screen.getByRole("heading", { name: "Give to whom?" })).toBeInTheDocument();
});

test("recipient search clears and changing recipient starts a fresh Give choice", async () => {
  const user = userEvent.setup(); setup(); await chooseSource(user);
  await user.click(screen.getByRole("button", { name: "Specific employee" }));
  await user.type(screen.getByRole("searchbox"), "Target");
  await user.click(screen.getByRole("button", { name: "Clear search" }));
  await user.click(screen.getByRole("button", { name: /Target Worker/ }));
  await user.click(screen.getByRole("radio", { name: "Swap shifts" }));
  await user.click(screen.getByRole("button", { name: "Close" }));
  await user.click(screen.getByRole("button", { name: /Other Worker/ }));
  expect(screen.getByRole("radio", { name: "Give shift" })).toHaveAttribute("aria-checked", "true");
  expect(screen.queryByRole("button", { name: "Choose their shift" })).not.toBeInTheDocument();
  expect(within(screen.getByRole("dialog")).getByRole("button", { name: "Save selection" })).toBeEnabled();
});

test("Review preserves decimal-hour precision and every projected month", async () => {
  const user = userEvent.setup(); const result = setup(); await chooseSource(user);
  const previewImplementation = mocks.preview.getMockImplementation()!;
  mocks.preview.mockImplementation(payload => {
    const query = previewImplementation(payload);
    return { ...query, data: query.data ? { ...query.data, projectedMonths: [
      { year: 2026, month: 10, hoursBefore: 298.48, hoursAfter: 293.48, workDaysBefore: 26, workDaysAfter: 26, freeDaysBefore: 5, freeDaysAfter: 5 },
      { year: 2026, month: 11, hoursBefore: 154, hoursAfter: 146.5, workDaysBefore: 22, workDaysAfter: 21, freeDaysBefore: 8, freeDaysAfter: 9 },
    ] } : undefined };
  });
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByText("298.48 → 293.48")).toBeVisible();
  await user.click(screen.getByText("View details"));
  expect(screen.getByText("154 → 146.5")).toBeVisible();
  expect(screen.getByText("22 → 21")).toBeVisible();
  expect(screen.getByText("8 → 9")).toBeVisible();
  expect(result.onSubmit).not.toHaveBeenCalled();
});
