import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import type { GraphRelatedScheduleHintDetail } from "@entities/containers/model/graphWorkspace";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";

describe("ContainerGraphMatrix related schedule hints", () => {
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
    const cellMap = { "7:1": "15:00 - 21:00" };
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
      expect(hintButton).not.toHaveTextContent("15:00 - 21:00");
      expect(hintButton.parentElement).toHaveTextContent("15:00 - 21:00, F35");

      fireEvent.click(hintButton, { detail: 1 });
      act(() => vi.advanceTimersByTime(181));

      expect(onVisualHintClick).toHaveBeenCalledWith(detail);
      expect(onCellChange).not.toHaveBeenCalled();
      expect(cellMap).toEqual({ "7:1": "15:00 - 21:00" });
    } finally {
      vi.useRealTimers();
    }
  });
});
