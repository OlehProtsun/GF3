import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { useRef, useState } from "react";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwapOfferPreview, ShiftSwapReturnCandidate, SwapWizardRequest } from "@entities/shift-swaps";
import { SwapScrollRegion } from "./SwapScrollRegion";
import { SwapCreateWizard } from "./SwapCreateWizard";
import { SwapShiftPickerDialog } from "./SwapShiftPickerDialog";

test.each([true, false])("missing suggestions use a safe eligible interval: %s", async hasWindow => {
  const onConfirm = vi.fn();
  render(<SwapShiftPickerDialog shifts={[{ ...candidate, suggestedFromTime: undefined, suggestedToTime: undefined,
    eligibleWindows: hasWindow ? candidate.eligibleWindows : [] }]}
    returnMode onBack={() => {}} onConfirm={onConfirm} />);
  await userEvent.click(screen.getByRole("radio"));
  const confirm = screen.getByRole("button", { name: "Confirm selection" });
  if (hasWindow) {
    expect(screen.getByDisplayValue("09:00")).toBeInTheDocument();
    await userEvent.click(confirm);
    expect(onConfirm).toHaveBeenCalledWith(expect.objectContaining({ period: { fromTime: "09:00", toTime: "18:00" } }));
  } else { expect(confirm).toBeDisabled(); expect(onConfirm).not.toHaveBeenCalled(); }
});

test("empty native time edits preserve the valid draft and range buttons", async () => {
  render(<SwapShiftPickerDialog shifts={[source]} onBack={() => {}} onConfirm={() => {}} />);
  await userEvent.click(screen.getByRole("radio"));
  await userEvent.click(screen.getByRole("button", { name: "Custom period" }));
  fireEvent.change(screen.getByDisplayValue("09:00"), { target: { value: "" } });
  expect(screen.getByDisplayValue("09:00")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Decrease From" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Increase To" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Confirm shift" })).toBeEnabled();
});

test("scroll cues follow overflow, scroll, content and resize without resetting position", () => {
  let resize = () => {};
  const disconnect = vi.fn();
  vi.stubGlobal("ResizeObserver", class {
    constructor(callback: () => void) { resize = callback; }
    observe() {} disconnect = disconnect;
  });
  try {
    const result = render(<SwapScrollRegion role="region" aria-label="Rows"><p>One</p></SwapScrollRegion>);
    const list = screen.getByRole("region", { name: "Rows" }); const frame = list.parentElement!;
    expect(frame).toHaveAttribute("data-scroll-top", "false");
    expect(frame).toHaveAttribute("data-scroll-bottom", "false");
    Object.defineProperties(list, { clientHeight: { configurable: true, value: 100 }, scrollHeight: { configurable: true, value: 300 } });
    act(() => resize()); expect(frame).toHaveAttribute("data-scroll-bottom", "true");
    list.scrollTop = 100; fireEvent.scroll(list); expect(frame).toHaveAttribute("data-scroll-top", "true");
    result.rerender(<SwapScrollRegion role="region" aria-label="Rows"><p>Two</p></SwapScrollRegion>);
    expect(list.scrollTop).toBe(100);
    list.scrollTop = 200; fireEvent.scroll(list); expect(frame).toHaveAttribute("data-scroll-bottom", "false");
    Object.defineProperty(list, "scrollHeight", { configurable: true, value: 400 });
    act(() => resize()); expect(frame).toHaveAttribute("data-scroll-bottom", "true");
    result.unmount(); expect(disconnect).toHaveBeenCalled();
  } finally { vi.unstubAllGlobals(); }
});

test("visible viewport geometry updates and listeners are removed on dismissal", () => {
  const viewport = new EventTarget();
  Object.assign(viewport, { height: 844, offsetTop: 0 });
  const remove = vi.spyOn(viewport, "removeEventListener");
  vi.stubGlobal("visualViewport", viewport);
  try {
    const result = setup(); const modal = screen.getByRole("dialog");
    expect(modal.style.getPropertyValue("--swap-visual-height")).toBe("844px");
    Object.assign(viewport, { height: 400, offsetTop: 50 });
    act(() => viewport.dispatchEvent(new Event("resize")));
    expect(modal).toHaveAttribute("data-compact-viewport", "true");
    expect(modal.style.getPropertyValue("--swap-visual-top")).toBe("50px");
    Object.assign(viewport, { height: 667 });
    act(() => viewport.dispatchEvent(new Event("scroll")));
    expect(modal).toHaveAttribute("data-compact-viewport", "false");
    result.unmount();
    expect(remove).toHaveBeenCalledWith("resize", expect.any(Function));
    expect(remove).toHaveBeenCalledWith("scroll", expect.any(Function));
  } finally { vi.unstubAllGlobals(); }
});

test.each(["loading", "invalid", "stale"])("%s preview cannot enable Send", async state => {
  const user = userEvent.setup(); setup(); await chooseSource(user);
  const implementation = mocks.preview.getMockImplementation()!;
  mocks.preview.mockImplementation(payload => {
    const query = implementation(payload);
    return { ...query, isFetching: state === "loading", data: query.data ? { ...query.data,
      isValid: state !== "invalid", source: { ...query.data.source, slotId: state === "stale" ? 999 : 100 } } : undefined };
  });
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByRole("button", { name: "Send request" })).toBeDisabled();
});

