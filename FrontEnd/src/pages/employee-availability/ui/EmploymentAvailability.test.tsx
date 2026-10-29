import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import { AVAILABILITY_KIND_ANY, AVAILABILITY_KIND_NONE } from "@entities/availability-groups/model/editor";
import { EmployeeAvailabilityPage } from "./EmployeeAvailabilityPage";

beforeEnglishTest(() => setTestLanguage("en"));

const mocks = vi.hoisted(() => ({
  availabilityQuery: vi.fn(),
  mutate: vi.fn(),
  mutationState: {
    isPending: false,
    error: null as Error | null,
  },
  employeeId: 12,
  presetRecords: {} as Record<number, { presets: string[] | null }>,
  savePresets: vi.fn(),
  presetError: null as Error | null,
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ session: { employeeId: mocks.employeeId } }),
}));

vi.mock("@entities/employee-availability", () => ({
  useEmployeeAvailabilityListQuery: () => mocks.availabilityQuery(),
  useSaveEmployeeAvailabilityMutation: () => ({
    mutate: mocks.mutate,
    isPending: mocks.mutationState.isPending,
    error: mocks.mutationState.error,
  }),
}));

vi.mock("@entities/employee-ui-state", () => ({
  useEmployeeAvailabilityTimePresetsQuery: (employeeId: number) => ({
    data: mocks.presetRecords[employeeId] ?? (mocks.presetRecords[employeeId] = { presets: null }),
    isFetching: false, error: mocks.presetError,
  }),
  useSaveEmployeeAvailabilityTimePresetsMutation: (employeeId: number) => ({
    isPending: false,
    mutate: (variables: unknown, callbacks: unknown) => mocks.savePresets(employeeId, variables, callbacks),
  }),
}));

const openAvailability: EmployeeAvailabilityGroup = {
  id: 5,
  name: "May Availability",
  year: 2026,
  month: 5,
  visibleFromUtc: "2026-05-01T08:00:00.000Z",
  visibleToUtc: "2026-05-20T18:00:00.000Z",
  canSubmit: true,
  isEditLocked: false,
  members: [
    { employeeId: 12, firstName: "Worker", lastName: "Bee", displayOrder: 4, slots: [] },
    { employeeId: 13, firstName: "Anna", lastName: "First", displayOrder: 1, slots: [{ id: 11, availabilityGroupMemberId: 51, dayOfMonth: 1, kind: 2, intervalStr: "10:00 - 14:00" }] },
    { employeeId: 14, firstName: "Bob", lastName: "Last", displayOrder: 2, slots: [] },
  ],
  slots: [
    {
      id: 1,
      availabilityGroupMemberId: 50,
      dayOfMonth: 1,
      kind: AVAILABILITY_KIND_NONE,
      intervalStr: null,
    },
    {
      id: 2,
      availabilityGroupMemberId: 50,
      dayOfMonth: 2,
      kind: AVAILABILITY_KIND_ANY,
      intervalStr: null,
    },
  ],
};

function renderPage(expandFirst = true) {
  window.history.replaceState({}, "", "/availability");

  const view = render(
    <BrowserRouter>
      <EmployeeAvailabilityPage />
    </BrowserRouter>,
  );
  const toggle = view.container.querySelector<HTMLButtonElement>("button[aria-controls^='employee-availability-content-']");
  if (expandFirst && toggle && !toggle.disabled) fireEvent.click(toggle);
  return view;
}

function getDayButton(dayOfMonth: number) {
  return document.querySelector<HTMLButtonElement>(`[data-own-availability-cell="${dayOfMonth}"]`)!;
}

class PagePointerEvent extends MouseEvent {
  pointerId = 1; isPrimary = true;
}
afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });
beforeEach(() => {
  vi.stubGlobal("PointerEvent", PagePointerEvent);
  window.localStorage.clear();
  mocks.employeeId = 12;
  mocks.presetRecords = {};
  mocks.presetError = null;
  mocks.savePresets.mockReset();
  mocks.savePresets.mockImplementation((employeeId, variables, callbacks) => {
    const result = { presets: variables.onlyIfUnset && mocks.presetRecords[employeeId]?.presets ? mocks.presetRecords[employeeId].presets : variables.presets };
    mocks.presetRecords[employeeId] = result;
    callbacks?.onSuccess?.(result);
  });
  mocks.availabilityQuery.mockReset();
  mocks.mutate.mockReset();
  mocks.mutationState.isPending = false;
  mocks.mutationState.error = null;
});

test.each(["pl", "uk"] as const)("automatic calendar is read only in %s", language => {
  setTestLanguage(language);
  mocks.availabilityQuery.mockReturnValue({ data: [{ ...openAvailability, isAutomatic: true, canSubmit: false }], isLoading: false, error: null });
  renderPage();
  expect(document.body.textContent).toContain(language === "pl" ? "Dostępność ustawiona automatycznie" : "Доступність установлена автоматично");
  fireEvent.click(getDayButton(1));
  expect(mocks.mutate).not.toHaveBeenCalled();
  expect(screen.queryByRole("button", { name: language === "pl" ? "Zapisz" : "Зберегти" })).not.toBeInTheDocument();
});
