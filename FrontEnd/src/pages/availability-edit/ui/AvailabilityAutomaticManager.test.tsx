import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import type { ComponentProps } from "react";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { Employee } from "@entities/employees/model/types";
import type { AvailabilityGroupEditor } from "@entities/availability-groups/ui/AvailabilityGroupEditor";
import { setLanguage } from "@shared/i18n";
import { AvailabilityEditPage } from "./AvailabilityEditPage";
import { AvailabilityProfilePage } from "@pages/availability-profile/ui/AvailabilityProfilePage";
import { ManagerPhoneAvailabilityDetailPage } from "@pages/manager-phone/ui/ManagerPhoneAvailabilityDetailPage";

type EditorProps = ComponentProps<typeof AvailabilityGroupEditor>;
const mocks = vi.hoisted(() => ({
  id: "4" as string | undefined,
  employees: [] as Employee[],
  employeesError: false,
  settings: { data: { automaticWeekdayAvailability: true } as { automaticWeekdayAvailability: boolean } | undefined, isError: false, isLoading: false, refetch: vi.fn() },
  group: { id: 4, name: "December", year: 2026, month: 12, publicationStatus: "private" },
  members: [{ id: 10, employeeId: 1, displayOrder: 0 }, { id: 20, employeeId: 2, displayOrder: 1 }],
  slots: [{ id: 1, availabilityGroupMemberId: 10, dayOfMonth: 1, kind: 2, intervalStr: "09:00 - 15:00" },
    { id: 2, availabilityGroupMemberId: 10, dayOfMonth: 5, kind: 1, intervalStr: null },
    { id: 3, availabilityGroupMemberId: 20, dayOfMonth: 1, kind: 1, intervalStr: null }],
  empty: [], save: vi.fn(), deleteGroup: vi.fn(), editor: null as EditorProps | null,
}));
vi.mock("react-router-dom", async original => ({ ...await original<object>(), useParams: () => ({ availabilityId: mocks.id }) }));
vi.mock("@entities/employment-contracts", () => ({ useEmploymentContractSettingsQuery: () => mocks.settings }));
vi.mock("@entities/employees/api/queries", () => ({ useEmployeesListQuery: () => ({ data: mocks.employees, isError: mocks.employeesError, isLoading: false }) }));
vi.mock("@app/providers/PresenceProvider", () => ({ managerEditResourceTypes: {}, useManagerEditLocks: () => ({ lockedByOtherState: null, isCheckingLocks: false }) }));
vi.mock("@shared/lib/useUnsavedChangesPrompt", () => ({ useUnsavedChangesPrompt: () => ({ dialog: null, runWithoutPrompt: (callback: () => void) => callback() }) }));
vi.mock("@entities/availability-groups", async original => ({ ...await original<object>(),
  useAvailabilityGroupByIdQuery: (id: number | null) => ({ data: id ? mocks.group : undefined }),
  useAvailabilityGroupItemsQuery: () => ({ data: undefined }),
  useAvailabilityGroupMembersQuery: () => ({ data: mocks.members }),
  useAvailabilityGroupSlotsQuery: () => ({ data: mocks.slots }),
  useAvailabilityTransferHintsQuery: () => ({ data: mocks.empty }),
  useAvailabilityTransferPreviewQuery: () => ({ data: mocks.empty }),
  useSaveAvailabilityGroupGraphMutation: () => ({ mutate: mocks.save, isPending: false }),
  useDeleteAvailabilityGroupMutation: () => ({ mutate: mocks.deleteGroup, isPending: false }),
}));
vi.mock("@entities/availability-binds", async original => ({ ...await original<object>(),
  useAvailabilityBindsListQuery: () => ({ data: mocks.empty }),
}));
vi.mock("@entities/availability-groups/ui", async original => {
  const actual = await original<typeof import("@entities/availability-groups/ui")>();
  return { ...actual, AvailabilityGroupEditor: (props: EditorProps) => { mocks.editor = props; return <actual.AvailabilityGroupEditor {...props} />; } };
});

beforeEach(() => {
  setLanguage("en"); mocks.id = "4"; mocks.save.mockReset(); mocks.editor = null;
  mocks.deleteGroup.mockReset(); mocks.employeesError = false;
  window.history.replaceState({}, "", "/availability/4");
  mocks.employees = [
    { id: 1, firstName: "Ada", lastName: "Worker", contractType: "o_prace", hasLoginAccount: false, isOnline: false },
    { id: 2, firstName: "Grace", lastName: "Worker", contractType: "zlecenie", hasLoginAccount: false, isOnline: false },
  ];
  mocks.settings.data = { automaticWeekdayAvailability: true }; mocks.settings.isError = false;
});
function mount(element: React.ReactNode) {
  return render(<QueryClientProvider client={new QueryClient()}><BrowserRouter>{element}</BrowserRouter></QueryClientProvider>);
}
function profileCell(container: HTMLElement, day: number, employeeIndex: number) {
  return container.querySelectorAll("tbody tr")[day - 1].querySelectorAll("td")[employeeIndex];
}

