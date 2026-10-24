import { expect, test } from "vitest";
import { parseManualShiftIntervals, enumerateManualShiftOptions, canReserveManualShift } from "./manualShiftIntervals";
const p = (fromTime = "09:00", toTime = "15:00") => ({ fromTime, toTime });
test("duplicates are capacity; touching pieces also offer their continuous union", () => {
  expect(parseManualShiftIntervals("09:00-15:00,09:00-15:00")).toEqual([p(), p()]);
  expect(enumerateManualShiftOptions("09:00-15:00,15:00-21:00").map(x => [x.fromTime,x.toTime])).toEqual([["09:00","15:00"],["09:00","21:00"],["15:00","21:00"]]);
  expect(canReserveManualShift("09:00-15:00,09:00-15:00", [p()], p())).toBe(true);
  expect(canReserveManualShift("09:00-15:00,09:00-15:00", [p(),p()], p())).toBe(false);
  expect(enumerateManualShiftOptions("09:00-15:00,09:00-15:00", [p()])).toHaveLength(1);
  expect(canReserveManualShift("09:00-15:00,16:00-21:00", [], p("09:00","21:00"))).toBe(false);
});
test.each(["free text", "09:00-15:00,", "25:00-26:00", "12:00-09:00", "09:00-09:00", "09:00-15:00,invalid"])("invalid cell %s never publishes", raw => {
  expect(parseManualShiftIntervals(raw)).toBeNull(); expect(enumerateManualShiftOptions(raw)).toEqual([]);
});
test("manual cells have no employee four-interval limit", () => expect(parseManualShiftIntervals(Array(8).fill("09:00-15:00").join(","))).toHaveLength(8));
