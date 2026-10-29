import { render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { setLanguage } from "@shared/i18n";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwap, ShiftSwapEmployee } from "@entities/shift-swaps";
import { EmployeeSwapPage } from "./EmployeeSwapPage";
import { SwapOfferDetailsDialog } from "./SwapOfferDetailsDialog";
import { BrowserRouter } from "react-router-dom";

const contract = vi.hoisted(() => ({ claim: vi.fn(), exception: vi.fn(), pending: false }));
vi.mock("@features/employee-profile/api/queries", () => ({ useEmployeeProfileQuery: () => ({ data: { contractType: "o_prace" } }) }));
vi.mock("@entities/employment-contracts", () => ({ useEmploymentContractSettingsQuery: () => ({ data: { restrictSwapsToEightHours: true } }) }));
vi.mock("@entities/shift-swap-approvals", () => ({
  useEmployeeContractApprovalsQuery: () => ({ data: [], error: null }),
  useCreateContractClaimMutation: () => ({ mutate: contract.claim, isPending: contract.pending }),
  useCreateGiveawayExceptionMutation: () => ({ mutate: contract.exception, isPending: false }),
  useCancelContractApprovalMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));
const mocks = vi.hoisted(() => ({
  schedulesQuery: vi.fn(),
  swapsQuery: vi.fn(),
  employeesQuery: vi.fn(),
  createMutate: vi.fn(),
  preview: vi.fn(),
  acceptMutate: vi.fn(),
  cancelMutate: vi.fn(),
  setPinMutate: vi.fn(),
  initialPinnedSwapIds: [] as number[],
  mutationState: {
    createPending: false,
    acceptPending: false,
    cancelPending: false,
  },
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({
    session: {
      employeeId: 12,
      displayName: "Owner Worker",
      userName: "owner",
    },
  }),
}));

vi.mock("@entities/employee-schedule", () => ({
  useEmployeeScheduleListQuery: () => mocks.schedulesQuery(),
}));

vi.mock("@entities/employee-ui-state", async () => {
  const { useState } = await import("react");
  let updatePinnedIds: ((updater: (current: number[]) => number[]) => void) | null = null;

  return {
    useEmployeeUiStateQuery: () => {
      const [pinnedSwapIds, setPinnedSwapIds] = useState<number[]>(() => mocks.initialPinnedSwapIds);
      updatePinnedIds = updater => setPinnedSwapIds(updater);
      return {
        data: { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds },
        error: null,
      };
    },
    useSetEmployeeSwapPinMutation: () => ({
      mutate: ({ swapId, pinned }: { swapId: number; pinned: boolean }) => {
        mocks.setPinMutate({ swapId, pinned });
        updatePinnedIds?.(current => pinned
          ? [swapId, ...current.filter(id => id !== swapId)]
          : current.filter(id => id !== swapId));
      },
      isPending: false,
    }),
  };
});

vi.mock("@entities/shift-swaps", async importOriginal => ({
  ...(await importOriginal<typeof import("@entities/shift-swaps")>()),
  useShiftSwapPreviewQuery: (payload: unknown) => mocks.preview(payload),
  useShiftSwapReturnCandidatesQuery: () => ({ data: { items: [], hasMore: false }, isFetching: false }),
  useEmployeeShiftSwapsQuery: () => mocks.swapsQuery(),
  useEmployeeShiftSwapEmployeesQuery: () => mocks.employeesQuery(),
  useCreateEmployeeShiftSwapMutation: () => ({
    mutate: mocks.createMutate,
    isPending: mocks.mutationState.createPending,
    error: null,
  }),
  useAcceptEmployeeShiftSwapMutation: () => ({
    mutate: mocks.acceptMutate,
    isPending: mocks.mutationState.acceptPending,
    error: null,
  }),
  useCancelEmployeeShiftSwapMutation: () => ({
    mutate: mocks.cancelMutate,
    isPending: mocks.mutationState.cancelPending,
    error: null,
  }),
}));

const schedules: EmployeeSchedule[] = [
  {
    id: 10,
    containerId: 2,
    containerName: "Main Container",
    shopId: 4,
    shopName: "Central Shop",
    name: "May Schedule",
    year: 2026,
    month: 5,
    publicationStatus: "public",
    employees: [
      {
        id: 1,
        employeeId: 12,
        firstName: "Owner",
        lastName: "Worker",
        displayName: "Owner Worker",
        minHoursMonth: 80,
        displayOrder: 1,
      },
      {
        id: 2,
        employeeId: 7,
        firstName: "Target",
        lastName: "Worker",
        displayName: "Target Worker",
        minHoursMonth: 80,
        displayOrder: 2,
      },
    ],
    slots: [
      { id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 12, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" },
      { id: 101, dayOfMonth: 2, slotNo: 1, employeeId: 7, fromTime: "09:00", toTime: "13:00", status: "ASSIGNED" },
    ],
  },
];

const targetEmployees: ShiftSwapEmployee[] = [
  {
    id: 7,
    firstName: "Target",
    lastName: "Worker",
    displayName: "Target Worker",
  },
  {
    id: 12,
    firstName: "Owner",
    lastName: "Worker",
    displayName: "Owner Worker",
  },
];

function createSwap(overrides: Partial<ShiftSwap> = {}): ShiftSwap {
  return {
    id: 5,
    scheduleId: 10,
    scheduleSlotId: 100,
    scheduleName: "May Schedule",
    containerName: "Main Container",
    shopName: "Central Shop",
    year: 2026,
    month: 5,
    dayOfMonth: 1,
    fromTime: "08:00",
    toTime: "16:00",
    fromEmployeeId: 7,
    fromEmployeeName: "Owner Worker",
    targetEmployeeId: null,
    targetEmployeeName: null,
    acceptedByEmployeeId: null,
    acceptedByEmployeeName: null,
    visibility: "public",
    status: "open",
    createdAtUtc: "2026-05-01T08:00:00.000Z",
    acceptedAtUtc: null,
    shiftHours: 8,
    currentEmployeeHoursBefore: 0,
    currentEmployeeHoursAfter: 8,
    currentEmployeeWorkDaysBefore: 0,
    currentEmployeeWorkDaysAfter: 1,
    currentEmployeeFreeDaysBefore: 31,
    currentEmployeeFreeDaysAfter: 30,
    fromEmployeeHoursBefore: 8,
    fromEmployeeHoursAfter: 0,
    isManagerCreated: false,
    manualColumnId: null,
    manualColumnName: null,
    isCreatedByCurrentEmployee: false,
    isScheduleLocked: false,
    canAccept: true,
    canAcceptFull: true,
    allowPartialAcceptance: false,
    eligibleClaimWindows: [],
    canCancel: false,
    ...overrides,
  };
}

let queryClient: QueryClient;
function renderPage() {
  return render(<QueryClientProvider client={queryClient}><BrowserRouter><EmployeeSwapPage /></BrowserRouter></QueryClientProvider>);
}

beforeEach(() => {
  window.history.replaceState({}, "", "/swap");
  setLanguage("en");
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  // jsdom does not implement the browser's modal dialog methods.
  Object.defineProperties(HTMLDialogElement.prototype, {
    showModal: { configurable: true, value(this: HTMLDialogElement) { this.open = true; } },
    close: { configurable: true, value(this: HTMLDialogElement) { this.open = false; } },
  });
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
  window.localStorage.clear();
  mocks.schedulesQuery.mockReset();
  mocks.swapsQuery.mockReset();
  mocks.employeesQuery.mockReset();
  mocks.createMutate.mockReset();
  mocks.preview.mockImplementation((payload: { fromTime: string; toTime: string; targetEmployeeId?: number; scheduleSlotId: number } | null) => ({
    data: payload ? { requestKind: "give", targetEmployeeId: payload.targetEmployeeId, targetEmployeeName: payload.targetEmployeeId ? "Target Worker" : "Everyone",
      source: { slotId: payload.scheduleSlotId, scheduleId: 10, scheduleName: "May Schedule", shopName: "Central Shop", year: 2026, month: 5, dayOfMonth: 1,
        fromTime: payload.fromTime, toTime: payload.toTime, durationMinutes: 450 }, projectedMonths: [], isValid: true } : undefined,
    isFetching: false, error: null,
  }));
  mocks.acceptMutate.mockReset();
  mocks.cancelMutate.mockReset();
  mocks.setPinMutate.mockReset();
  mocks.initialPinnedSwapIds = [];
  mocks.mutationState.createPending = false;
  mocks.mutationState.acceptPending = false;
  mocks.mutationState.cancelPending = false;

  mocks.schedulesQuery.mockReturnValue({ data: schedules, isLoading: false, error: null });
  mocks.swapsQuery.mockReturnValue({ data: [], isLoading: false, error: null });
  mocks.employeesQuery.mockReturnValue({ data: targetEmployees, isLoading: false, error: null });
});

beforeEach(() => { contract.claim.mockReset(); contract.exception.mockReset(); contract.pending = false; });
test("approval-required claim invokes only the pending route, without an instant transfer", async () => {
  mocks.swapsQuery.mockReturnValue({ data: [createSwap({requiresManagerApproval:true,canRequestManagerApproval:true})],error:null });
  renderPage(); const user = userEvent.setup();
  await user.click(screen.getByRole("button",{name:"View offer details for May Schedule"}));
  await user.click(screen.getByRole("button",{name:"Request manager approval"}));
  expect(screen.getByRole("dialog",{name:"Request manager approval?"})).toHaveTextContent("do not reserve offers");
  await user.click(screen.getByRole("button",{name:"Request manager approval"}));
  expect(contract.claim).toHaveBeenCalledWith({id:5},expect.anything()); expect(mocks.acceptMutate).not.toHaveBeenCalled();
});
test("pending requests prevent duplicate claim actions", () => {
  const onAccept = vi.fn(); render(<SwapOfferDetailsDialog swap={createSwap({requiresManagerApproval:true,approvalPendingForCurrentEmployee:true})}
    currentEmployeeId={12} busy={false} onClose={vi.fn()} onAccept={onAccept} onCancel={vi.fn()} />);
  expect(screen.getByRole("button",{name:"Manager approval pending"})).toBeDisabled(); expect(onAccept).not.toHaveBeenCalled();
});
test("whole-shift contract disables shortened source controls", async () => {
  renderPage(); const user = userEvent.setup(); await user.click(screen.getByRole("button",{name:"Create offer"}));
  await user.click(screen.getByRole("button",{name:/May Schedule/}));
  await user.click(screen.getByRole("radio",{name:/08:00.*16:00/}));
  expect(screen.getByRole("button",{name:"Custom period"})).toBeDisabled();
});
test("below-minimum preview offers an explicit exception action and blocks publication", async () => {
  mocks.preview.mockImplementation((payload: {fromTime:string;toTime:string;targetEmployeeId?:number;scheduleSlotId:number} | null) => ({
    data: payload ? { requestKind:"give",allowPartialAcceptance:false,targetEmployeeId:payload.targetEmployeeId,targetEmployeeName:"Everyone",
      source:{slotId:payload.scheduleSlotId,scheduleId:10,scheduleName:"May Schedule",shopName:"Central Shop",year:2026,month:5,dayOfMonth:1,
        fromTime:payload.fromTime,toTime:payload.toTime,durationMinutes:480}, projectedMonths:[],isValid:true,requiresManagerException:true,minimumMinutes:4800,projectedMinutes:4320 } : undefined,
    isFetching:false,error:null,
  }));
  renderPage(); const user = userEvent.setup(); await user.click(screen.getByRole("button",{name:"Create offer"}));
  await user.click(screen.getByRole("button",{name:/May Schedule/})); await user.click(screen.getByRole("radio",{name:/08:00.*16:00/}));
  await user.click(screen.getByRole("button",{name:"Confirm shift"}));
  await user.click(screen.getByRole("button",{name:"Continue"})); await user.click(screen.getByRole("button",{name:"Continue"}));
  const dialog = screen.getByRole("dialog"); await waitFor(() => expect(within(dialog).getByRole("button",{name:"Request exception"})).toBeEnabled());
  expect(within(dialog).getByRole("button",{name:"Send request"})).toBeDisabled();
  await user.click(within(dialog).getByRole("button",{name:"Request exception"})); expect(contract.exception).toHaveBeenCalledTimes(1); expect(mocks.createMutate).not.toHaveBeenCalled();
});
