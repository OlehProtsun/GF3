import { afterEach, expect, test, vi } from "vitest";
import { readGuideLanguage, rememberGuideLanguage } from "./guideLanguage";

afterEach(() => { vi.restoreAllMocks(); localStorage.clear(); });

test("defaults to Polish when no preference exists and saves guide language separately", () => {
  expect(readGuideLanguage("pl")).toBe("pl");
  rememberGuideLanguage("uk");
  expect(localStorage.getItem("gf3:guide-language")).toBe("uk");
  expect(readGuideLanguage("en")).toBe("uk");
  expect(localStorage.length).toBe(1);
  localStorage.setItem("gf3:guide-language", "unsupported");
  expect(readGuideLanguage("en")).toBe("en");
});

test("uses the available visit language when browser storage is denied", () => {
  const denied = () => { throw new DOMException("Storage denied", "SecurityError"); };
  vi.spyOn(Storage.prototype, "getItem").mockImplementation(denied);
  vi.spyOn(Storage.prototype, "setItem").mockImplementation(denied);
  expect(readGuideLanguage("pl")).toBe("pl");
  expect(() => rememberGuideLanguage("en")).not.toThrow();
});
