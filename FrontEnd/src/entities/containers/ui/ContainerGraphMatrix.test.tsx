import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage, t } from "@shared/i18n";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import type { GraphRelatedScheduleHintDetail } from "@entities/containers/model/graphWorkspace";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import styles from "./ContainerGraphMatrix.module.css";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PublicHolidayVisibilityProvider } from "@app/providers/PublicHolidayVisibilityProvider";
import { publicHolidaySettingsApi } from "@entities/public-holidays/api/publicHolidaySettingsApi";

beforeEnglishTest(() => setTestLanguage("en"));

describe("compact manual demand", () => {
  const raw = "09:00 - 15:00, 15:00 - 21:00, 09:00 - 15:00";
  const column = { employeeId: -1, kind: "manual" as const, manualColumnId: 1, graphEmployeeId: null, label: "Demand", minHoursMonth: null, totalMinutes: 0, totalText: "" };
  const rect = { left: 50, right: 100, top: 50, bottom: 78, width: 50, height: 28, x: 50, y: 50, toJSON: () => ({}) };
  const prepare = () => {
    const more = screen.getByRole("button", { name: "More: Shifts" });
    vi.spyOn(more, "getBoundingClientRect").mockReturnValue(rect);
    return more;
  };
  test("portal preserves duplicate demand, hover bridge, outside, Escape and scrolling", () => {
    vi.useFakeTimers();
    try {
      const { unmount } = render(<ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[column]} cellMap={{ "-1:1": raw }} readOnly />);
      const more = prepare();
      expect(more.closest("td")).toHaveTextContent("09:00 - 15:00, More");
      expect(more.closest("td")).not.toHaveTextContent("15:00 - 21:00");
      fireEvent.pointerEnter(more, { pointerType: "mouse" });
      act(() => vi.advanceTimersByTime(159));
      expect(screen.queryByRole("tooltip")).toBeNull();
      act(() => vi.advanceTimersByTime(1));
      const tooltip = screen.getByRole("tooltip");
      expect(tooltip.parentElement).toBe(document.body);
      expect(within(tooltip).getAllByText(/\d\d:\d\d/).map(node => node.textContent)).toEqual(["09:00 - 15:00", "15:00 - 21:00"]);
      fireEvent.pointerLeave(more, { pointerType: "mouse" });
      fireEvent.pointerEnter(tooltip, { pointerType: "mouse" });
      fireEvent.scroll(tooltip);
      act(() => vi.advanceTimersByTime(150));
      expect(tooltip).toBeInTheDocument();
      fireEvent.pointerLeave(tooltip, { pointerType: "mouse" });
      act(() => vi.advanceTimersByTime(150));
      expect(screen.queryByRole("tooltip")).toBeNull();
      fireEvent.click(more); fireEvent.keyDown(more, { key: "Escape" });
      expect(screen.queryByRole("tooltip")).toBeNull();
      fireEvent.click(more); fireEvent.pointerDown(document.body);
      expect(screen.queryByRole("tooltip")).toBeNull();
      fireEvent.click(more); fireEvent.scroll(window);
      expect(screen.queryByRole("tooltip")).toBeNull();
      fireEvent.click(more); fireEvent.resize(window);
      expect(screen.queryByRole("tooltip")).toBeNull();
      fireEvent.click(more); unmount();
      expect(screen.queryByRole("tooltip")).toBeNull();
    } finally { vi.useRealTimers(); }
  });
  const adjacentValues = ["09:00 - 15:00, 15:00 - 21:00", "10:00 - 14:00, 16:00 - 20:00", "11:00 - 15:00, 17:00 - 21:00"];
  const adjacentProps = { graph: { year: 2026, month: 5 }, columns: [column],
    cellMap: Object.fromEntries(adjacentValues.map((value, index) => [`-1:${index + 1}`, value])), readOnly: true };
  const prepareAdjacent = () => screen.getAllByRole("button", { name: "More: Shifts" }).map(more => {
    vi.spyOn(more, "getBoundingClientRect").mockReturnValue(rect);
    return more;
  });
  const enter = (element: HTMLElement) => fireEvent.pointerEnter(element, { pointerType: "mouse" });
  const leave = (element: HTMLElement) => fireEvent.pointerLeave(element, { pointerType: "mouse" });
  const advance = (ms: number) => act(() => vi.advanceTimersByTime(ms));
  const expectActive = (text?: string) => {
    const popups = screen.queryAllByRole("tooltip");
    expect(popups).toHaveLength(text ? 1 : 0);
    if (text) expect(popups[0]).toHaveTextContent(text);
  };
  test("rapid adjacent traversal cancels stale opens and late closes cannot hide another cell", () => {
    vi.useFakeTimers();
    try {
      render(<ContainerGraphMatrix {...adjacentProps} />);
      const [a, b, c] = prepareAdjacent();
      enter(a); advance(50); expectActive(); leave(a);
      enter(b); advance(50); expectActive(); leave(b);
      enter(c); advance(159); expectActive(); advance(1); expectActive("17:00 - 21:00");
      fireEvent.click(a); expectActive("15:00 - 21:00");
      enter(b); advance(100); leave(a); advance(60); expectActive("16:00 - 20:00");
      advance(90); expectActive("16:00 - 20:00");
      enter(c); advance(50); leave(c); enter(b); advance(200); expectActive("16:00 - 20:00");
    } finally { vi.useRealTimers(); }
  });
  test("click overrides another pending hover, toggles and promotes hover without stacking", () => {
    vi.useFakeTimers();
    try {
      render(<ContainerGraphMatrix {...adjacentProps} />);
      const [a, b] = prepareAdjacent();
      enter(a); advance(50); fireEvent.click(b); expectActive("16:00 - 20:00");
      advance(200); expectActive("16:00 - 20:00");
      fireEvent.click(b); expectActive();
      enter(b); advance(160); expectActive("16:00 - 20:00");
      fireEvent.click(b); expectActive("16:00 - 20:00");
      fireEvent.click(b); expectActive();
    } finally { vi.useRealTimers(); }
  });
  test.each(["source", "inline", "edit", "hint", "unmount"])("pending hover cannot reopen after %s change", change => {
    vi.useFakeTimers();
    try {
      const { rerender, unmount } = render(<ContainerGraphMatrix {...adjacentProps} readOnly={change !== "edit"} />);
      const [a] = prepareAdjacent(); enter(a); advance(80);
      if (change === "source") rerender(<ContainerGraphMatrix {...adjacentProps} cellMap={{ "-1:1": raw }} />);
      if (change === "inline") rerender(<ContainerGraphMatrix {...adjacentProps} readOnly={false} editMode="inline" />);
      if (change === "edit") fireEvent.doubleClick(screen.getByRole("button", { name: "Demand day 1" }));
      if (change === "hint") rerender(<ContainerGraphMatrix {...adjacentProps} visualHintMap={{ "-1:1": "Related" }} />);
      if (change === "unmount") unmount();
      advance(300); expectActive();
    } finally { vi.useRealTimers(); }
  });
  test("touch and pen do not arm hover; matrix scrolling dismisses and disconnected anchors cannot open", () => {
    vi.useFakeTimers();
    try {
      render(<ContainerGraphMatrix {...adjacentProps} />);
      const [a] = prepareAdjacent();
      for (const pointerType of ["touch", "pen"]) {
        fireEvent.pointerEnter(a, { pointerType }); advance(200); expectActive();
      }
      fireEvent.click(a); expectActive("15:00 - 21:00");
      fireEvent.scroll(a.closest("table")!.parentElement!); expectActive();
      vi.spyOn(a, "getBoundingClientRect").mockReturnValue({ ...rect, width: 0 });
      enter(a); advance(160); expectActive();
      vi.spyOn(a, "isConnected", "get").mockReturnValue(false);
      fireEvent.click(a); expectActive();
    } finally { vi.useRealTimers(); }
  });
  test("preview ownership is independent between matrices", () => {
    render(<><ContainerGraphMatrix {...adjacentProps} /><ContainerGraphMatrix {...adjacentProps} /></>);
    const buttons = prepareAdjacent();
    fireEvent.click(buttons[0]);
    // Native outside pointerdown dismisses normally; a synthetic click tests independent ownership.
    fireEvent.click(buttons[3]);
    expect(screen.getAllByRole("tooltip")).toHaveLength(2);
  });
  test.each([
    { width: 1000, height: 700, left: 50, top: 100, count: 2, expectedLeft: 108, expectedTop: 91, maxHeight: 320 },
    { width: 1000, height: 700, left: 900, top: 100, count: 2, expectedLeft: 672, expectedTop: 91, maxHeight: 320 },
    { width: 320, height: 700, left: 130, top: 20, count: 2, expectedLeft: 92, expectedTop: 56, maxHeight: 320 },
    { width: 320, height: 700, left: 130, top: 640, count: 2, expectedLeft: 92, expectedTop: 586, maxHeight: 320 },
    { width: 320, height: 400, left: 130, top: 170, count: 30, expectedLeft: 92, expectedTop: 206, maxHeight: 186 },
    { width: 10, height: 10, left: 1, top: 1, count: 30, expectedLeft: 8, expectedTop: 8, maxHeight: 1 },
  ])("adaptive placement $width/$left/$top with $count intervals", data => {
    vi.stubGlobal("innerWidth", data.width); vi.stubGlobal("innerHeight", data.height);
    try {
      render(<ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[column]} readOnly
        cellMap={{ "-1:1": Array(data.count).fill("09:00 - 15:00").join(", ") }} />);
      const more = prepare();
      vi.mocked(more.getBoundingClientRect).mockReturnValue({ ...rect, left: data.left, right: data.left + 50, top: data.top, bottom: data.top + 28 });
      fireEvent.click(more);
      const popup = screen.getByRole("tooltip");
      const numbers = [popup.style.left, popup.style.top, popup.style.width, popup.style.maxHeight].map(parseFloat);
      expect(numbers.every(Number.isFinite)).toBe(true);
      expect(numbers).toEqual([data.expectedLeft, data.expectedTop, Math.min(220, Math.max(1, data.width - 16)), data.maxHeight]);
      expect(numbers[0]).toBeGreaterThanOrEqual(0); expect(numbers[0] + numbers[2]).toBeLessThanOrEqual(data.width);
      expect(numbers[1]).toBeGreaterThanOrEqual(0);
      expect(numbers[1] + Math.min((data.count - 1) * 28 + 18, numbers[3])).toBeLessThanOrEqual(data.height);
    } finally { vi.unstubAllGlobals(); }
  });
  test("More isolates selection and editing; primary edits full raw data", async () => {
    const selected = vi.fn(), changed = vi.fn(), hint = vi.fn();
    render(<ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[column]} cellMap={{ "-1:1": raw }}
      onSelectedCellKeysChange={selected} onCellChange={changed} onVisualHintClick={hint} />);
    const more = prepare();
    const user = userEvent.setup();
    await user.dblClick(more);
    expect(selected).not.toHaveBeenCalled(); expect(changed).not.toHaveBeenCalled(); expect(hint).not.toHaveBeenCalled();
    expect(screen.queryByRole("textbox")).toBeNull();
    more.focus(); await user.keyboard("{Enter}"); await user.keyboard("{Escape}");
    expect(screen.queryByRole("tooltip")).toBeNull();
    await user.keyboard(" "); expect(screen.getByRole("tooltip")).toBeInTheDocument();
    await user.keyboard(" "); expect(screen.queryByRole("tooltip")).toBeNull();
    const primary = screen.getByRole("button", { name: "Demand day 1" });
    expect(primary.parentElement).toBe(more.parentElement);
    expect(primary).toHaveAttribute("title", raw);
    await user.click(primary); expect(selected).toHaveBeenCalled();
    await user.dblClick(primary);
    const editor = screen.getByRole("textbox", { name: "Demand day 1" });
    expect(editor).toHaveValue(raw);
    fireEvent.change(editor, { target: { value: `${raw}, 16:00 - 20:00` } });
    fireEvent.blur(editor);
    expect(changed).toHaveBeenCalledWith(-1, 1, `${raw}, 16:00 - 20:00`);
  });
  test.each(["", "09:00 - 15:00", "free text", "09:00 - 15:00, invalid"])("fallback retains %s", value => {
    render(<ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[column]} cellMap={{ "-1:1": value }} readOnly />);
    expect(screen.queryByRole("button", { name: "More: Shifts" })).toBeNull();
  });
  test("two intervals expose only the remaining interval, retaining custom colors", () => {
    render(<ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[column]}
      cellMap={{ "-1:1": "09:00 - 15:00, 15:00 - 21:00" }} readOnly
      styleMap={{ "-1:1": { id: 1, backgroundColor: "#ff0000", textColor: "#ffffff", backgroundColorArgb: null, textColorArgb: null } }} />);
    const more = prepare();
    expect(more.closest("td")).toHaveStyle({ backgroundColor: "#ff0000", color: "#ffffff" });
    fireEvent.click(more);
    expect(screen.getByRole("tooltip").children).toHaveLength(1);
    expect(screen.getByRole("tooltip")).toHaveTextContent("15:00 - 21:00");
  });
  test("employee width remains raw and a resized manual width survives source updates", () => {
    const props = { graph: { year: 2026, month: 5 }, columns: [column, { ...column, employeeId: 2, kind: "employee" as const, label: "Worker" }],
      cellMap: { "-1:1": raw, "2:1": raw }, allowColumnResize: true };
    const { container, rerender } = render(<ContainerGraphMatrix {...props} />);
    const widths = () => Array.from(container.querySelectorAll("col")).slice(1).map(el => (el as HTMLElement).style.getPropertyValue("--matrix-column-width"));
    expect(Number(widths()[0].match(/\d+/)?.[0])).toBeLessThan(Number(widths()[1].match(/\d+/)?.[0]));
    const handle = container.querySelector(`.${styles.columnResizeHandle}`)!;
    fireEvent.mouseDown(handle, { clientX: 100 });
    fireEvent.mouseMove(window, { clientX: 100 });
    fireEvent.mouseUp(window);
    expect(widths()[0]).toBe("135px");
    rerender(<ContainerGraphMatrix {...props} cellMap={{ ...props.cellMap, "-1:1": `${raw}, ${raw}` }} />);
    expect(widths()[0]).toBe("135px");
    expect(screen.getByRole("button", { name: "More: Shifts" })).toBeInTheDocument();
  });
  test("employee, inline and related hints retain their normal rendering", () => {
    const props = { graph: { year: 2026, month: 5 }, columns: [column], cellMap: { "-1:1": raw } };
    const { rerender } = render(<ContainerGraphMatrix {...props} columns={[{ ...column, kind: "employee" }]} />);
    expect(screen.queryByText("More")).toBeNull();
    rerender(<ContainerGraphMatrix {...props} editMode="inline" />);
    expect(screen.getByRole("textbox", { name: "Demand day 1" })).toHaveValue(raw);
    rerender(<ContainerGraphMatrix {...props} visualHintMap={{ "-1:1": "Related schedule" }} onVisualHintClick={vi.fn()} />);
    expect(screen.queryByText("More")).toBeNull();
    rerender(<ContainerGraphMatrix {...props} mutedSuffixMap={{ "-1:1": "Related schedule" }} />);
    expect(screen.queryByText("More")).toBeNull();
  });
  test("read-only toggle closes on changed source; compact width survives phone cap and language", async () => {
    const props = { graph: { year: 2026, month: 5 }, columns: [column], cellMap: { "-1:1": raw }, readOnly: true, maxAutoColumnWidth: 156 };
    const { container, rerender } = render(<ContainerGraphMatrix {...props} />);
    const more = prepare(); fireEvent.click(more); expect(screen.getByRole("tooltip")).toBeInTheDocument();
    fireEvent.click(more); expect(screen.queryByRole("tooltip")).toBeNull();
    fireEvent.click(more);
    const width = Number((container.querySelectorAll("col")[1] as HTMLElement).style.getPropertyValue("--matrix-column-width").match(/\d+/)?.[0]);
    expect(width).toBeGreaterThan(156); expect(width).toBeLessThan(300);
    rerender(<ContainerGraphMatrix {...props} cellMap={{ "-1:1": "10:00 - 14:00" }} />);
    expect(screen.queryByRole("tooltip")).toBeNull();
    act(() => setTestLanguage("pl"));
    rerender(<ContainerGraphMatrix {...props} />);
    expect(screen.getByRole("button", { name: `${t("More")}: ${t("Shifts")}` })).toHaveTextContent(t("More"));
  });
});

