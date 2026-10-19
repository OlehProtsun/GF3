import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { setLanguage, t } from "@shared/i18n";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwap, ShiftSwapEmployee } from "@entities/shift-swaps";
import { EmployeeSwapPage } from "./EmployeeSwapPage";
import styles from "./EmployeeSwapPage.module.css";

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
    canCancel: false,
    ...overrides,
  };
}

let queryClient: QueryClient;
function renderPage() {
  return render(<QueryClientProvider client={queryClient}><EmployeeSwapPage /></QueryClientProvider>);
}

beforeEach(() => {
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

describe("EmployeeSwapPage", () => {
  test("unequal Exchange confirmation preserves both participants, graphs and exact times", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap({ requestKind: "exchange", visibility: "private", targetEmployeeId: 12,
      targetEmployeeName: "Target Worker", fromTime: "09:17", toTime: "13:43", shiftHours: 4.43,
      returnScheduleId: 20, returnScheduleSlotId: 200, returnScheduleName: "June Return", returnShopName: "Second Shop",
      returnYear: 2026, returnMonth: 6, returnDayOfMonth: 3, returnFromTime: "10:00", returnToTime: "16:00", returnShiftHours: 6 })], error: null });
    renderPage();
    await user.click(screen.getByRole("button", { name: "View offer details for May Schedule" }));
    const details = screen.getByRole("dialog", { name: "Offer details" });
    expect(details).toHaveTextContent("Owner Worker");
    expect(details).toHaveTextContent("09:17–13:43"); expect(details).toHaveTextContent("10:00–16:00");
    await user.click(within(details).getByRole("button", { name: "Accept shift" }));
    const confirmation = screen.getByRole("dialog", { name: "Accept this shift?" });
    for (const text of ["May Schedule", "June Return", "09:17–13:43", "10:00–16:00", "Owner Worker"]) expect(confirmation).toHaveTextContent(text);
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
    await user.dblClick(within(confirmation).getByRole("button", { name: "Accept shift" }));
    expect(mocks.acceptMutate).toHaveBeenCalledTimes(1);
  });

  test("creates a private custom-period offer from one of the current employee shifts", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: "Create offer" }));
    await user.click(screen.getByRole("button", { name: /May Schedule/i }));

    const dialog = screen.getByRole("dialog", { name: "Create a swap offer" });
    await user.click(within(dialog).getByRole("radio", { name: /08:00.*16:00/i }));
    await user.click(within(dialog).getByRole("button", { name: "Custom period" }));
    await user.click(within(dialog).getByRole("button", { name: "Increase From" }));
    await user.click(within(dialog).getByRole("button", { name: "Decrease To" }));
    await user.click(within(dialog).getByRole("button", { name: "Confirm shift" }));

    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Specific employee" }));
    expect(screen.getByRole("button", { name: /Target Worker/i })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: /Target Worker/i }));
    await user.click(screen.getByRole("button", { name: "Save selection" }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Send request" }));

    expect(mocks.createMutate).toHaveBeenCalledWith(
      {
        requestKind: "give",
        scheduleId: 10,
        scheduleSlotId: 100,
        fromTime: "08:15",
        toTime: "15:45",
        targetEmployeeId: 7,
      },
      expect.objectContaining({
        onSuccess: expect.any(Function),
        onError: expect.any(Function),
      }),
    );
  });

  test("creates an offer from the selected fragment after a shift was split", async () => {
    const user = userEvent.setup();
    mocks.schedulesQuery.mockReturnValue({
      data: [{
        ...schedules[0],
        slots: [
          { id: 110, dayOfMonth: 1, slotNo: 1, employeeId: 12, fromTime: "09:00", toTime: "10:00", status: "ASSIGNED" },
          { id: 111, dayOfMonth: 1, slotNo: 2, employeeId: 12, fromTime: "11:00", toTime: "15:00", status: "ASSIGNED" },
        ],
      }],
      isLoading: false,
      error: null,
    });
    renderPage();

    await user.click(screen.getByRole("button", { name: "Create offer" }));
    await user.click(screen.getByRole("button", { name: /May Schedule/i }));
    const dialog = screen.getByRole("dialog", { name: "Create a swap offer" });
    await user.click(within(dialog).getByRole("radio", { name: /11:00.*15:00/i }));
    await user.click(within(dialog).getByRole("button", { name: "Custom period" }));
    await user.click(within(dialog).getByRole("button", { name: "Increase From" }));
    await user.click(within(dialog).getByRole("button", { name: "Decrease To" }));
    await user.click(within(dialog).getByRole("button", { name: "Confirm shift" }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Send request" }));

    expect(mocks.createMutate).toHaveBeenCalledWith(
      {
        requestKind: "give",
        scheduleId: 10,
        scheduleSlotId: 111,
        fromTime: "11:15",
        toTime: "14:45",
        targetEmployeeId: null,
      },
      expect.objectContaining({
        onSuccess: expect.any(Function),
        onError: expect.any(Function),
      }),
    );
  });

  test("keeps the offer action disabled until a shift is selected", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: "Create offer" }));

    expect(screen.getByRole("heading", { name: "Choose a schedule" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
    expect(mocks.createMutate).not.toHaveBeenCalled();
  });

  test("filters open swap offers by employee names and schedule name", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({
      data: [
        createSwap({
          id: 5,
          scheduleName: "Morning Core",
          fromEmployeeName: "Alicia Stone",
        }),
        createSwap({
          id: 6,
          scheduleName: "Night Support",
          fromEmployeeName: "Bohdan Reed",
          targetEmployeeName: "Marta Lane",
          visibility: "private",
        }),
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    const searchInput = screen.getByRole("searchbox", { name: "Search swaps by giver, receiver, date or schedule" });

    expect(screen.getByText("Morning Core")).toBeInTheDocument();
    expect(screen.getByText("Night Support")).toBeInTheDocument();

    await user.type(searchInput, "Marta");
    await waitFor(() => expect(screen.queryByText("Morning Core")).not.toBeInTheDocument());
    expect(screen.getByText("Night Support")).toBeInTheDocument();

    await user.clear(searchInput);
    await user.type(searchInput, "Alicia Stone");
    await waitFor(() => expect(screen.queryByText("Night Support")).not.toBeInTheDocument());
    expect(screen.getByText("Morning Core")).toBeInTheDocument();

    await user.clear(searchInput);
    await user.type(searchInput, "support");
    await waitFor(() => expect(screen.queryByText("Morning Core")).not.toBeInTheDocument());
    expect(screen.getByText("Night Support")).toBeInTheDocument();

    await user.clear(searchInput);
    await user.type(searchInput, "01/05/2026");
    await waitFor(() => expect(screen.getByText("Morning Core")).toBeInTheDocument());
    expect(screen.getByText("Night Support")).toBeInTheDocument();

    await user.clear(searchInput);
    await user.type(searchInput, "missing");
    await waitFor(() => expect(screen.queryByText("Night Support")).not.toBeInTheDocument());
    expect(screen.getByText('No open swap offers match "missing".')).toBeInTheDocument();
  });

  test("opens details without expanding the card and keeps pinning independent", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({
      data: [
        createSwap({ id: 5, scheduleName: "Morning Core" }),
        createSwap({ id: 6, scheduleName: "Night Support" }),
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    const card = screen.getByText("Night Support").closest("article");
    expect(card).not.toBeNull();
    await user.click(within(card as HTMLElement).getByText("Night Support"));

    expect(card).not.toHaveAttribute("data-expanded");
    expect(screen.getByRole("dialog", { name: "Offer details" })).toBeInTheDocument();
    expect(card).toHaveAttribute("data-pinned", "false");
    expect(mocks.setPinMutate).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(card).not.toHaveAttribute("data-expanded");
    expect(screen.getByRole("button", { name: "View offer details for Night Support" })).toHaveFocus();

    await user.click(within(card as HTMLElement).getByRole("button", { name: "Pin Night Support swap" }));

    expect(card).toHaveClass(styles.offerCardPinned);
    expect(card).toHaveAttribute("data-pinned", "true");
    expect(mocks.setPinMutate).toHaveBeenLastCalledWith({ swapId: 6, pinned: true });

    await user.click(within(card as HTMLElement).getByText("Night Support"));
    expect(card).not.toHaveAttribute("data-expanded");
    expect(screen.getByRole("dialog", { name: "Offer details" })).toBeInTheDocument();
    expect(card).toHaveAttribute("data-pinned", "true");

    if (screen.queryByRole("dialog", { name: "Offer details" })) await user.click(screen.getByRole("button", { name: "Close" }));
    await user.click(within(card as HTMLElement).getByRole("button", { name: "Unpin Night Support swap" }));
    expect(screen.getByRole("dialog", { name: "Unpin swap?" })).toBeInTheDocument();
    expect(card).toHaveAttribute("data-pinned", "true");

    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(card).toHaveAttribute("data-pinned", "true");

    if (screen.queryByRole("dialog", { name: "Offer details" })) await user.click(screen.getByRole("button", { name: "Close" }));
    await user.click(within(card as HTMLElement).getByRole("button", { name: "Unpin Night Support swap" }));
    await user.click(screen.getByRole("button", { name: "Unpin" }));
    expect(card).toHaveAttribute("data-pinned", "false");
    expect(mocks.setPinMutate).toHaveBeenLastCalledWith({ swapId: 6, pinned: false });
  });

  test("keeps long offer and history lists available in their dedicated panels", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({
      data: [
        ...Array.from({ length: 5 }, (_, index) => createSwap({
          id: index + 1,
          scheduleName: `Open ${index + 1}`,
        })),
        ...Array.from({ length: 5 }, (_, index) => createSwap({
          id: index + 101,
          scheduleName: `History ${index + 1}`,
          status: "accepted",
        })),
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    const offersSection = screen.getByRole("heading", { name: "Open swaps" }).closest("section");
    await user.click(screen.getByRole("button", { name: "Swap history" }));
    const historySection = screen.getByRole("heading", { name: "Recent swap activity" }).closest("section");
    expect(offersSection?.querySelector(`.${styles.offerListScrollable}`)).not.toBeNull();
    expect(historySection?.querySelector(`.${styles.offerListScrollable}`)).not.toBeNull();
  });

  test("accepts open offers, cancels own offers, and disables locked offers", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({
      data: [
        createSwap({ id: 5, canAccept: true, canCancel: false }),
        createSwap({
          id: 6,
          scheduleName: "Own offer",
          acceptanceUnavailableReason: "This is your own swap offer.",
          visibility: "private",
          targetEmployeeId: 7,
          targetEmployeeName: "Target Worker",
          isCreatedByCurrentEmployee: true,
          canAccept: false,
          canCancel: true,
        }),
        createSwap({
          id: 7,
          scheduleName: "Locked offer",
          isScheduleLocked: true,
          canAccept: false,
          canCancel: false,
        }),
        createSwap({
          id: 8,
          scheduleName: "Overlap offer",
          canAccept: false,
          canCancel: false,
          acceptanceUnavailableReason: "You already work during this time.",
          currentEmployeeHoursBefore: 8,
          currentEmployeeHoursAfter: 8,
          currentEmployeeWorkDaysBefore: 1,
          currentEmployeeWorkDaysAfter: 1,
          currentEmployeeFreeDaysBefore: 30,
          currentEmployeeFreeDaysAfter: 30,
        }),
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    const mayCard = screen.getByRole("button", { name: "Pin May Schedule swap" }).closest("article") as HTMLElement;
    await user.click(within(mayCard).getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    act(() => mocks.acceptMutate.mock.calls[0][1].onSuccess());
    const ownCard = screen.getByRole("button", { name: "Pin Own offer swap" }).closest("article") as HTMLElement;
    await user.click(within(ownCard).getByText("Own offer"));
    expect(within(ownCard).queryByRole("button", { name: "Accept shift" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel offer" }));
    act(() => mocks.cancelMutate.mock.calls[0][1].onSuccess());
    const lockedCard = screen.getByRole("button", { name: "Pin Locked offer swap" }).closest("article") as HTMLElement;
    await user.click(within(lockedCard).getByText("Locked offer"));
    expect(within(lockedCard).queryByRole("button", { name: "Accept shift" })).not.toBeInTheDocument();
    expect(within(screen.getByRole("dialog", { name: "Offer details" })).getByRole("button", { name: "Accept shift" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Close" }));
    const overlapCard = screen.getByRole("button", { name: "Pin Overlap offer swap" }).closest("article") as HTMLElement;
    await user.click(within(overlapCard).getByText("Overlap offer"));
    expect(within(overlapCard).queryByRole("button", { name: "Accept shift" })).not.toBeInTheDocument();

    expect(mocks.acceptMutate).toHaveBeenCalledWith(5, expect.objectContaining({
      onError: expect.any(Function),
    }));
    expect(mocks.cancelMutate).toHaveBeenCalledWith(6, expect.objectContaining({
      onError: expect.any(Function),
    }));
    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(screen.getByText("Schedule is locked while a manager is editing it.")).toBeInTheDocument();
    expect(screen.getByText("You already work during this time.")).toBeInTheDocument();
    expect(screen.getAllByText("Locked")).toHaveLength(1);
    expect(screen.getAllByText("Can")).toHaveLength(2);
    expect(screen.getAllByText("Can\u2019t")).toHaveLength(2);
    expect(screen.getByText("My offer")).toBeInTheDocument();
    expect(screen.getByText("This is your own swap offer.")).toBeInTheDocument();
    expect(screen.getByText("Private")).toBeInTheDocument();
  });
  test.each([
    ["public", {}],
    ["private", { visibility: "private", targetEmployeeId: 12 }],
    ["manager", { isManagerCreated: true, manualColumnName: "Manual counter" }],
    ["manager without column", { isManagerCreated: true, manualColumnName: null }],
  ] as [string, Partial<ShiftSwap>][])("confirms %s offer with full summary", async (_kind, overrides) => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap(overrides)], error: null });
    renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    const dialog = screen.getByRole("dialog", { name: "Accept this shift?" });
    expect(dialog.parentElement).toBe(document.body);
    const message = within(dialog).getByText(/Acceptance updates your schedule/);
    for (const detail of ["May Schedule", "Central Shop", "01.05.2026", "08:00–16:00", "8h",
      overrides.isManagerCreated ? overrides.manualColumnName || "Open shift" : "Owner Worker"]) {
      expect(message).toHaveTextContent(detail);
    }
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
  });

  test.each(["Back", "Escape", "backdrop"])("%s closes acceptance without a request", async method => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null });
    renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    if (method === "Back") await user.click(screen.getByRole("button", { name: "Back" }));
    if (method === "Escape") await user.keyboard("{Escape}");
    if (method === "backdrop") fireEvent.mouseDown(screen.getByRole("dialog", { name: "Accept this shift?" }));
    expect(screen.queryByRole("dialog", { name: "Accept this shift?" })).not.toBeInTheDocument();
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
  });

  test("confirm submits once, disables controls and closes on success", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null });
    renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    await user.dblClick(screen.getByRole("button", { name: "Accept shift" }));
    expect(mocks.acceptMutate).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("button", { name: "Accept shift" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Back" })).toBeDisabled();
    await user.keyboard("{Escape}");
    fireEvent.mouseDown(screen.getByRole("dialog", { name: "Accept this shift?" }));
    expect(screen.getByRole("dialog", { name: "Accept this shift?" })).toBeInTheDocument();
    act(() => mocks.acceptMutate.mock.calls[0][1].onSuccess());
    expect(screen.queryByRole("dialog", { name: "Accept this shift?" })).not.toBeInTheDocument();
  });

  test("server error refreshes authoritative data and allows fresh confirmation", async () => {
    const user = userEvent.setup();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null });
    renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    act(() => mocks.acceptMutate.mock.calls[0][1].onError(new Error("Server conflict")));
    expect(screen.getByText("Server conflict")).toBeInTheDocument();
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.employee() });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.employeeSchedules.all });
    await user.click(screen.getByRole("button", { name: "View offer details for May Schedule" }));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    expect(mocks.acceptMutate).toHaveBeenCalledTimes(2);
  });

  test.each(["missing", "ineligible", "terminal"])("rejects %s offer from updated cache", async kind => {
    const user = userEvent.setup();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null });
    const page = renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    mocks.swapsQuery.mockReturnValue({ data: kind === "missing" ? [] :
      [createSwap(kind === "ineligible" ? { canAccept: false } : { status: "accepted" })], error: null });
    page.rerender(<QueryClientProvider client={queryClient}><EmployeeSwapPage /></QueryClientProvider>);
    await user.click(screen.getByRole("button", { name: "Accept shift" }));
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
    expect(screen.getByText("This offer is no longer available. The swap list has been refreshed.")).toBeInTheDocument();
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.employee() });
  });

  test("localizes acceptance confirmation in Polish", async () => {
    setLanguage("pl");
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null });
    renderPage();
    await user.click(screen.getByText("May Schedule"));
    await user.click(screen.getByRole("button", { name: t("Accept shift") }));
    expect(screen.getByRole("dialog", { name: t("Accept this shift?") })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: t("Accept shift") })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: t("Back") }));
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
  });
  test.each(["creator", "recipient", "accepted recipient", "neutral"])("Exchange details preserve unequal durations and %s perspective", async viewpoint => {
    const user = userEvent.setup();
    const swap = createSwap({ requestKind: "exchange", fromEmployeeId: 7, targetEmployeeId: 9,
      targetEmployeeName: "Return Worker", visibility: "private", fromTime: "08:15", shiftHours: 7.75,
      returnScheduleName: "June Return", returnShopName: "North Store", returnYear: 2026, returnMonth: 6,
      returnDayOfMonth: 3, returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25,
      isCreatedByCurrentEmployee: viewpoint === "creator", canAccept: viewpoint === "recipient",
      ...(viewpoint === "recipient" ? { targetEmployeeId: 12 } : {}),
      ...(viewpoint === "accepted recipient" ? { status: "accepted", acceptedByEmployeeId: 12, acceptedByEmployeeName: "Viewer", canAccept: false } : {}),
    });
    mocks.swapsQuery.mockReturnValue({ data: [swap], error: null }); renderPage();
    if (viewpoint === "accepted recipient") await user.click(screen.getByRole("button", { name: "Swap history" }));
    const launcher = screen.getByRole("button", { name: "View offer details for May Schedule" });
    expect(within(launcher).getByText("Swap shifts")).toBeInTheDocument();
    expect(within(launcher).queryByText("June Return")).not.toBeInTheDocument();
    await user.click(launcher);
    const details = within(screen.getByRole("dialog", { name: "Offer details" }));
    const source = details.getByRole("region", { name: viewpoint === "creator" ? "Your shift (you give)" : viewpoint === "neutral" ? "Offered shift" : "Shift you receive" });
    const returned = details.getByRole("region", { name: viewpoint === "creator" ? "Shift you receive" : viewpoint === "neutral" ? "Return shift" : "Your shift (you give)" });
    expect(source).toHaveTextContent("May Schedule"); expect(source).toHaveTextContent("Central Shop");
    expect(source).toHaveTextContent("08:15–16:00"); expect(source).toHaveTextContent("7.75h");
    expect(returned).toHaveTextContent("June Return"); expect(returned).toHaveTextContent("North Store");
    expect(returned).toHaveTextContent("10:00–14:15"); expect(returned).toHaveTextContent("4.25h");
    if (viewpoint !== "neutral") {
      expect(within(source).getByText(viewpoint === "creator" ? "Give" : "Receive")).toBeInTheDocument();
      expect(within(returned).getByText(viewpoint === "creator" ? "Receive" : "Give")).toBeInTheDocument();
    } else {
      expect(details.queryByText("Give", { exact: true })).not.toBeInTheDocument();
      expect(details.queryByText("Receive", { exact: true })).not.toBeInTheDocument();
    }
    expect(details.queryByText("Your month")).not.toBeInTheDocument();
    if (swap.status === "accepted") {
      expect(details.queryByRole("button", { name: "Accept shift" })).not.toBeInTheDocument();
      expect(details.queryByRole("button", { name: "Cancel offer" })).not.toBeInTheDocument();
      expect(details.getByText("Viewer")).toBeInTheDocument();
    }
    await user.click(details.getByRole("button", { name: "Close" })); expect(launcher).toHaveFocus();
    if (swap.status === "accepted") expect(screen.getByRole("dialog", { name: "Recent swap activity" })).toBeInTheDocument();
  });

  test.each(["cancel", "backdrop", "close"])("details %s restores focus and locks background without mutating", async method => {
    const user = userEvent.setup(); mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null }); renderPage();
    const launcher = screen.getByRole("button", { name: "View offer details for May Schedule" });
    launcher.focus(); await user.keyboard("{Enter}");
    const dialog = screen.getByRole("dialog", { name: "Offer details" });
    expect(document.body.style.position).toBe("fixed");
    if (method === "cancel") fireEvent(dialog, new Event("cancel", { bubbles: true, cancelable: true }));
    else if (method === "backdrop") fireEvent.click(dialog);
    else await user.click(within(dialog).getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("dialog", { name: "Offer details" })).not.toBeInTheDocument();
    expect(document.body.style.position).not.toBe("fixed"); expect(launcher).toHaveFocus();
    expect(mocks.acceptMutate).not.toHaveBeenCalled(); expect(mocks.cancelMutate).not.toHaveBeenCalled();
  });

  test("cancel error stays in details and successful retry closes it", async () => {
    const user = userEvent.setup(); mocks.swapsQuery.mockReturnValue({ data: [createSwap({ isCreatedByCurrentEmployee: true, canAccept: false, canCancel: true })], error: null }); renderPage();
    await user.click(screen.getByRole("button", { name: "View offer details for May Schedule" }));
    await user.click(screen.getByRole("button", { name: "Cancel offer" }));
    act(() => mocks.cancelMutate.mock.calls[0][1].onError(new Error("Cancel failed")));
    expect(within(screen.getByRole("dialog", { name: "Offer details" })).getByText("Cancel failed")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel offer" }));
    act(() => mocks.cancelMutate.mock.calls[1][1].onSuccess());
    expect(screen.queryByRole("dialog", { name: "Offer details" })).not.toBeInTheDocument();
  });

  test.each(["missing", "locked", "accepted"])("details refreshes current %s offer and never accepts stale data", async change => {
    const user = userEvent.setup(); mocks.swapsQuery.mockReturnValue({ data: [createSwap()], error: null }); const page = renderPage();
    await user.click(screen.getByRole("button", { name: "View offer details for May Schedule" }));
    mocks.swapsQuery.mockReturnValue({ data: change === "missing" ? [] : [createSwap(change === "locked" ? { canAccept: false, isScheduleLocked: true } : { status: "accepted" })], error: null });
    page.rerender(<QueryClientProvider client={queryClient}><EmployeeSwapPage /></QueryClientProvider>);
    if (change === "missing") {
      expect(screen.queryByRole("dialog", { name: "Offer details" })).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Create offer" })).toHaveFocus();
    } else if (change === "locked") expect(screen.getByRole("button", { name: "Accept shift" })).toBeDisabled();
    else {
      expect(screen.queryByRole("button", { name: "Accept shift" })).not.toBeInTheDocument();
      await user.click(screen.getByRole("button", { name: "Close" }));
      expect(screen.getByRole("button", { name: "Create offer" })).toHaveFocus();
    }
    expect(mocks.acceptMutate).not.toHaveBeenCalled();
  });

  test.each([true, false])("legacy return duration only derives from valid bounds: %s", async valid => {
    const user = userEvent.setup(); mocks.swapsQuery.mockReturnValue({ data: [createSwap({ requestKind: "exchange", returnScheduleName: "Return", returnShiftHours: null,
      returnFromTime: valid ? "10:00" : null, returnToTime: "14:15" })], error: null }); renderPage();
    await user.click(screen.getByRole("button", { name: "View offer details for May Schedule" }));
    const panel = screen.getByRole("region", { name: "Return shift" });
    expect(panel).not.toHaveTextContent("undefined");
    if (valid) expect(panel).toHaveTextContent("4.25h"); else expect(within(panel).queryByText(/Shift hours/)).not.toBeInTheDocument();
    expect(panel).not.toHaveTextContent("Central Shop");
  });

  test("History locks independent root scrolling, preserves nested locks and restores focus and styles", async () => {
    const user = userEvent.setup();
    mocks.swapsQuery.mockReturnValue({ data: [createSwap({ status: "accepted", canAccept: false, canCancel: false })], error: null });
    const root = document.createElement("div"); root.id = "root"; root.style.overflowY = "auto"; root.scrollTop = 123;
    const container = document.createElement("div"); root.append(container); document.body.append(root);
    const previousBody = document.body.style.cssText;
    const page = render(<QueryClientProvider client={queryClient}><EmployeeSwapPage /></QueryClientProvider>, { container });
    try {
      const launcher = screen.getByRole("button", { name: "Swap history" }); await user.click(launcher);
      const history = screen.getByRole("dialog", { name: "Recent swap activity" });
      expect(history.parentElement).toBe(document.body); expect(root).not.toContainElement(history);
      expect(root.style.overflowY).toBe("hidden"); expect(document.body.style.position).toBe("fixed");
      const offerLauncher = within(history).getByRole("button", { name: "View offer details for May Schedule" });
      await user.click(offerLauncher);
      await user.click(within(screen.getByRole("dialog", { name: "Offer details" })).getByRole("button", { name: "Close" }));
      expect(offerLauncher).toHaveFocus(); expect(root.style.overflowY).toBe("hidden"); expect(document.body.style.position).toBe("fixed");
      fireEvent(history, new Event("cancel", { bubbles: true, cancelable: true }));
      expect(root.style.overflowY).toBe("auto"); expect(root.scrollTop).toBe(123);
      expect(document.body.style.cssText).toBe(previousBody); expect(launcher).toHaveFocus();
      await user.click(launcher); await user.click(within(history).getByRole("button", { name: "Close" }));
      expect(root.style.overflowY).toBe("auto"); expect(root.scrollTop).toBe(123);
    } finally { page.unmount(); root.remove(); }
  });

  test("History unpin confirmation stays inside its native dialog and Escape keeps History open", async () => {
    const user = userEvent.setup(); mocks.initialPinnedSwapIds = [5];
    mocks.swapsQuery.mockReturnValue({ data: [createSwap({ status: "cancelled", canAccept: false, canCancel: false })], error: null });
    renderPage(); await user.click(screen.getByRole("button", { name: "Swap history" }));
    const history = screen.getByRole("dialog", { name: "Recent swap activity" });
    await user.click(within(history).getByRole("button", { name: "Unpin May Schedule swap" }));
    expect(history).toContainElement(screen.getByRole("dialog", { name: "Unpin swap?" }));
    fireEvent(history, new Event("cancel", { bubbles: true, cancelable: true }));
    expect(screen.queryByRole("dialog", { name: "Unpin swap?" })).not.toBeInTheDocument();
    expect(history).toHaveAttribute("open"); expect(document.body.style.position).toBe("fixed");
    await user.click(within(history).getByRole("button", { name: "Unpin May Schedule swap" }));
    await user.click(screen.getByRole("button", { name: "Unpin", exact: true }));
    expect(mocks.setPinMutate).toHaveBeenLastCalledWith({ swapId: 5, pinned: false });
    expect(history).toHaveAttribute("open");
  });

});
