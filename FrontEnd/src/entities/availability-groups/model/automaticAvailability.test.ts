import { describe, expect, test } from "vitest";
import type { Employee } from "@entities/employees/model/types";
import { buildEffectiveManagerAvailabilityCellMap, getAutomaticManagerAvailabilityCellKeys, getAutomaticManagerEmployeeIds } from "./automaticAvailability";

const employees: Employee[] = [
  { id: 1, firstName: "Ada", lastName: "Worker", contractType: "o_prace", hasLoginAccount: true, isOnline: false },
  { id: 2, firstName: "Grace", lastName: "Worker", contractType: "zlecenie", hasLoginAccount: true, isOnline: false },
  { id: 3, firstName: "Other", lastName: "Worker", contractType: "o_prace", hasLoginAccount: true, isOnline: false },
];

describe("automatic manager availability", () => {
  test.each([[2026, 2, 28], [2028, 2, 29], [2026, 4, 30], [2026, 12, 31], [2027, 1, 31]])(
    "%i-%i projects all %i days using the local calendar without changing stored values", (year, month, count) => {
      const raw = Object.freeze({ "1:1": "09:00 - 15:00", "1:2": "-", "2:1": "manual", "3:1": "-" });
      const ids = getAutomaticManagerEmployeeIds([1, 2, 99], employees, true);
      expect([...ids]).toEqual([1]);
      const effective = buildEffectiveManagerAvailabilityCellMap(raw, ids, year, month);
      const keys = getAutomaticManagerAvailabilityCellKeys(ids, year, month);
      expect(keys.size).toBe(count);
      for (let day = 1; day <= count; day++) {
        const weekday = new Date(year, month - 1, day).getDay();
        expect(effective[`1:${day}`]).toBe(weekday === 0 || weekday === 6 ? "-" : "+");
        expect(keys.has(`1:${day}`)).toBe(true);
      }
      expect(effective[`1:${count + 1}`]).toBeUndefined();
      expect(effective["2:1"]).toBe("manual");
      expect(effective["3:1"]).toBe("-");
      expect(raw).toEqual({ "1:1": "09:00 - 15:00", "1:2": "-", "2:1": "manual", "3:1": "-" });
      expect(buildEffectiveManagerAvailabilityCellMap(raw, getAutomaticManagerEmployeeIds([1, 2], employees, false), year, month)).toEqual(raw);
    },
  );
  test("contract change restores custom intervals and missing slots stay absent in raw data", () => {
    const raw = { "1:1": "09:00 - 15:00" };
    const effective = buildEffectiveManagerAvailabilityCellMap(raw, new Set([1]), 2026, 12);
    expect(effective["1:1"]).toBe("+");
    expect(effective["1:5"]).toBe("-");
    expect(effective["1:7"]).toBe("+");
    const changed = employees.map(employee => ({ ...employee, contractType: "zlecenie" as const }));
    expect(buildEffectiveManagerAvailabilityCellMap(raw, getAutomaticManagerEmployeeIds([1], changed, true), 2026, 12)).toEqual(raw);
    expect(raw).not.toHaveProperty("1:7");
  });
});
