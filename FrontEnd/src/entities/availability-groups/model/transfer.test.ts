import { describe, expect, test } from "vitest";
import {
  buildAvailabilityTransferHintMap,
  buildAvailabilityProfileHintMap,
  buildAvailabilityTransferSourceHintData,
  buildAvailabilityTransferSourceHintMap,
  removeStagedAvailabilityTransferDay,
  stageAvailabilityTransfer,
  type AvailabilityTransferSource,
} from "./transfer";

describe("buildAvailabilityTransferHintMap", () => {
  test("shows existing month availability as a blue source hint before CFA is staged", () => {
    const sources = [
      {
        groupId: 35,
        groupName: "F35",
        memberId: 12,
        employeeId: 7,
        days: [
          { dayOfMonth: 3, kind: "INT", intervalStr: "09:00 - 15:00", canTransfer: true },
          { dayOfMonth: 4, kind: "NONE", canTransfer: false },
        ],
      },
    ] satisfies AvailabilityTransferSource[];

    expect(buildAvailabilityTransferSourceHintMap(sources)).toEqual({
      "7:3": "09:00 - 15:00 (F35)",
    });
    expect(buildAvailabilityTransferSourceHintData(sources).detailMap["7:3"]).toEqual({
      employeeId: 7,
      dayOfMonth: 3,
      sourceGroupId: 35,
      sourceGroupName: "F35",
    });
  });

  test("stages CFA values locally and removes the staged write when the target cell is edited", () => {
    const staged = stageAvailabilityTransfer({
      cellMap: { "7:3": "-" },
      transfers: [],
      employeeId: 7,
      sourceGroupId: 35,
      dayOfMonths: [3],
      sources: [{
        groupId: 35,
        groupName: "F35",
        memberId: 12,
        employeeId: 7,
        days: [{ dayOfMonth: 3, kind: "INT", intervalStr: "09:00 - 15:00", canTransfer: true }],
      }],
    });

    expect(staged.cellMap["7:3"]).toBe("09:00 - 15:00");
    expect(staged.transfers).toEqual([{ employeeId: 7, sourceGroupId: 35, dayOfMonths: [3] }]);
    expect(removeStagedAvailabilityTransferDay(staged.transfers, 7, 3)).toEqual([]);
  });

  test("shows moved intervals with their target availability and skips unavailable targets", () => {
    expect(buildAvailabilityTransferHintMap([
      {
        employeeId: 7,
        dayOfMonth: 3,
        targetGroupId: 35,
        targetGroupName: "F35",
        kind: "INT",
        intervalStr: "09:00 - 15:00",
      },
      {
        employeeId: 7,
        dayOfMonth: 4,
        targetGroupId: 35,
        targetGroupName: "F35",
        kind: "NONE",
      },
    ])).toEqual({
      "7:3": "09:00 - 15:00 (F35)",
    });
  });

  test("keeps same-month source hints visible in profile after CFA has been saved", () => {
    const sources = [{
      groupId: 31,
      groupName: "F31",
      memberId: 12,
      employeeId: 7,
      days: [{ dayOfMonth: 3, kind: "INT", intervalStr: "09:00 - 15:00", canTransfer: true }],
    }] satisfies AvailabilityTransferSource[];

    expect(buildAvailabilityProfileHintMap(sources, [])).toEqual({
      "7:3": "09:00 - 15:00 (F31)",
    });
  });

  test("prefers the saved CFA destination when both hint sources address the same cell", () => {
    const sources = [{
      groupId: 31,
      groupName: "F31",
      memberId: 12,
      employeeId: 7,
      days: [{ dayOfMonth: 3, kind: "INT", intervalStr: "09:00 - 15:00", canTransfer: true }],
    }] satisfies AvailabilityTransferSource[];

    expect(buildAvailabilityProfileHintMap(sources, [{
      employeeId: 7,
      dayOfMonth: 3,
      targetGroupId: 35,
      targetGroupName: "F35",
      kind: "INT",
      intervalStr: "09:00 - 15:00",
    }])).toEqual({
      "7:3": "09:00 - 15:00 (F35)",
    });
  });
});
