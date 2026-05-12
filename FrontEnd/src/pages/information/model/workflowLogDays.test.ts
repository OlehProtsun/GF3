import { describe, expect, it } from "vitest";
import type { WorkflowLog } from "@entities/workflow-logs";
import {
  formatWorkflowLogTime,
  getRoleLabel,
  getWorkflowDayKey,
  groupWorkflowLogsByDay,
  parseWorkflowLogDate,
} from "./workflowLogDays";

function createLog(id: number, occurredAtUtc: string, action = `Action ${id}`): WorkflowLog {
  return {
    id,
    occurredAtUtc,
    actorRole: id % 2 === 0 ? "employee" : "manager",
    actorEmployeeId: id % 2 === 0 ? id : null,
    actorName: `Actor ${id}`,
    action,
  };
}

describe("workflow log day model", () => {
  it("groups logs by day, orders days newest first, and orders logs newest first inside the day", () => {
    const groups = groupWorkflowLogsByDay([
      createLog(1, "2026-05-09T08:00:00.000Z"),
      createLog(2, "2026-05-10T09:00:00.000Z"),
      createLog(3, "2026-05-10T12:00:00.000Z"),
    ]);

    expect(groups).toHaveLength(2);
    expect(groups[0]?.key).toBe("2026-05-10");
    expect(groups[0]?.logs.map(log => log.id)).toEqual([3, 2]);
    expect(groups[1]?.key).toBe("2026-05-09");
  });

  it("keeps invalid dates in an Unknown bucket without throwing", () => {
    const groups = groupWorkflowLogsByDay([
      createLog(1, "not-a-date"),
      createLog(2, "2026-05-10T09:00:00.000Z"),
    ]);

    expect(groups.map(group => group.key)).toEqual(["2026-05-10", "unknown"]);
    expect(groups[1]?.label).toBe("Unknown");
    expect(groups[1]?.fullLabel).toBe("Unknown date");
  });

  it("formats times and role labels for timeline rendering", () => {
    expect(parseWorkflowLogDate("bad")).toBeNull();
    expect(getWorkflowDayKey(null)).toBe("unknown");
    expect(formatWorkflowLogTime("bad")).toBe("bad");
    expect(formatWorkflowLogTime("2026-05-10T09:05:06.000Z")).toMatch(/\d{2}:\d{2}:\d{2}/);
    expect(getRoleLabel("employee")).toBe("Employee");
    expect(getRoleLabel("manager")).toBe("Manager");
    expect(getRoleLabel("anything-else")).toBe("Manager");
  });
});
