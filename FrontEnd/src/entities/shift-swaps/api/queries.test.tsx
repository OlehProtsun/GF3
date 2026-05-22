import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useAcceptEmployeeShiftSwapMutation,
  useCancelEmployeeShiftSwapMutation,
  useCancelManagerManualShiftSwapMutation,
  useCreateEmployeeShiftSwapMutation,
  useCreateManagerManualShiftSwapMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  createEmployee: vi.fn(),
  acceptEmployee: vi.fn(),
  cancelEmployee: vi.fn(),
  createManagerManual: vi.fn(),
  cancelManagerManual: vi.fn(),
}));

vi.mock("./shiftSwapsApi", () => ({
  shiftSwapsApi: apiMocks,
}));

function MutationHarness() {
  const createEmployee = useCreateEmployeeShiftSwapMutation();
  const acceptEmployee = useAcceptEmployeeShiftSwapMutation();
  const cancelEmployee = useCancelEmployeeShiftSwapMutation();
  const createManager = useCreateManagerManualShiftSwapMutation();
  const cancelManager = useCancelManagerManualShiftSwapMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => createEmployee.mutate({
          scheduleId: 10,
          scheduleSlotId: 20,
          fromTime: "09:00",
          toTime: "13:00",
          targetEmployeeId: null,
        })}
      >
        create employee
      </button>
      <button type="button" onClick={() => acceptEmployee.mutate(5)}>
        accept employee
      </button>
      <button type="button" onClick={() => cancelEmployee.mutate(6)}>
        cancel employee
      </button>
      <button
        type="button"
        onClick={() => createManager.mutate({
          containerId: 2,
          graphId: 10,
          manualColumnId: 7,
          dayOfMonth: 1,
          fromTime: "09:00",
          toTime: "13:00",
        })}
      >
        create manager
      </button>
      <button
        type="button"
        onClick={() => cancelManager.mutate({
          containerId: 2,
          graphId: 10,
          id: 99,
        })}
      >
        cancel manager
      </button>
    </section>
  );
}

function renderHarness(client: QueryClient) {
  return render(
    <QueryClientProvider client={client}>
      <MutationHarness />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  Object.values(apiMocks).forEach(mock => mock.mockReset());
  apiMocks.createEmployee.mockResolvedValue({});
  apiMocks.acceptEmployee.mockResolvedValue({});
  apiMocks.cancelEmployee.mockResolvedValue({});
  apiMocks.createManagerManual.mockResolvedValue({});
  apiMocks.cancelManagerManual.mockResolvedValue(undefined);
});

describe("shift swap query mutations", () => {
  test("invalidates employee swap and schedule caches after employee actions", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const events: string[] = [];
    client.subscribe(event => events.push(`${event.type}:${event.key}`));
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create employee" }));
    await waitFor(() => {
      expect(apiMocks.createEmployee).toHaveBeenCalledTimes(1);
      expect(events).toContain(`invalidate:${JSON.stringify(queryKeys.shiftSwaps.employee())}`);
    });

    await user.click(screen.getByRole("button", { name: "accept employee" }));
    await waitFor(() => {
      expect(apiMocks.acceptEmployee).toHaveBeenCalledWith(5);
      expect(events).toContain(`invalidate:${JSON.stringify(queryKeys.employeeSchedules.all)}`);
    });

    await user.click(screen.getByRole("button", { name: "cancel employee" }));
    await waitFor(() => {
      expect(apiMocks.cancelEmployee).toHaveBeenCalledWith(6);
    });

    expect(events.filter(event => event === `invalidate:${JSON.stringify(queryKeys.shiftSwaps.employee())}`))
      .toHaveLength(3);
  });

  test("invalidates graph logs and graph slots after manager manual swap actions", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    const events: string[] = [];
    client.subscribe(event => events.push(`${event.type}:${event.key}`));
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create manager" }));
    await waitFor(() => {
      expect(apiMocks.createManagerManual).toHaveBeenCalledWith({
        containerId: 2,
        graphId: 10,
        manualColumnId: 7,
        dayOfMonth: 1,
        fromTime: "09:00",
        toTime: "13:00",
      });
    });

    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.graphLog(2, 10) });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.all });
    expect(events).toContain(`invalidate:${JSON.stringify(queryKeys.shiftSwaps.graphLog(2, 10))}`);

    await user.click(screen.getByRole("button", { name: "cancel manager" }));
    await waitFor(() => {
      expect(apiMocks.cancelManagerManual).toHaveBeenCalledWith(2, 10, 99);
    });

    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: queryKeys.containers.graphSlots(2, 10) });
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.shiftSwaps.graphLog(2, 10) },
      { queryKey: queryKeys.shiftSwaps.all },
      { queryKey: queryKeys.shiftSwaps.graphLog(2, 10) },
      { queryKey: queryKeys.shiftSwaps.all },
      { queryKey: queryKeys.containers.graphSlots(2, 10) },
    ]);
    expect(events).toContain(`invalidate:${JSON.stringify(queryKeys.containers.graphSlots(2, 10))}`);
  });
});