test.each([AvailabilityProfilePage, ManagerPhoneAvailabilityDetailPage])("%s shows the derived calendar and restores saved intervals on toggle", Page => {
  const view = mount(<Page />);
  expect(profileCell(view.container, 1, 0)).toHaveTextContent("+");
  expect(profileCell(view.container, 5, 0)).toHaveTextContent("-");
  expect(profileCell(view.container, 7, 0)).toHaveTextContent("+");
  expect(profileCell(view.container, 1, 1)).toHaveTextContent("-");
  mocks.settings.data = { automaticWeekdayAvailability: false };
  view.rerender(<QueryClientProvider client={new QueryClient()}><BrowserRouter><Page /></BrowserRouter></QueryClientProvider>);
  expect(profileCell(view.container, 1, 0)).toHaveTextContent("09:00 - 15:00");
});

test("editor locks automatic cells and transfer handlers, edits ordinary employees and saves raw values", () => {
  const view = mount(<AvailabilityEditPage />);
  expect(screen.queryByRole("textbox", { name: "Ada Worker day 1", exact: true })).toBeNull();
  const auto = screen.getByRole("button", { name: "Ada Worker day 1", exact: true });
  expect(auto).toHaveTextContent("+");
  for (const key of ["Delete", "Backspace", "F2", "x"]) fireEvent.keyDown(auto, { key });
  fireEvent.doubleClick(auto);
  fireEvent.paste(auto, { clipboardData: { getData: () => "pasted" } });
  act(() => mocks.editor!.onCellChange(1, 1, "blocked"));
  act(() => mocks.editor!.onChooseFromAnother(1));
  expect(screen.queryByRole("dialog")).toBeNull();
  expect(mocks.editor!.assignedEmployees[0].canChooseFromAnother).toBe(false);
  expect(mocks.editor!.assignedEmployees[0].label).toContain("read-only");
  fireEvent.change(screen.getByRole("textbox", { name: "Grace Worker day 1", exact: true }), { target: { value: "+" } });
  act(() => mocks.editor!.onSave());
  const saved = mocks.save.mock.calls[0][0];
  expect(saved.cellMap).toEqual({ "1:1": "09:00 - 15:00", "1:5": "-", "2:1": "+" });
  expect(saved.transfers).toEqual([]);
  mocks.settings.data = { automaticWeekdayAvailability: false };
  view.rerender(<QueryClientProvider client={new QueryClient()}><BrowserRouter><AvailabilityEditPage /></BrowserRouter></QueryClientProvider>);
  expect(screen.getByRole("textbox", { name: "Ada Worker day 1", exact: true })).toHaveValue("09:00 - 15:00");
});

test("newly assigned automatic employee appears before save and month changes recompute locked days", () => {
  mocks.id = undefined; mount(<AvailabilityEditPage />);
  act(() => mocks.editor!.onSelectedEmployeeIdChange(1)); act(() => mocks.editor!.onAddEmployee());
  expect(mocks.editor!.cellMap["1:1"]).toBeDefined();
  act(() => mocks.editor!.onYearChange(2028)); act(() => mocks.editor!.onMonthChange(2));
  expect(mocks.editor!.readOnlyCellKeys?.size).toBe(29);
  expect(mocks.editor!.cellMap["1:29"]).toBe("+");
  expect(mocks.editor!.cellMap["1:30"]).toBeUndefined();
  act(() => mocks.editor!.onNameChange("New group")); act(() => mocks.editor!.onSave());
  expect(mocks.save.mock.calls[0][0].cellMap).toEqual({});
});

test.each([AvailabilityProfilePage, ManagerPhoneAvailabilityDetailPage, AvailabilityEditPage])("%s hides matrix until policy resolves, exposes retry on failure", Page => {
  mocks.settings.data = undefined;
  const view = mount(<Page />);
  expect(view.container.querySelector("table")).toBeNull();
  mocks.settings.isError = true;
  view.rerender(<QueryClientProvider client={new QueryClient()}><BrowserRouter><Page /></BrowserRouter></QueryClientProvider>);
  expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
  expect(within(view.container).queryByRole("table")).toBeNull();
});

