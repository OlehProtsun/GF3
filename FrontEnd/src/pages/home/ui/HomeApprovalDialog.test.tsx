import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { beforeEach, expect, test, vi } from "vitest";
import { ApiError } from "@shared/api/httpClient";
import { setLanguage } from "@shared/i18n";
import { HomeApprovalDialog } from "./HomeApprovalDialog";
import type { HomeApprovalTarget } from "./homeActivity";

const mocks = vi.hoisted(() => ({
  lock: vi.fn(), lockMessage: vi.fn(), correction: vi.fn(), contract: vi.fn(), reject: vi.fn(), review: vi.fn(),
}));
vi.mock("@app/providers/PresenceProvider", () => ({ useManagerEditLocks: mocks.lock, buildManagerEditLockMessage: mocks.lockMessage }));
vi.mock("@entities/shift-corrections/api/shiftCorrectionsApi", () => ({ shiftCorrectionsApi: {
  getGraphRequest: mocks.correction, reject: mocks.reject,
} }));
vi.mock("@entities/shift-swap-approvals/api/shiftSwapApprovalsApi", () => ({ shiftSwapApprovalsApi: {
  getGraphRequest: mocks.contract, review: mocks.review,
} }));

const common = { id: 25, containerId: 3, scheduleId: 9, scheduleName: "November", shopName: "Main",
  year: 2026, month: 11, dayOfMonth: 4, employeeId: 7, employeeName: "Worker", status: "pending",
  createdAtUtc: "2026-11-04T08:00:00Z" };
const correction = { ...common, isDayOffRequest: true, requestedFromTime: "08:00", requestedToTime: "12:00" };
const contract = { ...common, kind: "claim", fromTime: "08:00", toTime: "12:00", fromEmployeeName: "Original Worker" };

beforeEach(() => {
  setLanguage("en");
  vi.resetAllMocks();
  mocks.lock.mockReturnValue({ isCheckingLocks: false, isLockedByOther: false });
  mocks.correction.mockResolvedValue(correction);
  mocks.contract.mockResolvedValue(contract);
});

function mount(kind: HomeApprovalTarget["kind"], removeTrigger = false) {
  const client = new QueryClient();
  function Fixture() {
    const fallback = useRef<HTMLDivElement>(null);
    const [trigger, setTrigger] = useState<HTMLElement | null>(null);
    return <>
      <div ref={fallback} tabIndex={0}>Stable feed</div>
      {(!trigger || !removeTrigger) && <button type="button" onClick={event => setTrigger(event.currentTarget)}>Open request</button>}
      {trigger && <HomeApprovalDialog target={{ kind, containerId: 3, graphId: 9, requestId: 25 }}
        trigger={trigger} fallbackFocusRef={fallback} onClose={() => setTrigger(null)} />}
    </>;
  }
  const user = userEvent.setup();
  render(<QueryClientProvider client={client}><Fixture /></QueryClientProvider>);
  return { user, open: () => user.click(screen.getByRole("button", { name: "Open request" })) };
}

test.each([false, true])("blocks review while checking or locked by another manager (locked=%s)", async locked => {
  mocks.lock.mockReturnValue({ isCheckingLocks: !locked, isLockedByOther: locked, lockedByOtherState: { lockedBy: "Other manager" } });
  mocks.lockMessage.mockReturnValue("This schedule is locked by Other manager.");
  const { open } = mount("correction");
  await open();
  const dialog = within(screen.getByRole("dialog", { name: "Review request" }));
  expect(await dialog.findByRole("button", { name: "Approve" })).toBeDisabled();
  expect(dialog.getByRole("button", { name: "Reject" })).toBeDisabled();
  expect(dialog.getByText(locked ? "This schedule is locked by Other manager." : "Checking schedule edit locks...")).toBeVisible();
  expect(mocks.lock).toHaveBeenLastCalledWith([{ resourceType: "schedule", resourceId: "3:9", containerId: 3, graphId: 9 }]);
  expect(mocks.correction).toHaveBeenCalledExactlyOnceWith(3, 9, 25, expect.any(AbortSignal));
  expect(mocks.contract).not.toHaveBeenCalled();
});

test("suspends the parent for contract confirmation and restores the feed when the trigger is gone", async () => {
  const { open, user } = mount("contract", true);
  await open();
  const parent = screen.getByRole("dialog", { name: "Review request" });
  expect(within(parent).getByRole("button", { name: "Close" })).toHaveFocus();
  const approve = await within(parent).findByRole("button", { name: "Approve" });
  await user.click(approve);
  const child = screen.getByRole("dialog", { name: "Approve shift transfer?" });
  expect(parent).not.toContainElement(child);
  expect(within(child).getByRole("button", { name: "Cancel" })).toHaveFocus();
  await user.keyboard("{Escape}");
  expect(child).not.toBeInTheDocument();
  expect(parent).toBeVisible();
  expect(approve).toHaveFocus();
  await user.keyboard("{Escape}");
  expect(parent).not.toBeInTheDocument();
  expect(screen.getByText("Stable feed")).toHaveFocus();
  expect(mocks.review).not.toHaveBeenCalled();
});

test("keeps the active dialog open during review then renders the terminal result", async () => {
  let finish: (value: typeof correction) => void = () => {};
  mocks.reject.mockReturnValue(new Promise(resolve => { finish = resolve; }));
  const { open, user } = mount("correction");
  await open();
  const parent = screen.getByRole("dialog", { name: "Review request" });
  const dialog = within(parent);
  await user.click(await dialog.findByRole("button", { name: "Reject" }));
  expect(dialog.getByRole("button", { name: "Close" })).toBeDisabled();
  await user.keyboard("{Escape}");
  expect(parent).toBeVisible();
  const terminal = { ...correction, status: "rejected" };
  mocks.correction.mockResolvedValue(terminal);
  finish(terminal);
  expect(await dialog.findByText("Rejected", { exact: true })).toBeVisible();
  await waitFor(() => expect(dialog.getByRole("button", { name: "Close" })).toBeEnabled());
  expect(dialog.queryByRole("button", { name: "Approve" })).toBeNull();
});

test("a missing exact request is unavailable without review controls", async () => {
  mocks.correction.mockRejectedValue(new ApiError({ status: 404, message: "Missing" }));
  const { open } = mount("correction");
  await open();
  expect(await screen.findByText("This request is no longer available.")).toBeVisible();
  expect(screen.queryByRole("button", { name: "Approve" })).toBeNull();
});
