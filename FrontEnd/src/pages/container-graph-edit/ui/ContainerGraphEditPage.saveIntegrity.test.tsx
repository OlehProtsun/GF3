import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import type { ComponentProps } from "react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ContainerGraphEditor } from "@entities/containers/ui/ContainerGraphEditor";
import type { GraphSlot } from "@entities/containers/model/types";
import type { PersistedGraphState } from "@entities/containers/model/graphSaveIntegrity";
import type { BatchUpdateGraphCellStylesDto, GraphVersionTreeDto, SaveGraphDto, SaveGraphSlotDto, UpsertGraphCellStyleDto } from "@entities/containers/api/dto";
import type { ShiftSwap } from "@entities/shift-swaps";
import { buildGraphNoteContent, parseGraphNoteContent } from "@entities/containers/model/graphNote";
import { readGraphRecovery } from "@entities/containers/model/graphDraftRecovery";
import { rgbHexToArgb } from "@entities/containers/model/graphWorkspace";
import { ApiError } from "@shared/api/httpClient";
import { isGraphSaveTrafficActive } from "@shared/api/graphSaveTraffic";
import { setLanguage } from "@shared/i18n";
import { ContainerGraphEditPage } from "./ContainerGraphEditPage";

type EditorProps = ComponentProps<typeof ContainerGraphEditor>;
const ids = [34, 36, 37, 38, 42];
const container = { id: 7, name: "Container" };
const defaults = vi.hoisted(() => ({
  graphById: vi.fn(), listGraphEmployees: vi.fn(), listGraphSlots: vi.fn(), listGraphCellStyles: vi.fn(),
  updateGraph: vi.fn(), replaceGraphSlots: vi.fn(), createGraphEmployee: vi.fn(),
  removeGraphEmployee: vi.fn(), updateGraphEmployee: vi.fn(), upsertGraphCellStyle: vi.fn(),
  applyGraphCellStyleBatch: vi.fn(), removeGraphCellStyle: vi.fn(), listGraphVersions: vi.fn(), commitGraphVersion: vi.fn(),
}));
const m = vi.hoisted(() => ({
  id: 34,
  managerId: 9,
  editor: null as EditorProps | null,
  states: new Map<number, PersistedGraphState>(),
  versions: new Map<number, GraphVersionTreeDto>(),
  offers: new Map<number, ShiftSwap[]>(),
  empty: [], shops: [{ id: 1, name: "Shop" }],
  groups: [] as Array<{ id: number; name: string; year: number; month: number }>,
  members: [] as Array<{ id: number; employeeId: number; displayOrder: number }>,
  employees: [{ id: 1, firstName: "Test", lastName: "One" }, { id: 2, firstName: "Test", lastName: "Two" }],
  navigate: vi.fn(),
  publish: vi.fn(),
  listGraphLog: vi.fn(),
  slotsFailed: false,
  urlopFailed: false,
  retrySlots: vi.fn(),
  retryUrlop: vi.fn(),
  locks: { lockedByOtherState: null as null | { owner: string }, isCheckingLocks: false },
  prompt: vi.fn(),
}));

vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => ({ session: { role: "manager", managerId: m.managerId } }) }));
vi.mock("react-router-dom", async importOriginal => ({
  ...await importOriginal<object>(), useParams: () => ({ containerId: "7", graphId: String(m.id) }),
  useLocation: () => ({ key: "test", pathname: `/container/7/graphs/${m.id}/edit`, search: `?openGraphIds=${ids.join(",")}` }),
  useNavigate: () => m.navigate,
}));
vi.mock("@app/providers/PresenceProvider", () => ({
  managerEditResourceTypes: { schedule: "schedule" }, useManagerEditLocks: () => m.locks,
  buildManagerEditLockMessage: () => "Another manager is editing this schedule.",
}));
vi.mock("@shared/lib/useUnsavedChangesPrompt", () => ({
  useUnsavedChangesPrompt: ({ when }: { when: boolean }) => {
    m.prompt(when);
    return { dialog: null, runWithoutPrompt: (callback: () => void) => callback() };
  },
}));
vi.mock("@shared/ui/ConfirmDialog", () => ({
  ConfirmDialog: ({ open, title, onConfirm, onCancel }: { open: boolean; title: string; onConfirm: () => void; onCancel: () => void }) =>
    open ? <div role="dialog" aria-label={title}><button onClick={onConfirm}>Confirm action</button><button onClick={onCancel}>Cancel</button></div> : null,
}));
vi.mock("@entities/containers/api/containersApi", async importOriginal => ({
  ...await importOriginal<object>(),
  get containersApi() { return defaults; },
}));
vi.mock("@entities/containers", async importOriginal => {
  const actual = await importOriginal<typeof import("@entities/containers")>();
  const query = (data: unknown) => ({ data, isLoading: false, isError: false });
  return { ...actual,
    useContainerByIdQuery: () => query(container),
    useContainerGraphsQuery: () => query([...m.states.values()].map(state => state.graph)),
    useGraphByIdQuery: () => query(m.states.get(m.id)?.graph),
    useGraphEmployeesQuery: () => query(m.states.get(m.id)?.employees),
    useGraphSlotsQuery: () => ({ ...query(m.states.get(m.id)?.slots), isError: m.slotsFailed, refetchWithSignal: m.retrySlots }),
    useGraphCellStylesQuery: () => query(m.states.get(m.id)?.styles),
    useGraphSlotsBatchQuery: () => query(undefined), useSchedulePresetsQuery: () => query(m.empty),
    useManagerGraphFillColorBindsQuery: () => query(m.empty), useManagerGraphTextColorBindsQuery: () => query(m.empty),
    ContainerGraphEditor: (props: EditorProps) => {
      m.editor = props;
      return <>
        <button onClick={props.onSave}>Save editor</button>
        <button onClick={props.onUndo}>Undo</button>
        <button onClick={props.onVersionCheckoutComplete}>Finish checkout</button>
        <output aria-label="Save error">{props.submitError}</output>
      </>;
    },
  };
});
vi.mock("@entities/employees/api/queries", () => ({ useEmployeesListQuery: () => ({ data: m.employees }) }));
vi.mock("@entities/shops/api/queries", () => ({ useShopsListQuery: () => ({ data: m.shops }) }));
vi.mock("@entities/employment-contracts", () => ({ useEmploymentContractSettingsQuery: () => ({ data: { automaticWeekdayAvailability: false } }) }));
vi.mock("@entities/availability-groups", async importOriginal => ({ ...await importOriginal<object>(),
  useAvailabilityGroupsListQuery: () => ({ data: m.groups }), useAvailabilityGroupMembersQuery: () => ({ data: m.members }),
  useAvailabilityGroupSlotsQuery: () => ({ data: m.empty }),
}));
vi.mock("@entities/availability-binds", async importOriginal => ({ ...await importOriginal<object>(),
  useAvailabilityBindsListQuery: () => ({ data: m.empty }),
}));
vi.mock("@entities/urlop", async importOriginal => ({ ...await importOriginal<object>(),
  useGraphApprovedUrlopQuery: () => ({ data: m.urlopFailed ? undefined : m.empty, isError: m.urlopFailed, refetchWithSignal: m.retryUrlop }),
}));
vi.mock("@entities/shift-swaps", async importOriginal => ({ ...await importOriginal<object>(),
  shiftSwapsApi: { listGraphLog: m.listGraphLog },
  useGraphShiftSwapLogQuery: () => ({ data: m.offers.get(m.id) ?? m.empty }),
  useCreateManagerManualShiftSwapMutation: () => ({ mutate: m.publish, isPending: false }),
}));

