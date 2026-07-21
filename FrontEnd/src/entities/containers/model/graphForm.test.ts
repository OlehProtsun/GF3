import { describe, expect, test } from "vitest";
import {
  applyGraphApiErrors,
  buildGraphFormErrors,
  createInitialGraphForm,
  isValidGraphShiftTime,
  parseIntegerField,
} from "./graphForm";

describe("container graph form model", () => {
  test("creates initial form with current year, default month, and optional shop", () => {
    const form = createInitialGraphForm(42);

    expect(form.shopId).toBe("42");
    expect(form.publicationStatus).toBe("private");
    expect(form.allowSwap).toBe(true);
    expect(form.peoplePerShift).toBe("1");
    expect(Number(form.year)).toBeGreaterThanOrEqual(2026);
    expect(Number(form.month)).toBeGreaterThanOrEqual(1);
    expect(Number(form.month)).toBeLessThanOrEqual(12);
  });

  test("parses integer fields strictly", () => {
    expect(parseIntegerField("42")).toBe(42);
    expect(parseIntegerField("  ")).toBeNull();
    expect(parseIntegerField("1.5")).toBeNull();
    expect(parseIntegerField("abc")).toBeNull();
  });

  test("validates graph shift times without allowing overnight or impossible values", () => {
    expect(isValidGraphShiftTime("06:00 - 14:00")).toBe(true);
    expect(isValidGraphShiftTime("6:00 - 14:00")).toBe(true);
    expect(isValidGraphShiftTime("14:00 - 06:00")).toBe(false);
    expect(isValidGraphShiftTime("06:00 - 06:00")).toBe(false);
    expect(isValidGraphShiftTime("25:00 - 26:00")).toBe(false);
  });

  test("builds validation errors for every invalid graph field", () => {
    const form = {
      ...createInitialGraphForm(),
      name: "",
      shopId: "999",
      year: "1999",
      month: "13",
      peoplePerShift: "0",
      shift1Time: "bad",
      shift2Time: "22:00 - 10:00",
      maxHoursPerEmpMonth: "0",
      maxConsecutiveDays: "-1",
      maxConsecutiveFull: "-1",
      maxFullPerMonth: "-1",
      availabilityGroupId: "404",
    };

    expect(buildGraphFormErrors(form, new Set([1]), new Set([2]))).toEqual({
      name: "Name is required.",
      shopId: "Select a valid shop.",
      year: "Enter a valid year.",
      month: "Month must be between 1 and 12.",
      peoplePerShift: "People per shift must be at least 1.",
      shift1Time: "Use HH:mm - HH:mm format.",
      shift2Time: "Use HH:mm - HH:mm format.",
      maxHoursPerEmpMonth: "Max hours must be at least 1.",
      maxConsecutiveDays: "Use 0 or more.",
      maxConsecutiveFull: "Use 0 or more.",
      maxFullPerMonth: "Use 0 or more.",
      availabilityGroupId: "Select a valid availability group.",
    });
  });

  test("accepts a valid graph form and optional empty availability group", () => {
    const form = {
      ...createInitialGraphForm(1),
      name: "May schedule",
      year: "2026",
      month: "5",
      availabilityGroupId: "",
    };

    expect(buildGraphFormErrors(form, new Set([1]), new Set())).toEqual({});
  });

  test("maps API validation errors with pascal or camel case keys", () => {
    expect(applyGraphApiErrors({
      Name: ["Name exists."],
      shopid: ["Shop missing."],
      AvailabilityGroupId: ["Availability missing."],
      Note: ["Note too long."],
    })).toEqual({
      name: "Name exists.",
      shopId: "Shop missing.",
      availabilityGroupId: "Availability missing.",
      note: "Note too long.",
    });
  });
});
