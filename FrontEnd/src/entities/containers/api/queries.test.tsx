import { act, render, renderHook, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { beginGraphSaveTraffic, finishGraphSaveTraffic } from "@shared/api/graphSaveTraffic";
import { getGraphCellKey } from "../model/graphWorkspace";
import type { GraphEmployee, GraphSlot } from "../model/types";
import {
  useCreateContainerMutation,
  useCreateSchedulePresetMutation,
  useSchedulePresetsQuery,
  useCreateGraphMutation,
  useDeleteContainerMutation,
  useDeleteGraphMutation,
  useSaveGraphWorkspaceMutation,
  useGraphSlotsBatchQuery,
  useUpdateContainerMutation,
  useUpdateGraphMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
  list: vi.fn(),
  byId: vi.fn(),
  listGraphs: vi.fn(),
  listSchedulePresets: vi.fn(),
  createSchedulePreset: vi.fn(),
  graphById: vi.fn(),
  createGraph: vi.fn(),
  updateGraph: vi.fn(),
  removeGraph: vi.fn(),
  generateGraph: vi.fn(),
  generateGraphPreview: vi.fn(),
  listGraphSlots: vi.fn(),
  replaceGraphSlots: vi.fn(),
  createGraphSlot: vi.fn(),
  updateGraphSlot: vi.fn(),
  removeGraphSlot: vi.fn(),
  listGraphEmployees: vi.fn(),
  createGraphEmployee: vi.fn(),
  updateGraphEmployee: vi.fn(),
  removeGraphEmployee: vi.fn(),
  listGraphCellStyles: vi.fn(),
  upsertGraphCellStyle: vi.fn(),
  removeGraphCellStyle: vi.fn(),
}));

