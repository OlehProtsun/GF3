import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { queryKeys } from "@shared/api/queryKeys";
import { useApproveShiftCorrectionMutation, useEmployeeCorrectionSchedulesQuery, useGraphShiftCorrectionsQuery, useRejectShiftCorrectionMutation } from "./queries";

const mocks = vi.hoisted(() => ({ listGraph: vi.fn(), listSchedules: vi.fn(), getGraphRequest: vi.fn(), approve: vi.fn(), reject: vi.fn() }));
vi.mock("./shiftCorrectionsApi", () => ({ shiftCorrectionsApi: mocks }));
let client: QueryClient;
beforeEach(() => {
  client = new QueryClient();
  for (const mock of Object.values(mocks)) mock.mockReset();
  mocks.listGraph.mockResolvedValue([]);
});
const wrapper = ({ children }: { children: React.ReactNode }) =>
  <QueryClientProvider client={client}>{children}</QueryClientProvider>;

describe("useGraphShiftCorrectionsQuery", () => {
  test.each([null, 0, -1, 1.5])("does not fetch unsaved/invalid graph %s", graphId => {
    renderHook(() => useGraphShiftCorrectionsQuery(2, graphId), { wrapper });
    expect(mocks.listGraph).not.toHaveBeenCalled();
  });
  test.each([null, 0, -1, 1.5])("does not fetch invalid container %s", containerId => {
    renderHook(() => useGraphShiftCorrectionsQuery(containerId, 9), { wrapper });
    expect(mocks.listGraph).not.toHaveBeenCalled();
  });
  test("fetches persisted graphs", async () => {
    const { result } = renderHook(() => useGraphShiftCorrectionsQuery(2, 9), { wrapper });
    await waitFor(() => expect(result.current.data).toEqual([]));
    expect(mocks.listGraph).toHaveBeenCalledExactlyOnceWith(2, 9, expect.any(AbortSignal));
    expect(client.getQueryState(queryKeys.shiftSwaps.graphCorrections(2, 9)).data).toEqual([]);
  });
});

test("catalog loads only when enabled and aborts when disabled or unmounted", async () => {
  mocks.listSchedules.mockImplementation(() => new Promise(() => {}));
  const { rerender, unmount } = renderHook(enabled => useEmployeeCorrectionSchedulesQuery(enabled), { wrapper, initialProps: false });
  expect(mocks.listSchedules).not.toHaveBeenCalled();
  rerender(true);
  await waitFor(() => expect(mocks.listSchedules).toHaveBeenCalledOnce());
  const signal = mocks.listSchedules.mock.calls[0][0] as AbortSignal;
  expect(signal.aborted).toBe(false);
  unmount();
  await waitFor(() => expect(signal.aborted).toBe(true));
});

test("loads exact correction independently of the capped graph list", async () => {
  client.setQueryData(queryKeys.shiftSwaps.graphCorrections(2, 9), Array.from({ length: 100 }, (_, id) => ({ id: id + 2 })));
  const key = queryKeys.shiftSwaps.graphCorrectionById(2, 9, 1);
  const request = { id: 1, status: "approved" };
  mocks.getGraphRequest.mockResolvedValue(request);
  mocks.listSchedules.mockResolvedValue([{ id: 9 }]);
  const view = renderHook(() => ({ exact: useGraphShiftCorrectionsQuery(2, 9, 1), catalog: useEmployeeCorrectionSchedulesQuery(true) }), { wrapper });
  await waitFor(() => expect(view.result.current.exact.data).toEqual([request]));
  await waitFor(() => expect(client.getQueryState(queryKeys.employeeSchedules.correctionCandidates()).data).toEqual([{ id: 9 }]));
  expect(mocks.getGraphRequest).toHaveBeenCalledExactlyOnceWith(2, 9, 1, expect.any(AbortSignal));
  expect(mocks.listGraph).not.toHaveBeenCalled();
  expect(client.getQueryState(key).data).toEqual([request]);
});

test.each(["approve", "reject"] as const)("%s seeds the terminal correction and refreshes the acting manager's feed", async action => {
  const terminal = { id: 1, status: action === "approve" ? "approved" : "rejected" };
  const key = queryKeys.shiftSwaps.graphCorrectionById(2, 9, 1);
  const invalidate = vi.spyOn(client, "invalidateQueries");
  mocks[action].mockResolvedValue(terminal);
  const { result } = renderHook(() => ({ approve: useApproveShiftCorrectionMutation(), reject: useRejectShiftCorrectionMutation() }), { wrapper });
  act(() => result.current[action].mutate({ containerId: 2, graphId: 9, id: 1 }));
  await waitFor(() => expect(client.getQueryState(key).data).toEqual([terminal]));
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.activity() });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.navBadges() });
  if (action === "approve") {
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.dashboard() });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.containers.graphEmployees(2, 9) });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.employeeSchedules.all });
    expect(mocks.approve).toHaveBeenCalledWith(2, 9, 1, undefined);
  }
});
