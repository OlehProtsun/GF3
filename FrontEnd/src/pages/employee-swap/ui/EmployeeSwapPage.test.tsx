import { render, screen, waitFor, within } from "@testing-library/react";
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
    email: "target@example.com",
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
    fromEmployeeId: 12,
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

function renderPage() {
  return render(<EmployeeSwapPage />);
}

beforeEach(() => {
  window.localStorage.clear();
  mocks.schedulesQuery.mockReset();
  mocks.swapsQuery.mockReset();
  mocks.employeesQuery.mockReset();
  mocks.createMutate.mockReset();
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
  test("creates a private custom-period offer from one of the current employee shifts", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: "Expand give away a shift" }));
    await user.click(screen.getByRole("button", { name: /May Schedule/i }));

    const dialog = screen.getByRole("dialog", { name: "Choose shift" });
    await user.click(within(dialog).getByRole("button", { name: /08:00 - 16:00/i }));
    await user.click(within(dialog).getByRole("button", { name: "Custom period" }));
    await user.click(within(dialog).getByRole("button", { name: "Increase start time" }));
    await user.click(within(dialog).getByRole("button", { name: "Decrease end time" }));
    await user.click(within(dialog).getByRole("button", { name: "Choose shift" }));

    await user.click(screen.getByRole("button", { name: "Specific employee" }));
    expect(screen.getByRole("button", { name: /Target Worker/i })).toBeEnabled();
    await user.click(screen.getByRole("button", { name: "Offer shift" }));

    expect(mocks.createMutate).toHaveBeenCalledWith(
      {
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

    await user.click(screen.getByRole("button", { name: "Expand give away a shift" }));
    await user.click(screen.getByRole("button", { name: /May Schedule/i }));
    const dialog = screen.getByRole("dialog", { name: "Choose shift" });
    await user.click(within(dialog).getByRole("button", { name: /11:00 - 15:00/i }));
    await user.click(within(dialog).getByRole("button", { name: "Custom period" }));
    await user.click(within(dialog).getByRole("button", { name: "Increase start time" }));
    await user.click(within(dialog).getByRole("button", { name: "Decrease end time" }));
    await user.click(within(dialog).getByRole("button", { name: "Choose shift" }));
    await user.click(screen.getByRole("button", { name: "Offer shift" }));

    expect(mocks.createMutate).toHaveBeenCalledWith(
      {
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

    await user.click(screen.getByRole("button", { name: "Expand give away a shift" }));

    expect(screen.getByText("Choose a schedule, then select one of your shifts from the dialog.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Offer shift" })).toBeDisabled();
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

  test("toggles details from the card and controls pinning from the pin button", async () => {
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

    expect(card).toHaveAttribute("data-expanded", "true");
    expect(card).toHaveAttribute("data-pinned", "false");
    expect(mocks.setPinMutate).not.toHaveBeenCalled();

    await user.click(within(card as HTMLElement).getByText("Night Support"));
    expect(card).toHaveAttribute("data-expanded", "false");

    await user.click(within(card as HTMLElement).getByRole("button", { name: "Pin Night Support swap" }));

    expect(card).toHaveClass(styles.offerCardPinned);
    expect(card).toHaveAttribute("data-pinned", "true");
    expect(mocks.setPinMutate).toHaveBeenLastCalledWith({ swapId: 6, pinned: true });

    await user.click(within(card as HTMLElement).getByText("Night Support"));
    expect(card).toHaveAttribute("data-expanded", "true");
    expect(card).toHaveAttribute("data-pinned", "true");

    await user.click(within(card as HTMLElement).getByRole("button", { name: "Unpin Night Support swap" }));
    expect(screen.getByRole("dialog", { name: "Unpin swap?" })).toBeInTheDocument();
    expect(card).toHaveAttribute("data-pinned", "true");

    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(card).toHaveAttribute("data-pinned", "true");

    await user.click(within(card as HTMLElement).getByRole("button", { name: "Unpin Night Support swap" }));
    await user.click(screen.getByRole("button", { name: "Unpin" }));
    expect(card).toHaveAttribute("data-pinned", "false");
    expect(mocks.setPinMutate).toHaveBeenLastCalledWith({ swapId: 6, pinned: false });
  });

  test("adds independent scroll containers when offers and history contain five swaps", () => {
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
    await user.click(screen.getByRole("button", { name: "Accept" }));
    const ownCard = screen.getByRole("button", { name: "Pin Own offer swap" }).closest("article") as HTMLElement;
    await user.click(within(ownCard).getByText("Own offer"));
    await user.click(screen.getByRole("button", { name: "Cancel offer" }));
    const lockedCard = screen.getByRole("button", { name: "Pin Locked offer swap" }).closest("article") as HTMLElement;
    await user.click(within(lockedCard).getByText("Locked offer"));
    const overlapCard = screen.getByRole("button", { name: "Pin Overlap offer swap" }).closest("article") as HTMLElement;
    await user.click(within(overlapCard).getByText("Overlap offer"));

    expect(mocks.acceptMutate).toHaveBeenCalledWith(5, expect.objectContaining({
      onError: expect.any(Function),
    }));
    expect(mocks.cancelMutate).toHaveBeenCalledWith(6, expect.objectContaining({
      onError: expect.any(Function),
    }));
    expect(screen.getByText("Schedule is locked while a manager is editing it.")).toBeInTheDocument();
    expect(screen.getByText("You already work during this time.")).toBeInTheDocument();
    expect(screen.getAllByText("Locked")).toHaveLength(1);
    expect(screen.getAllByText("Can")).toHaveLength(1);
    expect(screen.getAllByText("Can\u2019t")).toHaveLength(3);
    expect(screen.getByText("Private")).toBeInTheDocument();
  });
});
