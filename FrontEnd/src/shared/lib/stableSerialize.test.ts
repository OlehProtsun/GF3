import { describe, expect, test } from "vitest";
import { stableSerialize } from "./stableSerialize";

describe("stableSerialize", () => {
  test("sorts object keys recursively while preserving array order", () => {
    expect(stableSerialize({
      z: 1,
      nested: { beta: true, alpha: false },
      list: [{ b: 2, a: 1 }, { d: 4, c: 3 }],
    })).toBe('{"list":[{"a":1,"b":2},{"c":3,"d":4}],"nested":{"alpha":false,"beta":true},"z":1}');
  });

  test("serializes primitive and nullish values like JSON.stringify", () => {
    expect(stableSerialize(null)).toBe("null");
    expect(stableSerialize("value")).toBe('"value"');
    expect(stableSerialize([3, 2, 1])).toBe("[3,2,1]");
  });
});
