import { describe, expect, test } from "vitest";
import {
  buildActiveAvailabilityBindMap,
  formatBindKeyFromKeyboardEvent,
  isBindNavigationKey,
  isCommonEditorShortcut,
  isModifierOnlyKey,
  normalizeBindKey,
} from "./hotkeys";

describe("availability bind hotkeys", () => {
  test("normalizes modifier aliases, order, named keys, and letters", () => {
    expect(normalizeBindKey("shift + ctrl + a")).toBe("Ctrl+Shift+A");
    expect(normalizeBindKey("cmd+spacebar")).toBe("Meta+Space");
    expect(normalizeBindKey("alt+pgdn")).toBe("Alt+PageDown");
    expect(normalizeBindKey("f5")).toBe("F5");
  });

  test("rejects empty, modifier-only, and multi-base shortcuts", () => {
    expect(normalizeBindKey("")).toBeNull();
    expect(normalizeBindKey("Ctrl+Shift")).toBeNull();
    expect(normalizeBindKey("Ctrl+A+B")).toBeNull();
  });

  test("formats keyboard events and ignores modifier-only events", () => {
    expect(formatBindKeyFromKeyboardEvent({ key: "a", ctrlKey: true, shiftKey: true })).toBe("Ctrl+Shift+A");
    expect(formatBindKeyFromKeyboardEvent({ key: "ArrowDown", altKey: true })).toBe("Alt+Down");
    expect(formatBindKeyFromKeyboardEvent({ key: "Control", ctrlKey: true })).toBeNull();
  });

  test("detects editor and navigation shortcuts that should not become binds", () => {
    expect(isCommonEditorShortcut({ key: "z", ctrlKey: true })).toBe(true);
    expect(isCommonEditorShortcut({ key: "z", ctrlKey: true, shiftKey: true })).toBe(false);
    expect(isBindNavigationKey("Enter")).toBe(true);
    expect(isModifierOnlyKey("Meta")).toBe(true);
  });

  test("builds active bind map with normalized keys and skips inactive or invalid binds", () => {
    const map = buildActiveAvailabilityBindMap([
      { key: "ctrl+a", value: "+", isActive: true },
      { key: "bad+key+extra", value: "-", isActive: true },
      { key: "shift+b", value: "09:00 - 12:00", isActive: false },
      { key: "cmd+space", value: "", isActive: true },
    ]);

    expect([...map.entries()]).toEqual([
      ["Ctrl+A", "+"],
      ["Meta+Space", ""],
    ]);
  });
});
