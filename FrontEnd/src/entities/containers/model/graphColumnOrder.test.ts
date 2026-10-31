import { expect, test } from "vitest";
import { pinGeneratedColumnsFirst } from "./graphColumnOrder";

test.each([
  [[1, -2, 3], [], [1, -2, 3]],
  [[1, -2, 3], [-2], [-2, 1, 3]],
  [[1, 3, -2], [-2], [-2, 1, 3]],
  [[-2, 1, 3], [-2], [-2, 1, 3]],
  [[1, -3, -2, 4, -5], [-2, -3], [-3, -2, 1, 4, -5]],
  [[1, -2, -2, 1, 3], [-2, -2], [-2, 1, 3]],
  [[1, -2], [-3], [1, -2]],
])("pins only existing tagged columns: %j", (order, generated, expected) => {
  const result = pinGeneratedColumnsFirst(order, generated);
  expect(result).toEqual(expected);
  expect(pinGeneratedColumnsFirst(result, generated)).toEqual(result);
});