test("custom source and private Give survive edits with long names in the same modal", async () => {
  const user = userEvent.setup(); const result = setup(); const name = "Aleksandra Maria Kowalska-Wiśniewska";
  result.rerender(<QueryClientProvider client={result.client}><SwapCreateWizard {...result.props}
    employees={[{ ...employees[0], displayName: name }]} /></QueryClientProvider>);
  const modal = screen.getByRole("dialog");
  const sameModal = () => expect(screen.getAllByRole("dialog")).toEqual([modal]);
  await user.click(screen.getByRole("button", { name: /F27/ })); sameModal();
  await user.click(screen.getByRole("radio"));
  await user.click(screen.getByRole("button", { name: "Custom period" }));
  await user.click(screen.getByRole("button", { name: "Increase From" }));
  await user.click(screen.getByRole("button", { name: "Confirm shift" }));
  await user.click(screen.getByRole("button", { name: "Change shift" }));
  expect(screen.getByDisplayValue("09:15")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Confirm shift" }));
  await user.click(screen.getByRole("button", { name: "Continue" })); sameModal();
  await user.click(screen.getByRole("button", { name: "Specific employee" }));
  await user.click(screen.getByRole("button", { name })); sameModal();
  expect(screen.queryByRole("button", { name: "Choose their shift" })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  await user.click(screen.getByRole("button", { name: "Continue" })); sameModal();
  await user.click(screen.getByRole("button", { name: "Edit schedule" }));
  expect(screen.getByText(/09:15–16:30/)).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Continue" }));
  await user.click(screen.getByRole("button", { name: "Continue" }));
  await user.click(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledWith({ requestKind: "give", scheduleId: 10, scheduleSlotId: 100, fromTime: "09:15", toTime: "16:30", targetEmployeeId: 2 });
});

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
  eligibleWindows: [{ fromTime: "09:00", toTime: "18:00" }] };
const schedule: EmployeeSchedule = { id: 10, containerId: 1, containerName: "Main", shopId: 1, shopName: "Shop", name: "F27", year: 2026, month: 10,
  publicationStatus: "public", allowSwap: true, slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 1, status: "ASSIGNED", fromTime: "09:00", toTime: "16:30" }] };
const employees = [{ id: 2, firstName: "Target", lastName: "Worker", displayName: "Target Worker" }, { id: 3, firstName: "Other", lastName: "Worker", displayName: "Other Worker" }];

