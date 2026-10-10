import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { ContainerGraphShiftCorrectionsCard } from "./ContainerGraphShiftCorrectionsCard";

const mocks = vi.hoisted(() => ({ saveSetting: vi.fn() }));

vi.mock("@entities/shift-corrections", () => ({
  useGraphShiftCorrectionsQuery: () => ({
    data: [{
      id: 4, containerId: 2, scheduleId: 9, scheduleSlotId: 21, scheduleName: "August", shopName: "Central",
      year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Oleh Protsun",
      originalFromTime: "09:00", originalToTime: "15:30", requestedFromTime: "10:00", requestedToTime: "15:30",
      status: "pending", createdAtUtc: "2026-08-19T18:15:00Z",
    }],
    isLoading: false,
    isError: false,
  }),
  useShiftCorrectionSettingQuery: () => ({ data: { highlightColor: "#FDE68A" } }),
  useSaveShiftCorrectionSettingMutation: () => ({ mutate: mocks.saveSetting, isPending: false }),
  useApproveShiftCorrectionMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useRejectShiftCorrectionMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

describe("ContainerGraphShiftCorrectionsCard", () => {
  beforeEach(() => mocks.saveSetting.mockReset());

  test("expands request details in place", async () => {
    const user = userEvent.setup();
    render(
      <ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
        disabled={false} onApproved={() => undefined} />,
    );

    const request = screen.getByRole("listitem");
    expect(request).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByText("Selected request")).not.toBeInTheDocument();

    await user.click(request);
    const requestList = screen.getByRole("list", { name: "Shift correction requests" });
    expect(within(requestList).getByText("Selected request")).toBeVisible();
    expect(within(requestList).getAllByRole("listitem")).toHaveLength(1);
    expect(within(requestList).getByRole("listitem").tagName).toBe("ARTICLE");
    expect(screen.getByRole("button", { name: "Approve" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Reject" })).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Collapse request from Oleh Protsun" }));
    expect(screen.queryByText("Selected request")).not.toBeInTheDocument();
  });

  test("uses the custom color dialog and reports pending requests", async () => {
    const user = userEvent.setup();
    const onPendingCountChange = vi.fn();
    render(
      <ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
        disabled={false} onApproved={() => undefined} onPendingCountChange={onPendingCountChange} />,
    );

    await waitFor(() => expect(onPendingCountChange).toHaveBeenLastCalledWith(1));
    await user.click(screen.getByRole("button", { name: "Approved correction highlight color" }));
    const colorDialog = screen.getByRole("dialog", { name: "Choose approval color" });
    expect(colorDialog).toBeVisible();
    expect(colorDialog.parentElement).toBe(document.body);
    await user.click(screen.getByRole("button", { name: "Soft blue" }));
    await user.click(screen.getByRole("button", { name: "Use color" }));

    expect(mocks.saveSetting).toHaveBeenCalledWith("#DBEAFE", expect.objectContaining({ onError: expect.any(Function) }));
    expect(screen.queryByRole("dialog", { name: "Choose approval color" })).not.toBeInTheDocument();
  });
});
