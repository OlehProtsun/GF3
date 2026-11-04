import { describe, expect, test } from "vitest";
import { buildAvailabilityDraftSource, buildDraftFromAvailability, buildSaveSlots, buildEmployeeAvailabilityColumns, buildEmployeeAvailabilityCells, getContiguousAvailabilityDays, applyEmployeeAvailabilityValue } from "./employeeAvailabilityMatrix";
import type { EmployeeAvailabilityGroup, EmployeeAvailabilityMember } from "@entities/employee-availability";
const member = (employeeId: number, displayOrder: number): EmployeeAvailabilityMember => ({ employeeId, displayOrder, firstName: "Name", lastName: "Surname", slots: [] });
const group: EmployeeAvailabilityGroup = { id: 5, year: 2026, month: 5, name: "May", canSubmit: true, isEditLocked: false, slots: [] };
describe("employee availability matrix helpers", () => {
  test("orders self then manager order, deduplicates, and never fabricates self", () => {
    expect(buildEmployeeAvailabilityColumns([member(3, 0), member(2, 4), member(1, 2), member(3, 0)], 2).map(m => m.employeeId)).toEqual([2, 3, 1]);
    expect(buildEmployeeAvailabilityColumns([member(3, 0)], 2).map(m => m.employeeId)).toEqual([3]);
  });
  test("uses real month lengths and ignores invalid teammate dates deterministically", () => {
    const m = member(2, 0);
    m.slots = [0, 1, 1, 29, 31, 32, 1.5].map((dayOfMonth, id) => ({ id, availabilityGroupMemberId: 1, dayOfMonth, kind: id === 2 ? 2 : 0, intervalStr: id === 2 ? "training" : null }));
    expect(buildEmployeeAvailabilityCells([m], 2026, 2)).toEqual({ "2:1": "training" });
    expect(Object.keys(buildEmployeeAvailabilityCells([m], 2028, 2))).toEqual(["2:1", "2:29"]);
  });
  test("clips contiguous ranges up and down", () => {
    expect(getContiguousAvailabilityDays(5, 2, 31)).toEqual([2, 3, 4, 5]);
    expect(getContiguousAvailabilityDays(-2, 3, 28)).toEqual([1, 2, 3]);
    expect(getContiguousAvailabilityDays(27, 32, 28)).toEqual([27, 28]);
    expect(getContiguousAvailabilityDays(NaN, 4, 31)).toEqual([]);
  });
  test("applies only selected own days immutably and serializes every valid day", () => {
    const draft = buildDraftFromAvailability(group);
    const next = applyEmployeeAvailabilityValue(draft, [2, 3, 32], { kind: 2, normalizedCode: "training", intervalStr: "training" });
    expect(draft[2].available).toBe(false);
    expect(next[1]).toBe(draft[1]);
    expect(next[2]).toEqual({ available: true, intervalStr: "training" });
    expect(next[32]).toBeUndefined();
    const slots = buildSaveSlots(group, next);
    expect(slots).toHaveLength(31);
    expect(slots[1]).toEqual({ dayOfMonth: 2, kind: 2, intervalStr: "training" });
    expect(slots[30].dayOfMonth).toBe(31);
    expect(buildSaveSlots({ ...group, month: 2 }, next)).toHaveLength(28);
  });
  test("draft source depends on own slots, never team data", () => {
    expect(buildAvailabilityDraftSource(group)).toEqual(buildAvailabilityDraftSource({ ...group, members: [member(3, 0)] }));
    expect(buildAvailabilityDraftSource({ ...group, slots: [{ id: 1, availabilityGroupMemberId: 1, dayOfMonth: 1, kind: 0 }] }).sourceKey).not.toBe(buildAvailabilityDraftSource(group).sourceKey);
  });
});