test.each([AvailabilityProfilePage, ManagerPhoneAvailabilityDetailPage])("%s renders archived members and saved days without automatic defaults", Page => {
  mocks.employees = mocks.employees.filter(employee => employee.id !== 1);
  mocks.employees[0].contractType = "o_prace";
  const view = mount(<Page />);
  expect(screen.getByRole("table")).toBeInTheDocument();
  expect(screen.getByRole("columnheader", { name: /^Employee #1/ })).toBeInTheDocument();
  expect(screen.queryByText("Could not load this availability group.")).toBeNull();
  expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
  expect(profileCell(view.container, 1, 0)).toHaveTextContent("09:00 - 15:00");
  expect(profileCell(view.container, 5, 0)).toHaveTextContent("-");
  expect(profileCell(view.container, 7, 0)).not.toHaveTextContent("+");
  expect(profileCell(view.container, 1, 1)).toHaveTextContent("+");
  expect(profileCell(view.container, 5, 1)).toHaveTextContent("-");
  if (Page === ManagerPhoneAvailabilityDetailPage) {
    expect(screen.queryByRole("button", { name: "Edit", exact: true })).toBeNull();
    expect(screen.queryByRole("button", { name: "Delete", exact: true })).toBeNull();
  } else {
    fireEvent.click(screen.getByRole("button", { name: "Edit", exact: true }));
    expect(window.location.pathname).toBe("/availability/4/edit");
  }
});

test.each([false, true])("profile confirms deletion with archived members (all archived: %s)", allArchived => {
  mocks.employees = allArchived ? [] : mocks.employees.filter(employee => employee.id !== 1);
  mocks.deleteGroup.mockImplementation((_id, callbacks) => callbacks.onSuccess());
  mount(<AvailabilityProfilePage />);
  expect(screen.getByRole("columnheader", { name: /^Employee #1/ })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Delete", exact: true }));
  const dialog = screen.getByRole("dialog", { name: "Delete availability group" });
  expect(dialog).toHaveTextContent("This action cannot be undone.");
  expect(mocks.deleteGroup).not.toHaveBeenCalled();
  fireEvent.click(within(dialog).getByRole("button", { name: "Delete", exact: true }));
  expect(mocks.deleteGroup).toHaveBeenCalledExactlyOnceWith(4, expect.objectContaining({ onSuccess: expect.any(Function) }));
  expect(window.location.pathname).toBe("/availability");
});

test("editor preserves archived members on save and removes only the confirmed member", () => {
  mocks.employees = mocks.employees.filter(employee => employee.id !== 1);
  const view = mount(<AvailabilityEditPage />);
  expect(screen.getByRole("textbox", { name: "Employee #1 day 1", exact: true })).toHaveValue("09:00 - 15:00");
  expect(mocks.editor!.assignedEmployees).toEqual([
    expect.objectContaining({ id: 1, label: "Employee #1" }),
    expect.objectContaining({ id: 2, label: "Grace Worker" }),
  ]);
  expect(mocks.editor!.employees.map(employee => employee.id)).toEqual([2]);
  expect(mocks.editor!.readOnlyCellKeys?.has("1:7")).toBe(false);
  expect(mocks.editor!.cellMap["1:7"]).toBeUndefined();
  act(() => mocks.editor!.onSave());
  expect(mocks.save.mock.calls[0][0]).toEqual(expect.objectContaining({
    employeeIds: [1, 2], existingMembers: mocks.members, existingSlots: mocks.slots,
    cellMap: { "1:1": "09:00 - 15:00", "1:5": "-", "2:1": "-" },
  }));
  act(() => mocks.editor!.onRemoveEmployee(1));
  const dialog = screen.getByRole("dialog", { name: "Remove employee" });
  expect(dialog).toHaveTextContent("Employee #1");
  expect(mocks.editor!.assignedEmployees.map(employee => employee.id)).toEqual([1, 2]);
  fireEvent.click(within(dialog).getByRole("button", { name: "Remove", exact: true }));
  expect(mocks.editor!.assignedEmployees.map(employee => employee.id)).toEqual([2]);
  expect(view.container.querySelector("table")).toBeInTheDocument();
  act(() => mocks.editor!.onSave());
  expect(mocks.save.mock.calls[1][0]).toEqual(expect.objectContaining({
    employeeIds: [2], existingMembers: mocks.members, existingSlots: mocks.slots,
    cellMap: { "2:1": "-" },
  }));
});

test.each([AvailabilityProfilePage, ManagerPhoneAvailabilityDetailPage, AvailabilityEditPage])("%s retains the error path for failed employee queries", Page => {
  mocks.employeesError = true;
  const view = mount(<Page />);
  expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
  expect(view.container.querySelector("table")).toBeNull();
  if (Page === AvailabilityEditPage) {
    act(() => mocks.editor!.onSave());
    expect(mocks.save).not.toHaveBeenCalled();
  }
});
