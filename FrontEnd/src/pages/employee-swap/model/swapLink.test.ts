import { describe, expect, test } from "vitest";
import { createSwapLink, parseSwapLinkId } from "./swapLink";

describe("swap links", () => {
  test("creates only the canonical query", () => {
    expect(createSwapLink(12, "https://gf.example/other?token=x")).toBe("https://gf.example/swap?swapId=12");
    expect(parseSwapLinkId("?swapId=12")).toBe(12);
  });
  test.each([0, -1, 1.5, NaN, Infinity, Number.MAX_SAFE_INTEGER + 1])("rejects invalid creation ID %s", id => {
    expect(() => createSwapLink(id, "https://gf.example")).toThrow(RangeError);
  });
  test.each(["", "?swapId=", "?swapId=0", "?swapId=-1", "?swapId=%2B1", "?swapId=1.5", "?swapId=1x", "?swapId=1&swapId=2", "?swapId=9007199254740992", "?swapId=%ZZ", "?swapId=%201"])("rejects invalid query %s", query => {
    expect(parseSwapLinkId(query)).toBeNull();
  });
});
