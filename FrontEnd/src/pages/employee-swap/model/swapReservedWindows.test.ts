import { expect, test } from "vitest";
import type { ShiftSwap } from "@entities/shift-swaps/model/types";
import { getSwapSlotWindows } from "./swapReservedWindows";

const slot = { id: 1, fromTime: "09:00", toTime: "15:00" };
const period = (fromTime: string, toTime: string) => ({ fromTime, toTime });
const swap = (details: Partial<ShiftSwap>) => ({ status: "open", ...details }) as ShiftSwap;

test("combines source and return holds without reserving neighboring minutes", () => {
  expect(getSwapSlotWindows(slot, [
    swap({ returnScheduleSlotId: 1, returnFromTime: "13:30", returnToTime: "14:00" }),
    swap({ scheduleSlotId: 1, ...period("11:00", "12:00") }),
    swap({ scheduleSlotId: 1, ...period("09:00", "11:00") }),
    swap({ scheduleSlotId: 1, ...period("11:30", "12:30") }),
    swap({ scheduleSlotId: 1, ...period("12:30", "13:30"), status: "cancelled" }),
    swap({ scheduleSlotId: 2, ...period("09:00", "15:00") }),
  ])).toEqual({ reserved: [period("09:00", "12:30"), period("13:30", "14:00")],
    available: [period("12:30", "13:30"), period("14:00", "15:00")] });
});

test.each([null, "08:59", "9:00"])("conservatively holds inconsistent or legacy bounds: %s", from => {
  expect(getSwapSlotWindows(slot, [swap({ returnScheduleSlotId: 1, returnFromTime: from, returnToTime: "13:00" })]))
    .toEqual({ reserved: [period("09:00", "15:00")], available: [] });
});
