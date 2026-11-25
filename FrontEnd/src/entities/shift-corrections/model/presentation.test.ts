import { expect, test } from "vitest";
import { setLanguage } from "@shared/i18n";
import { formatShiftCorrectionDelta, shiftCorrectionOriginalMinutes } from "./presentation";

test.each([[0, "unchanged"], [-35, "−35m"], [-120, "−2h"], [-153, "−2h 33m"], [45, "+45m"]] as const)(
  "formats %s minutes as %s", (minutes, label) => {
    setLanguage("en");
    expect(formatShiftCorrectionDelta(minutes)).toBe(label);
  },
);

test("sums split and overnight shifts", () => {
  expect(shiftCorrectionOriginalMinutes([])).toBe(0);
  expect(shiftCorrectionOriginalMinutes([
    { fromTime: "08:00", toTime: "12:00" }, { fromTime: "21:45", toTime: "01:15" },
  ])).toBe(450);
});