test("pinned generated headers cannot be dragged or receive a column drop", () => {
  const onColumnMove = vi.fn();
  const columns = [-7, 1, -8].map(employeeId => ({ employeeId, kind: employeeId < 0 ? "manual" as const : "employee" as const,
    manualColumnId: employeeId < 0 ? -employeeId : null, graphEmployeeId: null, label: String(employeeId), minHoursMonth: null, totalMinutes: 0, totalText: "" }));
  render(<ContainerGraphMatrix graph={{ year: 2026, month: 1 }} columns={columns} cellMap={{}}
    pinnedColumnIds={new Set([-7])} onColumnMove={onColumnMove} />);
  const pinned = screen.getByRole("columnheader", { name: "-7" });
  const employee = screen.getByRole("columnheader", { name: "1" });
  expect(pinned).toHaveAttribute("draggable", "false");
  expect(employee).toHaveAttribute("draggable", "true");
  fireEvent.dragStart(employee, { dataTransfer: { setData: vi.fn() } });
  fireEvent.drop(pinned);
  expect(onColumnMove).not.toHaveBeenCalled();
});

describe("public holiday dates", () => {
  test.each([1, 5])("marks named dates in month %s without changing explicit styles or selection", month => {
    const { container } = render(<ContainerGraphMatrix graph={{ year: 2026, month }}
      columns={[{ employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" }]}
      cellMap={{ "7:1": "09:00 - 15:00" }} selectedCellKeys={["7:1"]} dayConflictMap={{ 1: true }}
      styleMap={{ "7:1": { id: 1, backgroundColor: "#ff0000", textColor: "#ffffff", backgroundColorArgb: null, textColorArgb: null },
        "0:1": { id: 2, backgroundColor: "#00ff00", textColor: "#000000", backgroundColorArgb: null, textColorArgb: null } }} />);
    const day = screen.getByTitle(month === 1 ? "New Year's Day" : "Labour Day");
    expect(day).toHaveAttribute("aria-label", expect.stringContaining("Holiday"));
    expect(day).toHaveClass(styles.dayCellConflict, styles.dayCellSelected);
    expect(day).toHaveStyle({ backgroundColor: "#00ff00" });
    expect(day.querySelector(`.${styles.dayButtonHoliday}`)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Ada day 1" }).closest("td")).toHaveStyle({ backgroundColor: "#ff0000" });
    expect(container.querySelectorAll("tbody th")[1]).not.toHaveAttribute("title");
    if (month === 5) {
      const sunday = screen.getByTitle("Constitution Day");
      expect(sunday.parentElement).toHaveClass(styles.weekendRow);
      expect(sunday.querySelector(`.${styles.dayButtonHoliday}`)).toBeInTheDocument();
    }
    expect(container.querySelector("td[class*='Holiday']")).toBeNull();
  });
  test("OFF removes holiday semantics through the real query context", async () => {
    const spy = vi.spyOn(publicHolidaySettingsApi, "get").mockResolvedValue({ showPublicHolidays: false });
    const client = new QueryClient();
    await act(async () => { render(<QueryClientProvider client={client}><PublicHolidayVisibilityProvider enabled>
      <ContainerGraphMatrix graph={{ year: 2026, month: 5 }} columns={[]} cellMap={{}} />
    </PublicHolidayVisibilityProvider></QueryClientProvider>); });
    expect(screen.queryByTitle("Labour Day")).toBeNull();
    expect(screen.queryByText("Hol.")).toBeNull();
    spy.mockRestore();
  });
});

describe("ContainerGraphMatrix related schedule hints", () => {
  test("marks an invalid styled cell for the visible error pulse", () => {
    const renderMatrix = (validationSignal: number) => (
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[
          { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{ "7:1": "09:00 - 08:00" }}
        cellErrors={{ "7:1": "From must be earlier than To." }}
        validationSignal={validationSignal}
        styleMap={{
          "7:1": {
            id: 1,
            backgroundColor: "#ef4444",
            textColor: "#ffffff",
            backgroundColorArgb: null,
            textColorArgb: null,
          },
        }}
      />
    );
    const { rerender } = render(renderMatrix(1));

    const invalidButton = screen.getByRole("button", { name: "Ada day 1" });
    expect(invalidButton).toHaveAttribute("aria-invalid", "true");
    expect(invalidButton.closest("td")).toHaveClass(styles.errorCell);

    rerender(renderMatrix(2));
    const restartedButton = screen.getByRole("button", { name: "Ada day 1" });
    expect(restartedButton).not.toBe(invalidButton);

    fireEvent.doubleClick(restartedButton);
    const editor = screen.getByRole("textbox", { name: "Ada day 1" });
    expect(editor.closest("td")).toHaveClass(styles.errorCell, styles.editingCell);

    fireEvent.blur(editor);
    expect(screen.getByRole("button", { name: "Ada day 1" }).closest("td")).not.toHaveClass(styles.editingCell);
  });

  test("renders text values muted while keeping shifts normal and supports regular profile weight", () => {
    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[
          { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: 8, kind: "employee", manualColumnId: null, graphEmployeeId: 80, label: "Grace", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{ "7:1": "Training", "8:1": "09:00 - 15:00" }}
        readOnly
        regularCellText
      />,
    );

    const textValue = screen.getByText("Training");
    const shiftValue = screen.getByText("09:00 - 15:00");

    expect(textValue).toHaveClass(styles.textValue);
    expect(shiftValue).not.toHaveClass(styles.textValue);
    expect(textValue.closest("table")).toHaveClass(styles.regularCellText);
  });

  test("merges touching shifts visually and mutes only the related schedule suffix", () => {
    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[
          { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{ "7:1": "09:00 - 15:00, 15:00 - 21:00, F27" }}
        mutedSuffixMap={{ "7:1": "F27" }}
        readOnly
      />,
    );

    const cellValue = screen.getByTitle("09:00 - 15:00, 15:00 - 21:00, F27");
    const relatedSchedule = screen.getByText("F27");

    expect(cellValue).toHaveTextContent("09:00 - 21:00, F27");
    expect(cellValue).not.toHaveClass(styles.textValue);
    expect(relatedSchedule).toHaveClass(styles.mutedSuffix);
  });

  test("adds a dashed visual row guide for a partial selection without selecting the other cells", () => {
    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[
          { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: 8, kind: "employee", manualColumnId: null, graphEmployeeId: 80, label: "Grace", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{}}
        selectedCellKeys={["7:1"]}
        onSelectedCellKeysChange={() => undefined}
      />,
    );

    const selectedCell = screen.getByRole("button", { name: "Ada day 1" }).closest("td");
    const visualOnlyCell = screen.getByRole("button", { name: "Grace day 1" }).closest("td");
    const dayCell = screen.getByText("th./01.01").closest("th");

    expect(selectedCell).toHaveClass(styles.selectedCell, styles.rowGuideCell);
    expect(visualOnlyCell).toHaveClass(styles.rowGuideCell);
    expect(visualOnlyCell).not.toHaveClass(styles.selectedCell);
    expect(dayCell).toHaveClass(styles.dayCellRowGuide);
    expect(dayCell).not.toHaveClass(styles.dayCellSelected);
  });

  test("keeps a fully selected day as the existing solid row selection", () => {
    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[
          { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: 8, kind: "employee", manualColumnId: null, graphEmployeeId: 80, label: "Grace", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{}}
        selectedCellKeys={["7:1", "8:1"]}
        onSelectedCellKeysChange={() => undefined}
      />,
    );

    const dayCell = screen.getByText("th./01.01").closest("th");

    expect(dayCell).toHaveClass(styles.dayCellSelected);
    expect(dayCell).not.toHaveClass(styles.dayCellRowGuide);
    expect(screen.getByRole("button", { name: "Ada day 1" }).closest("td")).not.toHaveClass(styles.rowGuideCell);
  });

  test("applies a bound fill color to the focused selected cell", () => {
    const onFillColorShortcut = vi.fn();

    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[{
          employeeId: 7,
          kind: "employee",
          manualColumnId: null,
          graphEmployeeId: 70,
          label: "Ada",
          minHoursMonth: null,
          totalMinutes: 0,
          totalText: "",
        }]}
        cellMap={{}}
        selectedCellKeys={["7:1"]}
        fillColorByKey={new Map([["F4", "#DBEAFE"]])}
        onSelectedCellKeysChange={() => undefined}
        onFillColorShortcut={onFillColorShortcut}
      />,
    );

    const cell = screen.getByRole("button", { name: "Ada day 1" });
    fireEvent.keyDown(cell, { key: "F4" });

    expect(onFillColorShortcut).toHaveBeenCalledWith("#DBEAFE", ["7:1"]);
  });

  test("applies a bound text color to the focused selected cell", () => {
    const onTextColorShortcut = vi.fn();

    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[{
          employeeId: 7,
          kind: "employee",
          manualColumnId: null,
          graphEmployeeId: 70,
          label: "Ada",
          minHoursMonth: null,
          totalMinutes: 0,
          totalText: "",
        }]}
        cellMap={{}}
        selectedCellKeys={["7:1"]}
        textColorByKey={new Map([["F5", "#0F172A"]])}
        onSelectedCellKeysChange={() => undefined}
        onTextColorShortcut={onTextColorShortcut}
      />,
    );

    fireEvent.keyDown(screen.getByRole("button", { name: "Ada day 1" }), { key: "F5" });

    expect(onTextColorShortcut).toHaveBeenCalledWith("#0F172A", ["7:1"]);
  });

  test("shows compact per-shift staffing counts in the day column when enabled", () => {
    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 22:00" }}
        columns={[
          { employeeId: 1, kind: "employee", manualColumnId: null, graphEmployeeId: 1, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: 2, kind: "employee", manualColumnId: null, graphEmployeeId: 2, label: "Grace", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: 3, kind: "employee", manualColumnId: null, graphEmployeeId: 3, label: "Linus", minHoursMonth: null, totalMinutes: 0, totalText: "" },
          { employeeId: -1, kind: "manual", manualColumnId: 1, graphEmployeeId: null, label: "Open", minHoursMonth: null, totalMinutes: 0, totalText: "" },
        ]}
        cellMap={{
          "1:1": "08:00 - 16:00",
          "2:1": "12:00 - 20:00",
          "3:1": "16:00 - 22:00",
          "-1:1": "08:00 - 22:00",
        }}
        showShiftStaffingCounts
        readOnly
      />,
    );

    expect(screen.getByLabelText("Shift 1: 2 employees; Shift 2: 2 employees")).toHaveTextContent("2,2");
    expect(screen.getByText("th./01.01")).toBeVisible();
  });

  test("exposes an employee header as a customization button", () => {
    const onColumnHeaderClick = vi.fn();
    const column = {
      employeeId: 7,
      kind: "employee" as const,
      manualColumnId: null,
      graphEmployeeId: 70,
      label: "Ada",
      minHoursMonth: null,
      totalMinutes: 0,
      totalText: "",
    };

    render(
      <ContainerGraphMatrix
        graph={{ year: 2026, month: 1 }}
        columns={[column]}
        cellMap={{}}
        readOnly
        onColumnHeaderClick={onColumnHeaderClick}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Customize columns from Ada" }));
    expect(onColumnHeaderClick).toHaveBeenCalledWith(column);
  });
  test("keeps a filled shift visible and renders the related schedule as a clickable blue suffix", () => {
    vi.useFakeTimers();
    const onVisualHintClick = vi.fn();
    const onCellChange = vi.fn();
    const cellMap = { "7:1": "09:00 - 15:00, 18:00 - 20:00" };
    const detail: GraphRelatedScheduleHintDetail = {
      employeeId: 7,
      dayOfMonth: 1,
      visualHint: "F35",
      relatedGraphs: [
        {
          graphId: 35,
          graphName: "F35",
          intervals: [{ from: "09:00", to: "15:00" }],
          intervalsText: "09:00 - 15:00",
          dayValues: [],
        },
      ],
    };

    try {
      render(
        <ContainerGraphMatrix
          graph={{ year: 2026, month: 1 }}
          columns={[{
            employeeId: 7,
            kind: "employee",
            manualColumnId: null,
            graphEmployeeId: 70,
            label: "Ada",
            minHoursMonth: null,
            totalMinutes: 0,
            totalText: "",
          }]}
          cellMap={cellMap}
          visualHintMap={{ "7:1": "F35" }}
          visualHintDetailMap={{ "7:1": detail }}
          onCellChange={onCellChange}
          onVisualHintClick={onVisualHintClick}
        />,
      );

      const hintButton = screen.getByRole("button", { name: "Ada day 1" });
      expect(hintButton).toHaveTextContent("F35");
      expect(hintButton).not.toHaveTextContent("09:00 - 15:00");
      expect(hintButton.parentElement).toHaveTextContent("09:00 - 15:00, 18:00 - 20:00, F35");

      fireEvent.click(hintButton, { detail: 1 });
      act(() => vi.advanceTimersByTime(181));

      expect(onVisualHintClick).toHaveBeenCalledWith(detail);
      expect(onCellChange).not.toHaveBeenCalled();
      expect(cellMap).toEqual({ "7:1": "09:00 - 15:00, 18:00 - 20:00" });
    } finally {
      vi.useRealTimers();
    }
  });
});

test("updates memoized matrix cells and weekdays without remounting user content", () => {
  const columns = [{ employeeId: 7, kind: "employee" as const, manualColumnId: null, graphEmployeeId: 70, label: "Ada", minHoursMonth: null, totalMinutes: 0, totalText: "" }];
  const cells = { "7:1": "09:00 - 12:00" };
  render(<ContainerGraphMatrix graph={{ year: 2026, month: 1 }} columns={columns} cellMap={cells} />);
  const cell = screen.getByRole("button", { name: "Ada day 1", exact: true });
  expect(screen.getByText("th./01.01")).toBeInTheDocument();
  act(() => setTestLanguage("pl"));
  expect(screen.getByRole("button", { name: t("{0} day {1}", "Ada", 1), exact: true })).toBe(cell);
  expect(screen.queryByText("th./01.01")).not.toBeInTheDocument();
  expect(cell).toHaveTextContent("09:00");
  act(() => setTestLanguage("en"));
  expect(screen.getByRole("button", { name: "Ada day 1", exact: true })).toBe(cell);
  expect(screen.getByText("th./01.01")).toBeInTheDocument();
});
