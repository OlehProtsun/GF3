import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { setLanguage } from "@shared/i18n";
import { shiftSwapApprovalsApi as api } from "@entities/shift-swap-approvals/api/shiftSwapApprovalsApi";
import type { ContractShiftApproval } from "@entities/shift-swap-approvals";
import { ContainerGraphContractApprovalsCard } from "./ContainerGraphContractApprovalsCard";
vi.mock("@entities/shift-swap-approvals/api/shiftSwapApprovalsApi", () => ({ shiftSwapApprovalsApi: { graph: vi.fn(), review: vi.fn() } }));
const pending: ContractShiftApproval = { id: 1, kind: "claim", status: "pending", employeeId: 2, employeeName: "Ada Worker", fromEmployeeId: 1, fromEmployeeName: "Oleh Owner", toEmployeeId: 2, toEmployeeName: "Ada Worker", scheduleId: 10, scheduleSlotId: 30,
  scheduleName: "October", shopId: 1, shopName: "Central", year: 2026, month: 10, dayOfMonth: 1, fromTime: "09:00", toTime: "17:00", shiftSwapRequestId: 5,
  createdAtUtc: "2026-10-01T00:00:00Z", isStale: false };
beforeEach(() => { setLanguage("en"); vi.mocked(api.graph).mockReset().mockResolvedValue([{ ...pending }]); vi.mocked(api.review).mockReset().mockImplementation(async input => {
  const result: ContractShiftApproval = { ...pending, status: input.action === "approve" ? "approved" : "rejected" };
  vi.mocked(api.graph).mockResolvedValue([result]); return result;
}); });
function mount(unsaved = false, disabled = false) { const approved = vi.fn(), count = vi.fn(); render(<QueryClientProvider client={new QueryClient()}><ContainerGraphContractApprovalsCard
  containerId={1} graphId={10} disabled={disabled} hasUnsavedChanges={unsaved} onApproved={approved} onPendingCountChange={count} /></QueryClientProvider>); return { approved,count }; }
test("pending count and approved schedule refresh occur once after explicit confirmation", async () => {
  const {approved,count} = mount(); const user = userEvent.setup(); await user.click(await screen.findByRole("button", {name:/Ada Worker/})); expect(screen.getByText("The offer is reserved while this pickup request awaits manager approval.")).toBeInTheDocument(); expect(count).toHaveBeenLastCalledWith(1);
  await user.click(screen.getByRole("button",{name:"Approve"})); expect(api.review).not.toHaveBeenCalled();
  await user.click(within(screen.getByRole("dialog",{name:"Approve shift transfer?"})).getByRole("button", {name:"Approve"}));
  await waitFor(() => expect(api.review).toHaveBeenCalledTimes(1));
  await waitFor(() => expect(approved).toHaveBeenCalledTimes(1));
  await waitFor(() => expect(count).toHaveBeenLastCalledWith(0));
});
test("reject only reviews the graph request and unsaved edits block both actions", async () => {
  const { approved } = mount(); const user = userEvent.setup(); await user.click(await screen.findByRole("button", {name:/Ada Worker/}));
  await user.click(screen.getByRole("button", {name:"Reject"})); await waitFor(() => expect(api.review).toHaveBeenCalledWith({containerId:1,graphId:10,id:1,action:"reject"}));
  expect(approved).not.toHaveBeenCalled();
  await waitFor(() => expect(screen.queryByText("The offer is reserved while this pickup request awaits manager approval.")).toBeNull());
});
test("unsaved changes keep review disabled", async () => {
  mount(true); const user = userEvent.setup(); await user.click(await screen.findByRole("button", {name:/Ada Worker/})); expect(screen.getByRole("button",{name:"Approve"})).toBeDisabled(); expect(screen.getByRole("button",{name:"Reject"})).toBeDisabled();
});

test("pending cards are first; keyboard expansion opens only one card", async () => {
  vi.mocked(api.graph).mockResolvedValue([{...pending,id:3,status:"approved",employeeName:"Completed"},{...pending,id:2,employeeName:"Second"},pending]);
  mount(); const user=userEvent.setup(); const cards=await screen.findAllByRole("button",{name:/Oleh Owner/});
  expect(cards.at(-1)).toHaveTextContent("Completed"); cards[0].focus(); await user.keyboard("{Enter}");
  expect(screen.getAllByText("Selected request")).toHaveLength(1);
  await user.click(screen.getByRole("button",{name:/^Ada Worker/}));
  expect(screen.getAllByText("Selected request")).toHaveLength(1);
  await user.click(screen.getByRole("button",{name:"Collapse request from Ada Worker"}));
  expect(screen.queryByText("Selected request")).toBeNull();
});

