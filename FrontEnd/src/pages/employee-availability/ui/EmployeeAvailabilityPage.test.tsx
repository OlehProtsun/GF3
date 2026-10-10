import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import { AVAILABILITY_KIND_ANY, AVAILABILITY_KIND_INTERVAL, AVAILABILITY_KIND_NONE } from "@entities/availability-groups/model/editor";
import { EmployeeAvailabilityPage } from "./EmployeeAvailabilityPage";

const mocks = vi.hoisted(() => ({
  availabilityQuery: vi.fn(),
  mutate: vi.fn(),
  mutationState: {
    isPending: false,
    error: null as Error | null,
  },
  employeeId: 12,
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ session: { employeeId: mocks.employeeId } }),
}));

vi.mock("@entities/employee-availability", () => ({
  useEmployeeAvailabilityListQuery: () => mocks.availabilityQuery(),
  useSaveEmployeeAvailabilityMutation: () => ({
    mutate: mocks.mutate,
    isPending: mocks.mutationState.isPending,
    error: mocks.mutationState.error,
  }),
}));

const openAvailability: EmployeeAvailabilityGroup = {
  id: 5,
  name: "May Availability",
  year: 2026,
  month: 5,
  visibleFromUtc: "2026-05-01T08:00:00.000Z",
  visibleToUtc: "2026-05-20T18:00:00.000Z",
  canSubmit: true,
  isEditLocked: false,
  slots: [
    {
      id: 1,
      availabilityGroupMemberId: 50,
      dayOfMonth: 1,
      kind: AVAILABILITY_KIND_NONE,
      intervalStr: null,
    },
    {
      id: 2,
      availabilityGroupMemberId: 50,
      dayOfMonth: 2,
      kind: AVAILABILITY_KIND_ANY,
      intervalStr: null,
    },
  ],
};

function renderPage() {
  window.history.replaceState({}, "", "/availability");

  return render(
    <BrowserRouter>
      <EmployeeAvailabilityPage />
    </BrowserRouter>,
  );
}

function getDayButton(dayOfMonth: number) {
  const dayLabel = screen.getByText(String(dayOfMonth), { selector: "span" });
  const button = dayLabel.closest("button");

  if (!button) {
    throw new Error(`Day ${dayOfMonth} button was not found.`);
  }

  return button;
}

beforeEach(() => {
  window.localStorage.clear();
  mocks.employeeId = 12;
  mocks.availabilityQuery.mockReset();
  mocks.mutate.mockReset();
  mocks.mutationState.isPending = false;
  mocks.mutationState.error = null;
});