const getState = (id = m.id) => m.states.get(id)!;
const argb = (value: string) => rgbHexToArgb(value);
const setStyle = (id: number, style: UpsertGraphCellStyleDto) => {
  const state = getState(id);
  const existing = state.styles.find(row => row.employeeId === style.employeeId && row.dayOfMonth === style.dayOfMonth);
  const next = { id: existing?.id ?? state.styles.length + 100, scheduleId: id, ...style };
  state.styles = [...state.styles.filter(row => row.id !== existing?.id), next];
  return next;
};
const paint = (field: "background" | "text", value: string, keys: string[]) => act(() => {
  if (field === "background") m.editor!.onApplyFillColorShortcut(value, keys);
  else m.editor!.onApplyTextColorShortcut(value, keys);
});
const open = () => {
  const client = new QueryClient();
  const page = () => <QueryClientProvider client={client}><ContainerGraphEditPage /></QueryClientProvider>;
  const view = render(page());
  return {
    ...view,
    client,
    refresh: () => view.rerender(page()),
    select: (id: number) => {
      fireEvent.click(screen.getByRole("tab", { name: `Graph ${id}` }));
      m.id = id;
      view.rerender(page());
      m.navigate.mockClear();
    },
  };
};
const save = () => {
  fireEvent.click(screen.getByRole("button", { name: "Save editor" }));
  fireEvent.click(screen.getByRole("button", { name: "Confirm action" }));
};
const renamed = (name: string) => act(() => m.editor!.onFieldChange("name")(name));
const calls = (mock: ReturnType<typeof vi.fn>) => mock.mock.calls.map(([, id]) => id);
const findRecoveryDialog = () => screen.queryByRole("dialog", { name: "Schedule save and recovery" });
const openRecovery = async (name = "Review issue") => {
  if (!findRecoveryDialog()) fireEvent.click(await screen.findByRole("button", { name, exact: true }));
  return within(screen.getByRole("dialog", { name: "Schedule save and recovery" }));
};
const restoreDrafts = async () => {
  const dialog = await openRecovery("Restore draft");
  fireEvent.click(dialog.getByRole("button", { name: "Restore draft" }));
  return dialog;
};
const expectNavigation = async () => {
  await waitFor(() => expect(m.navigate, findRecoveryDialog()?.textContent ?? m.editor?.submitError).toHaveBeenCalledOnce());
  expect(findRecoveryDialog()).not.toBeInTheDocument();
  expect(screen.queryByRole("region", { name: "Schedule save and recovery" })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Review issue" })).not.toBeInTheDocument();
};
const changedSlot = (id: number, fromTime = "08:00"): GraphSlot =>
  ({ id: 1, scheduleId: id, dayOfMonth: 3, slotNo: 1, employeeId: 2, status: 1, fromTime, toTime: "12:00" });
const expectNoWorkspaceWrites = () => {
  for (const mock of [defaults.replaceGraphSlots, defaults.createGraphEmployee, defaults.updateGraphEmployee, defaults.removeGraphEmployee]) {
    expect(mock).not.toHaveBeenCalled();
  }
};
afterEach(() => vi.useRealTimers());

const rateLimited = () => new ApiError({ message: "Too many requests. Please try again later.", status: 429, retryAfterMs: 1000 });

function editFiveTabs(edit: (id: number) => void) {
  const view = open();
  for (const id of ids) {
    if (id !== 34) view.select(id);
    edit(id);
  }
  return view;
}

beforeEach(() => {
  setLanguage("en");
  window.sessionStorage.clear();
  vi.clearAllMocks();
  Object.values(defaults).forEach(mock => mock.mockReset());
  m.states.clear(); m.versions.clear(); m.offers.clear();
  m.groups = []; m.members = [];
  m.slotsFailed = false; m.urlopFailed = false;
  m.retrySlots.mockReset(); m.retryUrlop.mockReset();
  m.id = 34; m.managerId = 9; m.editor = null; m.locks.lockedByOtherState = null;
  for (const id of ids) {
    m.states.set(id, {
      graph: {
        id, containerId: 7, shopId: 1, name: `Graph ${id}`, year: 2026, month: 11, publicationStatus: "public", allowSwap: true,
        peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
        maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10,
      },
      employees: m.employees.map(({ id: employeeId }, displayOrder) =>
        ({ id: employeeId, scheduleId: id, employeeId, displayOrder, isPriority: false })),
      slots: [changedSlot(id)],
      styles: [],
    });
    m.versions.set(id, { currentVersionId: 1, versions: [{ id: 1 }] as GraphVersionTreeDto["versions"] });
  }
  defaults.graphById.mockImplementation(async (_, id) => getState(id).graph);
  defaults.listGraphEmployees.mockImplementation(async (_, id) => getState(id).employees);
  defaults.listGraphSlots.mockImplementation(async (_, id) => getState(id).slots);
  defaults.listGraphCellStyles.mockImplementation(async (_, id) => getState(id).styles);
  defaults.listGraphVersions.mockImplementation(async (_, id) => m.versions.get(id));
  defaults.updateGraph.mockImplementation(async (_, id, payload: SaveGraphDto) => {
    getState(id).graph = { ...getState(id).graph, ...payload, note: payload.note ?? null };
  });
  defaults.replaceGraphSlots.mockImplementation(async (_, id, { slots }: { slots: SaveGraphSlotDto[] }) => {
    getState(id).slots = slots.map((slot, index) => ({ ...slot, id: index + 1, scheduleId: id, status: Number(slot.status) }));
  });
  defaults.applyGraphCellStyleBatch.mockImplementation(async (_, id, { changes }: BatchUpdateGraphCellStylesDto) => {
    const updates = changes.map(patch => {
      const current = getState(id).styles.find(row => row.employeeId === patch.employeeId && row.dayOfMonth === patch.dayOfMonth);
      const background = current?.backgroundColorArgb ?? null;
      const text = current?.textColorArgb ?? null;
      if ((patch.changeBackgroundColor && background !== patch.expectedBackgroundColorArgb && background !== patch.backgroundColorArgb) ||
          (patch.changeTextColor && text !== patch.expectedTextColorArgb && text !== patch.textColorArgb)) {
        throw new ApiError({ message: "Cell color changed", status: 409 });
      }
      return { employeeId: patch.employeeId, dayOfMonth: patch.dayOfMonth,
        backgroundColorArgb: patch.changeBackgroundColor ? patch.backgroundColorArgb ?? null : background,
        textColorArgb: patch.changeTextColor ? patch.textColorArgb ?? null : text,
      };
    });
    for (const update of updates) setStyle(id, update);
    getState(id).styles = getState(id).styles.filter(row => row.backgroundColorArgb != null || row.textColorArgb != null);
  });
  defaults.commitGraphVersion.mockImplementation(async (_, id) => {
    const previous = m.versions.get(id)!;
    const currentVersionId = (previous.currentVersionId ?? 0) + 1;
    const next = { currentVersionId, versions: [...previous.versions, { id: currentVersionId }] as GraphVersionTreeDto["versions"] };
    m.versions.set(id, next);
    return next;
  });
  m.listGraphLog.mockImplementation(async (_, id) => m.offers.get(id) ?? []);
});

test("preflights and writes only dirty tabs and navigates after both readbacks", async () => {
  const view = open();
  view.select(36); renamed("Second changed");
  view.select(37); view.select(38); view.select(42); renamed("Fifth changed");
  save();
  await expectNavigation();
  for (const mock of [defaults.graphById, defaults.listGraphEmployees, defaults.listGraphSlots, defaults.listGraphCellStyles]) {
    expect(calls(mock)).toEqual([36, 42, 36, 42]);
  }
  expect(calls(defaults.updateGraph)).toEqual([36, 42]);
  expect(calls(defaults.commitGraphVersion)).toEqual([36, 42]);
  expectNoWorkspaceWrites();
  expect(readGraphRecovery(9, 7)).toBeNull();
});

test("saves five dirty graphs in tab order after all bounded preflights", async () => {
  const trace: string[] = [];
  let inFlight = 0;
  let peak = 0;
  for (const [resource, mock] of [
    ["graph", defaults.graphById], ["employees", defaults.listGraphEmployees],
    ["slots", defaults.listGraphSlots], ["styles", defaults.listGraphCellStyles],
  ] as const) {
    const read = mock.getMockImplementation()!;
    mock.mockImplementation(async (...args) => {
      trace.push(`GET ${args[1]} ${resource}`);
      peak = Math.max(peak, ++inFlight);
      try { return await read(...args); } finally { inFlight--; }
    });
  }
  for (const mock of [defaults.updateGraph, defaults.replaceGraphSlots, defaults.applyGraphCellStyleBatch, defaults.commitGraphVersion]) {
    const write = mock.getMockImplementation()!;
    mock.mockImplementation(async (...args) => { trace.push(`WRITE ${args[1]}`); return write(...args); });
  }
  const keys = [1, 2].flatMap(id => Array.from({ length: 25 }, (_, index) => `${id}:${index + 1}`));
  editFiveTabs(id => {
    paint("background", "#123456", [...keys, ...Array.from({ length: id === 34 ? 6 : 0 }, (_, index) => `${index % 2 + 1}:${26 + Math.floor(index / 2)}`)]);
    paint("text", "#abcdef", ["0:26"]);
    act(() => m.editor!.onCellChange(1, 28, "Training"));
    if (id === 34 || id === 42) act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  });
  save();
  await expectNavigation();
  expect(trace.slice(0, 20)).toEqual(ids.flatMap(id => ["graph", "employees", "slots", "styles"].map(resource => `GET ${id} ${resource}`)));
  expect(peak).toBe(2);
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(ids);
  expect(calls(defaults.replaceGraphSlots)).toEqual([34, 42]);
  expect(calls(defaults.commitGraphVersion)).toEqual(ids);
  expect(defaults.upsertGraphCellStyle).not.toHaveBeenCalled();
  expect(defaults.removeGraphCellStyle).not.toHaveBeenCalled();
  for (const id of ids) {
    expect(getState(id).styles).toHaveLength(id === 34 ? 56 : 50);
    const note = parseGraphNoteContent(getState(id).graph.note);
    expect(note.textCells).toContainEqual({ employeeId: 1, dayOfMonth: 28, value: "Training" });
    expect(note.cellStyles).toContainEqual(expect.objectContaining({ employeeId: 0, dayOfMonth: 26, textColorArgb: argb("#abcdef") }));
  }
  expect(readGraphRecovery(9, 7)).toBeNull();
});

test("rate-limited third preflight keeps all drafts and writes nothing until the GET recovers", async () => {
  const view = editFiveTabs(() => paint("text", "#abcdef", ["1:1"]));
  const read = defaults.graphById.getMockImplementation()!;
  let available = false;
  defaults.graphById.mockImplementation(async (...args) => {
    if (args[1] === 37 && !available) throw rateLimited();
    return read(...args);
  });
  vi.useFakeTimers();
  save();
  expect(screen.getByRole("button", { name: "Saving..." })).toBeDisabled();
  expect(findRecoveryDialog()).not.toBeInTheDocument();
  await act(async () => vi.advanceTimersByTimeAsync(2000));
  vi.useRealTimers();
  const issue = await openRecovery();
  expect(issue.getByText("Graph 37 · Cannot verify")).toBeVisible();
  expect(screen.getByText("Graph 38 · Pending")).toBeVisible();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(readGraphRecovery(9, 7)).toHaveLength(5);
  expect(isGraphSaveTrafficActive(view.client, 7)).toBe(false);
  available = true;
  fireEvent.click(screen.getByRole("button", { name: "Retry unsaved graphs" }));
  await expectNavigation();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(ids);
});

test.each([false, true])("third rate-limited batch does not replay writes (persisted=%s)", async persisted => {
  editFiveTabs(() => paint("text", "#abcdef", ["1:1"]));
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(apply).mockImplementationOnce(apply)
    .mockImplementationOnce(async (...args) => {
      if (persisted) await apply(...args);
      throw rateLimited();
    });
  save();
  expect(await (await openRecovery()).findByText(persisted ? "Graph 37 · Partially saved" : "Graph 37 · Cannot verify")).toBeVisible();
  expect(screen.getByText("Graph 34 · Saved")).toBeVisible();
  expect(screen.getByText("Graph 36 · Saved")).toBeVisible();
  expect(screen.getByText("Graph 38 · Pending")).toBeVisible();
  expect(screen.getByText("Graph 42 · Pending")).toBeVisible();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(ids.slice(0, 3));
  expect(calls(defaults.commitGraphVersion)).toEqual(ids.slice(0, 2));
  expect(readGraphRecovery(9, 7)?.map(entry => entry.graphId)).toEqual(ids.slice(2));
  fireEvent.click(screen.getByRole("button", { name: "Retry unsaved graphs" }));
  await expectNavigation();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(persisted ? ids : [34, 36, 37, 37, 38, 42]);
  expect(calls(defaults.commitGraphVersion)).toEqual(ids);
});

test("a same-field change between preflight and the style batch preserves the whole draft", async () => {
  open();
  paint("background", "#123456", ["1:1", "1:2"]);
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(async (...args) => {
    setStyle(34, { employeeId: 1, dayOfMonth: 2, backgroundColorArgb: -7, textColorArgb: null });
    return apply(...args);
  });
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Conflict")).toBeVisible();
  expect(getState().styles).toHaveLength(1);
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(readGraphRecovery(9, 7)).toHaveLength(1);
});

test.each([false, true])("removing an employee never upserts their unsaved styles (persisted=%s)", async persisted => {
  if (persisted) setStyle(34, { employeeId: 2, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: null });
  open();
  paint("text", "#abcdef", ["2:1", "2:2"]);
  act(() => m.editor!.onRemoveEmployee(2));
  fireEvent.click(within(screen.getByRole("dialog", { name: "Remove employee" })).getByRole("button", { name: "Confirm action" }));
  defaults.removeGraphEmployee.mockImplementationOnce(async (_, id, assignmentId) => {
    getState(id).employees = getState(id).employees.filter(row => row.id !== assignmentId);
  });
  save();
  await expectNavigation();
  expect(getState().employees.map(row => row.employeeId)).toEqual([1]);
  expect(getState().styles).toEqual([]);
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(persisted ? 1 : 0);
  if (persisted) expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledWith(7, 34, { changes: [{
    employeeId: 2, dayOfMonth: 1, changeBackgroundColor: true, expectedBackgroundColorArgb: -7,
    backgroundColorArgb: null, changeTextColor: false,
  }] });
});

test("unmount cancels a waiting preflight without leaving the traffic gate active", async () => {
  const view = open();
  paint("text", "#abcdef", ["1:1"]);
  defaults.graphById.mockRejectedValue(rateLimited());
  vi.useFakeTimers();
  save();
  await act(async () => vi.advanceTimersByTimeAsync(1));
  expect(isGraphSaveTrafficActive(view.client, 7)).toBe(true);
  view.unmount();
  await act(async () => vi.advanceTimersByTimeAsync(3000));
  expect(isGraphSaveTrafficActive(view.client, 7)).toBe(false);
  expect(defaults.graphById).toHaveBeenCalledOnce();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(readGraphRecovery(9, 7)).toHaveLength(1);
});

test("one color-only tab persists real, manual and header styles without writing the slot matrix", async () => {
  getState(34).graph.note = buildGraphNoteContent("Memo", [{ id: 1, label: "Manual", cells: { "2": "08:00 - 12:00" } }]);
  const view = open();
  for (const id of [36, 37, 38, 42, 34]) view.select(id);
  paint("background", "#123456", ["1:1", "2:1", "-1:2", "0:1"]);
  paint("text", "#abcdef", ["1:1", "2:1", "-1:2", "0:1"]);
  save();
  await expectNavigation();
  expectNoWorkspaceWrites();
  expect(calls(defaults.updateGraph)).toEqual([34]);
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34]);
  expect(defaults.applyGraphCellStyleBatch.mock.calls[0][2].changes).toHaveLength(2);
  expect(defaults.upsertGraphCellStyle).not.toHaveBeenCalled();
  expect(calls(defaults.commitGraphVersion)).toEqual([34]);
  expect(parseGraphNoteContent(getState().graph.note).cellStyles).toEqual([
    { employeeId: -1, dayOfMonth: 2, backgroundColorArgb: argb("#123456"), textColorArgb: argb("#abcdef") },
    { employeeId: 0, dayOfMonth: 1, backgroundColorArgb: argb("#123456"), textColorArgb: argb("#abcdef") },
  ]);
  view.unmount(); open();
  for (const key of ["1:1", "2:1", "-1:2", "0:1"]) {
    expect(m.editor!.styleMap[key]).toMatchObject({ backgroundColorArgb: argb("#123456"), textColorArgb: argb("#abcdef") });
  }
});