test("the editor gate suppresses five workspace mutation refetches", async () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const { result } = renderHook(() => useSaveGraphWorkspaceMutation(), {
    wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  beginGraphSaveTraffic(client, 2);
  const ids = [65, 66, 67, 68, 69];
  try {
    for (const graphId of ids) {
      await act(async () => new Promise<void>((resolve, reject) =>
        result.current.mutate({ ...updateWorkspaceInput(), graphId }, { onSuccess: () => resolve(), onError: reject })));
    }
    expect(apiMocks.updateGraph.mock.calls.map(([, id]) => id)).toEqual(ids);
    expect(invalidate).not.toHaveBeenCalled();
  } finally { await finishGraphSaveTraffic(client, 2, ids); }
  expect(invalidate.mock.calls.every(([filters]) => filters.refetchType === "none")).toBe(true);
});

test("exact graph list refresh leaves active graph detail, slot and version observers alone", async () => {
  const client = new QueryClient();
  const reads = [vi.fn(), vi.fn(), vi.fn(), vi.fn()].map(read => read.mockResolvedValue([]));
  const keys = [queryKeys.containers.graphs(2), queryKeys.containers.graphById(2, 66),
    queryKeys.containers.graphSlots(2, 66), queryKeys.containers.graphVersions(2, 66)];
  function Observers() {
    useQuery({ queryKey: keys[0], queryFn: reads[0], staleTime: 60_000 });
    useQuery({ queryKey: keys[1], queryFn: reads[1], staleTime: 60_000 });
    useQuery({ queryKey: keys[2], queryFn: reads[2], staleTime: 60_000 });
    useQuery({ queryKey: keys[3], queryFn: reads[3], staleTime: 60_000 });
    return null;
  }
  render(<QueryClientProvider client={client}><Observers /></QueryClientProvider>);
  await waitFor(() => reads.forEach(read => expect(read).toHaveBeenCalledOnce()));
  await act(async () => client.invalidateQueries({ queryKey: keys[0], exact: true }));
  expect(reads.map(read => read.mock.calls.length)).toEqual([2, 1, 1, 1]);
});

test("loads related slot batches in pairs with a stable result order", async () => {
  let active = 0;
  let peak = 0;
  apiMocks.listGraphSlots.mockImplementation(async (_, id) => {
    peak = Math.max(peak, ++active);
    await Promise.resolve();
    active--;
    return [{ id }];
  });
  const client = new QueryClient();
  const { result } = renderHook(() => useGraphSlotsBatchQuery(2, [9, 3, 5, 1, 7]), {
    wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  await waitFor(() => expect(Object.keys(result.current.data ?? {})).toEqual(["1", "3", "5", "7", "9"]));
  expect(peak).toBe(2);
  expect(apiMocks.listGraphSlots.mock.calls.map(([, id]) => id)).toEqual([1, 3, 5, 7, 9]);
});

test.each([false, true])("settles employee writes in pairs and respects cancellation (canceled: %s)", async canceled => {
  let active = 0;
  let peak = 0;
  const controller = new AbortController();
  apiMocks.createGraphEmployee.mockImplementation(async (_, scheduleId, payload) => {
    peak = Math.max(peak, ++active);
    await Promise.resolve();
    if (canceled) controller.abort();
    active--;
    return { ...payload, scheduleId, id: payload.employeeId };
  });
  const client = new QueryClient();
  const { result } = renderHook(() => useSaveGraphWorkspaceMutation(), {
    wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  const saved = vi.fn();
  const failed = vi.fn();
  act(() => result.current.mutate({
    ...createWorkspaceInput(),
    cellMap: {},
    employeeAssignments: [1, 2, 3, 4, 5].map(employeeId => ({ id: null, employeeId, isPriority: false })),
    signal: controller.signal,
  }, { onSuccess: saved, onError: failed }));
  await waitFor(() => expect(canceled ? failed : saved).toHaveBeenCalledOnce());
  expect(peak).toBe(2);
  expect(apiMocks.createGraphEmployee).toHaveBeenCalledTimes(canceled ? 2 : 5);
});

vi.mock("./containersApi", () => ({
  containersApi: apiMocks,
}));

function SaveWorkspaceHarness({ mode }: { mode: "create" | "update" }) {
  const saveWorkspace = useSaveGraphWorkspaceMutation();

  return (
    <button
      type="button"
      onClick={() => saveWorkspace.mutate(mode === "create" ? createWorkspaceInput() : updateWorkspaceInput())}
    >
      save workspace
    </button>
  );
}

function CrudMutationHarness() {
  const createContainer = useCreateContainerMutation();
  const updateContainer = useUpdateContainerMutation();
  const deleteContainer = useDeleteContainerMutation();
  const createGraph = useCreateGraphMutation();
  const updateGraph = useUpdateGraphMutation();
  const deleteGraph = useDeleteGraphMutation();

  return (
    <section>
      <button type="button" onClick={() => createContainer.mutate({ name: " Main ", note: " Note " })}>
        create container
      </button>
      <button type="button" onClick={() => updateContainer.mutate({ id: 2, payload: { name: "Main", note: "Updated" } })}>
        update container
      </button>
      <button type="button" onClick={() => deleteContainer.mutate(2)}>
        delete container
      </button>
      <button type="button" onClick={() => createGraph.mutate({ containerId: 2, payload: graphPayload })}>
        create graph
      </button>
      <button type="button" onClick={() => updateGraph.mutate({ containerId: 2, graphId: 66, payload: graphPayload })}>
        update graph
      </button>
      <button type="button" onClick={() => deleteGraph.mutate({ containerId: 2, graphId: 66 })}>
        delete graph
      </button>
    </section>
  );
}

function renderHarness(client: QueryClient, mode: "create" | "update") {
  return render(
    <QueryClientProvider client={client}>
      <SaveWorkspaceHarness mode={mode} />
    </QueryClientProvider>,
  );
}

function renderCrudHarness(client: QueryClient) {
  return render(
    <QueryClientProvider client={client}>
      <CrudMutationHarness />
    </QueryClientProvider>,
  );
}

const graphPayload = {
  shopId: 3,
  name: "May rota",
  year: 2026,
  month: 2,
  publicationStatus: "public" as const,
  peoplePerShift: 2,
  shift1Time: "08:00 - 16:00",
  shift2Time: "16:00 - 22:00",
  maxHoursPerEmpMonth: 180,
  maxConsecutiveDays: 5,
  maxConsecutiveFull: 3,
  maxFullPerMonth: 18,
  note: "Planner note",
  availabilityGroupId: 9,
};

function createWorkspaceInput() {
  return {
    containerId: 2,
    graphId: null,
    payload: graphPayload,
    employeeAssignments: [
      { id: null, employeeId: 7, minHoursMonth: 80, isPriority: false },
      { id: null, employeeId: 0, minHoursMonth: 90, isPriority: false },
      { id: null, employeeId: 5, minHoursMonth: null, isPriority: false },
      { id: null, employeeId: 7, minHoursMonth: 120, isPriority: false },
    ],
    cellMap: {
      [getGraphCellKey(7, 1)]: "9:00-13:00",
      [getGraphCellKey(5, 2)]: "10:30 - 14:45",
    },
  };
}

function updateWorkspaceInput() {
  const existingEmployees: GraphEmployee[] = [
    { id: 70, scheduleId: 66, employeeId: 7, minHoursMonth: 80, isPriority: false, displayOrder: 0 },
    { id: 50, scheduleId: 66, employeeId: 5, minHoursMonth: 120, isPriority: false, displayOrder: 1 },
    { id: 90, scheduleId: 66, employeeId: 9, minHoursMonth: 60, displayOrder: 2 },
  ];
  const existingSlots: GraphSlot[] = [
    { id: 701, scheduleId: 66, dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 7, status: 1 },
    { id: 502, scheduleId: 66, dayOfMonth: 2, slotNo: 1, fromTime: "10:00", toTime: "14:00", employeeId: 5, status: 1 },
    { id: 901, scheduleId: 66, dayOfMonth: 3, slotNo: 1, fromTime: "08:00", toTime: "12:00", employeeId: 9, status: 1 },
  ];

  return {
    containerId: 2,
    graphId: 66,
    payload: {
      ...graphPayload,
      name: "Updated rota",
    },
    employeeAssignments: [
      { id: 50, employeeId: 5, minHoursMonth: 130, isPriority: false },
      { id: 70, employeeId: 7, minHoursMonth: 80, isPriority: false },
    ],
    cellMap: {
      [getGraphCellKey(7, 1)]: "09:00 - 13:00",
      [getGraphCellKey(5, 2)]: "10:00 - 15:00",
    },
    existingEmployees,
    existingSlots,
  };
}

beforeEach(() => {
  Object.values(apiMocks).forEach(mock => mock.mockReset());
  apiMocks.create.mockResolvedValue({ id: 2 });
  apiMocks.update.mockResolvedValue(undefined);
  apiMocks.remove.mockResolvedValue(undefined);
  apiMocks.createGraph.mockResolvedValue({ id: 66 });
  apiMocks.updateGraph.mockResolvedValue(undefined);
  apiMocks.removeGraph.mockResolvedValue(undefined);
  apiMocks.createGraphEmployee.mockImplementation(
    async (_containerId: number, graphId: number, payload: { employeeId: number; minHoursMonth?: number | null; isPriority: boolean; displayOrder: number }) => ({
      id: payload.employeeId === 7 ? 70 : 50,
      scheduleId: graphId,
      employeeId: payload.employeeId,
      minHoursMonth: payload.minHoursMonth ?? null,
      isPriority: payload.isPriority,
      displayOrder: payload.displayOrder,
    }),
  );
  apiMocks.updateGraphEmployee.mockResolvedValue(undefined);
  apiMocks.removeGraphEmployee.mockResolvedValue(undefined);
  apiMocks.replaceGraphSlots.mockResolvedValue(undefined);
});

describe("container graph workspace query mutation", () => {
  test("invalidates the right container and graph caches after CRUD mutations", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderCrudHarness(client);

    await user.click(screen.getByRole("button", { name: "create container" }));
    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({ name: " Main ", note: " Note " });
    });

    await user.click(screen.getByRole("button", { name: "update container" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(2, { name: "Main", note: "Updated" });
    });

    await user.click(screen.getByRole("button", { name: "delete container" }));
    await waitFor(() => {
      expect(apiMocks.remove).toHaveBeenCalledWith(2);
    });

    await user.click(screen.getByRole("button", { name: "create graph" }));
    await waitFor(() => {
      expect(apiMocks.createGraph).toHaveBeenCalledWith(2, graphPayload);
    });

    await user.click(screen.getByRole("button", { name: "update graph" }));
    await waitFor(() => {
      expect(apiMocks.updateGraph).toHaveBeenCalledWith(2, 66, graphPayload);
    });

    await user.click(screen.getByRole("button", { name: "delete graph" }));
    await waitFor(() => {
      expect(apiMocks.removeGraph).toHaveBeenCalledWith(2, 66);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.containers.all },
      { queryKey: queryKeys.containers.all },
      { queryKey: queryKeys.containers.byId(2) },
      { queryKey: queryKeys.containers.all },
      { queryKey: queryKeys.containers.byId(2) },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.schedulePresets() },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.graphById(2, 66) },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.graphById(2, 66) },
      { queryKey: queryKeys.containers.graphSlots(2, 66) },
      { queryKey: queryKeys.containers.graphEmployees(2, 66) },
      { queryKey: queryKeys.containers.graphCellStyles(2, 66) },
    ]);
  });

  test("creates a graph workspace with unique employees, normalized slots, and cross-surface invalidation", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client, "create");

    await user.click(screen.getByRole("button", { name: "save workspace" }));

    await waitFor(() => {
      expect(apiMocks.createGraph).toHaveBeenCalledWith(2, graphPayload);
      expect(apiMocks.replaceGraphSlots).toHaveBeenCalledTimes(1);
    });

    expect(apiMocks.createGraphEmployee).toHaveBeenNthCalledWith(1, 2, 66, {
      employeeId: 7,
      minHoursMonth: 80, isPriority: false,
      displayOrder: 0,
    });
    expect(apiMocks.createGraphEmployee).toHaveBeenNthCalledWith(2, 2, 66, {
      employeeId: 5,
      minHoursMonth: null, isPriority: false,
      displayOrder: 1,
    });
    expect(apiMocks.replaceGraphSlots).toHaveBeenCalledWith(2, 66, {
      slots: [
        { dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 7, status: 1 },
        { dayOfMonth: 2, slotNo: 1, fromTime: "10:30", toTime: "14:45", employeeId: 5, status: 1 },
      ],
    });
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.containers.list() },
      { queryKey: queryKeys.containers.byId(2), exact: true },
      { queryKey: queryKeys.containers.graphs(2), exact: true },
      { queryKey: queryKeys.containers.graphById(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphEmployees(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphSlots(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphCellStyles(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphVersions(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphSlotsBatches(2), predicate: expect.any(Function) },
      { queryKey: queryKeys.containers.graphRecordsPrefix(2) },
      { queryKey: queryKeys.employeeSchedules.all },
      { queryKey: queryKeys.shiftSwaps.all },
    ]);
  });

  test("updates an existing graph workspace by reordering employees, replacing slots, and removing deselected employees", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client, "update");

    await user.click(screen.getByRole("button", { name: "save workspace" }));

    await waitFor(() => {
      expect(apiMocks.updateGraph).toHaveBeenCalledWith(2, 66, {
        ...graphPayload,
        name: "Updated rota",
      });
      expect(apiMocks.replaceGraphSlots).toHaveBeenCalledTimes(1);
    });

    expect(apiMocks.createGraph).not.toHaveBeenCalled();
    expect(apiMocks.createGraphEmployee).not.toHaveBeenCalled();
    expect(apiMocks.updateGraphEmployee).toHaveBeenCalledWith(2, 66, 50, {
      employeeId: 5,
      minHoursMonth: 130, isPriority: false,
      displayOrder: 0,
    });
    expect(apiMocks.updateGraphEmployee).toHaveBeenCalledWith(2, 66, 70, {
      employeeId: 7,
      minHoursMonth: 80, isPriority: false,
      displayOrder: 1,
    });
    expect(apiMocks.replaceGraphSlots).toHaveBeenCalledWith(2, 66, {
      slots: [
        { dayOfMonth: 2, slotNo: 1, fromTime: "10:00", toTime: "15:00", employeeId: 5, status: 1 },
        { dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 7, status: 1 },
      ],
    });
    expect(apiMocks.removeGraphEmployee).toHaveBeenCalledWith(2, 66, 90);
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.containers.list() },
      { queryKey: queryKeys.containers.byId(2), exact: true },
      { queryKey: queryKeys.containers.graphs(2), exact: true },
      { queryKey: queryKeys.containers.graphById(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphEmployees(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphSlots(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphCellStyles(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphVersions(2, 66), exact: true },
      { queryKey: queryKeys.containers.graphSlotsBatches(2), predicate: expect.any(Function) },
      { queryKey: queryKeys.containers.graphRecordsPrefix(2) },
      { queryKey: queryKeys.employeeSchedules.all },
      { queryKey: queryKeys.shiftSwaps.all },
    ]);
  });
});


