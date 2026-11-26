import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ComponentProps } from "react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { createInitialGraphForm } from "../model/graphForm";
import { buildGraphTotals, type GraphMatrixColumn } from "../model/graphWorkspace";
import type { Graph } from "../model/types";
import { ContainerGraphEditor } from "./ContainerGraphEditor";

const mocks = vi.hoisted(() => ({ remove: vi.fn(), removeManual: vi.fn(), add: vi.fn() }));
vi.mock("@entities/shift-corrections", () => ({ useGraphShiftCorrectionsQuery: () => ({ data: [] }) }));
vi.mock("@entities/shift-swap-approvals", () => ({ useGraphContractApprovalsQuery: () => ({ data: [] }) }));
vi.mock("@entities/containers/api/queries", async importOriginal => ({
  ...await importOriginal<typeof import("@entities/containers/api/queries")>(),
  useGraphManualShiftHighlightSettingQuery: () => ({}),
  useGraphShiftSwapHighlightSettingQuery: () => ({}),
}));

const graph: Graph = { id: 1, containerId: 1, shopId: 1, name: "First graph", year: 2026, month: 11,
  note: "", publicationStatus: "private", allowSwap: true, peoplePerShift: 1,
  shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00", maxHoursPerEmpMonth: 160,
  maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
const worker: GraphMatrixColumn = { kind: "employee", employeeId: 2, graphEmployeeId: 1, manualColumnId: null,
  label: "Worker", minHoursMonth: null, totalMinutes: 0, totalText: "" };
const manual: GraphMatrixColumn = { ...worker, kind: "manual", employeeId: -3, graphEmployeeId: null, manualColumnId: 3, label: "Manual demand" };
type Props = ComponentProps<typeof ContainerGraphEditor>;
const noop = () => undefined;
const asyncNoop = async () => undefined;
const base: Props = {
  graph, form: createInitialGraphForm(), formErrors: {}, shops: [], availabilityGroups: [], employees: [], schedulePresets: [],
  selectedSchedulePresetId: null, graphEmployeeRows: [], binds: [], manualColumns: [{ columnId: 3, label: "Manual demand", cells: {} }],
  selectedEmployeeId: null, selectedBindClientId: null, scheduleColumns: [worker, manual], cellMap: {}, cellErrors: {}, styleMap: {},
  dayConflictMap: {}, totals: buildGraphTotals([], []), previewColumns: [{ employeeId: 4, memberId: 1, label: "Preview worker" }],
  previewCellMap: {}, previewYear: 2026, previewMonth: 11, previewAvailabilitySelection: "", previewAvailabilityOptions: [],
  bindValueByKey: new Map(), valueBindKeys: new Set(), fillColorBinds: [], fillColorByKey: new Map(),
  textColorBinds: [], textColorByKey: new Map(), selectedCellKeys: [], previewSelectedCellKeys: [],
  fillColor: "#FFFFFF", textColor: "#000000", isLoading: false, hasLoadError: false, isSaving: false, isGenerating: false,
  isStylingBusy: false, isBindsLoading: false, isBindBusy: false, isFillColorBindBusy: false,
  isTextColorBindBusy: false, isSchedulePresetsLoading: false, canUndo: false,
  onFieldChange: () => noop, onSelectedEmployeeIdChange: noop, onPreviewAvailabilitySelectionChange: noop,
  onSelectedBindChange: noop, onApplySchedulePreset: noop, onCreateSchedulePreset: asyncNoop, onBindFieldChange: noop,
  onBindCommit: noop, onAddEmployee: noop, onRemoveEmployee: mocks.remove, onAddBind: noop, onDeleteBind: noop,
  onManualColumnLabelChange: noop, onAddManualColumn: mocks.add, onDeleteManualColumn: mocks.removeManual,
  onPublishManualShift: noop, onCancelManualShift: noop, onRefreshManualShift: noop, onCancelPendingManualShift: noop,
  onEmployeePriorityChange: noop, onEmployeeMinHoursChange: noop, onColumnMove: noop, onCellChange: noop,
  onSelectedCellKeysChange: noop, onPreviewSelectedCellKeysChange: noop, onFillColorChange: noop, onTextColorChange: noop,
  onApplyFillColor: noop, onApplyFillColorShortcut: noop, onBindFillColor: asyncNoop, onDeleteFillColorBind: asyncNoop,
  onApplyTextColor: noop, onApplyTextColorShortcut: noop, onBindTextColor: asyncNoop, onDeleteTextColorBind: asyncNoop,
  onClearCellStyle: noop, onClearAllCellStyles: noop, onUndo: noop, onVersionCheckoutComplete: noop, onSave: noop, onGenerate: noop,
};

beforeEach(() => {
  setLanguage("en");
  vi.clearAllMocks();
  vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true, addEventListener: vi.fn(), removeEventListener: vi.fn() })));
});
afterEach(() => vi.unstubAllGlobals());

test("the editable matrix delegates column actions and closes stale menus", async () => {
  const client = new QueryClient();
  const element = (props: Partial<Props> = {}) => <QueryClientProvider client={client}>
    <ContainerGraphEditor {...base} {...props} />
  </QueryClientProvider>;
  const view = render(element());
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Actions for Worker" }));
  await user.click(screen.getByRole("menuitem", { name: "Remove employee from schedule" }));
  expect(mocks.remove).toHaveBeenCalledExactlyOnceWith(2);
  await user.click(screen.getByRole("button", { name: "Actions for manual column Manual demand" }));
  await user.click(screen.getByRole("menuitem", { name: "Delete manual column" }));
  expect(mocks.removeManual).toHaveBeenCalledExactlyOnceWith(3);
  await user.click(screen.getByRole("button", { name: "Actions for Worker" }));
  view.rerender(element({ scheduleColumns: [manual] }));
  expect(screen.queryByRole("menu")).toBeNull();
  await user.click(screen.getByRole("button", { name: "Actions for manual column Manual demand" }));
  view.rerender(element({ graph: { ...graph, id: 2 }, manualShiftPublishError: "Cancel open manual offers first." }));
  expect(screen.queryByRole("menu")).toBeNull();
  expect(screen.getByText("Cancel open manual offers first.")).toBeVisible();
  const preview = screen.getByRole("columnheader", { name: "Preview worker" }).closest("table")!;
  expect(within(preview).queryByRole("button", { name: /Actions for|Add manual column/ })).toBeNull();
  expect(screen.getAllByRole("button", { name: /Add manual column/ })).toHaveLength(2);
}, 15_000);
