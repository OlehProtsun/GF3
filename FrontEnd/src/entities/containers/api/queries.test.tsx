import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { getGraphCellKey } from "../model/graphWorkspace";
import type { GraphEmployee, GraphSlot } from "../model/types";
import {
  useCreateContainerMutation,
  useCreateGraphMutation,
  useDeleteContainerMutation,
  useDeleteGraphMutation,
  useSaveGraphWorkspaceMutation,
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
      { id: null, employeeId: 7, minHoursMonth: 80 },
      { id: null, employeeId: 0, minHoursMonth: 90 },
      { id: null, employeeId: 5, minHoursMonth: null },
      { id: null, employeeId: 7, minHoursMonth: 120 },
    ],
    cellMap: {
      [getGraphCellKey(7, 1)]: "9:00-13:00",
      [getGraphCellKey(5, 2)]: "10:30 - 14:45",
    },
  };
}

function updateWorkspaceInput() {
  const existingEmployees: GraphEmployee[] = [
    { id: 70, scheduleId: 66, employeeId: 7, minHoursMonth: 80, displayOrder: 0 },
    { id: 50, scheduleId: 66, employeeId: 5, minHoursMonth: 120, displayOrder: 1 },
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
      { id: 50, employeeId: 5, minHoursMonth: 130 },
      { id: 70, employeeId: 7, minHoursMonth: 80 },
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
    async (_containerId: number, graphId: number, payload: { employeeId: number; minHoursMonth?: number | null; displayOrder: number }) => ({
      id: payload.employeeId === 7 ? 70 : 50,
      scheduleId: graphId,
      employeeId: payload.employeeId,
      minHoursMonth: payload.minHoursMonth ?? null,
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
      { queryKey: queryKeys.containers.schedulePresets(2) },
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
      minHoursMonth: 80,
      displayOrder: 0,
    });
    expect(apiMocks.createGraphEmployee).toHaveBeenNthCalledWith(2, 2, 66, {
      employeeId: 5,
      minHoursMonth: null,
      displayOrder: 1,
    });
    expect(apiMocks.replaceGraphSlots).toHaveBeenCalledWith(2, 66, {
      slots: [
        { dayOfMonth: 1, slotNo: 1, fromTime: "09:00", toTime: "13:00", employeeId: 7, status: 1 },
        { dayOfMonth: 2, slotNo: 1, fromTime: "10:30", toTime: "14:45", employeeId: 5, status: 1 },
      ],
    });
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.containers.all },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.graphById(2, 66) },
      { queryKey: queryKeys.containers.graphEmployees(2, 66) },
      { queryKey: queryKeys.containers.graphSlots(2, 66) },
      { queryKey: queryKeys.containers.graphCellStyles(2, 66) },
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
      minHoursMonth: 130,
      displayOrder: 0,
    });
    expect(apiMocks.updateGraphEmployee).toHaveBeenCalledWith(2, 66, 70, {
      employeeId: 7,
      minHoursMonth: 80,
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
      { queryKey: queryKeys.containers.all },
      { queryKey: queryKeys.containers.graphs(2) },
      { queryKey: queryKeys.containers.graphById(2, 66) },
      { queryKey: queryKeys.containers.graphEmployees(2, 66) },
      { queryKey: queryKeys.containers.graphSlots(2, 66) },
      { queryKey: queryKeys.containers.graphCellStyles(2, 66) },
      { queryKey: queryKeys.employeeSchedules.all },
      { queryKey: queryKeys.shiftSwaps.all },
    ]);
  });
});
