import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { setLanguage } from "@shared/i18n";
import { shiftSwapApprovalsApi as api } from "@entities/shift-swap-approvals/api/shiftSwapApprovalsApi";
import type { ContractShiftApproval } from "@entities/shift-swap-approvals";
import { ContainerGraphContractApprovalsCard } from "./ContainerGraphContractApprovalsCard";
vi.mock("@entities/shift-swap-approvals/api/shiftSwapApprovalsApi", () => ({ shiftSwapApprovalsApi: { graph: vi.fn(), review: vi.fn() } }));
const pending: ContractShiftApproval = { id: 1, kind: "claim", status: "pending", employeeId: 2, employeeName: "Ada Worker", scheduleId: 10, scheduleSlotId: 30,
  scheduleName: "October", shopId: 1, shopName: "Central", year: 2026, month: 10, dayOfMonth: 1, fromTime: "09:00", toTime: "17:00", shiftSwapRequestId: 5,
  createdAtUtc: "2026-10-01T00:00:00Z", isStale: false };
beforeEach(() => { setLanguage("en"); vi.mocked(api.graph).mockReset().mockResolvedValue([{ ...pending }]); vi.mocked(api.review).mockReset().mockImplementation(async input => {
  const result: ContractShiftApproval = { ...pending, status: input.action === "approve" ? "approved" : "rejected" };
  vi.mocked(api.graph).mockResolvedValue([result]); return result;
}); });
function mount(unsaved = false) { const approved = vi.fn(), count = vi.fn(); render(<QueryClientProvider client={new QueryClient()}><ContainerGraphContractApprovalsCard
  containerId={1} graphId={10} disabled={false} hasUnsavedChanges={unsaved} onApproved={approved} onPendingCountChange={count} /></QueryClientProvider>); return { approved,count }; }
test("pending count and approved schedule refresh occur once after explicit confirmation", async () => {
  const {approved,count} = mount(); const user = userEvent.setup(); await screen.findByText("Ada Worker"); expect(count).toHaveBeenLastCalledWith(1);
  await user.click(screen.getByRole("button",{name:"Approve"})); expect(api.review).not.toHaveBeenCalled();
  await user.click(within(screen.getByRole("dialog",{name:"Approve shift transfer?"})).getByRole("button", {name:"Approve"}));
  await waitFor(() => expect(api.review).toHaveBeenCalledTimes(1));
  await waitFor(() => expect(approved).toHaveBeenCalledTimes(1));
  await waitFor(() => expect(count).toHaveBeenLastCalledWith(0));
});
test("reject only reviews the graph request and unsaved edits block both actions", async () => {
  const { approved } = mount(); const user = userEvent.setup(); await screen.findByText("Ada Worker");
  await user.click(screen.getByRole("button", {name:"Reject"})); await waitFor(() => expect(api.review).toHaveBeenCalledWith({containerId:1,graphId:10,id:1,action:"reject"}));
  expect(approved).not.toHaveBeenCalled();
});
test("unsaved changes keep review disabled", async () => {
  mount(true); await screen.findByText("Ada Worker"); expect(screen.getByRole("button",{name:"Approve"})).toBeDisabled(); expect(screen.getByRole("button",{name:"Reject"})).toBeDisabled();
});