test.each([
  ["claim","Oleh Owner","Ada Worker",{}],
  ["claim","Manager-created shift","Ada Worker",{isManagerCreated:true,fromEmployeeName:null}],
  ["claim","Unknown employee","Ada Worker",{fromEmployeeName:null,fromEmployeeId:null}],
  ["below_minimum_give","Ada Worker","Private Recipient",{fromEmployeeName:null,toEmployeeName:"Private Recipient",targetEmployeeId:3}],
  ["below_minimum_give","Ada Worker","Open recipient",{fromEmployeeName:null,toEmployeeName:null,targetEmployeeId:null}],
] as const)("%s roles: %s → %s",async(kind,from,to,fields)=>{
  vi.mocked(api.graph).mockResolvedValue([{...pending,kind,...fields}]); mount(); const user=userEvent.setup();
  await user.click(await screen.findByRole("button",{name:/Ada Worker/})); const transfer=screen.getByRole("group",{name:"Shift transfer"});
  expect(transfer).toHaveTextContent(`Gives shift${from}`); expect(transfer).toHaveTextContent(`Receives shift${to}`);
});

test("exchange reverses participants and includes return date and hours",async()=>{
  vi.mocked(api.graph).mockResolvedValue([{...pending,requestKind:"exchange",returnDayOfMonth:7,returnMonth:11,returnYear:2026,returnFromTime:"10:00",returnToTime:"16:00"}]);
  mount(); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  const back=screen.getByRole("group",{name:"Return shift"}); expect(back).toHaveTextContent("Gives shiftAda Worker");
  expect(back).toHaveTextContent("Receives shiftOleh Owner"); expect(back).toHaveTextContent("10:00 – 16:00"); expect(screen.getByText("07.11.2026")).toBeInTheDocument();
});

test.each(["pending","approved","rejected","cancelled","expired"] as const)("%s renders and completed requests have no actions",async status=>{
  vi.mocked(api.graph).mockResolvedValue([{...pending,status}]); mount(); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  expect(screen.getByText(status[0].toUpperCase()+status.slice(1),{exact:true})).toBeInTheDocument();
  expect(screen.queryByRole("button",{name:"Approve"}) !== null).toBe(status === "pending");
});

test("cancelling confirmation does not mutate; API failure remains visible",async()=>{
  vi.mocked(api.review).mockRejectedValue(new Error("Review unavailable")); mount(); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  await user.click(screen.getByRole("button",{name:"Approve"})); await user.click(within(screen.getByRole("dialog")).getByRole("button",{name:"Cancel"}));
  expect(api.review).not.toHaveBeenCalled(); await user.click(screen.getByRole("button",{name:"Reject"})); expect(await screen.findByText("Review unavailable")).toBeInTheDocument();
});

test("edit lock keeps both actions disabled",async()=>{
  mount(false,true); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  expect(screen.getByRole("button",{name:"Approve"})).toBeDisabled(); expect(screen.getByRole("button",{name:"Reject"})).toBeDisabled();
});

test("mutation loading disables repeated actions",async()=>{
  vi.mocked(api.review).mockImplementation(()=>new Promise(()=>{})); mount(); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  await user.click(screen.getByRole("button",{name:"Reject"})); await waitFor(()=>expect(screen.getAllByRole("button",{name:"Applying..."})).toHaveLength(2));
  screen.getAllByRole("button",{name:"Applying..."}).forEach(button=>expect(button).toBeDisabled());
});

test("long names remain available and minimum exception keeps direct review",async()=>{
  const name="A very long employee name with multiple surnames Żółć";
  vi.mocked(api.graph).mockResolvedValue([{...pending,kind:"below_minimum_give",employeeName:name,fromEmployeeName:name,toEmployeeName:null,targetEmployeeId:null}]); mount(); const user=userEvent.setup();
  await user.click(await screen.findByRole("button",{name:new RegExp(name)})); expect(screen.getAllByText(name).length).toBeGreaterThan(0);
  await user.click(screen.getByRole("button",{name:"Approve"})); await waitFor(()=>expect(api.review).toHaveBeenCalledTimes(1)); expect(screen.queryByRole("dialog")).toBeNull();
});

test.each([
  ["pl", "Oddaje zmianę", "Przejmuje zmianę"],
  ["uk", "Віддає зміну", "Отримує зміну"],
] as const)("transfer captions are localized in %s",async(language,gives,receives)=>{
  setLanguage(language); mount(); const user=userEvent.setup(); await user.click(await screen.findByRole("button",{name:/Ada Worker/}));
  expect(screen.getByText(gives)).toBeInTheDocument(); expect(screen.getByText(receives)).toBeInTheDocument();
});
