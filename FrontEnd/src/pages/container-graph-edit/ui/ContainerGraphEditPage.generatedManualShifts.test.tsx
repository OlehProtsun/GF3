import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import type { ComponentProps } from "react";
import type { ContainerGraphEditor } from "@entities/containers/ui/ContainerGraphEditor";
import type { Graph, GraphSlot, GraphCellStyle } from "@entities/containers/model/types";
import type { ShiftSwap } from "@entities/shift-swaps/model/types";
import { buildGraphNoteContent, parseGraphNoteContent } from "@entities/containers/model/graphNote";
import { setLanguage } from "@shared/i18n";
import { QueryClient } from "@tanstack/react-query";
import { ContainerGraphEditPage } from "./ContainerGraphEditPage";

type EditorProps = ComponentProps<typeof ContainerGraphEditor>;
const mocks = vi.hoisted(() => ({
  params: { containerId: "1", graphId: "1" as string | undefined },
  location: { key: "test", pathname: "/container/1/graphs/1/edit", search: "?openGraphIds=1,2" },
  graphs: [] as Graph[], slots: [] as GraphSlot[], offers: [] as ShiftSwap[], styles: [] as GraphCellStyle[],
  graphEmployees: [{ id: 1, scheduleId: 1, employeeId: 1, displayOrder: 0 }],
  employees: [{ id: 1, firstName: "Ada", lastName: "Lovelace", hasLoginAccount: true, isOnline: false }],
  shops: [{ id: 1, name: "Shop" }], groups: [{ id: 1, name: "Availability", year: 2026, month: 2 }],
  members: [{ id: 1, employeeId: 1, displayOrder: 0 }], empty: [],
  container: { id: 1, name: "Container" },
  generate: vi.fn(), save: vi.fn(), publish: vi.fn(), navigate: vi.fn(),
  commit: vi.fn(), listStyles: vi.fn(),
  editor: null as EditorProps | null, queryClient: null as QueryClient | null,
}));

