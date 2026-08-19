import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { EmployeeShiftCorrectionDialog } from "./EmployeeShiftCorrectionDialog";

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
  test("shows split shifts and sends the selected boundary adjustment", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
        isSending={false} onCancel={() => undefined} onSubmit={onSubmit} />,
    );

    expect(screen.getByText("08:00 – 12:00")).toBeVisible();
    expect(screen.getByText("15:00 – 19:00")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Adjust end of 08:00 to 12:00" }));
    await user.click(screen.getByRole("button", { name: "+30m" }));
    expect(screen.getAllByText("08:00 – 12:30")).toHaveLength(2);
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
          id: 1, containerId: 2, scheduleId: 9, scheduleSlotId: 21, scheduleName: "August", shopName: "Central",
          year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Zoe Young",
          originalFromTime: "08:00", originalToTime: "12:00", requestedFromTime: "08:00", requestedToTime: "12:30",
          status: "pending", createdAtUtc: "2026-08-19T12:00:00Z",
        }]}
        onCancel={() => undefined} onSubmit={() => undefined} />,
    );

    expect(screen.getByRole("button", { name: "Adjust start of 08:00 to 12:00" })).toBeDisabled();
    expect(screen.getByText("Request pending")).toBeVisible();
  });

  test("expands the time scale when an adjusted boundary moves beyond the initial range", async () => {
    const user = userEvent.setup();
    render(
      <EmployeeShiftCorrectionDialog open schedule={schedule} employeeId={7} existingRequests={[]}
        isSending={false} onCancel={() => undefined} onSubmit={() => undefined} />,
    );

    expect(screen.queryByText("23:00")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Adjust end of 15:00 to 19:00" }));
    const addHourButton = screen.getByRole("button", { name: "+1h" });
    await user.click(addHourButton);
    await user.click(addHourButton);
    await user.click(addHourButton);
    await user.click(addHourButton);

    expect(screen.getByText("23:00")).toBeVisible();
    expect(screen.getAllByText("15:00 – 23:00")).toHaveLength(2);
  });
});