beforeEach(() => {
  setLanguage("en");
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
  Object.defineProperties(HTMLDialogElement.prototype, {
    showModal: { configurable: true, value(this: HTMLDialogElement) { this.open = true; } },
    close: { configurable: true, value(this: HTMLDialogElement) { this.open = false; } },
  });
  mocks.candidates.mockImplementation(() => ({ data: { items: [candidate, { ...candidate, slotId: 201, canSelect: false, unavailableReason: "Not enough hours." }], hasMore: false }, isFetching: false }));
  mocks.preview.mockImplementation((payload: SwapWizardRequest | null) => ({ data: payload ? {
    requestKind: payload.requestKind, targetEmployeeId: payload.targetEmployeeId, targetEmployeeName: payload.targetEmployeeId ? "Target Worker" : "Everyone",
    source: { ...source, fromTime: payload.fromTime, toTime: payload.toTime }, return: payload.requestKind === "exchange" ? { ...candidate, fromTime: payload.returnFromTime, toTime: payload.returnToTime } : null,
    isValid: true, projectedMonths: [{ year: 2026, month: 10, hoursBefore: 7.5, hoursAfter: payload.requestKind === "exchange" ? 7.5 : 0, workDaysBefore: 1, workDaysAfter: 1, freeDaysBefore: 30, freeDaysAfter: 30 }],
  } satisfies ShiftSwapOfferPreview : undefined, isFetching: false }));
});
test("locks independent app scrollers and restores inline styles and positions on close", () => {
  const app = document.createElement("div"); app.id = "root";
  const scroller = document.createElement("div");
  scroller.style.setProperty("overflow-y", "auto", "important");
  scroller.style.overscrollBehavior = "contain";
  scroller.scrollTop = 80; scroller.scrollLeft = 12;
  app.append(scroller); document.body.append(app);
  const originalBody = document.body.style.cssText;
  const originalHtml = document.documentElement.style.cssText;
  const originalScroller = scroller.style.cssText;
  vi.stubGlobal("scrollY", 120);
  try {
    const result = setup();
    expect(scroller.style.overflowY).toBe("hidden");
    expect(scroller.style.overscrollBehavior).toBe("none");
    expect(document.body.style.position).toBe("fixed");
    expect(document.body.style.top).toBe("-120px");
    expect(scroller.scrollTop).toBe(80);
    expect(scroller.scrollLeft).toBe(12);
    result.unmount();
    expect(scroller.style.cssText).toBe(originalScroller);
    expect(document.body.style.cssText).toBe(originalBody);
    expect(document.documentElement.style.cssText).toBe(originalHtml);
    expect(scroller.scrollTop).toBe(80);
    expect(scroller.scrollLeft).toBe(12);
    expect(window.scrollTo).toHaveBeenCalledWith({ left: window.scrollX, top: window.scrollY, behavior: "instant" });
  } finally { app.remove(); vi.unstubAllGlobals(); }
});
function setup() {
  const onSubmit = vi.fn(); const onClose = vi.fn();
  const props = { schedules: [schedule], employees, employeeId: 1, employeeName: "Owner Worker", swaps: [], busy: false, onSubmit, onClose };
  const client = new QueryClient();
  const result = render(<QueryClientProvider client={client}><SwapCreateWizard {...props} /></QueryClientProvider>);
  return { ...result, onSubmit, onClose, props, client };
}

test("keyboard users can choose another row when the saved return becomes unavailable", async () => {
  const unavailable = { ...candidate, canSelect: false, unavailableReason: "Shift unavailable." };
  const available = { ...candidate, slotId: 202, dayOfMonth: 4 };
  render(<SwapShiftPickerDialog shifts={[unavailable, available]} selection={{ shift: candidate, period: { fromTime: "09:00", toTime: "16:30" } }}
    returnMode onBack={() => {}} onConfirm={() => {}} />);
  screen.getByRole("heading", { name: "Choose their shift" }).focus();
  await userEvent.setup().tab();
  expect(screen.getByRole("radio", { name: /04.10.2026/ })).toHaveFocus();
});

