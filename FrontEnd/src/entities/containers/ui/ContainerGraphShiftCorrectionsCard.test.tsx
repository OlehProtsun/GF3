import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { ContainerGraphShiftCorrectionsCard } from "./ContainerGraphShiftCorrectionsCard";

beforeEnglishTest(() => setTestLanguage("en"));

const mocks = vi.hoisted(() => ({ saveSetting: vi.fn(), approve: vi.fn(), reject: vi.fn(), graphQuery: vi.fn(), settingQuery: vi.fn(), commit: vi.fn(), approvePending: vi.fn(), rejectPending: vi.fn() }));

vi.mock("@entities/containers/api/containersApi", () => ({ containersApi: { commitGraphVersion: mocks.commit } }));

vi.mock("@entities/shift-corrections", () => ({
  useGraphShiftCorrectionsQuery: (containerId: number | null, graphId: number | null) => {
    return mocks.graphQuery(containerId, graphId) ?? ({
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
  useApproveShiftCorrectionMutation: () => ({ mutate: mocks.approve, isPending: mocks.approvePending() ?? false }),
  useRejectShiftCorrectionMutation: () => ({ mutate: mocks.reject, isPending: mocks.rejectPending() ?? false }),
}));

beforeEach(() => Object.values(mocks).forEach(mock => mock.mockReset()));

describe("ContainerGraphShiftCorrectionsCard", () => {


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
    expect(screen.getByText("Current shift")).toBeVisible();
    expect(screen.getByText("Requested shift")).toBeVisible();
    expect(screen.getByText("09:00 – 15:30")).toBeVisible();
    expect(screen.getByText("10:00 – 15:30")).toBeVisible();
    expect(screen.getByText("Start +1h")).toBeVisible();
    expect(screen.getByText("End unchanged")).toBeVisible();
    expect(screen.getByText("Duration −1h")).toBeVisible();
    expect(screen.getByText("12.08.2026")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Collapse request from Oleh Protsun" }));
    expect(screen.queryByText("Selected request")).not.toBeInTheDocument();
  });

  test("explains why unsaved edits disable approval while rejection remains available", async () => {
    render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges
      disabled={false} onApproved={vi.fn()} />);
    await userEvent.click(screen.getByRole("listitem"));
    expect(screen.getByText("Save your current schedule edits before approving.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Approve" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Reject" })).toBeEnabled();
  });

  test("commits a version before calling onApproved", async () => {
    const onApproved = vi.fn();
    mocks.commit.mockResolvedValue(undefined);
    mocks.approve.mockImplementation((_input, options) => options.onSuccess());
    render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
      disabled={false} onApproved={onApproved} />);
    await userEvent.click(screen.getByRole("listitem"));
    await userEvent.click(screen.getByRole("button", { name: "Approve" }));
    await waitFor(() => expect(onApproved).toHaveBeenCalledOnce());
    expect(mocks.commit).toHaveBeenCalledWith(2, 9);
  });

  test("keeps a failed approval pending with a readable error", async () => {
    mocks.approve.mockImplementation((_input, options) => options.onError(new Error("Approval unavailable")));
    render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
      disabled={false} onApproved={vi.fn()} />);
    await userEvent.click(screen.getByRole("listitem"));
    await userEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(screen.getByText("Approval unavailable")).toBeVisible();
    expect(screen.getByRole("button", { name: "Approve" })).toBeEnabled();
    expect(mocks.commit).not.toHaveBeenCalled();
    await userEvent.click(screen.getByRole("button", { name: "Collapse request from Oleh Protsun" }));
    expect(screen.getByText("Pending")).toBeVisible();
  });

  test("shows resolved badges, sorts pending first and expands exactly one item", async () => {
    const base = { containerId: 2, scheduleId: 9, scheduleSlotId: 21, scheduleName: "August", shopName: "Central",
      year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Oleh Protsun",
      originalFromTime: "09:00", originalToTime: "15:30", requestedFromTime: "10:00", requestedToTime: "15:30",
      createdAtUtc: "2026-08-19T18:15:00Z", reviewedByManagerName: "Ada" };
    mocks.graphQuery.mockReturnValue({ data: [
      { ...base, id: 1, status: "approved" }, { ...base, id: 2, status: "rejected" }, { ...base, id: 3, status: "pending" },
    ], isLoading: false, isError: false });
    render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
      disabled={false} onApproved={vi.fn()} />);
    expect(screen.getAllByRole("listitem")[0]).toHaveTextContent("Pending");
    expect(screen.getAllByText("OP")).toHaveLength(3);
    expect(screen.getByText("Approved")).toBeVisible();
    expect(screen.getByText("Rejected")).toBeVisible();
    await userEvent.click(screen.getAllByRole("listitem")[0]);
    await userEvent.click(screen.getAllByRole("listitem")[1]);
    expect(screen.getAllByText("Selected request")).toHaveLength(1);
    expect(screen.getAllByRole("listitem")).toHaveLength(3);
    expect(screen.getByText("Reviewed by Ada")).toBeVisible();
    expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Reject" })).not.toBeInTheDocument();
  });

  test.each([
    [false, false, "No correction requests yet."],
    [true, false, "Loading requests..."],
    [false, true, "Could not load shift corrections."],
  ])("preserves internal query states (loading %s, error %s)", (isLoading, isError, message) => {
    mocks.graphQuery.mockReturnValue({ data: [], isLoading, isError });
    const onPendingCountChange = vi.fn();
    render(<ContainerGraphShiftCorrectionsCard containerId={2} graphId={9} hasUnsavedChanges={false}
      disabled={false} onApproved={vi.fn()} onPendingCountChange={onPendingCountChange} />);
    expect(screen.getByText(message)).toBeVisible();
    expect(screen.getByText(/0\s+pending/)).toBeVisible();
    expect(onPendingCountChange).toHaveBeenLastCalledWith(0);
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

const historyRequest = {
  id: 4, containerId: 2, scheduleId: 9, scheduleSlotId: 21, scheduleName: "August", shopName: "Central",
  year: 2026, month: 8, dayOfMonth: 12, employeeId: 7, employeeName: "Oleh Protsun",
  originalFromTime: "09:00", originalToTime: "15:30", requestedFromTime: "10:00", requestedToTime: "15:30",
  status: "pending", createdAtUtc: "2026-08-19T18:15:00Z", reviewedByManagerName: "Ada",
};

function showHistory(data: typeof historyRequest[]) {
  mocks.graphQuery.mockReturnValue({ data, isLoading: false, isError: false });
}

const cardProps = { containerId: 2, graphId: 9, hasUnsavedChanges: false, disabled: false, onApproved: vi.fn() };

test.each(["approved", "rejected"])("collapses after successful %s and lets the history item expand again", async status => {
  showHistory([{ ...historyRequest }]);
  mocks.commit.mockResolvedValue(undefined);
  const mutation = status === "approved" ? mocks.approve : mocks.reject;
  mutation.mockImplementation((_input, options) => {
    showHistory([{ ...historyRequest, status }]);
    options.onSuccess({ ...historyRequest, status });
  });
  const view = render(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  await userEvent.click(screen.getByRole("listitem"));
  await userEvent.click(screen.getByRole("button", { name: status === "approved" ? "Approve" : "Reject" }));
  view.rerender(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  expect(screen.queryByText("Selected request")).not.toBeInTheDocument();
  expect(screen.getByRole("listitem").tagName).toBe("BUTTON");
  expect(screen.getByRole("listitem")).toHaveTextContent(status === "approved" ? "Approved" : "Rejected");
  expect(screen.getByText(/0\s+pending/)).toBeVisible();
  await userEvent.click(screen.getByRole("listitem"));
  expect(within(screen.getByRole("listitem")).getByText(status === "approved" ? "Approved" : "Rejected")).toBeVisible();
  expect(screen.getByText("Reviewed by Ada")).toBeVisible();
  expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Reject" })).not.toBeInTheDocument();
});

test("keeps failed rejection expanded and pending", async () => {
  mocks.reject.mockImplementation((_input, options) => options.onError(new Error("Rejection unavailable")));
  render(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  await userEvent.click(screen.getByRole("listitem"));
  await userEvent.click(screen.getByRole("button", { name: "Reject" }));
  expect(screen.getByText("Rejection unavailable")).toBeVisible();
  expect(within(screen.getByRole("listitem")).getByText("Pending")).toBeVisible();
  expect(screen.getByRole("button", { name: "Reject" })).toBeEnabled();
});

test.each(["approve", "reject"])("keeps %s loading state visible and disables both actions", async action => {
  const { rerender } = render(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  await userEvent.click(screen.getByRole("listitem"));
  (action === "approve" ? mocks.approvePending : mocks.rejectPending).mockReturnValue(true);
  rerender(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  expect(screen.getByRole("button", { name: action === "approve" ? "Applying..." : "Rejecting..." })).toBeDisabled();
  expect(screen.getByRole("button", { name: action === "approve" ? "Reject" : "Approve" })).toBeDisabled();
});

test.each([
  ["en", "Approved", "Reviewed by Ada"],
  ["pl", "Zatwierdzono", "Rozpatrzono przez: Ada"],
  ["uk", "Схвалено", "Розглянуто: Ada"],
] as const)("localizes expanded status and reviewer in %s", async (language, status, reviewer) => {
  setTestLanguage(language);
  showHistory([{ ...historyRequest, status: "approved" }]);
  render(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  await userEvent.click(screen.getByRole("listitem"));
  expect(screen.getByText(status)).toBeVisible();
  expect(screen.getByText(reviewer)).toBeVisible();
});

test("supports keyboard expansion and preserves multiple pending requests and long history", async () => {
  showHistory(Array.from({ length: 30 }, (_, index) => ({ ...historyRequest, id: index + 1,
    employeeName: `Long Employee Name With Several Surnames ${index}`, scheduleName: "Long schedule name ".repeat(5),
    status: index < 3 ? "pending" : index % 2 ? "approved" : "rejected" })));
  const onPendingCountChange = vi.fn();
  render(<ContainerGraphShiftCorrectionsCard {...cardProps} onPendingCountChange={onPendingCountChange} />);
  const list = screen.getByRole("list");
  expect(list).toHaveAttribute("tabindex", "0");
  expect(screen.getAllByRole("listitem")).toHaveLength(30);
  expect(onPendingCountChange).toHaveBeenLastCalledWith(3);
  screen.getAllByRole("listitem")[0].focus();
  await userEvent.keyboard("{Enter}");
  expect(screen.getByText("Selected request")).toBeVisible();
  expect(screen.getByText("Long schedule name ".repeat(5).trim(), { exact: false })).toBeVisible();
  expect(screen.getAllByRole("listitem")).toHaveLength(30);
});


test.each(["pending", "approved", "rejected"])("renders the reference icons and avatar for %s requests", async status => {
  showHistory([{ ...historyRequest, status }]);
  render(<ContainerGraphShiftCorrectionsCard {...cardProps} />);
  const row = screen.getByRole("listitem");
  expect(row.querySelectorAll("svg")).toHaveLength(2);
  await userEvent.click(row);
  const article = screen.getByRole("listitem");
  expect(within(article).getByText("O")).toHaveAttribute("aria-hidden", "true");
  const metadata = article.querySelectorAll("dt");
  expect(metadata[0]).toHaveTextContent("Date");
  expect(metadata[1]).toHaveTextContent("Schedule");
  metadata.forEach(item => expect(item.querySelector("svg")).toBeInTheDocument());
  for (const label of ["Start +1h", "End unchanged", "Duration −1h"]) {
    expect(within(article).getByText(label).querySelector("svg")).toBeInTheDocument();
  }
  expect(article.querySelector("em svg")).toBeInTheDocument();
  expect(article.querySelectorAll("svg")).toHaveLength(status === "pending" ? 10 : 9);
  expect(within(article).queryByText("→")).not.toBeInTheDocument();
  if (status !== "pending") {
    expect(article.querySelector("p svg")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
  }
});