test("merges a local text color with a remote background and slot change", async () => {
  open();
  paint("text", "#abcdef", ["1:1"]);
  setStyle(34, { employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: null });
  getState().slots = [changedSlot(34, "09:00")];
  getState().graph = { ...getState().graph, lastUpdatedAtUtc: "2026-11-08T21:00:00Z" };
  save();
  await expectNavigation();
  expectNoWorkspaceWrites();
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  expect(getState().slots).toEqual([changedSlot(34, "09:00")]);
  expect(getState().styles[0]).toMatchObject({ backgroundColorArgb: -7, textColorArgb: argb("#abcdef") });
  expect(m.editor!.cellMap["2:3"]).toBe("09:00 - 12:00");
});

test("opening a saved roster with different availability members does not turn a color edit into workspace writes", async () => {
  getState().graph.availabilityGroupId = 1;
  m.groups = [{ id: 1, name: "Availability", year: 2026, month: 11 }];
  m.members = [{ id: 1, employeeId: 1, displayOrder: 0 }];
  open();
  expect(m.editor!.graphEmployeeRows.map(row => row.employeeId)).toEqual([1, 2]);
  expect(m.editor!.hasUnsavedChanges).toBe(false);
  paint("text", "#abcdef", ["1:1"]);
  save();
  await expectNavigation();
  expectNoWorkspaceWrites();
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  expect(getState().slots).toEqual([changedSlot(34)]);
});