describe("EmployeeAvailabilityPage", () => {
  test("saves an edited preset across days and remounts without changing the current day", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false, error: null });
    const view = renderPage();
    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog", { name: "Day 1 availability" });
    await user.click(within(dialog).getByRole("button", { name: "Edit preset 9:00 - 15:00" }));
    const input = within(dialog).getByRole("textbox", { name: "Time range" });
    await user.clear(input);
    await user.type(input, "830-1630");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));
    expect(within(dialog).getByRole("textbox", { name: "Custom" })).toHaveValue("");
    expect(mocks.mutate).not.toHaveBeenCalled();
    await user.click(within(dialog).getByRole("button", { name: "Close" }));
    expect(getDayButton(1)).toHaveTextContent("-");
    view.unmount();

    renderPage();
    await user.click(getDayButton(3));
    const nextDialog = screen.getByRole("dialog", { name: "Day 3 availability" });
    await user.click(within(nextDialog).getByRole("button", { name: "8:30 - 16:30", exact: true }));
    await user.click(within(nextDialog).getByRole("button", { name: "Save" }));
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(mocks.mutate.mock.calls[0][0].payload.slots[2]).toEqual({
      dayOfMonth: 3, kind: AVAILABILITY_KIND_INTERVAL, intervalStr: "08:30 - 16:30",
    });
  });

  test("validates preset ranges and cancels without changing the preset or day draft", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false, error: null });
    renderPage();
    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog", { name: "Day 1 availability" });
    await user.click(within(dialog).getByRole("button", { name: "15:00 - 21:00", exact: true }));
    await user.click(within(dialog).getByRole("button", { name: "Edit preset 9:00 - 15:00" }));
    const input = within(dialog).getByRole("textbox", { name: "Time range" });
    for (const value of ["", "20:00 - 09:00", "+", "day off", "25:00 - 26:00"]) {
      await user.clear(input);
      if (value) await user.type(input, value);
      expect(within(dialog).getByRole("button", { name: "Save" })).toBeDisabled();
    }
    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));
    expect(within(dialog).getByRole("textbox", { name: "Custom" })).toHaveValue("15:00 - 21:00");
    expect(within(dialog).getByRole("button", { name: "9:00 - 15:00", exact: true })).toBeInTheDocument();
    expect(window.localStorage.length).toBe(0);
  });

  test("keeps presets separate when the employee account changes", async () => {
    const user = userEvent.setup();
    window.localStorage.setItem("gf3:employee-availability:time-presets:12", JSON.stringify([
      "08:00 - 16:00", "15:00 - 21:00", "09:00 - 21:00",
    ]));
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false, error: null });
    const view = renderPage();
    await user.click(getDayButton(1));
    expect(screen.getByRole("button", { name: "8:00 - 16:00", exact: true })).toBeInTheDocument();
    mocks.employeeId = 13;
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    expect(screen.queryByRole("button", { name: "8:00 - 16:00", exact: true })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "9:00 - 15:00", exact: true })).toBeInTheDocument();
  });

  test("reports blocked storage writes and allows cancelling the preset edit", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false, error: null });
    renderPage();
    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog", { name: "Day 1 availability" });
    await user.click(within(dialog).getByRole("button", { name: "Edit preset 9:00 - 15:00" }));
    const spy = vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("blocked"); });
    try {
      await user.click(within(dialog).getByRole("button", { name: "Save" }));
      expect(within(dialog).getByRole("alert")).toHaveTextContent("Could not save this preset.");
      await user.click(within(dialog).getByRole("button", { name: "Cancel" }));
      expect(within(dialog).getByRole("button", { name: "9:00 - 15:00", exact: true })).toBeInTheDocument();
    } finally {
      spy.mockRestore();
    }
  });

  test.each(["{invalid", JSON.stringify(["bad", "15:00 - 21:00", "09:00 - 21:00"])])(
    "falls back to default presets for malformed stored values: %s", async stored => {
      const user = userEvent.setup();
      window.localStorage.setItem("gf3:employee-availability:time-presets:12", stored);
      mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false, error: null });
      renderPage();
      await user.click(getDayButton(1));
      expect(screen.getByRole("button", { name: "9:00 - 15:00", exact: true })).toBeInTheDocument();
    },
  );

  test("edits a day value and saves a full month payload", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({
      data: [openAvailability],
      isLoading: false,
      error: null,
    });
    mocks.mutate.mockImplementation((
      _payload: {
        id: number;
        payload: { slots: Array<{ dayOfMonth: number; kind: number; intervalStr: string | null }> };
      },
      options?: { onSuccess?: (availability: EmployeeAvailabilityGroup) => void },
    ) => {
      options?.onSuccess?.({
        ...openAvailability,
        slots: _payload.payload.slots.map((slot: { dayOfMonth: number; kind: number; intervalStr: string | null }) => ({
          id: slot.dayOfMonth,
          availabilityGroupMemberId: 50,
          ...slot,
        })),
      });
    });

    renderPage();

    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog", { name: "Day 1 availability" });

    await user.click(within(dialog).getByRole("button", { name: "15:00 - 21:00" }));
    await user.click(within(dialog).getByRole("button", { name: "Save" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(mocks.mutate).toHaveBeenCalledTimes(1);
    const [payload] = mocks.mutate.mock.calls[0];

    expect(payload.id).toBe(5);
    expect(payload.payload.slots).toHaveLength(31);
    expect(payload.payload.slots[0]).toEqual({
      dayOfMonth: 1,
      kind: AVAILABILITY_KIND_INTERVAL,
      intervalStr: "15:00 - 21:00",
    });
    expect(payload.payload.slots[1]).toEqual({
      dayOfMonth: 2,
      kind: AVAILABILITY_KIND_ANY,
      intervalStr: null,
    });
    expect(payload.payload.slots[30]).toEqual({
      dayOfMonth: 31,
      kind: AVAILABILITY_KIND_NONE,
      intervalStr: null,
    });
    expect(await screen.findByText("Availability saved successfully.")).toBeInTheDocument();
  });

  test("blocks edits when the manager lock is active", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({
      data: [
        {
          ...openAvailability,
          isEditLocked: true,
          editLockedBy: "Manager Anna",
        },
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    expect(await screen.findByText("This availability is currently being edited by Manager Anna. You cannot edit it right now."))
      .toBeInTheDocument();

    await user.click(getDayButton(1));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  test("shows the published-window empty state", () => {
    mocks.availabilityQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });

    renderPage();

    expect(screen.getByText("Nothing is public for your account yet.")).toBeInTheDocument();
    expect(screen.getByText("When a manager publishes an availability window for you, it will show up here.")).toBeInTheDocument();
  });
});
