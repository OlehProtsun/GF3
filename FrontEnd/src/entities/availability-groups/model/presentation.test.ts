import { describe, expect, test } from "vitest";
import {
  filterAvailabilityGroups,
  formatAvailabilityDateTimeLabel,
  getAvailabilityGroupPeriodLabel,
  getAvailabilityKindLabel,
  getAvailabilityMemberLastModifiedLabel,
  getAvailabilityMemberNames,
  getAvailabilityMonthLabel,
  getAvailabilityPublicationStatusLabel,
  getAvailabilityVisibilityWindowLabel,
  getAvailabilityWindowStatusLabel,
  isAvailabilityWindowOpen,
  normalizeAvailabilityPublicationStatus,
  sortAvailabilityGroups,
  summarizeAvailabilitySlots,
} from "./presentation";
import type { AvailabilityGroup } from "./types";

describe("availability presentation model", () => {
  test("formats period, publication status, windows, and kinds", () => {
    const now = new Date("2026-05-13T12:00:00Z");

    expect(getAvailabilityMonthLabel(5)).toBe("May");
    expect(getAvailabilityMonthLabel(5, "short")).toBe("May");
    expect(getAvailabilityMonthLabel(99)).toBe("Month 99");
    expect(getAvailabilityGroupPeriodLabel({ month: 5, year: 2026 })).toBe("May 2026");
    expect(getAvailabilityGroupPeriodLabel(null)).toBe("Unknown period");
    expect(normalizeAvailabilityPublicationStatus("PUBLIC")).toBe("public");
    expect(normalizeAvailabilityPublicationStatus("draft")).toBe("private");
    expect(getAvailabilityPublicationStatusLabel("public")).toBe("Public");
    expect(isAvailabilityWindowOpen({
      publicationStatus: "public",
      visibleFromUtc: "2026-05-01T00:00:00Z",
      visibleToUtc: "2026-05-31T23:59:59Z",
    }, now)).toBe(true);
    expect(getAvailabilityWindowStatusLabel({
      publicationStatus: "private",
      visibleFromUtc: null,
      visibleToUtc: null,
    }, now)).toBe("Closed");
    expect(formatAvailabilityDateTimeLabel("bad")).toBe("Not set");
    expect(getAvailabilityVisibilityWindowLabel({ visibleFromUtc: null, visibleToUtc: null })).toBe("Not configured");
    expect(getAvailabilityMemberLastModifiedLabel(null)).toBe("No employee edits");
    expect(getAvailabilityKindLabel("Available")).toBe("Any shift");
    expect(getAvailabilityKindLabel(2)).toBe("Custom interval");
    expect(getAvailabilityKindLabel(99)).toBe("Unknown");
  });

  test("filters, sorts, summarizes slots, and resolves member names", () => {
    const groups: AvailabilityGroup[] = [
      { id: 2, name: "June", year: 2026, month: 6, publicationStatus: "private" },
      { id: 1, name: "May", year: 2026, month: 5, publicationStatus: "public" },
      { id: 3, name: "Old", year: 2025, month: 12, publicationStatus: "public" },
    ];

    expect(sortAvailabilityGroups(groups).map(group => group.name)).toEqual(["June", "May", "Old"]);
    expect(filterAvailabilityGroups(groups, "public").map(group => group.name)).toEqual(["May", "Old"]);
    expect(filterAvailabilityGroups(groups, "jun").map(group => group.name)).toEqual(["June"]);
    expect(summarizeAvailabilitySlots([
      { id: 1, availabilityGroupMemberId: 1, dayOfMonth: 1, kind: "Available" },
      { id: 2, availabilityGroupMemberId: 1, dayOfMonth: 2, kind: "Unavailable" },
      { id: 3, availabilityGroupMemberId: 1, dayOfMonth: 3, kind: "Preferred", intervalStr: "09:00 - 12:00" },
      { id: 4, availabilityGroupMemberId: 1, dayOfMonth: 4, kind: 99 },
    ])).toEqual({ any: 1, none: 1, interval: 1, other: 1 });
    expect(getAvailabilityMemberNames([
      { id: 1, availabilityGroupId: 1, employeeId: 2, displayOrder: 1 },
      { id: 2, availabilityGroupId: 1, employeeId: 1, displayOrder: 2 },
    ], new Map([[1, "Ada Lovelace"]]))).toEqual(["Ada Lovelace", "Employee #2"]);
  });
});