test("color conflict preflights all dirty schedules and writes none", async () => {
  const view = open();
  paint("background", "#123456", ["1:1"]);
  view.select(36);
  renamed("Second changed");
  setStyle(34, { employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: null });
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Conflict")).toBeVisible();
  expect(screen.getByText("Conflicting background color at employee 1, day 1.")).toBeVisible();
  expect(screen.getByText("Graph 36 · Pending")).toBeVisible();
  expect(calls(defaults.graphById)).toEqual([34, 36]);
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(readGraphRecovery(9, 7)?.map(entry => entry.graphId)).toEqual([34, 36]);
  expect(m.prompt).toHaveBeenLastCalledWith(true);
});

test("clearing a local background keeps independently added remote text", async () => {
  setStyle(34, { employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: null });
  open();
  act(() => m.editor!.onSelectedCellKeysChange(["1:1"]));
  act(() => m.editor!.onClearCellStyle());
  setStyle(34, { employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: -8 });
  save();
  await expectNavigation();
  expect(getState().styles[0]).toMatchObject({ backgroundColorArgb: null, textColorArgb: -8 });
  expect(defaults.removeGraphCellStyle).not.toHaveBeenCalled();
});

test("same-mounted saves advance the baseline and an unchanged Save sends nothing", async () => {
  open();
  paint("background", "#123456", ["1:1"]);
  save();
  await waitFor(() => expect(m.navigate).toHaveBeenCalledTimes(1));
  getState().graph = { ...getState().graph, lastUpdatedAtUtc: "2026-11-08T21:00:00Z" };
  paint("text", "#abcdef", ["1:1"]);
  save();
  await waitFor(() => expect(m.navigate).toHaveBeenCalledTimes(2));
  save();
  expect(await screen.findByText("Nothing to save.")).toBeVisible();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
  expect(defaults.updateGraph).not.toHaveBeenCalled();
});