describe("global schedule presets", () => {
  test("shares one cache across containers and refreshes B when A creates a preset", async () => {
    const client = new QueryClient();
    const invalidate = vi.spyOn(client, "invalidateQueries");
    const initial = [{ id: 8, containerId: null, name: "Retained" }];
    const added = { id: 9, containerId: 1, name: "New" };
    apiMocks.listSchedulePresets.mockResolvedValue(initial);
    apiMocks.createSchedulePreset.mockResolvedValue(added);
    const wrapper = ({ children }: { children: React.ReactNode }) =>
      <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    const b = renderHook(() => useSchedulePresetsQuery(2), { wrapper });
    await waitFor(() => expect(b.result.current.data).toEqual(initial));
    const a = renderHook(() => useSchedulePresetsQuery(1), { wrapper });
    await waitFor(() => expect(a.result.current.data).toEqual(initial));
    expect(a.result.current.data).toEqual(b.result.current.data);
    apiMocks.listSchedulePresets.mockResolvedValue([...initial, added]);
    const create = renderHook(() => useCreateSchedulePresetMutation(), { wrapper });
    act(() => create.result.current.mutate({ containerId: 1, payload: { ...graphPayload, name: "New", scheduleName: "Schedule", employees: [] } }));
    await waitFor(() => expect(b.result.current.data).toEqual([...initial, added]));
    expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.containers.schedulePresets() });
    expect(apiMocks.listSchedulePresets).toHaveBeenCalledWith(2, expect.any(AbortSignal));
  });

  test.each([null, 0, -1, 1.5])("does not fetch for invalid container %s", containerId => {
    const client = new QueryClient();
    renderHook(() => useSchedulePresetsQuery(containerId), { wrapper: ({ children }) =>
      <QueryClientProvider client={client}>{children}</QueryClientProvider> });
    expect(apiMocks.listSchedulePresets).not.toHaveBeenCalled();
  });
});