test("restores the explicit launcher even when it was not focused before opening", async () => {
  function FocusHarness() {
    const launcher = useRef<HTMLButtonElement>(null);
    const [open, setOpen] = useState(true);
    return <><button ref={launcher}>Launch swap</button>{open && <SwapCreateWizard schedules={[schedule]} employees={employees}
      employeeId={1} employeeName="Owner Worker" swaps={[]} busy={false} onSubmit={() => {}} onClose={() => setOpen(false)} returnFocusRef={launcher} />}</>;
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

test.each([
  ["early-half", "09:00", "12:45", null],
  ["late-half", "12:45", "16:30", 2],
  ["custom-minute", "12:01", "12:02", 2],
] as const)("%s Give submits exact periods and targeting once", async (_name, fromTime, toTime, targetEmployeeId) => {
  const user = userEvent.setup(); const result = setup();
  await user.click(screen.getByRole("button", { name: /F27/ }));
  await user.click(screen.getByRole("radio"));
  await user.click(screen.getByRole("button", { name: "Custom period" }));
  fireEvent.change(screen.getByDisplayValue("09:00"), { target: { value: fromTime } });
  fireEvent.change(screen.getByDisplayValue("16:30"), { target: { value: toTime } });
  await user.click(screen.getByRole("button", { name: "Confirm shift" }));
  await user.click(screen.getByRole("button", { name: "Continue" }));
  if (targetEmployeeId) {
    await user.click(screen.getByRole("button", { name: "Specific employee" }));
    await user.click(screen.getByRole("button", { name: "Target Worker" }));
    await user.click(screen.getByRole("button", { name: "Save selection" }));
  }
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByRole("region", { name: "Your shift (you give)" })).toHaveTextContent(`${fromTime} – ${toTime}`);
  await user.dblClick(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledTimes(1);
  expect(result.onSubmit).toHaveBeenCalledWith({ requestKind: "give", scheduleId: 10, scheduleSlotId: 100, fromTime, toTime, targetEmployeeId });
});

test("public Give has three steps, authoritative review and submits once", async () => {
  const user = userEvent.setup(); const result = setup();
  expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
  await chooseSource(user);
  expect(screen.queryByRole("group", { name: "Employees" })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByText("After sending, eligible employees will be able to view and accept your offer.")).toBeInTheDocument();
  expect(screen.queryByText("Your statistics")).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Reciprocal exchange")).not.toBeInTheDocument();
  expect(screen.queryByRole("region", { name: "Employee shift (they give)" })).not.toBeInTheDocument();
  const back = screen.getByRole("button", { name: "Back", exact: true });
  expect(back.nextElementSibling).toHaveAttribute("aria-label", "Request progress");
  await user.click(back);
  expect(screen.getByRole("heading", { name: "Give to whom?" })).toHaveFocus();
  await user.click(screen.getByRole("button", { name: "Continue" }));
  await user.dblClick(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledTimes(1);
  expect(result.onSubmit).toHaveBeenCalledWith({ requestKind: "give", scheduleId: 10, scheduleSlotId: 100, fromTime: "09:00", toTime: "16:30", targetEmployeeId: null });
});

test("specific Exchange edits return bounds independently in a single modal", async () => {
  const user = userEvent.setup(); const result = setup(); const modal = screen.getByRole("dialog"); await chooseSource(user);
  await user.click(screen.getByRole("button", { name: "Specific employee" }));
  expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
  await user.type(screen.getByRole("searchbox"), "Target");
  expect(screen.queryByRole("button", { name: /Other Worker/ })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: /Target Worker/ }));
  expect(screen.getAllByRole("dialog")).toEqual([modal]);
  await user.click(screen.getByRole("radio", { name: "Swap shifts" }));
  expect(screen.getByRole("button", { name: "Save selection" })).toBeDisabled();
  await user.click(screen.getByRole("button", { name: "Choose their shift" }));
  await user.click(screen.getByRole("button", { name: /F35/ }));
  expect(screen.getAllByRole("dialog")).toHaveLength(1);
  expect(screen.getByRole("dialog")).toBe(modal);
  const disabled = screen.getByRole("radio", { name: /Not enough hours/ }); expect(disabled).toBeDisabled();
  await user.click(screen.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/, exact: false }));
  expect(screen.getByRole("button", { name: "Full shift" })).toBeDisabled();
  await user.click(screen.getByRole("button", { name: "Increase From" }));
  expect(screen.getByDisplayValue("16:30")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Confirm selection" }));
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByText("Employee shift (they give)")).toBeInTheDocument();
  expect(screen.getByLabelText("Reciprocal exchange")).toBeInTheDocument();
  expect(screen.queryByText("Your statistics")).not.toBeInTheDocument();
  expect(screen.getAllByRole("dialog")).toEqual([modal]);
  expect(result.onSubmit).not.toHaveBeenCalled();
  await user.click(screen.getByRole("button", { name: "Back", exact: true }));
  expect(screen.getByRole("button", { name: /Target Worker/ })).toHaveTextContent("Swap shifts");
  await user.click(screen.getByRole("button", { name: "Continue" }));
  expect(screen.getByLabelText("Reciprocal exchange")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Send request" }));
  expect(result.onSubmit).toHaveBeenCalledWith(expect.objectContaining({ requestKind: "exchange", targetEmployeeId: 2, returnScheduleId: 20, returnScheduleSlotId: 200, returnFromTime: "09:15", returnToTime: "16:30" }));
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
  expect(screen.getByRole("button", { name: "Back", exact: true })).toBeDisabled();
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
  await user.click(screen.getByRole("button", { name: "Back" }));
  await user.click(screen.getByRole("button", { name: /Other Worker/ }));
  expect(screen.getByRole("radio", { name: "Give shift" })).toHaveAttribute("aria-checked", "true");
  expect(screen.queryByRole("button", { name: "Choose their shift" })).not.toBeInTheDocument();
  expect(within(screen.getByRole("dialog")).getByRole("button", { name: "Save selection" })).toBeEnabled();
});

test("Review omits projected statistics without changing preview or Send gating", async () => {
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
  expect(screen.queryByText("Your statistics")).not.toBeInTheDocument();
  expect(screen.queryByText("Estimated, pending acceptance")).not.toBeInTheDocument();
  expect(screen.queryByText("298.48 → 293.48")).not.toBeInTheDocument();
  expect(screen.queryByText("View details")).not.toBeInTheDocument();
  expect(screen.getByRole("region", { name: "Your shift (you give)" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Send request" })).toBeEnabled();
  expect(result.onSubmit).not.toHaveBeenCalled();
});


test("return schedules isolate shifts, preserve confirmed choices and reset incompatible drafts", async () => {
  const user = userEvent.setup(); setup();
  const first = { ...candidate, fromTime: "09:00", toTime: "15:00", eligibleWindows: [{ fromTime: "09:00", toTime: "15:00" }] };
  const second = { ...candidate, scheduleId: 30, scheduleName: "F99", slotId: 300, month: 11, dayOfMonth: 5,
    eligibleWindows: [{ fromTime: "08:00", toTime: "18:00" }] };
  mocks.candidates.mockReturnValue({ data: { items: [first, second], hasMore: false }, isFetching: false });
  await chooseSource(user);
  await user.click(screen.getByRole("button", { name: "Specific employee" }));
  const list = screen.getByRole("group", { name: "Employees" });
  expect(within(list).getAllByRole("button")[0]).toHaveTextContent("Other Worker");
  await user.click(screen.getByRole("button", { name: "Target Worker" }));
  await user.click(screen.getByRole("radio", { name: "Swap shifts" }));
  await user.click(screen.getByRole("button", { name: "Choose their shift" }));
  await user.click(screen.getByRole("button", { name: /F35/ }));
  expect(screen.getAllByRole("radio")).toHaveLength(1);
  await user.click(screen.getByRole("radio"));
  expect(screen.getByRole("button", { name: "Full shift" })).toBeEnabled();
  await user.click(screen.getByRole("button", { name: "Confirm selection" }));
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  expect(within(screen.getByRole("group", { name: "Employees" })).getAllByRole("button")[0]).toHaveTextContent("Target WorkerSwap shifts");
  await user.click(screen.getByRole("button", { name: /Target Worker/ }));
  await user.click(screen.getByRole("button", { name: "Change" }));
  expect(screen.getByRole("button", { name: /F35/ })).toHaveAttribute("aria-pressed", "true");
  await user.click(screen.getByRole("button", { name: /F35/ }));
  expect(screen.getByRole("radio")).toHaveAttribute("aria-checked", "true");
  await user.click(screen.getByRole("button", { name: "Back" }));
  await user.click(screen.getByRole("button", { name: /F99/ }));
  expect(screen.getByRole("radio")).toHaveAttribute("aria-checked", "false");
  expect(screen.getByRole("button", { name: "Confirm selection" })).toBeDisabled();
  await user.click(screen.getByRole("radio"));
  await user.click(screen.getByRole("button", { name: "Custom period" }));
  await user.click(screen.getByRole("button", { name: "Increase From" }));
  expect(screen.getByDisplayValue("18:00")).not.toHaveAttribute("readonly");
  await user.click(screen.getByRole("button", { name: "Decrease To" }));
  expect(screen.getByDisplayValue("08:15")).toBeInTheDocument();
  expect(screen.getByDisplayValue("17:45")).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Confirm selection" }));
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  await user.type(screen.getByRole("searchbox"), "Other");
  expect(screen.queryByRole("button", { name: /Target Worker/ })).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: /Other Worker/ }));
  expect(screen.getByRole("radio", { name: "Give shift" })).toHaveAttribute("aria-checked", "true");
  await user.click(screen.getByRole("button", { name: "Save selection" }));
  await user.click(screen.getByRole("button", { name: "Clear search" }));
  expect(within(screen.getByRole("group", { name: "Employees" })).getAllByRole("button")[0]).toHaveTextContent("Other WorkerGive shift");
});

test("a same-day return must remove the target's overlap with the incoming period", async () => {
  const user = userEvent.setup();
  render(<SwapShiftPickerDialog returnMode outgoing={{ shift: source, period: { fromTime: "09:00", toTime: "16:30" } }}
    shifts={[{ ...candidate, dayOfMonth: source.dayOfMonth, eligibleWindows: [{ fromTime: "08:00", toTime: "18:00" }] }]}
    onConfirm={() => {}} onBack={() => {}} />);
  await user.click(screen.getByRole("radio"));
  await user.click(screen.getByRole("button", { name: "Custom period" }));
  fireEvent.change(screen.getByDisplayValue("08:00"), { target: { value: "10:00" } });
  expect(screen.getByRole("button", { name: "Confirm selection" })).toBeDisabled();
});


test.each(["en", "pl"] as const)("contextual names and Back/Close semantics are localized: %s", async language => {
  setLanguage(language); const user = userEvent.setup(); const result = setup();
  const pl = language === "pl";
  const close = pl ? "Zamknij" : "Close", back = pl ? "Wstecz" : "Back";
  const owner = screen.getByText("Owner Worker", { selector: "strong" });
  expect(owner).toHaveStyle({ color: "#246bff" });
  expect(owner.closest("p")).toHaveTextContent(pl ? "Wybierz grafik pracownika: Owner Worker." : "Select a schedule for Owner Worker.");
  expect(screen.getByRole("button", { name: close })).toHaveTextContent("×");
  await user.click(screen.getByRole("button", { name: /F27/ }));
  expect(screen.getByRole("heading", { name: pl ? "Wybierz swoją zmianę" : "Select your shift" })).toHaveFocus();
  expect(screen.getByText("Owner Worker", { selector: "strong" }).closest("p")).toHaveTextContent(pl ? "Wybierz zmianę z grafiku pracownika: Owner Worker." : "Choose a shift from Owner Worker's schedule.");
  expect(screen.queryByRole("button", { name: close })).not.toBeInTheDocument();
  expect(screen.getAllByRole("button", { name: back })).toHaveLength(1);
  expect(screen.getByRole("button", { name: back }).querySelector("svg")).toBeInTheDocument();
  await user.click(screen.getByRole("radio"));
  await user.click(screen.getByRole("button", { name: pl ? "Potwierdź zmianę" : "Confirm shift" }));
  await user.click(screen.getByRole("button", { name: pl ? "Kontynuuj" : "Continue" }));
  await user.click(screen.getByRole("button", { name: pl ? "Wybrany pracownik" : "Specific employee" }));
  await user.click(screen.getByRole("button", { name: /Target Worker/ }));
  await user.click(screen.getByRole("radio", { name: pl ? "Wymień zmiany" : "Swap shifts" }));
  await user.click(screen.getByRole("button", { name: pl ? "Wybierz zmianę tej osoby" : "Choose their shift" }));
  const scheduleHeading = pl ? "Wybierz grafik tej osoby" : "Choose their schedule";
  expect(screen.getByRole("heading", { name: scheduleHeading })).toHaveFocus();
  const other = screen.getByText("Target Worker", { selector: "strong" });
  expect(other.closest("p")).toHaveTextContent(pl ? "Wybierz grafik pracownika: Target Worker." : "Select a schedule for Target Worker.");
  expect(other.style.color).toBe("");
  await user.click(screen.getByRole("button", { name: /F35/ }));
  expect(screen.getByText("Target Worker", { selector: "strong" }).closest("p")).toHaveTextContent(pl ? "Wybierz zmianę z grafiku pracownika: Target Worker, aby zamienić się zmianami." : "Select a shift from Target Worker's schedule to swap with yours.");
  expect(screen.getAllByRole("button", { name: back })).toHaveLength(1);
  fireEvent(screen.getByRole("dialog"), new Event("cancel", { bubbles: true, cancelable: true }));
  expect(screen.getByRole("heading", { name: scheduleHeading })).toHaveFocus();
  await user.click(screen.getByRole("button", { name: back }));
  expect(screen.getByRole("heading", { name: pl ? "Rodzaj prośby" : "Request type" })).toHaveFocus();
  await user.click(screen.getByRole("button", { name: back }));
  expect(result.onClose).not.toHaveBeenCalled();
  await user.click(screen.getByRole("button", { name: close }));
  expect(result.onClose).toHaveBeenCalledOnce();
});
