import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { EmployeeShiftCorrectionDialog } from "./EmployeeShiftCorrectionDialog";

beforeEnglishTest(() => setTestLanguage("en"));

const schedule: EmployeeSchedule = {
  id: 9,
  containerId: 2,
  containerName: "Main",
  shopId: 3,
  shopName: "Central",
  name: "August",
  year: 2026,
  month: 8,
  publicationStatus: "public",
  slots: [
    { id: 21, dayOfMonth: 12, slotNo: 1, employeeId: 7, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" },
    { id: 22, dayOfMonth: 12, slotNo: 2, employeeId: 7, fromTime: "15:00", toTime: "19:00", status: "ASSIGNED" },
  ],
};

describe("EmployeeShiftCorrectionDialog", () => {
  test.each([[2026, 2, 28], [2028, 2, 29], [2026, 4, 30], [2026, 12, 31]])(
    "lists every day for %s-%s (%s days) and defaults to the first workday", (year, month, count) => {
      render(<EmployeeShiftCorrectionDialog open schedule={{ ...schedule, year, month }} employeeId={7}
        existingRequests={[]} isSending={false} onCancel={vi.fn()} onSubmit={vi.fn()} />);
      const tabs = within(screen.getByRole("tablist", { name: "Schedule days" })).getAllByRole("tab");
      expect(tabs).toHaveLength(count);
      expect(tabs[11]).toHaveAttribute("aria-selected", "true");
    },
  );

  test("requests a day-off shift only after two valid exact times, without a source slot", async () => {
    const onSubmit = vi.fn();
    render(<EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
      isSending={false} onCancel={vi.fn()} onSubmit={onSubmit} />);
    await userEvent.click(screen.getAllByRole("tab")[0]);
    expect(screen.getByText("Day off")).toBeVisible();
    expect(screen.getByText("Request a shift on this day")).toBeVisible();
    const from = screen.getByLabelText("New start time"); const to = screen.getByLabelText("New end time");
    const send = screen.getByRole("button", { name: "Send request" });
    expect(from).toHaveValue(""); expect(to).toHaveValue(""); expect(send).toBeDisabled();
    fireEvent.change(from, { target: { value: "08:15" } }); expect(send).toBeDisabled();
    fireEvent.change(to, { target: { value: "08:15" } }); expect(send).toBeDisabled();
    fireEvent.change(to, { target: { value: "07:00" } }); expect(send).toBeDisabled();
    fireEvent.change(to, { target: { value: "16:45" } }); expect(send).toBeEnabled();
    expect(screen.getByText("Day off → 08:15–16:45")).toBeVisible();
    await userEvent.click(send);
    expect(onSubmit).toHaveBeenCalledWith({ scheduleId: 9, dayOfMonth: 1, requestedFromTime: "08:15", requestedToTime: "16:45" });
    expect(onSubmit.mock.calls[0][0]).not.toHaveProperty("scheduleSlotId");
    await userEvent.click(screen.getAllByRole("tab")[1]);
    expect(screen.getByLabelText("New start time")).toHaveValue("");
    expect(screen.getByLabelText("New end time")).toHaveValue("");
  });

  test.each(["pending", "unavailable", "sending"])("blocks day-off resubmission when %s", async reason => {
    render(<EmployeeShiftCorrectionDialog open schedule={{ ...schedule, slots: [] }} employeeId={7}
      unavailableDays={reason === "unavailable" ? new Set([1]) : new Set()}
      existingRequests={reason === "pending" ? [{ id: 1, containerId: 2, scheduleId: 9, scheduleSlotId: null,
        isDayOffRequest: true, scheduleName: "August", shopName: "Central", year: 2026, month: 8,
        dayOfMonth: 1, employeeId: 7, employeeName: "Zoe", originalFromTime: null, originalToTime: null,
        requestedFromTime: "08:00", requestedToTime: "16:00", status: "pending", createdAtUtc: "2026-08-01T00:00:00Z" }] : []}
      isSending={reason === "sending"} onCancel={vi.fn()} onSubmit={vi.fn()} />);
    expect(screen.getAllByRole("tab")[0]).toHaveAttribute("aria-selected", "true");
    expect(screen.getByLabelText("New start time")).toBeDisabled();
    expect(screen.getByRole("button", { name: reason === "sending" ? "Sending..." : "Send request" })).toBeDisabled();
  });

  test.each(["en", "pl", "uk"] as const)("localizes day-off controls in %s", language => {
    setTestLanguage(language);
    render(<EmployeeShiftCorrectionDialog open schedule={{ ...schedule, slots: [] }} employeeId={7}
      existingRequests={[]} isSending={false} onCancel={vi.fn()} onSubmit={vi.fn()} />);
    expect(screen.getByText(language === "en" ? "Request a shift on this day" : language === "pl" ? "Poproś o zmianę w tym dniu" : "Подати запит на зміну в цей день")).toBeVisible();
  });

  test("opens the native modal and closes it on teardown", () => {
    const { unmount } = render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
        isSending={false} onCancel={() => undefined} onSubmit={() => undefined} />,
    );
    const dialog = screen.getByRole("dialog") as HTMLDialogElement;
    expect(dialog.open).toBe(true);
    unmount();
    expect(dialog.open).toBe(false);
  });

  test("shows split shifts and sends the selected boundary adjustment", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
        isSending={false} onCancel={() => undefined} onSubmit={onSubmit} />,
    );

    expect(screen.getByText("08:00 – 12:00")).toBeVisible();
    expect(screen.getByText("15:00 – 19:00")).toBeVisible();

    expect(screen.getByText("Choose a day")).toBeVisible();
    expect(screen.getByText("Choose what to change")).toBeVisible();
    expect(screen.getByText("Set the new time")).toBeVisible();

    const firstShift = screen.getByRole("group", { name: "Change 08:00 to 12:00" });
    await user.click(within(firstShift).getByRole("button", { name: "Change end time from 12:00" }));
    await user.click(screen.getByRole("button", { name: "Move selected time 30 minutes later" }));
    expect(screen.getByText("08:00 – 12:30")).toBeVisible();
    expect(screen.getByText("End time: 30 min later")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Send request" }));

    expect(onSubmit).toHaveBeenCalledWith({
      scheduleId: 9,
      scheduleSlotId: 21,
      requestedFromTime: "08:00",
      requestedToTime: "12:30",
    });
  });

  test("disables a shift that already has a pending request", () => {
    render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} isSending={false}
        existingRequests={[{
          id: 1, containerId: 2, scheduleId: 9, scheduleSlotId: 21, isDayOffRequest: false, scheduleName: "August", shopName: "Central",
          year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Zoe Young",
          originalFromTime: "08:00", originalToTime: "12:00", requestedFromTime: "08:00", requestedToTime: "12:30",
          status: "pending", createdAtUtc: "2026-08-19T12:00:00Z",
        }]}
        onCancel={() => undefined} onSubmit={() => undefined} />,
    );

    const pendingShift = screen.getByRole("group", { name: "Change 08:00 to 12:00" });
    expect(within(pendingShift).getByRole("button", { name: "Change start time from 08:00" })).toBeDisabled();
    expect(screen.getByText("Pending approval")).toBeVisible();
  });

  test("accepts an exact requested time", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
        isSending={false} onCancel={() => undefined} onSubmit={onSubmit} />,
    );

    const secondShift = screen.getByRole("group", { name: "Change 15:00 to 19:00" });
    await user.click(within(secondShift).getByRole("button", { name: "Change end time from 19:00" }));
    await user.clear(screen.getByLabelText("New end time"));
    await user.type(screen.getByLabelText("New end time"), "23:00");
    await user.click(screen.getByRole("button", { name: "Send request" }));

    expect(onSubmit).toHaveBeenCalledWith({
      scheduleId: 9,
      scheduleSlotId: 22,
      requestedFromTime: "15:00",
      requestedToTime: "23:00",
    });
  });
});
