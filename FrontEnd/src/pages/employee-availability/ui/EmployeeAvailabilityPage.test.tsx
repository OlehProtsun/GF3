import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import { AVAILABILITY_KIND_ANY, AVAILABILITY_KIND_INTERVAL, AVAILABILITY_KIND_NONE } from "@entities/availability-groups/model/editor";
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

function getGroupToggle(name = "May Availability") {
  return screen.getByRole("button", { name: new RegExp(`^${name} `) });
}

function getDayButton(dayOfMonth: number) {
  return document.querySelector<HTMLButtonElement>(`[data-own-availability-cell="${dayOfMonth}"]`)!;
}

class PagePointerEvent extends MouseEvent {
  pointerId = 1; isPrimary = true;
}
afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });
async function selectRange(anchor: number, target: number) {
  const viewport = screen.getByRole("region", { name: "Team availability" });
  vi.spyOn(viewport, "getBoundingClientRect").mockReturnValue({ left: 0, right: 300, top: 0, bottom: 400 } as DOMRect);
  Object.defineProperty(document, "elementFromPoint", { configurable: true, value: () => document.querySelector(`[data-availability-day="${target}"]`) });
  fireEvent.pointerDown(getDayButton(anchor), { button: 0, clientX: 100, clientY: 100 });
  await act(async () => { await new Promise(resolve => setTimeout(resolve, 360)); });
  fireEvent.pointerMove(window, { clientX: 100, clientY: 200 });
  fireEvent.pointerUp(window);
}

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

