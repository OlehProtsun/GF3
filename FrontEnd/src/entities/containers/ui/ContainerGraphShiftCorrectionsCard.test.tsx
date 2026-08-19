import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import { ContainerGraphShiftCorrectionsCard } from "./ContainerGraphShiftCorrectionsCard";

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
  useSaveShiftCorrectionSettingMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useApproveShiftCorrectionMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useRejectShiftCorrectionMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

describe("ContainerGraphShiftCorrectionsCard", () => {
  test("keeps request details collapsed until the request is clicked", async () => {
    const user = userEvent.setup();
    render(
      <ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
        disabled={false} onApproved={() => undefined} />,
    );

    const request = screen.getByRole("listitem");
    expect(request).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByText("Selected request")).not.toBeInTheDocument();

    await user.click(request);
    expect(request).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Selected request")).toBeVisible();
    expect(screen.getByRole("button", { name: "Approve" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Reject" })).toBeVisible();

    await user.click(request);
    expect(screen.queryByText("Selected request")).not.toBeInTheDocument();
  });
});
