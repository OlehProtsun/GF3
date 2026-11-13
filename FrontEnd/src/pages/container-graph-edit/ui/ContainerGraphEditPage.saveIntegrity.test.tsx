import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { readFileSync } from "node:fs";
import type { ComponentProps } from "react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ContainerGraphEditor } from "@entities/containers/ui/ContainerGraphEditor";
import type { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import type { GraphSlot } from "@entities/containers/model/types";
import type { PersistedGraphState } from "@entities/containers/model/graphSaveIntegrity";
import type { BatchUpdateGraphCellStylesDto, GraphVersionTreeDto, SaveGraphDto, SaveGraphSlotDto, UpsertGraphCellStyleDto } from "@entities/containers/api/dto";
import type { ShiftSwap } from "@entities/shift-swaps";
import { buildGraphNoteContent, parseGraphNoteContent } from "@entities/containers/model/graphNote";
import { createGraphFormFromGraph } from "@entities/containers/model/graphForm";
import { buildGraphCellMap, rgbHexToArgb } from "@entities/containers/model/graphWorkspace";
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
  openIds: [34, 36, 37, 38, 42],
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
  useLocation: () => ({ key: "test", pathname: `/container/7/graphs/${m.id}/edit`, search: `?openGraphIds=${m.openIds.join(",")}` }),
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
// Pacing itself is covered with deterministic clocks in graphSavePacer.test and real HTTP in Playwright.
vi.mock("@shared/api/graphSavePacer", () => ({ createGraphSavePacer: () => ({ run: <T,>(send: () => Promise<T>) => send() }) }));
vi.mock("@shared/ui/ConfirmDialog", () => ({
  ConfirmDialog: ({ open, title, message, footerSlot, onConfirm, onCancel, confirmText, cancelText, confirmDisabled, cancelDisabled }: ComponentProps<typeof ConfirmDialog>) =>
    open ? <div role="dialog" aria-label={title}><p>{message}</p>{footerSlot}
      <button disabled={confirmDisabled} onClick={onConfirm}>{confirmText === "Try again" || confirmDisabled ? confirmText : "Confirm action"}</button>
      <button disabled={cancelDisabled} onClick={onCancel}>{cancelText ?? "Cancel"}</button></div> : null,
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
const failure = async () => {
  await screen.findByRole("button", { name: "Try again" });
  return within(screen.getByRole("dialog", { name: /Save schedule/ }));
};
const retry = () => fireEvent.click(screen.getByRole("button", { name: "Try again" }));
const expectNavigation = async () => {
  await waitFor(() => expect(m.navigate, screen.queryByRole("dialog", { name: /Save schedule/ })?.textContent ?? m.editor?.submitError).toHaveBeenCalledOnce());
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
  m.publish.mockReset();
  m.employees = [{ id: 1, firstName: "Test", lastName: "One" }, { id: 2, firstName: "Test", lastName: "Two" }];
  m.groups = []; m.members = [];
  m.slotsFailed = false; m.urlopFailed = false;
  m.retrySlots.mockReset(); m.retryUrlop.mockReset();
  m.openIds = [...ids];
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

function tenGraphs(employeeCount = 2) {
  m.openIds = [34, 36, 37, 38, 42, 43, 44, 45, 46, 47];
  m.employees = Array.from({ length: employeeCount }, (_, index) => ({ id: index + 1, firstName: "Worker", lastName: String(index) }));
  const template = structuredClone(getState(34));
  for (const id of m.openIds) {
    m.states.set(id, { ...structuredClone(template), graph: { ...template.graph, id, name: `Graph ${id}` },
      slots: template.slots.map(slot => ({ ...slot, scheduleId: id })),
      employees: m.employees.map(({ id: employeeId }, displayOrder) => ({ id: employeeId, employeeId, scheduleId: id, displayOrder, isPriority: false })),
    });
    m.versions.set(id, { currentVersionId: 1, versions: [{ id: 1 }] as GraphVersionTreeDto["versions"] });
  }
}

test.each([1, 10])("unchanged %s-tab Save navigates with zero writes and versions", async count => {
  tenGraphs(); m.openIds = m.openIds.slice(0, count);
  open(); save(); await expectNavigation();
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  expectNoWorkspaceWrites();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(defaults.graphById).not.toHaveBeenCalled();
  expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
  expect(screen.queryByText("Nothing to save.")).not.toBeInTheDocument();
});

test("already reconciled colors navigate without writes, versions or unrelated invalidations", async () => {
  const view = open(); paint("text", "#abcdef", ["1:1"]);
  setStyle(34, { employeeId: 1, dayOfMonth: 1, textColorArgb: argb("#abcdef"), backgroundColorArgb: null });
  const invalidate = vi.spyOn(view.client, "invalidateQueries");
  save(); await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(invalidate).not.toHaveBeenCalled();
});

test("three dirty graphs of ten write precisely their changed resource categories", async () => {
  tenGraphs(); const view = open();
  paint("text", "#abcdef", ["1:1"]);
  view.select(38); renamed("Metadata only");
  view.select(47); act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  save(); await expectNavigation();
  expect(calls(defaults.updateGraph)).toEqual([38]);
  expect(calls(defaults.replaceGraphSlots)).toEqual([47]);
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34]);
  expect(calls(defaults.commitGraphVersion)).toEqual([34, 38, 47]);
  expect(defaults.removeGraphEmployee).not.toHaveBeenCalled();
  expect(defaults.upsertGraphCellStyle).not.toHaveBeenCalled();
});

test("ten graphs retain 500 mixed edited cells each after authoritative fixture reload", async () => {
  tenGraphs(20); const view = open();
  const keys = Array.from({ length: 20 }, (_, employee) => Array.from({ length: 25 }, (_, day) => `${employee + 1}:${day + 1}`)).flat();
  for (const id of m.openIds) {
    if (id !== 34) view.select(id);
    paint("background", "#123456", keys);
    paint("text", "#abcdef", keys.filter((_, index) => index % 2 === 0));
    act(() => {
      for (let day = 1; day <= 25; day++) m.editor!.onCellChange(1, day, day % 2 ? "09:00 - 12:00" : "Training");
      m.editor!.onAddManualColumn();
    });
    act(() => m.editor!.onCellChange(-1, 1, "Manual note"));
  }
  save(); await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(10);
  expect(defaults.replaceGraphSlots).toHaveBeenCalledTimes(10);
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(10);
  expect(defaults.removeGraphEmployee).not.toHaveBeenCalled();
  expect(defaults.upsertGraphCellStyle).not.toHaveBeenCalled();
  for (const id of m.openIds) {
    const persisted = structuredClone(getState(id));
    expect(persisted.employees).toHaveLength(20);
    expect(persisted.styles).toHaveLength(500);
    expect(persisted.styles.every(style => style.backgroundColorArgb === argb("#123456"))).toBe(true);
    expect(persisted.styles.filter(style => style.textColorArgb === argb("#abcdef"))).toHaveLength(250);
    expect(persisted.slots.filter(slot => slot.employeeId === 1)).toHaveLength(13);
    const note = parseGraphNoteContent(persisted.graph.note);
    expect(note.textCells.filter(cell => cell.value === "Training")).toHaveLength(12);
    expect(note.manualColumns[0].cells["1"]).toBe("Manual note");
  }
});

test.each([false, true])("seven-graph incident verifies normalized slots and resumes committed F35 without writes (readback lost=%s)", async lost => {
  m.openIds = [36, 39, 38, 34, 37, 40, 41]; m.id = 36;
  m.employees = Array.from({ length: 17 }, (_, i) => ({ id: i + 1, firstName: "Synthetic", lastName: String(i) }));
  const template = structuredClone(getState(34));
  const privateFixture = process.env.GF3_SAVE_PRIVATE_FIXTURE ? JSON.parse(readFileSync(process.env.GF3_SAVE_PRIVATE_FIXTURE, "utf8")) as {
    graphs: Array<{ graphId: number; before: { slots: GraphSlot[]; employeeOrder: number[] }; after: { slots: GraphSlot[]; employeeOrder: number[] } }>;
  } : undefined;
  if (privateFixture) {
    const allIds = [...new Set(privateFixture.graphs.flatMap(row => row.after.employeeOrder))];
    m.employees = allIds.map(id => ({ id, firstName: "Fixture", lastName: String(id) }));
  }
  for (const id of m.openIds) {
    const fixture = privateFixture?.graphs.find(row => row.graphId === id);
    const count = id === 36 ? 91 : id === 39 ? 201 : 20;
    const slots = fixture?.before.slots ?? Array.from({ length: count }, (_, i) => ({ ...changedSlot(id),
      employeeId: i % 17 + 1, dayOfMonth: Math.floor(i / 17) + 1, slotNo: i % 17 + 3,
    }));
    const roster = fixture?.after.employeeOrder ?? m.employees.map(row => row.id);
    m.states.set(id, { ...structuredClone(template), graph: { ...template.graph, id, name: `Graph ${id}` },
      slots: slots.map((slot, i) => ({ ...slot, id: i + 1, scheduleId: id })),
      employees: roster.map((employeeId, displayOrder) => ({ id: employeeId, employeeId, scheduleId: id, displayOrder, isPriority: false })),
    });
    const version = id === 36 ? 4 : id === 39 ? 20 : 1;
    m.versions.set(id, { currentVersionId: version, versions: [{ id: version }] as GraphVersionTreeDto["versions"] });
  }
  defaults.replaceGraphSlots.mockImplementation(async (_, id, { slots }: { slots: SaveGraphSlotDto[] }) => {
    const groups = new Map<string, SaveGraphSlotDto[]>();
    for (const slot of slots) {
      const key = `${slot.dayOfMonth}/${slot.fromTime}/${slot.toTime}`;
      groups.set(key, [...groups.get(key) ?? [], slot]);
    }
    getState(id).slots = [...groups.values()].flatMap(rows => rows.sort((a, b) => a.slotNo - b.slotNo || (a.employeeId ?? Infinity) - (b.employeeId ?? Infinity))
      .map((slot, i) => ({ ...slot, slotNo: i + 1, status: slot.employeeId ? 1 : 0, id: i + 1, scheduleId: id })));
  });
  const read = defaults.listGraphSlots.getMockImplementation()!;
  let outage = lost;
  defaults.listGraphSlots.mockImplementation(async (_, id) => {
    if (id === 39 && m.versions.get(id)?.currentVersionId === 21 && outage) throw new Error("Readback unavailable");
    return read(_, id);
  });
  const view = open();
  for (const id of m.openIds) {
    if (id !== 36) view.select(id);
    const fixture = privateFixture?.graphs.find(row => row.graphId === id);
    act(() => {
      if (fixture) {
        const map = buildGraphCellMap(fixture.after.slots);
        for (const employee of fixture.after.employeeOrder) for (let day = 1; day <= 30; day++)
          m.editor!.onCellChange(employee, day, map[`${employee}:${day}`] ?? "");
      } else {
        m.editor!.onCellChange(1, 20, "09:00 - 15:00");
        if (id === 36 || id === 39) m.editor!.onCellChange(2, 21, "09:00 - 15:00");
      }
    });
    paint("background", "#123456", [`${getState(id).employees[0].employeeId}:1`]);
  }
  save();
  if (lost) {
    await failure();
    expect(calls(defaults.commitGraphVersion)).toEqual([36, 39]);
    outage = false; retry();
  }
  await expectNavigation();
  expect(calls(defaults.commitGraphVersion)).toEqual(m.openIds);
  expect(calls(defaults.replaceGraphSlots)).toEqual(m.openIds);
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(m.openIds);
  expect(getState(36).slots).toHaveLength(93);
  expect(getState(39).slots).toHaveLength(203);
  expect(m.versions.get(39)?.currentVersionId).toBe(21);
  expect(m.navigate).toHaveBeenCalledWith("/container/7/graphs/41?openGraphIds=36%2C39%2C38%2C34%2C37%2C40%2C41");
});

test.each(["employee", "day", "time", "count", "status", "style"])("a committed %s mismatch remains unsaved and never replays the version", async field => {
  open();
  act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  paint("background", "#123456", ["2:3"]);
  const commit = defaults.commitGraphVersion.getMockImplementation()!;
  defaults.commitGraphVersion.mockImplementationOnce(async (...args) => {
    const result = await commit(...args);
    const state = getState();
    if (field === "employee") state.slots[0].employeeId = 1;
    if (field === "day") state.slots[0].dayOfMonth = 4;
    if (field === "time") state.slots[0].toTime = "13:00";
    if (field === "count") state.slots.push({ ...state.slots[0], id: 99 });
    if (field === "status") state.slots[0].status = 2;
    if (field === "style") state.styles = [];
    return result;
  });
  save(); await failure(); retry(); await failure();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(defaults.replaceGraphSlots).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(m.editor!.cellMap["2:3"]).toBe("09:00 - 12:00");
});

test("server-only renumbering before preflight causes no conflict or redundant slot replacement", async () => {
  open(); renamed("Metadata edit");
  getState().slots = getState().slots.map(slot => ({ ...slot, slotNo: 99 }));
  save(); await expectNavigation();
  expect(defaults.updateGraph).toHaveBeenCalledOnce();
  expect(defaults.replaceGraphSlots).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
});

test("Cancel during preflight closes the same dialog with zero writes and editable drafts", async () => {
  tenGraphs(); const view = open(); paint("text", "#abcdef", ["1:1"]);
  let release!: () => void;
  const read = defaults.graphById.getMockImplementation()!;
  defaults.graphById.mockImplementationOnce(async (...args) => { await new Promise<void>(resolve => { release = resolve; }); return read(...args); });
  save();
  const progress = screen.getByRole("progressbar", { name: "Verified save progress" });
  expect(progress).toHaveAttribute("value", "0");
  await waitFor(() => expect(release).toBeTypeOf("function"));
  fireEvent.click(screen.getByRole("button", { name: "Cancel saving" }));
  expect(screen.getByRole("button", { name: "Cancel saving" })).toBeDisabled();
  await act(async () => release());
  await waitFor(() => expect(m.editor!.isSaving).toBe(false));
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(isGraphSaveTrafficActive(view.client, 7)).toBe(false);
  expect(screen.queryByRole("dialog", { name: /Save schedule/ })).not.toBeInTheDocument();
  paint("background", "#123456", ["1:2"]);
  expect(m.prompt).toHaveBeenLastCalledWith(true);
});

test("Cancel during graph two finishes it and retry writes only graphs three to ten", async () => {
  tenGraphs(); const view = open();
  for (const id of m.openIds) { if (id !== 34) view.select(id); paint("text", "#abcdef", ["1:1"]); }
  let release!: () => void;
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(apply).mockImplementationOnce(async (...args) => {
    await new Promise<void>(resolve => { release = resolve; }); return apply(...args);
  });
  save(); await waitFor(() => expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(2));
  expect(screen.getByRole("progressbar")).toHaveAttribute("value", "11");
  fireEvent.click(screen.getByRole("button", { name: "Cancel saving" }));
  await act(async () => release());
  await waitFor(() => expect(m.editor!.isSaving).toBe(false));
  expect(calls(defaults.commitGraphVersion)).toEqual([34, 36]);
  expect(m.navigate).not.toHaveBeenCalled();
  save(); await expectNavigation();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual(m.openIds);
  expect(calls(defaults.commitGraphVersion)).toEqual(m.openIds);
});

test("new edits after failed readback survive reconciliation of an acknowledged save", async () => {
  open(); paint("text", "#abcdef", ["1:1"]);
  const read = defaults.graphById.getMockImplementation()!;
  defaults.graphById.mockImplementationOnce(read).mockRejectedValueOnce(new Error("Offline"));
  save(); await failure();
  fireEvent.click(screen.getByRole("button", { name: "Return to editor" }));
  paint("background", "#123456", ["1:2"]);
  save(); await expectNavigation();
  expect(getState().styles).toHaveLength(2);
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
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
  expect(screen.getAllByRole("button", { name: "Saving..." }).every(button => button.hasAttribute("disabled"))).toBe(true);
  expect(findRecoveryDialog()).not.toBeInTheDocument();
  await act(async () => vi.advanceTimersByTimeAsync(6000));
  vi.useRealTimers();
  await failure();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(isGraphSaveTrafficActive(view.client, 7)).toBe(false);
  available = true;
  retry();
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
  await failure();
  expect(getState().styles).toHaveLength(1);
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
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
  await failure();
  expect(screen.getByRole("dialog", { name: /Save schedule/ })).toHaveTextContent("Other saved changes were detected. Your edits are still here. Automatic merge is unavailable.");
  expect(calls(defaults.graphById)).toEqual([34, 36]);
  expect(defaults.updateGraph).not.toHaveBeenCalled();
  expect(defaults.applyGraphCellStyleBatch).not.toHaveBeenCalled();
  expect(m.navigate).not.toHaveBeenCalled();
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
  await waitFor(() => expect(m.navigate).toHaveBeenCalledTimes(3));
  expect(screen.queryByText("Nothing to save.")).not.toBeInTheDocument();
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
  await failure();
  expect(defaults.commitGraphVersion).not.toHaveBeenCalled();
  expect(m.navigate).not.toHaveBeenCalled();
  retry();
  await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(persisted ? 1 : 2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(getState().styles).toHaveLength(2);
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
  await failure();
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34, 36]);
  expect(calls(defaults.commitGraphVersion)).toEqual([34]);
  retry();
  await waitFor(() => expect(m.navigate).toHaveBeenCalledOnce());
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([34, 36, 36, 42]);
  expect(calls(defaults.commitGraphVersion)).toEqual([34, 36, 42]);
});

test.each(["graph", "slots", "version"] as const)("keeps the workspace draft after a %s write fails", async stage => {
  open();
  renamed("Changed name");
  act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  const writeFailure = new ApiError({ message: "Injected write error", status: 409 });
  if (stage === "graph") defaults.updateGraph.mockRejectedValueOnce(writeFailure);
  if (stage === "slots") defaults.replaceGraphSlots.mockRejectedValueOnce(writeFailure);
  if (stage === "version") defaults.commitGraphVersion.mockRejectedValueOnce(writeFailure);
  save();
  await failure();
  expect(m.navigate).not.toHaveBeenCalled();
  expect(m.editor!.form.name).toBe("Changed name");
  expect(defaults.listGraphVersions).toHaveBeenCalledTimes(stage === "version" ? 2 : 1);
});

test("an ambiguous version commit is checked and never replayed", async () => {
  open(); paint("text", "#abcdef", ["1:1"]);
  const commit = defaults.commitGraphVersion.getMockImplementation()!;
  defaults.commitGraphVersion.mockImplementationOnce(async (...args) => { await commit(...args); throw new Error("Lost commit response"); });
  save(); await failure();
  expect(defaults.listGraphVersions).toHaveBeenCalledTimes(2);
  retry(); await failure();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(m.navigate).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Return to editor" }));
  expect(m.editor!.isSaving).toBe(false);
  paint("background", "#123456", ["1:2"]);
  expect(m.editor!.styleMap["1:2"]).toBeDefined();
});

test.each([false, true])("a lost metadata PUT resumes only missing resources after readback (applied=%s)", async applied => {
  open(); renamed("Changed name");
  act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  const update = defaults.updateGraph.getMockImplementation()!;
  defaults.updateGraph.mockImplementationOnce(async (...args) => {
    if (applied) await update(...args);
    throw new Error("Lost PUT response");
  });
  save(); await failure(); retry(); await expectNavigation();
  expect(defaults.updateGraph).toHaveBeenCalledTimes(applied ? 1 : 2);
  expect(defaults.replaceGraphSlots).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(getState().graph.name).toBe("Changed name");
  expect(getState().slots[0].fromTime).toBe("09:00");
});

test.each([false, true])("an unknown employee POST is reconciled without blind replay (applied=%s)", async applied => {
  m.employees = [...m.employees, { id: 3, firstName: "New", lastName: "Worker" }];
  open(); act(() => m.editor!.onSelectedEmployeeIdChange(3)); act(() => m.editor!.onAddEmployee());
  defaults.createGraphEmployee.mockImplementationOnce(async (_, id, payload) => {
    if (applied) getState(id).employees = [...getState(id).employees, { ...payload, id: 3, scheduleId: id }];
    throw new Error("Lost employee POST response");
  });
  save(); await failure(); retry();
  if (applied) await expectNavigation();
  else { await failure(); expect(m.navigate).not.toHaveBeenCalled(); }
  expect(defaults.createGraphEmployee).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(applied ? 1 : 0);
});

test("reverting edits after a partial PUT saves the current in-memory values", async () => {
  open(); renamed("Partial name");
  act(() => m.editor!.onCellChange(2, 3, "09:00 - 12:00"));
  const update = defaults.updateGraph.getMockImplementation()!;
  defaults.updateGraph.mockImplementationOnce(async (...args) => { await update(...args); throw new Error("Lost PUT response"); });
  save(); await failure();
  fireEvent.click(screen.getByRole("button", { name: "Return to editor" }));
  renamed("Graph 34"); act(() => m.editor!.onCellChange(2, 3, "08:00 - 12:00"));
  save(); await expectNavigation();
  expect(getState().graph.name).toBe("Graph 34");
  expect(getState().slots[0].fromTime).toBe("08:00");
  expect(defaults.updateGraph).toHaveBeenCalledTimes(2);
  expect(defaults.replaceGraphSlots).not.toHaveBeenCalled();
});

test("new edits after an unversioned acknowledged style create one final version", async () => {
  open(); paint("background", "#123456", ["1:1"]);
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(async (...args) => { await apply(...args); throw new Error("Lost style response"); });
  save(); await failure();
  fireEvent.click(screen.getByRole("button", { name: "Return to editor" }));
  paint("text", "#abcdef", ["1:2"]);
  save(); await expectNavigation();
  expect(getState().styles).toHaveLength(2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledTimes(2);
});

test("a previous version 429 never authorizes replay after a later unknown response", async () => {
  open(); paint("text", "#abcdef", ["1:1", "1:2"]);
  defaults.commitGraphVersion.mockImplementationOnce(async () => {
    getState().styles = getState().styles.filter(row => row.dayOfMonth !== 2);
    throw rateLimited();
  }).mockRejectedValueOnce(new Error("Unknown version response"));
  save(); await failure();
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
  retry(); await failure();
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
  expect(m.navigate).not.toHaveBeenCalled();
});

test("a confirmed commit with an unavailable readback can be verified without replaying writes", async () => {
  open();
  paint("text", "#abcdef", ["1:1"]);
  const read = defaults.graphById.getMockImplementation()!;
  defaults.graphById.mockImplementationOnce(read).mockRejectedValueOnce(new Error("Readback unavailable"));
  save();
  await failure();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  retry();
  await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
});

test("background errors retain editable in-memory changes while required data is unavailable", async () => {
  const view = open();
  paint("text", "#abcdef", ["1:1"]);
  act(() => m.editor!.onCellChange(1, 28, "Training"));
  const { cellMap, styleMap } = m.editor!;
  m.slotsFailed = true; m.urlopFailed = true; view.refresh();
  expect(m.editor!.hasLoadError).toBe(false);
  expect(m.editor!.isSaveBlocked).toBe(true);
  expect(m.editor!.isGenerationBlocked).toBe(true);
  expect(screen.queryByRole("button", { name: "Review issue" })).not.toBeInTheDocument();
  expect(m.editor!.cellMap).toEqual(cellMap); expect(m.editor!.styleMap).toEqual(styleMap);
  m.slotsFailed = false; m.urlopFailed = false; view.refresh();
  save(); await expectNavigation();
});

test("a definite version 429 is reconciled before retry without rewriting colors", async () => {
  open(); paint("text", "#abcdef", ["1:1"]);
  defaults.commitGraphVersion.mockRejectedValueOnce(rateLimited());
  save(); await expectNavigation();
  expect(defaults.applyGraphCellStyleBatch).toHaveBeenCalledOnce();
  expect(defaults.commitGraphVersion).toHaveBeenCalledTimes(2);
  expect(m.versions.get(34)?.versions).toHaveLength(2);
});

test("a lost manual publication response reconciles its unique offer without duplicating it", async () => {
  getState().graph.note = buildGraphNoteContent("", [{ id: 1, label: "Manual", cells: { "2": "08:00 - 12:00" } }]);
  open();
  act(() => m.editor!.onPublishManualShift({ manualColumnId: 1, dayOfMonth: 2, fromTime: "08:00", toTime: "12:00", allowPartialAcceptance: false }));
  m.publish.mockImplementationOnce((variables, callbacks) => {
    getState().slots = [...getState().slots, { ...changedSlot(34), id: 100, employeeId: null, status: 0, dayOfMonth: 2 }];
    m.offers.set(34, [{ ...variables, id: 20, scheduleId: 34, scheduleSlotId: 100, isManagerCreated: true, status: "open" }]);
    callbacks.onError(new Error("Lost offer response"));
  });
  save(); await failure(); retry(); await expectNavigation();
  expect(m.publish).toHaveBeenCalledOnce(); expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
});

test("a definite manual POST 429 resumes only the offer proven absent", async () => {
  getState().graph.note = buildGraphNoteContent("", [{ id: 1, label: "Manual", cells: { "2": "08:00 - 12:00", "4": "08:00 - 12:00" } }]);
  open();
  for (const dayOfMonth of [2, 4]) act(() => m.editor!.onPublishManualShift({
    manualColumnId: 1, dayOfMonth, fromTime: "08:00", toTime: "12:00", allowPartialAcceptance: false,
  }));
  let rejected = false;
  m.publish.mockImplementation((variables, callbacks) => {
    if (variables.dayOfMonth === 4 && !rejected) { rejected = true; callbacks.onError(rateLimited()); return; }
    const id = variables.dayOfMonth + 100;
    getState().slots = [...getState().slots, { ...changedSlot(34), id, employeeId: null, status: 0, dayOfMonth: variables.dayOfMonth }];
    const offer = { ...variables, id, scheduleId: 34, scheduleSlotId: id, isManagerCreated: true, status: "open" };
    m.offers.set(34, [...m.offers.get(34) ?? [], offer]); callbacks.onSuccess(offer);
  });
  save(); await expectNavigation();
  expect(m.publish.mock.calls.map(([row]) => row.dayOfMonth)).toEqual([2, 4, 4]);
  expect(m.offers.get(34)).toHaveLength(2);
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
});

test("a conflicting version after a rejected commit never overwrites or commits again", async () => {
  open(); paint("text", "#abcdef", ["1:1"]);
  defaults.commitGraphVersion.mockRejectedValueOnce(rateLimited());
  const list = defaults.listGraphVersions.getMockImplementation()!;
  defaults.listGraphVersions.mockImplementationOnce(list).mockImplementation(async (_, id) => ({
    ...m.versions.get(id), currentVersionId: 999, versions: [{ id: 1 }, { id: 999 }],
  }));
  save(); await failure(); retry(); await failure();
  expect(defaults.commitGraphVersion).toHaveBeenCalledOnce();
  expect(m.navigate).not.toHaveBeenCalled();
});

test("a fourth-graph failure retains seven editable graphs and never rewrites the first three", async () => {
  tenGraphs(); const view = open();
  for (const id of m.openIds) { if (id !== 34) view.select(id); paint("text", "#abcdef", ["1:1"]); }
  const apply = defaults.applyGraphCellStyleBatch.getMockImplementation()!;
  defaults.applyGraphCellStyleBatch.mockImplementationOnce(apply).mockImplementationOnce(apply)
    .mockImplementationOnce(apply).mockRejectedValueOnce(new Error("Offline"));
  save(); await failure();
  expect(calls(defaults.commitGraphVersion)).toEqual(m.openIds.slice(0, 3));
  fireEvent.click(screen.getByRole("button", { name: "Return to editor" }));
  expect(m.editor!.isSaving).toBe(false);
  save(); await expectNavigation();
  expect(calls(defaults.commitGraphVersion)).toEqual(m.openIds);
  expect(calls(defaults.applyGraphCellStyleBatch)).toEqual([...m.openIds.slice(0, 4), ...m.openIds.slice(3)]);
});

test("legacy manager drafts are removed without hydration across repeated edit entries", async () => {
  const baseline = structuredClone(getState());
  sessionStorage.setItem("gf3:graph-drafts:9:7", JSON.stringify([{ version: 1, graphId: 34,
    persistedBaseline: baseline, comparisonBaseline: { nonStyleFingerprint: "original", styles: [] }, editedAt: Date.now(),
    draft: {
      graphId: 34, form: { ...createGraphFormFromGraph(baseline.graph), name: "Stale" },
      graphEmployeeRows: baseline.employees.map(row => ({ ...row, minHoursMonth: "" })),
      selectedSchedulePresetId: null, selectedEmployeeId: null, cellMap: {}, manualColumns: [],
      pendingManualShiftPublishes: [], scheduleColumnOrder: [1, 2], selectedCellKeys: [], styleRecords: [],
      autoAvailabilityStyleSuppressions: {}, fillColor: "#123456", textColor: "#abcdef",
      syncedAvailabilityGroupId: null, previewAvailabilitySelection: "__schedule-details__",
    },
  }]));
  sessionStorage.setItem("gf3:graph-drafts:9:8", "old");
  sessionStorage.setItem("gf3:graph-drafts:99:7", "other manager");
  sessionStorage.setItem("setting", "keep");
  for (let cycle = 0; cycle < 3; cycle++) {
    const view = open();
    expect(m.editor!.isSaveBlocked).toBe(false);
    expect(screen.getByRole("button", { name: "Save editor" }).closest('[inert]')).toBeNull();
    expect(screen.queryByRole("button", { name: "Restore draft" })).not.toBeInTheDocument();
    expect(sessionStorage.getItem("gf3:graph-drafts:9:7")).toBeNull();
    renamed(`Saved cycle ${cycle}`); save(); await expectNavigation();
    expect(getState().graph.name).toBe(`Saved cycle ${cycle}`);
    view.unmount(); m.navigate.mockClear();
  }
  expect(sessionStorage.getItem("gf3:graph-drafts:9:8")).toBeNull();
  expect(sessionStorage.getItem("gf3:graph-drafts:99:7")).toBe("other manager");
  expect(sessionStorage.getItem("setting")).toBe("keep");
});

test("unavailable storage cannot disable editing or the unsaved changes warning", () => {
  sessionStorage.setItem("gf3:graph-drafts:9:7", "obsolete");
  const storage = vi.spyOn(Storage.prototype, "key").mockImplementation(() => { throw new Error("Unavailable"); });
  open(); renamed("Draft in memory");
  expect(m.prompt).toHaveBeenLastCalledWith(true);
  expect(m.editor!.isSaveBlocked).toBe(false);
  expect(screen.queryByRole("button", { name: "Restore draft" })).not.toBeInTheDocument();
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