vi.mock("react-router-dom", async importOriginal => ({
  ...await importOriginal<object>(), useParams: () => mocks.params, useLocation: () => mocks.location, useNavigate: () => mocks.navigate,
}));
vi.mock("@tanstack/react-query", async importOriginal => ({ ...await importOriginal<object>(), useQueryClient: () => mocks.queryClient }));
vi.mock("@app/providers/PresenceProvider", () => ({
  managerEditResourceTypes: { graph: "graph" }, useManagerEditLocks: () => ({ lockedByOtherState: null, isCheckingLocks: false }),
  buildManagerEditLockMessage: () => null,
}));
vi.mock("@shared/lib/useUnsavedChangesPrompt", () => ({ useUnsavedChangesPrompt: () => ({ dialog: null, runWithoutPrompt: (callback: () => void) => callback() }) }));
vi.mock("@shared/ui/ConfirmDialog", () => ({ ConfirmDialog: ({ open, onConfirm }: { open: boolean; onConfirm: () => void }) => open ? <button onClick={onConfirm}>Confirm save</button> : null }));
vi.mock("@entities/containers", async importOriginal => {
  const actual = await importOriginal<Record<string, unknown>>();
  const query = (data: unknown) => ({ data, isLoading: false, isError: false });
  const mutation = () => ({ mutate: vi.fn(), isPending: false });
  const overrides: Record<string, unknown> = {};
  for (const key of Object.keys(actual)) {
    if (key.startsWith("use") && key.endsWith("Query")) overrides[key] = () => query(mocks.empty);
    if (key.startsWith("use") && key.endsWith("Mutation")) overrides[key] = mutation;
  }
  return { ...actual, ...overrides,
    useContainerByIdQuery: () => query(mocks.container), useContainerGraphsQuery: () => query(mocks.graphs),
    useGraphByIdQuery: () => query(mocks.graphs.find(graph => graph.id === Number(mocks.params.graphId))),
    useGraphEmployeesQuery: () => query(mocks.graphEmployees), useGraphSlotsQuery: () => query(mocks.slots),
    useGraphCellStylesQuery: () => query(mocks.styles),
    useGenerateGraphPreviewMutation: () => ({ mutate: mocks.generate, isPending: false }),
    useSaveGraphWorkspaceMutation: () => ({ mutate: mocks.save, isPending: false }),
    containersApi: { ...actual.containersApi as object, listGraphCellStyles: mocks.listStyles, commitGraphVersion: mocks.commit },
    ContainerGraphEditor: (props: EditorProps) => {
      mocks.editor = props;
      return <div>
        <button onClick={props.onGenerate}>Generate</button><button onClick={props.onSave}>Save editor</button>
        <button onClick={props.onUndo}>Undo</button>
        <output data-testid="columns">{JSON.stringify(props.manualColumns)}</output>
        <output data-testid="matrix">{JSON.stringify(props.cellMap)}</output>
        <output data-testid="errors">{props.submitError}</output>
      </div>;
    },
  };
});
vi.mock("@entities/employees/api/queries", () => ({ useEmployeesListQuery: () => ({ data: mocks.employees }) }));
vi.mock("@entities/shops/api/queries", () => ({ useShopsListQuery: () => ({ data: mocks.shops }) }));
vi.mock("@entities/availability-groups", async importOriginal => ({ ...await importOriginal<object>(),
  useAvailabilityGroupsListQuery: () => ({ data: mocks.groups }), useAvailabilityGroupMembersQuery: () => ({ data: mocks.members }),
  useAvailabilityGroupSlotsQuery: () => ({ data: mocks.empty }),
}));
vi.mock("@entities/availability-binds", async importOriginal => ({ ...await importOriginal<object>(),
  useAvailabilityBindsListQuery: () => ({ data: mocks.empty }),
  useCreateAvailabilityBindMutation: () => ({ isPending: false }), useUpdateAvailabilityBindMutation: () => ({ isPending: false }), useDeleteAvailabilityBindMutation: () => ({ isPending: false }),
}));
vi.mock("@entities/shift-swaps", async importOriginal => ({ ...await importOriginal<object>(),
  useGraphShiftSwapLogQuery: () => ({ data: mocks.offers }), useCreateManagerManualShiftSwapMutation: () => ({ mutate: mocks.publish, isPending: false }),
  useCancelManagerManualShiftSwapMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

const graph: Graph = { id: 1, containerId: 1, shopId: 1, name: "Graph one", year: 2026, month: 2, publicationStatus: "public", allowSwap: true,
  peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5,
  maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: 1 };
const preview = () => Array.from({ length: 28 }, (_, i): GraphSlot[] => ([{
  id: i + 1, scheduleId: 1, dayOfMonth: i + 1, slotNo: 1, employeeId: i ? 1 : null, status: i ? 1 : 0, fromTime: "08:00", toTime: "12:00",
}, { id: i + 100, scheduleId: 1, dayOfMonth: i + 1, slotNo: 1, employeeId: 1, status: 1, fromTime: "12:00", toTime: "16:00" }])).flat();
beforeEach(() => {
  setLanguage("en"); vi.clearAllMocks();
  mocks.params = { containerId: "1", graphId: "1" };
  mocks.graphs = [{ ...graph }, { ...graph, id: 2, name: "Graph two" }];
  mocks.slots = []; mocks.offers = []; mocks.styles = []; mocks.editor = null; mocks.queryClient = new QueryClient();
  mocks.generate.mockImplementation((_variables, callbacks) => callbacks.onSuccess({ slots: preview() }));
  mocks.save.mockImplementation((variables, callbacks) => callbacks.onSuccess({ graphId: variables.graphId ?? 1 }));
  mocks.publish.mockImplementation((_variables, callbacks) => callbacks.onSuccess({}));
  mocks.listStyles.mockResolvedValue([]); mocks.commit.mockResolvedValue({});
});
const columns = () => mocks.editor!.manualColumns;
const generate = async () => { fireEvent.click(screen.getByText("Generate")); await waitFor(() => expect(mocks.generate).toHaveBeenCalled()); };

test("preview displays generated matrix cell, repeats stably, saves provenance and reloads", async () => {
  const view = render(<ContainerGraphEditPage />);
  await generate();
  expect(columns()).toHaveLength(1); expect(columns()[0].label).toBe("Do obsadzenia");
  expect(mocks.editor!.hasUnsavedChanges).toBe(true);
  expect(mocks.editor!.cellMap["-1:1"]).toBe("08:00 - 12:00");
  const original = structuredClone(columns());
  await generate(); expect(columns()).toEqual(original);
  expect(mocks.publish).not.toHaveBeenCalled();
  fireEvent.click(screen.getByText("Save editor")); fireEvent.click(screen.getByText("Confirm save"));
  await waitFor(() => expect(mocks.commit).toHaveBeenCalled());
  const savedNote = mocks.save.mock.calls[0][0].payload.note;
  expect(parseGraphNoteContent(savedNote).manualColumns[0].source).toBe("generated-unfilled");
  view.unmount(); mocks.graphs = [{ ...graph, note: savedNote }];
  render(<ContainerGraphEditPage />); expect(columns()).toEqual(original);
});

test.each(["error", "missing", "empty"])("failed or malformed %s preview keeps columns", async kind => {
  mocks.graphs[0].note = buildGraphNoteContent("", [{ id: 4, label: "Auto", cells: { "1": "09:00 - 13:00" }, source: "generated-unfilled" }]);
  mocks.generate.mockImplementation((_variables, callbacks) => kind === "error" ? callbacks.onError(new Error("Preview failed")) : callbacks.onSuccess(kind === "empty" ? { slots: [] } : {}));
  render(<ContainerGraphEditPage />); const before = structuredClone(columns());
  await generate(); expect(columns()).toEqual(before);
  expect(screen.getByTestId("errors")).toHaveTextContent(kind === "error" ? "Preview failed" : "returned no slots");
});

test("open and queued offers survive regeneration without duplicating seats; undo restores generated state", async () => {
  mocks.graphs[0].note = buildGraphNoteContent("", [{ id: 4, label: "Auto", cells: { "1": "08:00 - 12:00", "2": "08:00 - 12:00" }, source: "generated-unfilled" }]);
  mocks.offers = [{ scheduleId: 1, manualColumnId: 4, dayOfMonth: 1, isManagerCreated: true, status: "open" } as ShiftSwap];
  render(<ContainerGraphEditPage />);
  act(() => mocks.editor!.onPublishManualShift({ manualColumnId: 4, dayOfMonth: 2, fromTime: "08:00", toTime: "12:00", targetEmployeeId: null }));
  await generate();
  expect(columns()).toHaveLength(1); expect(Object.keys(columns()[0].cells)).toEqual(["1", "2"]);
  expect(mocks.editor!.pendingManualShiftPublishes).toHaveLength(1);
  expect(mocks.publish).not.toHaveBeenCalled();
  act(() => mocks.editor!.onCancelPendingManualShift?.(mocks.editor!.pendingManualShiftPublishes[0].clientId));
  await generate(); expect(columns()[0].cells).toEqual({ "1": "08:00 - 12:00" });
  fireEvent.click(screen.getByText("Undo")); expect(columns()[0].cells).toEqual({ "1": "08:00 - 12:00", "2": "08:00 - 12:00" });
});

test("undo removes new generated columns and session navigation preserves each graph draft", async () => {
  const view = render(<ContainerGraphEditPage />); await generate();
  fireEvent.click(screen.getByText("Undo")); expect(columns()).toEqual([]);
  await generate(); const first = structuredClone(columns());
  fireEvent.click(screen.getByRole("tab", { name: "Graph two" }));
  mocks.params = { containerId: "1", graphId: "2" }; view.rerender(<ContainerGraphEditPage />);
  expect(columns()).toEqual([]);
  fireEvent.click(screen.getByRole("tab", { name: "Graph one" }));
  mocks.params = { containerId: "1", graphId: "1" }; view.rerender(<ContainerGraphEditPage />);
  expect(columns()).toEqual(first);
});

test.each(["success", "error"])("ignores stale %s response after graph session changes", async outcome => {
  let callbacks: { onSuccess: (value: { slots: GraphSlot[] }) => void; onError: (error: Error) => void };
  mocks.generate.mockImplementation((_variables, value) => { callbacks = value; });
  const view = render(<ContainerGraphEditPage />); await generate();
  fireEvent.click(screen.getByRole("tab", { name: "Graph two" }));
  mocks.params = { containerId: "1", graphId: "2" }; view.rerender(<ContainerGraphEditPage />);
  await act(async () => { if (outcome === "success") callbacks.onSuccess({ slots: preview() }); else callbacks.onError(new Error("Stale failure")); });
  expect(columns()).toEqual([]); expect(screen.getByTestId("errors")).toBeEmptyDOMElement();
});

test("only latest preview applies, with request form values and latest custom columns", async () => {
  const callbacks: Array<{ onSuccess: (value: { slots: GraphSlot[] }) => void }> = [];
  mocks.generate.mockImplementation((_variables, value) => callbacks.push(value));
  render(<ContainerGraphEditPage />); await generate(); await generate();
  act(() => mocks.editor!.onAddManualColumn());
  act(() => mocks.editor!.onFieldChange("peoplePerShift", "3"));
  await act(async () => callbacks[1].onSuccess({ slots: preview() }));
  expect(columns()).toHaveLength(2); expect(columns()[0].source).toBeUndefined();
  const current = structuredClone(columns());
  await act(async () => callbacks[0].onSuccess({ slots: [] }));
  expect(columns()).toEqual(current); expect(screen.getByTestId("errors")).toBeEmptyDOMElement();
});

test("removed generated columns clean only their selection and styles", async () => {
  mocks.graphs[0].note = buildGraphNoteContent("", [
    { id: 4, label: "Auto", cells: { "1": "08:00 - 12:00" }, source: "generated-unfilled" },
    { id: 5, label: "Custom", cells: { "1": "Keep me" } },
  ], [1, -4, -5], [
    { employeeId: -4, dayOfMonth: 1, backgroundColorArgb: 123, textColorArgb: null },
    { employeeId: -5, dayOfMonth: 1, backgroundColorArgb: 456, textColorArgb: null },
  ]);
  mocks.generate.mockImplementation((_variables, callbacks) => callbacks.onSuccess({ slots: preview().map(slot => ({ ...slot, employeeId: 1, status: 1 })) }));
  render(<ContainerGraphEditPage />);
  act(() => mocks.editor!.onSelectedCellKeysChange(["-4:1", "-5:1"]));
  await generate();
  expect(columns().map(column => column.columnId)).toEqual([5]);
  expect(mocks.editor!.selectedCellKeys).toEqual(["-5:1"]);
  expect(mocks.editor!.styleMap["-4:1"]).toBeUndefined();
  expect(mocks.editor!.styleMap["-5:1"].backgroundColorArgb).toBe(456);
});

test("stale response cannot update a newly opened create-mode session", async () => {
  let resolve: (value: { slots: GraphSlot[] }) => void;
  mocks.generate.mockImplementation((_variables, callbacks) => { resolve = callbacks.onSuccess; });
  const view = render(<ContainerGraphEditPage />); await generate();
  mocks.params = { containerId: "1", graphId: undefined }; view.rerender(<ContainerGraphEditPage />);
  await act(async () => resolve({ slots: preview() }));
  expect(columns()).toEqual([]); expect(screen.getByTestId("errors")).toBeEmptyDOMElement();
});
