import { act, renderHook, waitFor } from "@testing-library/react";
import type { PropsWithChildren } from "react";
import { beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { SaveAvailabilityGroupGraphDto } from "./dto";
import { useSaveAvailabilityGroupGraphMutation, type SaveAvailabilityGroupGraphInput } from "./queries";

const api = vi.hoisted(() => ({
  saveGraph: vi.fn<(payload: SaveAvailabilityGroupGraphDto) => Promise<{ id: number; sourceGroupIds: number[]; changed: boolean }>>(),
  create: vi.fn(),
  update: vi.fn(),
  createMember: vi.fn(),
  updateMember: vi.fn(),
  removeMember: vi.fn(),
  createSlot: vi.fn(),
  updateSlot: vi.fn(),
  removeSlot: vi.fn(),
  slots: vi.fn(),
  transferDays: vi.fn(),
}));
vi.mock("./availabilityGroupsApi", () => ({ availabilityGroupsApi: api }));

beforeEach(() => {
  vi.resetAllMocks();
  api.saveGraph.mockResolvedValue({ id: 44, sourceGroupIds: [], changed: true });
});

function save(input: SaveAvailabilityGroupGraphInput) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: 3 } } });
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const success = vi.fn();
  const failure = vi.fn();
  const { result } = renderHook(useSaveAvailabilityGroupGraphMutation, {
    wrapper: ({ children }: PropsWithChildren) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  act(() => result.current.mutate(input, { onSuccess: success, onError: failure }));
  return { invalidate, success, failure };
}

function expectSingleWrite() {
  const { saveGraph, ...granular } = api;
  expect(saveGraph).toHaveBeenCalledTimes(1);
  for (const request of Object.values(granular)) expect(request).not.toHaveBeenCalled();
}

test.each([[2026, 2, 28], [2028, 2, 29], [2026, 11, 30], [2026, 12, 31]])(
  "creates %i/%i with %i days for each employee in one request", async (year, month, days) => {
    const { invalidate, success } = save({
      id: null,
      payload: { name: "  Availability  ", year, month, publicationStatus: "private" },
      employeeIds: [7, 5, 7, 3, 2, 1],
      cellMap: { "7:1": "+", "5:2": "9:00-13:30", "3:3": "Training" },
    });

    await waitFor(() => expect(success).toHaveBeenCalledWith({ id: 44, sourceGroupIds: [], changed: true }));
    expectSingleWrite();
    const [payload] = api.saveGraph.mock.calls[0];
    expect(payload).toMatchObject({
      id: null, name: `Availability : ${String(month).padStart(2, "0")}.${year}`, year, month, publicationStatus: "private", transfers: [],
    });
    expect(payload.members.map(member => member.employeeId)).toEqual([7, 5, 3, 2, 1]);
    for (const member of payload.members) {
      expect(member.days.map(day => day.dayOfMonth)).toEqual(Array.from({ length: days }, (_, index) => index + 1));
      expect(member.days.at(-1)).toEqual({ dayOfMonth: days, kind: 1, intervalStr: null });
    }
    expect(payload.members[0].days[0]).toEqual({ dayOfMonth: 1, kind: 0, intervalStr: null });
    expect(payload.members[1].days[1]).toEqual({ dayOfMonth: 2, kind: 2, intervalStr: "09:00 - 13:30" });
    expect(payload.members[2].days[2]).toEqual({ dayOfMonth: 3, kind: 1, intervalStr: "Training" });
    expect(invalidate).toHaveBeenCalledExactlyOnceWith({ queryKey: queryKeys.availabilityGroups.all });
  },
);

test("edits metadata, selected employees and staged CFA using only semantic identities", async () => {
  api.saveGraph.mockResolvedValue({ id: 44, sourceGroupIds: [35], changed: true });
  const publication = {
    publicationStatus: "public" as const,
    visibleFromUtc: "2026-12-01T00:00:00Z",
    visibleToUtc: "2026-12-31T23:00:00Z",
  };
  const { invalidate, success } = save({
    id: 44,
    payload: { name: " Updated availability ", year: 2026, month: 12, ...publication },
    employeeIds: [5, 7],
    cellMap: { "5:1": "+", "7:2": "08:15 - 12:45", "7:3": "+", "9:1": "Removed" },
    transfers: [{ employeeId: 7, sourceGroupId: 35, dayOfMonths: [3, 2, 3] }],
  });

  await waitFor(() => expect(success).toHaveBeenCalledWith({ id: 44, sourceGroupIds: [35], changed: true }));
  expectSingleWrite();
  const [payload] = api.saveGraph.mock.calls[0];
  expect(payload).toMatchObject({
    id: 44, name: "Updated availability", ...publication,
    transfers: [{ employeeId: 7, sourceGroupId: 35, dayOfMonths: [2, 3] }],
  });
  expect(payload.members.map(member => member.employeeId)).toEqual([5, 7]);
  expect(payload.members[1].days.slice(1, 3)).toEqual([
    { dayOfMonth: 2, kind: 2, intervalStr: "08:15 - 12:45" },
    { dayOfMonth: 3, kind: 0, intervalStr: null },
  ]);
  expect(invalidate).toHaveBeenCalledExactlyOnceWith({ queryKey: queryKeys.availabilityGroups.all });
});

test("does not replay or invalidate a rejected save", async () => {
  const error = new Error("Save was rejected");
  api.saveGraph.mockRejectedValue(error);
  const { invalidate, success, failure } = save({
    id: null, payload: { name: "Availability", year: 2026, month: 12 }, employeeIds: [1], cellMap: {},
  });
  await waitFor(() => expect(failure).toHaveBeenCalledExactlyOnceWith(error));
  expectSingleWrite();
  expect(invalidate).not.toHaveBeenCalled();
  expect(success).not.toHaveBeenCalled();
});