test.each([false, true])("retry finishes only missing color operations after a failed response (persisted=%s)", async persisted => {
  open();
  paint("background", "#123456", ["1:1", "1:2"]);
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(async (...args) => {
    if (persisted) await apply(...args);
    throw new Error("Lost style response");
  });
  save();
  expect(await (await openRecovery()).findByText(persisted ? "Graph 34 · Partially saved" : "Graph 34 · Cannot verify")).toBeVisible();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(readGraphRecovery(9, 7)?.[0].needsStyleVersion).toBe(true);
  fireEvent.click(screen.getByRole("button", { name: "Retry unsaved graphs" }));
  await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(persisted ? 1 : 2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(getState().styles).toHaveLength(2);
  expect(readGraphRecovery(9, 7)).toBeNull();
});

test("a failed second graph preserves earlier verified results and unsent drafts", async () => {
  const view = open();
  for (const id of [34, 36, 42]) {
    if (id !== 34) view.select(id);
    paint("text", "#abcdef", ["1:1"]);
  }
  const normalUpsert = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(normalUpsert).mockRejectedValueOnce(new Error("Network error"));
  save();
  expect(await (await openRecovery()).findByText("Graph 36 · Cannot verify")).toBeVisible();
  expect(screen.getByText("Graph 34 · Saved")).toBeVisible();
  expect(screen.getByText("Graph 42 · Pending")).toBeVisible();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34, 36]);
  expect(calls(defaults.commitGraphVersion)).toEqual([34]);
  expect(readGraphRecovery(9, 7)?.map(entry => entry.graphId)).toEqual([36, 42]);
  fireEvent.click(screen.getByRole("button", { name: "Retry unsaved graphs" }));
  await waitFor(() => expect(m.navigate).toHaveBeenCalledOnce());
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34, 36, 36, 42]);
  expect(calls(defaults.commitGraphVersion)).toEqual([34, 36, 42]);
});

