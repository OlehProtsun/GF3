import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { EmployeeAvailabilityMatrix } from "./EmployeeAvailabilityMatrix";
import { buildDraftFromAvailability } from "../model/employeeAvailabilityMatrix";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import styles from "./EmployeeAvailabilityPage.module.css";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PublicHolidayVisibilityProvider } from "@app/providers/PublicHolidayVisibilityProvider";
import { publicHolidaySettingsApi } from "@entities/public-holidays/api/publicHolidaySettingsApi";
class TestPointerEvent extends MouseEvent {
  pointerId: number; isPrimary: boolean;
  constructor(type: string, options: PointerEventInit = {}) { super(type, options); this.pointerId = options.pointerId ?? 1; this.isPrimary = options.isPrimary ?? true; }
}
const group: EmployeeAvailabilityGroup = { id: 5, year: 2026, month: 5, name: "May", canSubmit: true, isEditLocked: false, slots: [], members: [
  { employeeId: 3, firstName: "Zoe", lastName: "Bee", displayOrder: 0, slots: [{ id: 1, availabilityGroupMemberId: 2, dayOfMonth: 1, kind: 2, intervalStr: "10:00 - 14:00" }] },
  { employeeId: 12, firstName: "Own", lastName: "Person", displayOrder: 4, slots: [] },
  { employeeId: 2, firstName: "Anna", lastName: "Bee", displayOrder: 2, slots: [] },
] };
function setup(canEdit = true, g = group) {
  const select = vi.fn(); const edit = vi.fn();
  const props = { group: g, currentEmployeeId: 12, ownDraft: buildDraftFromAvailability(g), canEdit, selectedDays: [], onSelectedDaysChange: select, onRequestEdit: edit };
  const view = render(<EmployeeAvailabilityMatrix {...props} />);
  const cell = (day: number) => view.container.querySelector<HTMLButtonElement>(`[data-own-availability-cell="${day}"]`)!;
  return { ...view, props, select, edit, cell };
}
beforeEach(() => { setLanguage("en"); vi.stubGlobal("PointerEvent", TestPointerEvent); vi.useFakeTimers(); });
afterEach(() => { vi.useRealTimers(); vi.unstubAllGlobals(); vi.restoreAllMocks(); });
describe("EmployeeAvailabilityMatrix", () => {
  test("OFF removes holiday labels and titles but keeps weekends and editing", async () => {
    vi.spyOn(publicHolidaySettingsApi, "get").mockResolvedValue({ showPublicHolidays: false });
    const edit = vi.fn();
    let view!: ReturnType<typeof render>;
    await act(async () => { view = render(<QueryClientProvider client={new QueryClient()}><PublicHolidayVisibilityProvider enabled>
      <EmployeeAvailabilityMatrix group={group} currentEmployeeId={12} ownDraft={{}} canEdit selectedDays={[]}
        onSelectedDaysChange={vi.fn()} onRequestEdit={edit} />
    </PublicHolidayVisibilityProvider></QueryClientProvider>); });
    const date = view.container.querySelector('[data-availability-day="1"] th');
    expect(date).not.toHaveClass(styles.holidayDay); expect(date).not.toHaveAttribute("title");
    expect(screen.queryByText("Holiday")).toBeNull();
    expect(view.container.querySelector('[data-availability-day="3"]')).toHaveClass(styles.weekend);
    fireEvent.click(view.container.querySelector('[data-own-availability-cell="1"]')!);
    expect(edit).toHaveBeenCalledWith([1]);
  });
  test.each([
    ["en", "Holiday", "Labour Day", "Constitution Day"],
    ["pl", "Święto", "Święto Pracy", "Święto Konstytucji 3 Maja"],
    ["uk", "Свято", "День праці", "День Конституції 3 травня"],
  ] as const)("localizes holiday day cells in %s without changing employee cells or ordinary weekends", (language, label, labour, constitution) => {
    setLanguage(language);
    const v = setup();
    const dayCell = (day: number) => v.container.querySelector(`[data-availability-day="${day}"] th`)!;
    expect(dayCell(1)).toHaveClass(styles.holidayDay);
    expect(dayCell(1)).toHaveAttribute("title", labour);
    expect(within(dayCell(1) as HTMLElement).getByText(label)).toBeVisible();
    expect(dayCell(3)).toHaveClass(styles.holidayDay);
    expect(dayCell(3)).toHaveAttribute("title", constitution);
    expect(dayCell(3).parentElement).toHaveClass(styles.weekend);
    for (const day of [2, 4, 10]) {
      expect(dayCell(day)).not.toHaveClass(styles.holidayDay);
      expect(dayCell(day)).not.toHaveAttribute("title");
      expect(within(dayCell(day) as HTMLElement).queryByText(label)).toBeNull();
    }
    expect(dayCell(10).parentElement).toHaveClass(styles.weekend);
    expect(v.container.querySelector("td[class*='holiday']")).toBeNull();
    fireEvent.click(v.cell(1));
    expect(v.edit).toHaveBeenCalledExactlyOnceWith([1]);
    v.rerender(<EmployeeAvailabilityMatrix {...v.props} group={{ ...group, month: 6 }} />);
    expect(dayCell(1)).not.toHaveClass(styles.holidayDay);
    expect(dayCell(4)).toHaveClass(styles.holidayDay);
  });
  test("renders dates, own first, all teammate values and real month lengths", () => {
    const view = setup();
    expect(screen.getAllByRole("row")).toHaveLength(32);
    expect(screen.getAllByRole("columnheader").map(node => node.getAttribute("aria-label") ?? node.textContent)).toEqual(["Day", "Own Person (You)", "Zoe Bee", "Anna Bee"]);
    expect(within(screen.getAllByRole("row")[1]).getByText("10:00 - 14:00")).toBeInTheDocument();
    expect(screen.getAllByRole("button")).toHaveLength(31);
    view.rerender(<EmployeeAvailabilityMatrix {...view.props} group={{ ...group, month: 2 }} />);
    expect(screen.getAllByRole("row")).toHaveLength(29);
  });
  test("short tap is one edit and movement over 8px cancels pending tap", () => {
    const v = setup();
    fireEvent.pointerDown(v.cell(1), { button: 0, clientX: 10, clientY: 10 });
    act(() => vi.advanceTimersByTime(349));
    expect(v.select).not.toHaveBeenCalled();
    fireEvent.pointerUp(window, {}); fireEvent.click(v.cell(1), { detail: 1 });
    expect(v.edit).toHaveBeenCalledExactlyOnceWith([1]);
    v.edit.mockClear();
    fireEvent.pointerDown(v.cell(2), { button: 0, clientX: 10, clientY: 10 });
    fireEvent.pointerMove(window, { clientX: 19, clientY: 10 });
    act(() => vi.advanceTimersByTime(400));
    fireEvent.pointerUp(window, {}); fireEvent.click(v.cell(2), { detail: 1 });
    expect(v.edit).not.toHaveBeenCalled();
  });
  test.each([[2, 5, [2, 3, 4, 5]], [5, 2, [2, 3, 4, 5]], [3, 3, [3]]])("hold/drag %s to %s selects contiguous own days once", (anchor, target, expected) => {
    const v = setup();
    const viewport = screen.getByRole("region");
    vi.spyOn(viewport, "getBoundingClientRect").mockReturnValue({ left: 0, right: 300, top: 0, bottom: 400 } as DOMRect);
    Object.defineProperty(document, "elementFromPoint", { configurable: true, value: vi.fn(() => v.container.querySelector(`[data-availability-day="${target}"]`)) });
    fireEvent.pointerDown(v.cell(anchor), { button: 0, clientX: 100, clientY: 100 });
    act(() => vi.advanceTimersByTime(350));
    expect(v.select).toHaveBeenLastCalledWith([anchor]);
    fireEvent.pointerMove(window, { clientX: 100, clientY: 200 });
    expect(v.select).toHaveBeenLastCalledWith(expected);
    fireEvent.pointerUp(window, {}); fireEvent.click(v.cell(anchor), { detail: 1 });
    expect(v.edit).toHaveBeenCalledExactlyOnceWith(expected);
  });
  test.each(["cancel", "Escape", "disable", "unmount"])("cleans active selection on %s without editing", reason => {
    const v = setup();
    fireEvent.pointerDown(v.cell(1), { button: 0 }); act(() => vi.advanceTimersByTime(350));
    if (reason === "cancel") fireEvent.pointerCancel(window, {});
    if (reason === "Escape") fireEvent.keyDown(window, { key: "Escape" });
    if (reason === "disable") v.rerender(<EmployeeAvailabilityMatrix {...v.props} canEdit={false} />);
    if (reason === "unmount") v.unmount();
    fireEvent.pointerUp(window, {});
    expect(v.edit).not.toHaveBeenCalled();
  });
  test("teammates, secondary button and nonprimary touches cannot select", () => {
    const v = setup();
    fireEvent.pointerDown(screen.getByText("10:00 - 14:00"), { button: 0 });
    fireEvent.pointerDown(v.cell(1), { button: 2 });
    fireEvent.pointerDown(v.cell(1), { button: 0, isPrimary: false });
    act(() => vi.advanceTimersByTime(400)); fireEvent.pointerUp(window, {});
    expect(v.edit).not.toHaveBeenCalled(); expect(v.select).not.toHaveBeenCalled();
  });
  test("missing team is safe and missing self leaves colleagues read only", () => {
    const v = setup(true, { ...group, members: undefined });
    expect(screen.getByRole("status")).toHaveTextContent("Team availability could not be shown.");
    v.rerender(<EmployeeAvailabilityMatrix {...v.props} group={{ ...group, members: group.members?.filter(m => m.employeeId !== 12) }} />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });
  test("auto-scrolls only the bounded matrix, updates range and stops at bounds and release", () => {
    const v = setup(); const viewport = screen.getByRole("region");
    vi.spyOn(viewport, "getBoundingClientRect").mockReturnValue({ left: 0, right: 300, top: 0, bottom: 200 } as DOMRect);
    Object.defineProperties(viewport, { clientHeight: { value: 200 }, scrollHeight: { value: 600 } });
    Object.defineProperty(document, "elementFromPoint", { configurable: true, value: () => v.container.querySelector(`[data-availability-day="${Math.min(31, 3 + Math.floor(viewport.scrollTop / 20))}"]`) });
    fireEvent.pointerDown(v.cell(2), { button: 0, clientX: 100, clientY: 100 });
    act(() => vi.advanceTimersByTime(350));
    fireEvent.pointerMove(window, { clientX: 100, clientY: 199 });
    act(() => vi.advanceTimersByTime(2000));
    expect(viewport.scrollTop).toBe(400);
    expect(v.select.mock.calls.at(-1)?.[0].length).toBeGreaterThan(2);
    expect(window.scrollY).toBe(0);
    fireEvent.pointerUp(window, {});
    const top = viewport.scrollTop;
    act(() => vi.advanceTimersByTime(500));
    expect(viewport.scrollTop).toBe(top);
    expect(v.edit).toHaveBeenCalledTimes(1);
  });

});
