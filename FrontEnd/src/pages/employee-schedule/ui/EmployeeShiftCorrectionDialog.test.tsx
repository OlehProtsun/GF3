import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { ComponentProps } from "react";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftCorrectionRequest } from "@entities/shift-corrections";
import { setLanguage, t } from "@shared/i18n";
import { EmployeeShiftCorrectionDialog } from "./EmployeeShiftCorrectionDialog";

beforeEach(() => setLanguage("en"));

const schedule: EmployeeSchedule = {
  id: 9, containerId: 2, containerName: "Main", shopId: 3, shopName: "Central",
  name: "August", year: 2026, month: 8, publicationStatus: "public",
  slots: [
    { id: 21, dayOfMonth: 12, slotNo: 1, employeeId: 7, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" },
    { id: 22, dayOfMonth: 12, slotNo: 2, employeeId: 7, fromTime: "15:00", toTime: "19:00", status: "ASSIGNED" },
  ],
};

const pending = (overrides: Partial<ShiftCorrectionRequest> = {}): ShiftCorrectionRequest => ({
  id: 1, containerId: 2, scheduleId: 9, scheduleSlotId: 21, isDayOffRequest: false,
  scheduleName: "August", shopName: "Central", year: 2026, month: 8, dayOfMonth: 12,
  employeeId: 7, employeeName: "Zoe", originalFromTime: "08:00", originalToTime: "12:00",
  requestedFromTime: "08:00", requestedToTime: "12:30", comment: null, status: "pending",
  createdAtUtc: "2026-08-01T00:00:00Z", ...overrides,
});

function setup(overrides: Partial<ComponentProps<typeof EmployeeShiftCorrectionDialog>> = {}) {
  const props = {
    open: true, schedules: [schedule], employeeId: 7, existingRequests: [], activeSwaps: [], isSending: false,
    onCancel: vi.fn(), onSubmit: vi.fn(), ...overrides,
  };
  return { ...render(<EmployeeShiftCorrectionDialog {...props} />), props, user: userEvent.setup() };
}

const next = () => screen.getByRole("button", { name: t("Continue") });
const send = () => screen.getByRole("button", { name: "Send request" });
const dialog = () => screen.getByRole("dialog");
const dayChoices = () => within(screen.getByRole("radiogroup")).getAllByRole("radio");

async function chooseSchedule(user: ReturnType<typeof userEvent.setup>, name = /^August/) {
  await user.click(screen.getByRole("button", { name }));
  await user.click(next());
}

async function chooseShift(user: ReturnType<typeof userEvent.setup>, name = /08:00 – 12:00/) {
  await user.click(screen.getByRole("radio", { name }));
  await user.click(next());
}

async function review(user: ReturnType<typeof userEvent.setup>) {
  await user.click(next());
  expect(screen.getByRole("heading", { name: "Add a comment" })).toBeVisible();
  await user.click(next());
  expect(screen.getByRole("heading", { name: "Review your request" })).toBeVisible();
}

describe("EmployeeShiftCorrectionDialog", () => {
  test.each([[2026, 2, 28], [2028, 2, 29], [2026, 4, 30], [2026, 12, 31]])(
    "lists every day for %s-%s (%s days) and presents each split shift for selection", async (year, month, count) => {
      const { user } = setup({ schedules: [{ ...schedule, year, month }] });
      await chooseSchedule(user);
      expect(dayChoices()).toHaveLength(count + 1);
      expect(next()).toBeDisabled();
      await user.click(screen.getByRole("radio", { name: /08:00 – 12:00/ }));
      expect(screen.getByRole("radio", { name: /08:00 – 12:00/ })).toBeChecked();
      expect(next()).toBeEnabled();
    },
  );

  test("requests a day-off shift only after two valid exact times, without a source slot", async () => {
    const { props, user } = setup();
    await chooseSchedule(user);
    const firstDay = dayChoices()[0];
    expect(firstDay).toHaveTextContent("Day off");
    expect(firstDay).toHaveTextContent("Request a shift on this day");
    await user.click(firstDay);
    await user.click(next());
    const start = screen.getByLabelText("Start time");
    const end = screen.getByLabelText("End time");
    expect(start).toHaveValue("");
    expect(end).toHaveValue("");
    expect(next()).toBeDisabled();
    fireEvent.change(start, { target: { value: "08:15" } });
    expect(next()).toBeDisabled();
    fireEvent.change(end, { target: { value: "08:15" } });
    expect(next()).toBeDisabled();
    fireEvent.change(end, { target: { value: "07:00" } });
    expect(next()).toBeDisabled();
    fireEvent.change(end, { target: { value: "16:45" } });
    expect(next()).toBeEnabled();
    await review(user);
    expect(screen.getByText("Request shift on a day off")).toBeVisible();
    expect(screen.getByText("08:15 – 16:45")).toBeVisible();
    await user.click(send());
    expect(props.onSubmit).toHaveBeenCalledWith({ scheduleId: 9, dayOfMonth: 1, requestedFromTime: "08:15", requestedToTime: "16:45" });
    expect(vi.mocked(props.onSubmit).mock.calls[0][0]).not.toHaveProperty("scheduleSlotId");
    for (let i = 0; i < 3; i++) await user.click(screen.getAllByRole("button", { name: "Back" }).at(-1)!);
    await user.click(dayChoices()[1]);
    await user.click(next());
    expect(screen.getByLabelText("Start time")).toHaveValue("");
    expect(screen.getByLabelText("End time")).toHaveValue("");
  });

  test.each(["pending", "unavailable"])("blocks day-off resubmission when %s", async reason => {
    const own = { ...schedule, slots: [] };
    const other = { ...schedule, id: 10, name: "Other", slots: [{ ...schedule.slots[0], dayOfMonth: 1 }] };
    const { user } = setup({
      schedules: reason === "unavailable" ? [own, other] : [own],
      existingRequests: reason === "pending" ? [pending({ dayOfMonth: 1, scheduleSlotId: null,
        isDayOffRequest: true, originalFromTime: null, originalToTime: null })] : [],
    });
    await chooseSchedule(user);
    const blocked = dayChoices()[0];
    expect(blocked).toBeDisabled();
    expect(blocked).toHaveTextContent(reason === "pending" ? "Pending approval" : "Already working on another schedule (Other)");
    expect(next()).toBeDisabled();
  });

  test("blocks duplicate send and cancellation while the request is sending", async () => {
    const { props, rerender, user } = setup();
    await chooseSchedule(user);
    await user.click(dayChoices()[0]);
    await user.click(next());
    fireEvent.change(screen.getByLabelText("Start time"), { target: { value: "08:00" } });
    fireEvent.change(screen.getByLabelText("End time"), { target: { value: "16:00" } });
    await review(user);
    await user.click(send());
    rerender(<EmployeeShiftCorrectionDialog {...props} isSending />);
    expect(screen.getByRole("button", { name: "Sending..." })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Close shift correction dialog" })).toBeDisabled();
    expect(props.onSubmit).toHaveBeenCalledTimes(1);
    expect(props.onCancel).not.toHaveBeenCalled();
  });

  test.each(["en", "pl", "uk"] as const)("localizes day-off controls in %s", async language => {
    setLanguage(language);
    const { user } = setup({ schedules: [{ ...schedule, slots: [] }] });
    await chooseSchedule(user);
    expect(dayChoices()[0]).toHaveTextContent(language === "en" ? "Request a shift on this day" : language === "pl"
      ? "Poproś o zmianę w tym dniu" : "Подати запит на зміну в цей день");
  });

  test("opens the native modal and closes it on teardown", () => {
    const { unmount } = setup();
    const element = dialog() as HTMLDialogElement;
    expect(element.open).toBe(true);
    unmount();
    expect(element.open).toBe(false);
  });

  test("shows split shifts and sends the selected boundary adjustment", async () => {
    const { props, user } = setup();
    expect(screen.getByRole("heading", { name: "Choose a schedule" })).toBeVisible();
    await chooseSchedule(user);
    expect(screen.getByRole("heading", { name: "Choose a day" })).toBeVisible();
    expect(screen.getByRole("radio", { name: /08:00 – 12:00/ })).toBeVisible();
    expect(screen.getByRole("radio", { name: /15:00 – 19:00/ })).toBeVisible();
    await chooseShift(user);
    expect(screen.getByRole("heading", { name: "Adjust shift time" })).toBeVisible();
    const later = screen.getByRole("button", { name: /End time · Move selected time 15 minutes later/ });
    await user.click(later);
    await user.click(later);
    expect(screen.getByText("08:00 – 12:30")).toBeVisible();
    await review(user);
    await user.click(send());
    expect(props.onSubmit).toHaveBeenCalledWith({ scheduleId: 9, scheduleSlotId: 21, requestedFromTime: "08:00", requestedToTime: "12:30" });
  });

  test("retains the optional draft after a failed send and clears it when reopened", async () => {
    const { props, rerender, user } = setup();
    const draft = "  Pierwszy wiersz\nДругий рядок 🌙  ";
    const comment = () => screen.getByRole("textbox", { name: "Comment (optional)" });
    const enterTime = async () => {
      await chooseSchedule(user);
      await chooseShift(user);
      fireEvent.change(screen.getByLabelText("End time"), { target: { value: "12:30" } });
      await user.click(next());
    };
    await enterTime();
    expect(comment()).toHaveAttribute("maxLength", "1000");
    fireEvent.change(comment(), { target: { value: draft } });
    expect(screen.getByText(`${draft.length} / 1000 characters`)).toBeVisible();
    await user.click(next());
    expect(screen.getByText(/Pierwszy wiersz/)).toBeVisible();
    await user.click(send());
    rerender(<EmployeeShiftCorrectionDialog {...props} errorMessage="Network unavailable." />);
    expect(screen.getByRole("alert")).toHaveTextContent("Network unavailable.");
    expect(screen.getByText(/Pierwszy wiersz/)).toBeVisible();
    await user.click(send());
    expect(props.onSubmit).toHaveBeenCalledTimes(2);
    expect(props.onSubmit).toHaveBeenLastCalledWith({ scheduleId: 9, scheduleSlotId: 21, requestedFromTime: "08:00",
      requestedToTime: "12:30", comment: draft.trim() });
    await user.click(screen.getByRole("button", { name: "Close shift correction dialog" }));
    expect(props.onCancel).toHaveBeenCalledOnce();
    rerender(<EmployeeShiftCorrectionDialog {...props} open={false} />);
    rerender(<EmployeeShiftCorrectionDialog {...props} />);
    await enterTime();
    expect(comment()).toHaveValue("");
  });

  test("disables a shift that already has a pending request", async () => {
    const { user } = setup({ existingRequests: [pending()] });
    await chooseSchedule(user);
    const blocked = screen.getByRole("radio", { name: /08:00 – 12:00/ });
    expect(blocked).toBeDisabled();
    expect(blocked).toHaveTextContent("Pending approval");
    await chooseShift(user, /15:00 – 19:00/);
    expect(screen.getByRole("button", { name: /Request day off/ })).toBeDisabled();
  });

  test("accepts an exact requested time", async () => {
    const { props, user } = setup();
    await chooseSchedule(user);
    await chooseShift(user, /15:00 – 19:00/);
    fireEvent.change(screen.getByLabelText("End time"), { target: { value: "23:00" } });
    await review(user);
    await user.click(send());
    expect(props.onSubmit).toHaveBeenCalledWith({ scheduleId: 9, scheduleSlotId: 22, requestedFromTime: "15:00", requestedToTime: "23:00" });
  });

  test.each([1, 2])("removes %s shifts using only schedule and day, with pending review", async count => {
    const { props, rerender, user } = setup({ schedules: [{ ...schedule, slots: schedule.slots.slice(0, count) }] });
    await chooseSchedule(user);
    await chooseShift(user);
    await user.click(screen.getByRole("button", { name: /Request day off/ }));
    expect(screen.queryByLabelText("End time")).toBeNull();
    expect(screen.getByText("This request requires manager approval.")).toBeVisible();
    await review(user);
    expect(screen.getByText("Request day off")).toBeVisible();
    expect(screen.getByText(`Duration change · −${4 * count}h`)).toBeVisible();
    await user.click(send());
    expect(props.onSubmit).toHaveBeenCalledWith({ scheduleId: 9, requestType: "removeShift", dayOfMonth: 12 });
    rerender(<EmployeeShiftCorrectionDialog {...props} submitted />);
    expect(screen.getByText("Pending approval")).toBeVisible();
  });

  test("blocks removal for an open return-side swap but leaves time adjustment available", async () => {
    const { user } = setup({ activeSwaps: [{ status: "open", scheduleSlotId: 100, returnScheduleSlotId: 22 }] });
    await chooseSchedule(user);
    await chooseShift(user);
    expect(screen.getByRole("button", { name: /Request day off/ })).toBeDisabled();
    fireEvent.change(screen.getByLabelText("End time"), { target: { value: "12:30" } });
    expect(next()).toBeEnabled();
  });
});