test.each(["graph", "slots", "version"] as const)("keeps the workspace draft after a %s write fails", async stage => {
  open();
  renamed("Changed name");
  act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  const failure = new ApiError({ message: "Injected write error", status: 409 });
  if (stage === "graph") defaults.updateGraph.mockRejectedValueOnce(failure);
  if (stage === "slots") defaults.replaceGraphSlots.mockRejectedValueOnce(failure);
  if (stage === "version") defaults.commitGraphVersion.mockRejectedValueOnce(failure);
  save();
  expect(await (await openRecovery()).findByText(`Graph 34 · ${stage === "graph" ? "Cannot verify" : "Partially saved"}`)).toBeVisible();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(m.editor!.form.name).toBe("Changed name");
  expect(readGraphRecovery(9, 7)?.[0].blockedBy).toBe("workspace-write");
  expect(defaults.listGraphVersions).toHaveBeenCalledTimes(stage === "version" ? 2 : 1);
});

test("an ambiguous version commit is checked against the version tree and never replayed", async () => {
  const view = open();
  paint("text", "#abcdef", ["1:1"]);
  const commit = defaults.commitGraphVersion.getMockImplementation()!;
  defaults.commitGraphVersion.mockImplementationOnce(async (...args) => {
    await commit(...args);
    throw new Error("Lost commit response");
  });
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Cannot verify")).toBeVisible();
  expect(defaults.listGraphVersions).toHaveBeenCalledTimes(2);
  expect(readGraphRecovery(9, 7)?.[0].blockedBy).toBe("version-commit");
  view.unmount(); open();
  await restoreDrafts();
  fireEvent.click(screen.getByRole("button", { name: "Review saved schedule" }));
  await waitFor(() => expect(defaults.listGraphVersions).toHaveBeenCalledTimes(3));
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(m.navigate).not.toHaveBeenCalled();
});

test("a confirmed commit with an unavailable readback can be verified without replaying writes", async () => {
  open();
  paint("text", "#abcdef", ["1:1"]);
  const read = defaults.graphById.getMockImplementation()!;
  defaults.graphById.mockImplementationOnce(read).mockRejectedValueOnce(new Error("Readback unavailable"));
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Cannot verify")).toBeVisible();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  fireEvent.click(screen.getByRole("button", { name: "Review saved schedule" }));
  expect(await (await openRecovery()).findByText("Graph 34 · Saved")).toBeVisible();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(readGraphRecovery(9, 7)).toBeNull();
});

test("background errors preserve the draft and retry only failed reads before allowing Save", async () => {
  const view = open();
  paint("text", "#abcdef", ["1:1"]);
  act(() => {
    m.editor!.onCellChange(1, 28, "Training");
    m.editor!.onCellChange(2, 3, "09:00 - 12:00");
  });
  const { cellMap, styleMap } = m.editor!;
  m.slotsFailed = true;
  m.urlopFailed = true;
  view.refresh();
  expect(m.editor!.hasLoadError).toBe(false);
  expect(m.editor!.isLoading).toBe(false);
  expect(m.editor!.isSaveBlocked).toBe(true);
  expect(m.editor!.isGenerationBlocked).toBe(true);
  expect(screen.queryByText(/Schedule data could not be refreshed/)).not.toBeInTheDocument();
  const issue = await openRecovery();
  expect(issue.getByText(/Schedule data could not be refreshed/)).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Save editor" }));
  expect(screen.queryByRole("button", { name: "Confirm action" })).not.toBeInTheDocument();
  m.retryUrlop.mockRejectedValueOnce(rateLimited()).mockImplementationOnce(async () => { m.urlopFailed = false; });
  m.retrySlots.mockImplementationOnce(async () => { m.slotsFailed = false; });
  vi.useFakeTimers();
  fireEvent.click(screen.getByRole("button", { name: "Retry loading" }));
  expect(m.retrySlots).not.toHaveBeenCalled();
  await act(async () => vi.advanceTimersByTimeAsync(1000));
  vi.useRealTimers();
  expect(m.retryUrlop).toHaveBeenCalledTimes(2);
  expect(m.retrySlots).toHaveBeenCalledOnce();
  expect(defaults.graphById).not.toHaveBeenCalled();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  view.refresh();
  expect(m.editor!.cellMap).toEqual(cellMap);
  expect(m.editor!.styleMap).toEqual(styleMap);
  expect(m.editor!.isSaveBlocked).toBe(false);
  expect(m.editor!.isGenerationBlocked).toBe(false);
  fireEvent.click(issue.getByRole("button", { name: "Close" }));
  save();
  await expectNavigation();
});

test("a rate-limited version commit can retry without rewriting confirmed colors", async () => {
  open();
  paint("text", "#abcdef", ["1:1"]);
  const list = defaults.listGraphVersions.getMockImplementation()!;
  const limited = rateLimited();
  defaults.listGraphVersions.mockImplementationOnce(list).mockRejectedValueOnce(limited)
    .mockRejectedValueOnce(limited).mockRejectedValueOnce(limited);
  defaults.commitGraphVersion.mockRejectedValueOnce(limited);
  vi.useFakeTimers();
  save();
  await act(async () => vi.advanceTimersByTimeAsync(2000));
  vi.useRealTimers();
  expect(await (await openRecovery()).findByText("Graph 34 · Cannot verify")).toBeVisible();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(readGraphRecovery(9, 7)?.[0].blockedBy).toBe("version-commit");
  fireEvent.click(screen.getByRole("button", { name: "Review saved schedule" }));
  expect(await (await openRecovery()).findByText("Graph 34 · Partially saved")).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Retry unsaved graphs" }));
  await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
  expect(m.versions.get(34)?.versions).toHaveLength(2);
});

