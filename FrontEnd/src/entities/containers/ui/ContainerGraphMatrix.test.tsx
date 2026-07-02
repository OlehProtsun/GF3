import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import type { GraphRelatedScheduleHintDetail } from "@entities/containers/model/graphWorkspace";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";

describe("ContainerGraphMatrix related schedule hints", () => {
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