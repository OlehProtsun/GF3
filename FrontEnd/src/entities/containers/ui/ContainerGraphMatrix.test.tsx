import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import type { GraphRelatedScheduleHintDetail } from "@entities/containers/model/graphWorkspace";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import styles from "./ContainerGraphMatrix.module.css";

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