test("priority-only changes use employee PUT even when hours and order are unchanged", async () => {
  const client = new QueryClient();
  const { result } = renderHook(() => useSaveGraphWorkspaceMutation(), { wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  const input = updateWorkspaceInput();
  input.employeeAssignments = input.existingEmployees.map(employee => ({ id: employee.id, employeeId: employee.employeeId, minHoursMonth: employee.minHoursMonth ?? null, isPriority: employee.employeeId === 7 }));
  act(() => result.current.mutate(input));
  await waitFor(() => expect(apiMocks.updateGraphEmployee).toHaveBeenCalledWith(2, 66, 70, { employeeId: 7, minHoursMonth: 80, displayOrder: 0, isPriority: true }));
  expect(apiMocks.updateGraphEmployee).toHaveBeenCalledTimes(1);
});

test("failed priority save clears pending state and retries the same flags", async () => {
  const client = new QueryClient();
  const { result } = renderHook(() => useSaveGraphWorkspaceMutation(), { wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  const input = updateWorkspaceInput();
  input.employeeAssignments = input.existingEmployees.map(employee => ({ id: employee.id, employeeId: employee.employeeId,
    minHoursMonth: employee.minHoursMonth ?? null, isPriority: employee.employeeId === 7 }));
  const failed = vi.fn(); const saved = vi.fn();
  apiMocks.updateGraphEmployee.mockRejectedValueOnce(new Error("Synthetic save failure"));
  act(() => result.current.mutate(input, { onError: failed }));
  await waitFor(() => expect(failed).toHaveBeenCalled());
  expect(result.current.isPending).toBe(false);
  act(() => result.current.mutate(input, { onSuccess: saved }));
  await waitFor(() => expect(saved).toHaveBeenCalled());
  expect(result.current.isPending).toBe(false);
  expect(apiMocks.updateGraphEmployee).toHaveBeenLastCalledWith(2, 66, 70, { employeeId: 7, minHoursMonth: 80, displayOrder: 0, isPriority: true });
});

test.each([
  { edit: "slot", graph: 0, slots: 1, employees: 0 },
  { edit: "note", graph: 1, slots: 0, employees: 0 },
  { edit: "priority", graph: 0, slots: 0, employees: 1 },
  { edit: "none", graph: 0, slots: 0, employees: 0 },
])("sends only changed workspace resources for a $edit edit", async ({ edit, graph, slots, employees }) => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const { result } = renderHook(() => useSaveGraphWorkspaceMutation(), {
    wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  const input = updateWorkspaceInput();
  const existingGraph = { ...input.payload, id: 66, containerId: 2, lastUpdatedAtUtc: "2026-11-08T10:00:00Z" };
  const existingEmployees = input.existingEmployees.slice(0, 2);
  const saved = vi.fn();
  act(() => {
    result.current.mutate({
      ...input, existingGraph, existingEmployees,
      payload: { ...input.payload, note: edit === "note" ? undefined : input.payload.note },
      existingSlots: [
        { ...input.existingSlots[0], toTime: "11:00" },
        { ...input.existingSlots[0], id: 702, fromTime: "11:00" },
        input.existingSlots[1],
      ].reverse().map(slot => ({ ...slot, status: "Assigned", slotNo: 4 })),
      employeeAssignments: existingEmployees.map(row => ({ ...row, isPriority: edit === "priority" && row.employeeId === 7 })),
      cellMap: { "7:1": "09:00 - 13:00", "5:2": edit === "slot" ? "10:00 - 15:00" : "10:00 - 14:00" },
    }, { onSuccess: saved });
  });
  await waitFor(() => expect(saved).toHaveBeenCalled());
  expect(apiMocks.updateGraph).toHaveBeenCalledTimes(graph);
  expect(apiMocks.replaceGraphSlots).toHaveBeenCalledTimes(slots);
  expect(apiMocks.updateGraphEmployee).toHaveBeenCalledTimes(employees);
  if (edit === "none") expect(invalidate).not.toHaveBeenCalled();
});
