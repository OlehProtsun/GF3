import { render, renderHook, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { useState } from "react";
import {
  useGraphShiftSwapHighlightSettingQuery,
  useGraphManualShiftHighlightSettingQuery,
  useSaveGraphManualShiftHighlightSettingMutation,
  useAcceptEmployeeShiftSwapMutation,
  useCancelEmployeeShiftSwapMutation,
  useCancelManagerManualShiftSwapMutation,
  useCreateEmployeeShiftSwapMutation,
  useCreateManagerManualShiftSwapMutation,
  useShiftSwapReturnCandidatesQuery,
  useShiftSwapPreviewQuery,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  getHighlightSetting: vi.fn(),
  getManualHighlightSetting: vi.fn(),
  saveManualHighlightSetting: vi.fn(),
  createEmployee: vi.fn(),
  acceptEmployee: vi.fn(),
  cancelEmployee: vi.fn(),
  createManagerManual: vi.fn(),
  cancelManagerManual: vi.fn(),
  listReturnCandidates: vi.fn(),
  previewEmployee: vi.fn(),
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
      <button type="button" onClick={() => acceptEmployee.mutate({ id: 5 })}>
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
  apiMocks.getHighlightSetting.mockResolvedValue({ highlightColor: "#BBF7D0" });
  apiMocks.createEmployee.mockResolvedValue({});
  apiMocks.acceptEmployee.mockResolvedValue({});
  apiMocks.cancelEmployee.mockResolvedValue({});
  apiMocks.createManagerManual.mockResolvedValue({});
  apiMocks.cancelManagerManual.mockResolvedValue(undefined);
});

describe("shift swap query mutations", () => {
  test("manual setting caches confirmed values independently and preserves them on failed save", async () => {
    const client = new QueryClient();
    apiMocks.getManualHighlightSetting.mockResolvedValue({ highlightColor: "#BAE6FD" });
    apiMocks.saveManualHighlightSetting.mockResolvedValue({ highlightColor: "#AABBCC" });
    const wrapper = ({ children }: { children: React.ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    const { result } = renderHook(() => ({ query: useGraphManualShiftHighlightSettingQuery(1, 2), mutation: useSaveGraphManualShiftHighlightSettingMutation() }), { wrapper });
    await waitFor(() => expect(result.current.query.data?.highlightColor).toBe("#BAE6FD"));
    result.current.mutation.mutate({ containerId: 1, graphId: 2, highlightColor: "#AABBCC" });
    await waitFor(() => expect(result.current.query.data?.highlightColor).toBe("#AABBCC"));
    expect(client.getQueryState(queryKeys.shiftSwaps.graphHighlightSetting(1, 2)).data).toBeUndefined();
    apiMocks.saveManualHighlightSetting.mockRejectedValue(new Error("Conflict"));
    result.current.mutation.mutate({ containerId: 1, graphId: 2, highlightColor: "#112233" });
    await waitFor(() => expect(result.current.mutation.error).toBeTruthy());
    expect(result.current.query.data?.highlightColor).toBe("#AABBCC");
  });
  test("keeps candidate reads disabled without a draft and follows candidate pagination", async () => {
    apiMocks.listReturnCandidates.mockImplementation(async ({ page }: { page: number }) => ({ items: [{ slotId: page }], page, pageSize: 1, hasMore: page < 2 }));
    apiMocks.previewEmployee.mockResolvedValue({ isValid: true });
    function Reads() {
      const [enabled, setEnabled] = useState(false);
      const candidates = useShiftSwapReturnCandidatesQuery(enabled ? { sourceScheduleId: 10, sourceSlotId: 20, fromTime: "09:00", toTime: "13:00", targetEmployeeId: 7, page: 2, pageSize: 1 } : null);
      const preview = useShiftSwapPreviewQuery(enabled ? { requestKind: "give", scheduleId: 10, scheduleSlotId: 20, fromTime: "09:00", toTime: "13:00" } : null);
      return <><button onClick={() => setEnabled(true)}>load draft</button><output>{candidates.data?.items.length ?? 0}:{String(preview.data?.isValid ?? false)}</output></>;
    }
    render(<QueryClientProvider client={new QueryClient()}><Reads /></QueryClientProvider>);
    expect(apiMocks.listReturnCandidates).not.toHaveBeenCalled();
    expect(apiMocks.previewEmployee).not.toHaveBeenCalled();
    await userEvent.setup().click(screen.getByRole("button", { name: "load draft" }));
    await waitFor(() => expect(screen.getByText("2:true")).toBeInTheDocument());
    expect(apiMocks.listReturnCandidates).toHaveBeenCalledTimes(2);
    expect(apiMocks.listReturnCandidates).toHaveBeenLastCalledWith(expect.objectContaining({ targetEmployeeId: 7, page: 2 }), expect.any(AbortSignal));
  });
  test("invalidates employee swap and schedule caches after employee actions", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
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
      expect(apiMocks.acceptEmployee).toHaveBeenCalledWith({ id: 5 });
      expect(events).toContain(`invalidate:${JSON.stringify(queryKeys.employeeSchedules.all)}`);
    });

    await user.click(screen.getByRole("button", { name: "cancel employee" }));
    await waitFor(() => {
      expect(apiMocks.cancelEmployee).toHaveBeenCalledWith(6);
    });

    expect(events.filter(event => event === `invalidate:${JSON.stringify(queryKeys.shiftSwaps.employee())}`))
      .toHaveLength(3);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: queryKeys.shiftSwaps.all });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: queryKeys.containers.all });
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

const highlightWrapper = ({ children }: { children: React.ReactNode }) =>
  <QueryClientProvider client={new QueryClient()}>{children}</QueryClientProvider>;

describe("useGraphShiftSwapHighlightSettingQuery", () => {
  test.each([null, 0, -1, 1.5])("does not fetch unsaved/invalid graph %s", graphId => {
    renderHook(() => useGraphShiftSwapHighlightSettingQuery(2, graphId), { wrapper: highlightWrapper });
    expect(apiMocks.getHighlightSetting).not.toHaveBeenCalled();
  });
  test.each([null, 0, -1, 1.5])("does not fetch invalid container %s", containerId => {
    renderHook(() => useGraphShiftSwapHighlightSettingQuery(containerId, 9), { wrapper: highlightWrapper });
    expect(apiMocks.getHighlightSetting).not.toHaveBeenCalled();
  });
  test("fetches persisted graphs", async () => {
    const { result } = renderHook(() => useGraphShiftSwapHighlightSettingQuery(2, 9), { wrapper: highlightWrapper });
    await waitFor(() => expect(result.current.data).toEqual({ highlightColor: "#BBF7D0" }));
    expect(apiMocks.getHighlightSetting).toHaveBeenCalledExactlyOnceWith(2, 9, expect.any(AbortSignal));
  });
});
