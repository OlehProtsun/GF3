import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { ContainerGraphShiftCorrectionsCard } from "./ContainerGraphShiftCorrectionsCard";

beforeEnglishTest(() => setTestLanguage("en"));

const mocks = vi.hoisted(() => ({ saveSetting: vi.fn(), approve: vi.fn(), reject: vi.fn(), graphQuery: vi.fn(), settingQuery: vi.fn() }));

vi.mock("@entities/shift-corrections", () => ({
  useGraphShiftCorrectionsQuery: (containerId: number | null, graphId: number | null) => {
    mocks.graphQuery(containerId, graphId);
    return ({
    data: [{
      id: 4, containerId: 2, scheduleId: 9, scheduleSlotId: 21, scheduleName: "August", shopName: "Central",
      year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Oleh Protsun",
      originalFromTime: "09:00", originalToTime: "15:30", requestedFromTime: "10:00", requestedToTime: "15:30",
      status: "pending", createdAtUtc: "2026-08-19T18:15:00Z",
    }],
    isLoading: false,
    isError: false,
  }); },
  useShiftCorrectionSettingQuery: (enabled: boolean) => { mocks.settingQuery(enabled); return { data: { highlightColor: "#FDE68A" } }; },
  useSaveShiftCorrectionSettingMutation: () => ({ mutate: mocks.saveSetting, isPending: false }),
  useApproveShiftCorrectionMutation: () => ({ mutate: mocks.approve, isPending: false }),
  useRejectShiftCorrectionMutation: () => ({ mutate: mocks.reject, isPending: false }),
}));

describe("ContainerGraphShiftCorrectionsCard", () => {
  beforeEach(() => Object.values(mocks).forEach(mock => mock.mockClear()));

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


test.each([0, null, -1])("disables correction actions and settings for unsaved graph %s", async graphId => {
  const user = userEvent.setup();
  render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={graphId}
    hasUnsavedChanges={false} disabled={false} onApproved={() => undefined} />);
  expect(mocks.settingQuery).toHaveBeenLastCalledWith(false);
  expect(mocks.graphQuery).toHaveBeenLastCalledWith(2, graphId);
  expect(screen.getByRole("button", { name: "Approved correction highlight color" })).toBeDisabled();
  await user.click(screen.getByRole("listitem"));
  const approve = screen.getByRole("button", { name: "Approve" });
  const reject = screen.getByRole("button", { name: "Reject" });
  expect(approve).toBeDisabled();
  expect(reject).toBeDisabled();
  await user.click(approve);
  await user.click(reject);
  expect(mocks.approve).not.toHaveBeenCalled();
  expect(mocks.reject).not.toHaveBeenCalled();
});

test("allows approval and rejection for persisted graphs", async () => {
  const user = userEvent.setup();
  render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9}
    hasUnsavedChanges={false} disabled={false} onApproved={() => undefined} />);
  expect(mocks.settingQuery).toHaveBeenLastCalledWith(true);
  await user.click(screen.getByRole("listitem"));
  await user.click(screen.getByRole("button", { name: "Approve" }));
  await user.click(screen.getByRole("button", { name: "Reject" }));
  expect(mocks.approve).toHaveBeenCalledWith(expect.objectContaining({ containerId: 2, graphId: 9, id: 4 }), expect.any(Object));
  expect(mocks.reject).toHaveBeenCalledWith({ containerId: 2, graphId: 9, id: 4 }, expect.any(Object));
});
