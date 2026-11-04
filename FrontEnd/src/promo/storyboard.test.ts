import { describe, expect, it } from "vitest";
import fixtureSource from "./fixtures.ts?raw";
import { availability, days, employees, fixtureVersion, shifts, swapOffer } from "./fixtures";
import { PROMO_DURATION_SECONDS, PROMO_FPS, SCENES } from "./storyboard";

describe("promo storyboard and fixtures", () => {
  it("has nine contiguous scenes in the approved order", () => {
    expect(SCENES.map(scene => scene.id)).toEqual(["pain", "reveal", "manager", "availability", "schedule", "swap", "more", "connected", "outro"]);
    expect(SCENES[0].start).toBe(0);
    expect(SCENES.at(-1)?.end).toBe(75);
    expect(new Set(SCENES.map(scene => scene.id)).size).toBe(9);
    for (const [index, scene] of SCENES.entries()) {
      expect(scene.end).toBeGreaterThan(scene.start);
      expect(scene.headline.trim()).not.toBe("");
      expect(Object.isFrozen(scene)).toBe(true);
      if (index > 0) expect(scene.start).toBeCloseTo(SCENES[index - 1].end, 8);
    }
    expect(PROMO_DURATION_SECONDS * PROMO_FPS).toBe(2250);
  });
  it("resolves every fixed fixture reference and contains no random or clock generation", () => {
    expect(fixtureVersion).toBe("gf3-promo-1");
    const employeeIds = new Set(employees.map(employee => employee.id));
    const dayIds = new Set(days.map(day => day.id));
    for (const item of [...shifts, ...availability]) {
      expect(employeeIds.has(item.employeeId)).toBe(true);
      expect(dayIds.has(item.dayId)).toBe(true);
      expect(Object.isFrozen(item)).toBe(true);
    }
    expect(shifts.some(shift => shift.id === swapOffer.shiftId)).toBe(true);
    expect(employeeIds.has(swapOffer.candidateId)).toBe(true);
    expect(Object.isFrozen(shifts)).toBe(true);
    expect(fixtureSource).not.toMatch(/Date\.now|Math\.random|new Date|fetch\s*\(/);
  });
});