test("a lost manual publication response survives refresh without publishing again", async () => {
  getState().graph.note = buildGraphNoteContent("", [{ id: 1, label: "Manual", cells: { "2": "08:00 - 12:00" } }]);
  const view = open();
  act(() => m.editor!.onPublishManualShift({ manualColumnId: 1, dayOfMonth: 2, fromTime: "08:00", toTime: "12:00", allowPartialAcceptance: false }));
  m.publish.mockImplementationOnce((variables, callbacks) => {
    getState().slots = [...getState().slots, { ...changedSlot(34), id: 100, employeeId: null, status: 0, dayOfMonth: 2 }];
    m.offers.set(34, [{ ...variables, id: 20, scheduleId: 34, scheduleSlotId: 100, isManagerCreated: true, status: "open" }]);
    callbacks.onError(new Error("Lost offer response"));
  });
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Partially saved")).toBeVisible();
  expect(readGraphRecovery(9, 7)?.[0].manualPublicationAttempted).toBe(true);
  view.unmount(); open();
  await restoreDrafts();
  fireEvent.click(screen.getByRole("button", { name: "Review saved schedule" }));
  await screen.findByText(/Review the saved schedule or discard this draft/);
  expect(m.publish).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
});

test("explicit recovery retains the original baseline and discard opens server data", async () => {
  let view = open();
  renamed("Recovered name");
  act(() => window.dispatchEvent(new Event("pagehide")));
  view.unmount();
  getState().slots = [changedSlot(34, "09:00")];
  m.managerId = 99;
  view = open();
  expect(screen.queryByRole("button", { name: "Restore draft" })).not.toBeInTheDocument();
  view.unmount(); m.managerId = 9;
  view = open();
  expect(m.editor!.form.name).toBe("Graph 34");
  const restored = await restoreDrafts();
  fireEvent.click(restored.getByRole("button", { name: "Close" }));
  expect(m.editor!.form.name).toBe("Recovered name");
  save();
  expect(await (await openRecovery()).findByText("Graph 34 · Conflict")).toBeVisible();
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  view.unmount(); open();
  const issue = await openRecovery("Restore draft");
  fireEvent.click(issue.getByRole("button", { name: "Discard local draft" }));
  const confirmation = () => within(screen.getByRole("dialog", { name: "Discard local draft" }));
  expect(readGraphRecovery(9, 7)).toHaveLength(1);
  fireEvent.click(confirmation().getByRole("button", { name: "Cancel" }));
  expect(readGraphRecovery(9, 7)).toHaveLength(1);
  fireEvent.click((await openRecovery("Restore draft")).getByRole("button", { name: "Discard local draft" }));
  fireEvent.click(confirmation().getByRole("button", { name: "Confirm action" }));
  expect(readGraphRecovery(9, 7)).toBeNull();
  expect(m.editor!.form.name).toBe("Graph 34");
});

test("corrupt storage is ignored and unavailable storage keeps the navigation guard", async () => {
  window.sessionStorage.setItem("gf3:graph-drafts:9:7", "{bad");
  open();
  expect((await openRecovery()).getByText("Some local drafts could not be restored.")).toBeVisible();
  const storage = vi.spyOn(Storage.prototype, "setItem").mockImplementationOnce(() => { throw new Error("Quota"); });
  renamed("Draft in memory");
  act(() => window.dispatchEvent(new Event("pagehide")));
  expect(await screen.findByText(/restoring after refresh is unavailable/)).toBeVisible();
  expect(m.prompt).toHaveBeenLastCalledWith(true);
  storage.mockRestore();
});

test("style removal, undo, checkout and edit locks retain their editor behavior", async () => {
  setStyle(34, { employeeId: 1, dayOfMonth: 1, backgroundColorArgb: -7, textColorArgb: null });
  const view = open();
  act(() => m.editor!.onSelectedCellKeysChange(["1:1"]));
  act(() => m.editor!.onClearCellStyle());
  fireEvent.click(screen.getByRole("button", { name: "Undo" }));
  expect(m.editor!.styleMap["1:1"]).toBeDefined();
  act(() => m.editor!.onSelectedCellKeysChange(["1:1"]));
  act(() => m.editor!.onClearCellStyle());
  save();
  await waitFor(() => expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce());
  expect(defaults.removeGraphCellStyle).not.toHaveBeenCalled();
  await waitFor(() => expect(m.navigate).toHaveBeenCalledOnce());
  fireEvent.click(screen.getByRole("button", { name: "Finish checkout" }));
  expect(m.navigate).toHaveBeenLastCalledWith(0);
  view.unmount();
  m.locks.lockedByOtherState = { owner: "other" };
  open();
  expect(screen.getByText("Another manager is editing this schedule.")).toBeVisible();
  expect(screen.queryByRole("button", { name: "Save editor" })).not.toBeInTheDocument();
});
