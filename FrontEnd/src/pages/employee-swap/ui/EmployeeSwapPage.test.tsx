import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwap, ShiftSwapEmployee } from "@entities/shift-swaps";
import { EmployeeSwapPage } from "./EmployeeSwapPage";

const mocks = vi.hoisted(() => ({
  schedulesQuery: vi.fn(),
  swapsQuery: vi.fn(),
  employeesQuery: vi.fn(),
  createMutate: vi.fn(),
  acceptMutate: vi.fn(),
  cancelMutate: vi.fn(),
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

vi.mock("@entities/shift-swaps", () => ({
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
  mocks.schedulesQuery.mockReset();
  mocks.swapsQuery.mockReset();
  mocks.employeesQuery.mockReset();
  mocks.createMutate.mockReset();
  mocks.acceptMutate.mockReset();
  mocks.cancelMutate.mockReset();
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

  test("keeps the offer action disabled until a shift is selected", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: "Expand give away a shift" }));

    expect(screen.getByText("Choose a schedule, then select one of your shifts from the dialog.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Offer shift" })).toBeDisabled();
    expect(mocks.createMutate).not.toHaveBeenCalled();
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

    await user.click(screen.getByRole("button", { name: "Expand May Schedule swap" }));
    await user.click(screen.getByRole("button", { name: "Accept" }));
    await user.click(screen.getByRole("button", { name: "Expand Own offer swap" }));
    await user.click(screen.getByRole("button", { name: "Cancel offer" }));
    await user.click(screen.getByRole("button", { name: "Expand Locked offer swap" }));
    await user.click(screen.getByRole("button", { name: "Expand Overlap offer swap" }));

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