describe("EmployeeAvailabilityPage", () => {
  function submittedGroup(kind = AVAILABILITY_KIND_NONE, id = 5): EmployeeAvailabilityGroup {
    return { ...openAvailability, id, name: id === 5 ? "May Availability" : "Another group", employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z",
      slots: Array.from({ length: 31 }, (_, i) => ({ id: i + 1, availabilityGroupMemberId: 50, dayOfMonth: i + 1, kind, intervalStr: null })) };
  }

  test("estimate uses only saved own values and appears between the legend and actions in its expanded card", async () => {
    const user = userEvent.setup();
    const saved = submittedGroup();
    mocks.availabilityQuery.mockReturnValue({ data: [saved, submittedGroup(AVAILABILITY_KIND_ANY, 6)], isLoading: false });
    renderPage();
    const card = getGroupToggle().closest("section")!;
    const summary = within(card).getByRole("region", { name: "Estimated working hours" });
    expect(summary).toHaveTextContent("0 h");
    expect(summary).toHaveTextContent("No available working hours in your saved availability.");
    expect(summary.previousElementSibling).toHaveTextContent("Other time");
    expect(summary.nextElementSibling).toContainElement(screen.getByRole("button", { name: "Save availability" }));
    await user.click(getDayButton(1));
    await user.click(screen.getByRole("radio", { name: /^Available/ }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Save", exact: true }));
    expect(summary).toHaveTextContent("0 h");
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    expect(summary).toHaveTextContent("0 h");
    await user.click(getGroupToggle("Another group"));
    await user.click(screen.getByRole("button", { name: "Leave page" }));
    expect(screen.getAllByRole("region", { name: "Estimated working hours" })).toHaveLength(1);
    expect(within(getGroupToggle("Another group").closest("section")!).getByRole("region", { name: "Estimated working hours" })).toHaveTextContent("~150–250 h");
  });

  test("a full month without a submission timestamp stays hidden, then a successful save exposes its saved estimate", async () => {
    const user = userEvent.setup();
    const unsubmitted = { ...submittedGroup(), employeeLastModifiedAtUtc: null };
    mocks.availabilityQuery.mockReturnValue({ data: [unsubmitted], isLoading: false });
    const view = renderPage();
    expect(screen.queryByText("Estimated working hours")).not.toBeInTheDocument();
    mocks.mutate.mockImplementation((_request, options) => {
      const saved = submittedGroup(AVAILABILITY_KIND_ANY);
      mocks.availabilityQuery.mockReturnValue({ data: [saved], isLoading: false });
      options.onSuccess(saved);
    });
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    expect(screen.getByRole("region", { name: "Estimated working hours" })).toHaveTextContent("~150–250 h");
    view.unmount();
    renderPage();
    expect(screen.getByRole("region", { name: "Estimated working hours" })).toHaveTextContent("~150–250 h");
  });

  test("failed saving retains the previous estimate and incomplete or unsupported saved data has no numeric estimate", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [submittedGroup(AVAILABILITY_KIND_ANY)], isLoading: false });
    const view = renderPage();
    await user.click(screen.getByRole("button", { name: "Clear", exact: true }));
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    mocks.mutationState.error = new Error("Offline");
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    expect(screen.getByRole("region", { name: "Estimated working hours" })).toHaveTextContent("~150–250 h");
    for (const group of [{ ...submittedGroup(), slots: [] }, { ...submittedGroup(AVAILABILITY_KIND_INTERVAL), slots: submittedGroup(AVAILABILITY_KIND_INTERVAL).slots.map(slot => ({ ...slot, intervalStr: "morning" })) }]) {
      mocks.availabilityQuery.mockReturnValue({ data: [group], isLoading: false });
      view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
      expect(screen.getByRole("region", { name: "Estimated working hours" })).toHaveTextContent("Estimate unavailable");
      expect(screen.queryByText(/~\d+–\d+ h/)).toBeNull();
    }
  });
  test("all cards start collapsed with real names, dates and statuses; no selector or matrix is exposed", () => {
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability, { ...openAvailability, id: 6, name: "June", month: 6, canSubmit: false }, { ...openAvailability, id: 7, name: "Locked", isEditLocked: true }], isLoading: false });
    renderPage(false);
    expect(screen.getByRole("heading", { name: "Public windows", level: 1 })).toBeInTheDocument();
    for (const name of ["May Availability", "June", "Locked"]) expect(getGroupToggle(name)).toHaveAttribute("aria-expanded", "false");
    expect(within(getGroupToggle()).getByText("May 2026")).toBeInTheDocument();
    expect(screen.getByText("June 2026")).toBeInTheDocument();
    expect(screen.getByText("Open")).toBeInTheDocument();
    expect(screen.getByText("Closed")).toBeInTheDocument();
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
    expect(screen.queryByText("Team availability")).not.toBeInTheDocument();
  });

  test("expand, keyboard collapse and switching keep one full editor in its own card above other cards", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability, { ...openAvailability, id: 6, name: "June", month: 6 }], isLoading: false });
    const view = renderPage(false);
    await user.click(getGroupToggle("June"));
    const juneCard = getGroupToggle("June").closest("section")!;
    expect(within(juneCard).getByRole("table")).toBeInTheDocument();
    expect(within(juneCard).getByText("Visible")).toBeInTheDocument();
    expect(within(juneCard).getByText("Closes")).toBeInTheDocument();
    expect(within(juneCard).getByText("Team availability")).toBeInTheDocument();
    expect(within(juneCard).getByRole("button", { name: "Save availability" })).toBeInTheDocument();
    expect(within(juneCard).getByRole("button", { name: "Clear", exact: true })).toBeInTheDocument();
    expect(view.container.querySelector("section")).toBe(juneCard);
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "false");
    await user.click(getGroupToggle());
    expect(screen.getAllByRole("table")).toHaveLength(1);
    expect(getGroupToggle("June")).toHaveAttribute("aria-expanded", "false");
    expect(view.container.querySelector("section")).toBe(getGroupToggle().closest("section"));
    getGroupToggle().focus(); await user.keyboard("{Enter}");
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save availability" })).not.toBeInTheDocument();
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  test("dirty collapse requires approval, cancelling retains edits and confirming discards only after approval", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage();
    await user.click(screen.getByRole("button", { name: "Clear", exact: true }));
    await user.click(getGroupToggle());
    expect(screen.getByRole("dialog", { name: "Leave without saving?" })).toBeInTheDocument();
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "true");
    await user.click(screen.getByRole("button", { name: "Stay here" }));
    expect(getDayButton(2)).toHaveTextContent("−");
    await user.click(getGroupToggle());
    await user.click(screen.getByRole("button", { name: "Leave page" }));
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
    await user.click(getGroupToggle());
    expect(getDayButton(2)).toHaveTextContent("+");
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  test("query reordering preserves the expanded group by ID", async () => {
    const user = userEvent.setup();
    const june = { ...openAvailability, id: 6, name: "June", month: 6 };
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability, june], isLoading: false });
    const view = renderPage(false);
    await user.click(getGroupToggle("June"));
    mocks.availabilityQuery.mockReturnValue({ data: [june, openAvailability], isLoading: false });
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    expect(getGroupToggle("June")).toHaveAttribute("aria-expanded", "true");
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "false");
    expect(screen.getAllByRole("row")).toHaveLength(31);
  });

  test.each([[1, "Not available"], [2, "Available (whole day)"]])("opening day %s selects %s and hides Other time", async (day, label) => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(Number(day)));
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getAllByRole("radio")).toHaveLength(3);
    expect(within(dialog).getByRole("radio", { name: new RegExp(`^${label}`.replace("(", "\\(").replace(")", "\\)")) })).toHaveAttribute("aria-checked", "true");
    expect(within(dialog).queryByRole("region", { name: "Other time" })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: /Clear/ })).not.toBeInTheDocument();
  });

  test("Custom time alone reveals time controls; switching hides them and Save applies only the chosen semantic value", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog");
    await user.click(within(dialog).getByRole("radio", { name: /^Custom time/ }));
    expect(within(dialog).getByRole("region", { name: "Other time" })).toBeInTheDocument();
    const custom = within(dialog).getByRole("region", { name: "Other time" });
    expect(within(custom).getAllByRole("button", { name: /^\d{2}:\d{2} – / })).toHaveLength(3);
    await user.click(within(custom).getByRole("button", { name: "15:00 – 21:00" }));
    expect(within(custom).getByLabelText("From")).toHaveValue("15:00");
    expect(within(custom).getByLabelText("To")).toHaveValue("21:00");
    expect(mocks.mutate).not.toHaveBeenCalled();
    await user.click(within(dialog).getByRole("radio", { name: /^Available \(whole day\)/ }));
    expect(within(dialog).queryByRole("region", { name: "Other time" })).not.toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Save", exact: true }));
    expect(getDayButton(1)).toHaveTextContent("+");
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    expect(mocks.mutate.mock.calls[0][0].payload.slots[0]).toEqual({ dayOfMonth: 1, kind: 0, intervalStr: null });
  });

  test.each(["08:30 - 16:30", "training"])("restores existing custom value %s and Cancel never applies dialog edits", async value => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [{ ...openAvailability, slots: [{ id: 1, availabilityGroupMemberId: 50, dayOfMonth: 1, kind: 2, intervalStr: value }] }], isLoading: false });
    renderPage(); await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByRole("radio", { name: /^Custom time/ })).toHaveAttribute("aria-checked", "true");
    expect(within(dialog).getByRole("region", { name: "Other time" })).toBeInTheDocument();
    if (value === "training") expect(within(dialog).getByRole("textbox", { name: "Custom" })).toHaveValue(value);
    else expect(within(dialog).getByLabelText("From")).toHaveValue("08:30");
    await user.click(within(dialog).getByRole("radio", { name: /^Not available/ }));
    await user.click(within(dialog).getByRole("button", { name: "Cancel", exact: true }));
    expect(getDayButton(1)).toHaveTextContent(value);
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  test("validates custom intervals and preserves supported custom text editing", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog");
    await user.click(within(dialog).getByRole("radio", { name: /^Custom time/ }));
    fireEvent.change(within(dialog).getByLabelText("From"), { target: { value: "20:00" } });
    expect(within(dialog).getByRole("button", { name: "Save", exact: true })).toBeDisabled();
    fireEvent.change(within(dialog).getByLabelText("To"), { target: { value: "21:00" } });
    expect(within(dialog).getByRole("button", { name: "Save", exact: true })).toBeEnabled();
    await user.click(within(dialog).getByRole("button", { name: "Use custom value" }));
    const input = within(dialog).getByRole("textbox", { name: "Custom" });
    await user.clear(input); await user.type(input, "25:00 - 26:00");
    expect(within(dialog).getByRole("button", { name: "Save", exact: true })).toBeDisabled();
    await user.clear(input); await user.type(input, "training");
    await user.click(within(dialog).getByRole("button", { name: "Save", exact: true }));
    expect(getDayButton(1)).toHaveTextContent("training");
  });

  test("edits, adds and removes server presets without changing the selected day; reload restores the server collection", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    const view = renderPage(); await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog");
    await user.click(within(dialog).getByRole("radio", { name: /^Custom time/ }));
    await user.click(within(dialog).getByRole("button", { name: "Edit presets" }));
    await user.click(within(dialog).getByRole("button", { name: "Edit preset 09:00 - 15:00" }));
    let form = within(dialog).getByRole("form", { name: "Edit preset" });
    fireEvent.change(within(form).getByLabelText("From"), { target: { value: "21:00" } });
    expect(within(form).getByRole("button", { name: "Save preset" })).toBeDisabled();
    fireEvent.change(within(form).getByLabelText("From"), { target: { value: "08:30" } });
    fireEvent.change(within(form).getByLabelText("To"), { target: { value: "16:30" } });
    await user.click(within(form).getByRole("button", { name: "Save preset" }));
    expect(within(dialog).getByRole("button", { name: "Edit preset 08:30 - 16:30" })).toBeInTheDocument();
    expect(within(dialog).getByLabelText("From")).toHaveValue("09:00");
    expect(getDayButton(1)).toHaveTextContent("−");
    await user.click(within(dialog).getByRole("button", { name: "Add preset" }));
    form = within(dialog).getByRole("form", { name: "Edit preset" });
    fireEvent.change(within(form).getByLabelText("From"), { target: { value: "10:00" } });
    fireEvent.change(within(form).getByLabelText("To"), { target: { value: "18:00" } });
    await user.click(within(form).getByRole("button", { name: "Save preset" }));
    await user.click(within(dialog).getByRole("button", { name: "Remove preset 10:00 - 18:00" }));
    expect(mocks.presetRecords[12].presets).toEqual(["08:30 - 16:30", "15:00 - 21:00", "09:00 - 21:00"]);
    expect(mocks.mutate).not.toHaveBeenCalled();
    view.unmount(); window.localStorage.clear(); renderPage(); await user.click(getDayButton(3));
    await user.click(screen.getByRole("radio", { name: /^Custom time/ }));
    await user.click(screen.getByRole("button", { name: "08:30 – 16:30" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Save", exact: true }));
    expect(getDayButton(3)).toHaveTextContent("08:30 - 16:30");
  });

  test("server presets win over stale local data and stay separate between employees", async () => {
    const user = userEvent.setup();
    mocks.presetRecords[12] = { presets: ["08:00 - 16:00", "15:00 - 21:00", "09:00 - 21:00"] };
    window.localStorage.setItem("gf3:employee-availability:time-presets:12", JSON.stringify(["07:00 - 15:00", "15:00 - 21:00", "09:00 - 21:00"]));
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    const view = renderPage(); await user.click(getDayButton(1)); await user.click(screen.getByRole("radio", { name: /^Custom time/ }));
    expect(screen.getByRole("button", { name: "08:00 – 16:00" })).toBeInTheDocument();
    expect(mocks.savePresets).not.toHaveBeenCalled();
    mocks.employeeId = 13; view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    await user.click(screen.getByRole("radio", { name: /^Custom time/ }));
    expect(screen.queryByRole("button", { name: "08:00 – 16:00" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "09:00 – 15:00" })).toBeInTheDocument();
  });

  test("imports valid legacy local presets only when the database has no preference", async () => {
    const user = userEvent.setup();
    const legacy = ["08:00 - 16:00", "15:00 - 21:00", "09:00 - 21:00", "12:00 - 18:00"];
    window.localStorage.setItem("gf3:employee-availability:time-presets:12", JSON.stringify(legacy));
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(1));
    expect(mocks.savePresets).toHaveBeenCalledWith(12, { presets: legacy, onlyIfUnset: true }, expect.anything());
    expect(mocks.presetRecords[12].presets).toEqual(legacy);
  });

  test("server failure keeps preset edits open; unavailable localStorage does not prevent a successful database save", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(1)); await user.click(screen.getByRole("radio", { name: /^Custom time/ }));
    await user.click(screen.getByRole("button", { name: "Edit presets" }));
    await user.click(screen.getByRole("button", { name: "Edit preset 09:00 - 15:00" }));
    mocks.savePresets.mockImplementationOnce((_employee, _variables, callbacks) => callbacks.onError(new Error("offline")));
    await user.click(screen.getByRole("button", { name: "Save preset" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Could not save this preset.");
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("blocked"); });
    await user.click(screen.getByRole("button", { name: "Save preset" }));
    expect(screen.queryByRole("form", { name: "Edit preset" })).not.toBeInTheDocument();
    expect(mocks.presetRecords[12].presets).toHaveLength(3);
  });

  test.each(["{invalid", JSON.stringify(["bad", "15:00 - 21:00", "09:00 - 21:00"])])("invalid local data falls back to three default presets: %s", async stored => {
    const user = userEvent.setup();
    window.localStorage.setItem("gf3:employee-availability:time-presets:12", stored);
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage(); await user.click(getDayButton(1)); await user.click(screen.getByRole("radio", { name: /^Custom time/ }));
    expect(screen.getByRole("button", { name: "09:00 – 15:00" })).toBeInTheDocument();
    expect(mocks.savePresets).not.toHaveBeenCalled();
  });

  test("edits a day value and saves a full month payload", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({
      data: [openAvailability],
      isLoading: false,
      error: null,
    });
    mocks.mutate.mockImplementation((
      _payload: {
        id: number;
        payload: { slots: Array<{ dayOfMonth: number; kind: number; intervalStr: string | null }> };
      },
      options?: { onSuccess?: (availability: EmployeeAvailabilityGroup) => void },
    ) => {
      options?.onSuccess?.({
        ...openAvailability,
        slots: _payload.payload.slots.map((slot: { dayOfMonth: number; kind: number; intervalStr: string | null }) => ({
          id: slot.dayOfMonth,
          availabilityGroupMemberId: 50,
          ...slot,
        })),
      });
    });

    renderPage();

    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog", { name: "Day 1 availability" });

    await user.click(within(dialog).getByRole("radio", { name: /^Custom time/ }));
    await user.click(within(dialog).getByRole("button", { name: "15:00 – 21:00" }));
    await user.click(within(dialog).getByRole("button", { name: "Save" }));
    await user.click(screen.getByRole("button", { name: "Save availability" }));

    expect(mocks.mutate).toHaveBeenCalledTimes(1);
    const [payload] = mocks.mutate.mock.calls[0];

    expect(payload.id).toBe(5);
    expect(payload.payload.slots).toHaveLength(31);
    expect(payload.payload.slots[0]).toEqual({
      dayOfMonth: 1,
      kind: AVAILABILITY_KIND_INTERVAL,
      intervalStr: "15:00 - 21:00",
    });
    expect(payload.payload.slots[1]).toEqual({
      dayOfMonth: 2,
      kind: AVAILABILITY_KIND_ANY,
      intervalStr: null,
    });
    expect(payload.payload.slots[30]).toEqual({
      dayOfMonth: 31,
      kind: AVAILABILITY_KIND_NONE,
      intervalStr: null,
    });
    expect(await screen.findByText("Availability saved successfully.")).toBeInTheDocument();
  });

  test("blocks edits when the manager lock is active", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({
      data: [
        {
          ...openAvailability,
          isEditLocked: true,
          editLockedBy: "Manager Anna",
        },
      ],
      isLoading: false,
      error: null,
    });

    renderPage();

    expect(await screen.findByText("This availability is currently being edited by Manager Anna. You cannot edit it right now."))
      .toBeInTheDocument();

    await user.click(getDayButton(1));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  test("shows the published-window empty state", () => {
    mocks.availabilityQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });

    renderPage();

    expect(screen.getByText("Nothing is public for your account yet.")).toBeInTheDocument();
    expect(screen.getByText("When a manager publishes an availability window for you, it will show up here.")).toBeInTheDocument();
  });
  test.each([2, 5])("shows the correct month table (%s), self first and colleagues read only", month => {
    mocks.availabilityQuery.mockReturnValue({ data: [{ ...openAvailability, month }], isLoading: false });
    renderPage();
    expect(screen.getAllByRole("row")).toHaveLength(month === 2 ? 29 : 32);
    expect(screen.getAllByRole("columnheader").map(node => node.getAttribute("aria-label") ?? node.textContent)).toEqual(["Day", "Worker Bee (You)", "Anna First", "Bob Last"]);
    expect(screen.getByText("10:00 - 14:00")).toBeInTheDocument();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
  });

  test("batch selection applies once to own draft; mixed values have no preselected choice and Cancel is safe", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    renderPage();
    await selectRange(1, 3);
    let dialog = screen.getByRole("dialog", { name: "3 days selected" });
    expect(within(dialog).queryByRole("radio", { checked: true })).not.toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Save", exact: true })).toBeDisabled();
    await user.click(within(dialog).getByRole("radio", { name: /^Custom time/ }));
    await user.click(within(dialog).getByRole("button", { name: "09:00 – 15:00", exact: true }));
    await user.click(within(dialog).getByRole("button", { name: "Save", exact: true }));
    expect(mocks.mutate).not.toHaveBeenCalled();
    for (const day of [1, 2, 3]) expect(getDayButton(day)).toHaveTextContent("09:00 - 15:00");
    expect(getDayButton(4)).toHaveTextContent("−");
    expect(screen.getByText("10:00 - 14:00")).toBeInTheDocument();
    await selectRange(3, 1);
    dialog = screen.getByRole("dialog");
    await user.click(within(dialog).getByRole("radio", { name: /^Not available/ }));
    await user.click(within(dialog).getByRole("button", { name: "Cancel", exact: true }));
    expect(getDayButton(1)).toHaveTextContent("09:00 - 15:00");
    await selectRange(1, 3);
    await user.click(within(screen.getByRole("dialog")).getByRole("radio", { name: /^Not available/ }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Save", exact: true }));
    for (const day of [1, 2, 3]) expect(getDayButton(day)).toHaveTextContent("−");
  });

  test("teammate refresh retains unsaved own draft and save uses normalized server slots", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    const view = renderPage();
    await user.click(getDayButton(1));
    await user.click(screen.getByRole("radio", { name: /^Available \(whole day\)/ }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Save", exact: true }));
    const refreshed = { ...openAvailability, members: openAvailability.members!.map(member => member.employeeId === 13 ? { ...member, slots: [{ id: 11, availabilityGroupMemberId: 51, dayOfMonth: 1, kind: 2, intervalStr: "11:00 - 16:00" }] } : member) };
    mocks.availabilityQuery.mockReturnValue({ data: [refreshed], isLoading: false });
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    expect(getDayButton(1)).toHaveTextContent("+");
    expect(screen.getByText("11:00 - 16:00")).toBeInTheDocument();
    mocks.mutate.mockImplementation((_request, options) => options.onSuccess({ ...refreshed, slots: [] }));
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    expect(getDayButton(1)).toHaveTextContent("−");
    const request = mocks.mutate.mock.calls[0][0];
    expect(request.payload.slots).toHaveLength(31);
    expect(Object.keys(request.payload)).toEqual(["slots"]);
    expect(request.payload.slots.every((slot: object) => !Object.hasOwn(slot, "employeeId"))).toBe(true);
  });

  test("dirty group switch prompts and Clear affects only self", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability, { ...openAvailability, id: 6, name: "June", month: 6 }], isLoading: false });
    renderPage();
    await user.click(screen.getByRole("button", { name: "Clear", exact: true }));
    expect(getDayButton(2)).toHaveTextContent("−");
    expect(screen.getByText("10:00 - 14:00")).toBeInTheDocument();
    await user.click(getGroupToggle("June"));
    expect(screen.getByRole("dialog", { name: "Leave without saving?" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Stay here" }));
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "true");
    await user.click(getGroupToggle("June"));
    await user.click(screen.getByRole("button", { name: "Leave page" }));
    expect(getGroupToggle("June")).toHaveAttribute("aria-expanded", "true");
    expect(getGroupToggle()).toHaveAttribute("aria-expanded", "false");
    expect(screen.getAllByRole("row")).toHaveLength(31);
  });

  test.each(["closed", "locked", "pending", "missing self"])("%s keeps team visible and all edit entry points disabled", state => {
    mocks.availabilityQuery.mockReturnValue({ data: [{ ...openAvailability, canSubmit: state !== "closed", isEditLocked: state === "locked", members: state === "missing self" ? openAvailability.members!.filter(member => member.employeeId !== 12) : openAvailability.members }], isLoading: false });
    const view = renderPage();
    if (state === "pending") {
      mocks.mutationState.isPending = true;
      view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    }
    expect(screen.getByText("10:00 - 14:00")).toBeInTheDocument();
    for (const button of document.querySelectorAll("[data-own-availability-cell]")) expect(button).toBeDisabled();
    expect(screen.getByRole("button", { name: state === "pending" ? "Saving..." : "Save availability" })).toBeDisabled();
  });

  test("loading, fetch failure and older missing members remain safe", () => {
    mocks.availabilityQuery.mockReturnValue({ data: [], isLoading: true, error: new Error("Load failed") });
    const view = renderPage();
    expect(screen.getByText("Checking public availability windows.")).toBeInTheDocument();
    expect(screen.getByText("Load failed")).toBeInTheDocument();
    mocks.availabilityQuery.mockReturnValue({ data: [{ ...openAvailability, members: undefined }], isLoading: false });
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    fireEvent.click(getGroupToggle());
    expect(screen.getByText("Team availability could not be shown.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save availability" })).toBeDisabled();
  });

  test("save failure retains draft and sheet traps and restores focus", async () => {
    const user = userEvent.setup();
    mocks.availabilityQuery.mockReturnValue({ data: [openAvailability], isLoading: false });
    const view = renderPage();
    await user.click(getDayButton(1));
    const dialog = screen.getByRole("dialog");
    expect(document.body.style.overflow).toBe("hidden");
    const first = within(dialog).getByRole("button", { name: "Close" });
    expect(first).toHaveFocus();
    await user.tab({ shift: true });
    expect(within(dialog).getByRole("button", { name: "Save", exact: true })).toHaveFocus();
    await user.click(within(dialog).getByRole("radio", { name: /^Available \(whole day\)/ }));
    await user.click(within(dialog).getByRole("button", { name: "Save", exact: true }));
    expect(getDayButton(1)).toHaveFocus();
    expect(document.body.style.overflow).toBe("");
    await user.click(screen.getByRole("button", { name: "Save availability" }));
    mocks.mutationState.error = new Error("Save failed");
    view.rerender(<BrowserRouter><EmployeeAvailabilityPage /></BrowserRouter>);
    expect(screen.getByText("Save failed")).toBeInTheDocument();
    expect(getDayButton(1)).toHaveTextContent("+");
    expect(screen.queryByText("Availability saved successfully.")).not.toBeInTheDocument();
  });

});
