import type { AvailabilityBind } from "./types";

const modifierOrder = ["Ctrl", "Alt", "Shift", "Meta"] as const;
const modifierAliases = new Map<string, (typeof modifierOrder)[number]>([
  ["ctrl", "Ctrl"],
  ["control", "Ctrl"],
  ["ctl", "Ctrl"],
  ["alt", "Alt"],
  ["shift", "Shift"],
  ["meta", "Meta"],
  ["cmd", "Meta"],
  ["command", "Meta"],
  ["win", "Meta"],
  ["windows", "Meta"],
  ["super", "Meta"],
]);

const namedKeyAliases = new Map<string, string>([
  ["esc", "Escape"],
  ["escape", "Escape"],
  ["return", "Enter"],
  ["enter", "Enter"],
  ["space", "Space"],
  ["spacebar", "Space"],
  [" ", "Space"],
  ["arrowup", "Up"],
  ["up", "Up"],
  ["arrowdown", "Down"],
  ["down", "Down"],
  ["arrowleft", "Left"],
  ["left", "Left"],
  ["arrowright", "Right"],
  ["right", "Right"],
  ["backspace", "Backspace"],
  ["delete", "Delete"],
  ["del", "Delete"],
  ["insert", "Insert"],
  ["ins", "Insert"],
  ["home", "Home"],
  ["end", "End"],
  ["pageup", "PageUp"],
  ["pagedown", "PageDown"],
  ["pgup", "PageUp"],
  ["pgdn", "PageDown"],
  ["tab", "Tab"],
]);

const modifierOnlyKeys = new Set(["Control", "Shift", "Alt", "Meta"]);
const navigationKeys = new Set(["Tab", "Enter", "Escape", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"]);

type KeyboardDescriptor = {
  key: string;
  ctrlKey?: boolean;
  altKey?: boolean;
  shiftKey?: boolean;
  metaKey?: boolean;
};

type AvailabilityBindLike = Pick<AvailabilityBind, "key" | "value" | "isActive">;

function normalizeModifierToken(rawToken: string) {
  return modifierAliases.get(rawToken.trim().toLowerCase()) ?? null;
}

function normalizeBaseKeyToken(rawToken: string) {
  const token = rawToken.trim();
  if (!token) {
    return null;
  }

  const lowered = token.toLowerCase();
  const aliased = namedKeyAliases.get(lowered);
  if (aliased) {
    return aliased;
  }

  if (/^f\d{1,2}$/i.test(token)) {
    return token.toUpperCase();
  }

  if (token.length === 1) {
    return /[a-z]/i.test(token) ? token.toUpperCase() : token;
  }

  if (/^[a-z]+$/i.test(token)) {
    return token.charAt(0).toUpperCase() + token.slice(1).toLowerCase();
  }

  return token;
}

export function isModifierOnlyKey(key: string) {
  return modifierOnlyKeys.has(key);
}

export function isBindNavigationKey(key: string) {
  return navigationKeys.has(key);
}

export function isCommonEditorShortcut(event: KeyboardDescriptor) {
  const primaryModifier = Boolean(event.ctrlKey || event.metaKey);
  if (!primaryModifier || event.altKey || event.shiftKey) {
    return false;
  }

  return ["A", "C", "V", "X", "Y", "Z"].includes((event.key ?? "").toUpperCase());
}

export function normalizeBindKey(rawValue: string) {
  const trimmed = rawValue.trim();
  if (!trimmed) {
    return null;
  }

  if (!trimmed.includes("+")) {
    return normalizeBaseKeyToken(trimmed);
  }

  const tokens = trimmed
    .split("+")
    .map(token => token.trim())
    .filter(Boolean);

  if (tokens.length < 2) {
    return null;
  }

  const modifiers = new Set<(typeof modifierOrder)[number]>();
  let baseKey: string | null = null;

  for (const token of tokens) {
    const modifier = normalizeModifierToken(token);
    if (modifier) {
      modifiers.add(modifier);
      continue;
    }

    if (baseKey) {
      return null;
    }

    baseKey = normalizeBaseKeyToken(token);
    if (!baseKey) {
      return null;
    }
  }

  if (!baseKey) {
    return null;
  }

  return [...modifierOrder.filter(modifier => modifiers.has(modifier)), baseKey].join("+");
}

export function formatBindKeyFromKeyboardEvent(event: KeyboardDescriptor) {
  if (!event.key || isModifierOnlyKey(event.key)) {
    return null;
  }

  const baseKey = normalizeBaseKeyToken(event.key);
  if (!baseKey) {
    return null;
  }

  const parts: string[] = [];
  if (event.ctrlKey) {
    parts.push("Ctrl");
  }

  if (event.altKey) {
    parts.push("Alt");
  }

  if (event.shiftKey) {
    parts.push("Shift");
  }

  if (event.metaKey) {
    parts.push("Meta");
  }

  parts.push(baseKey);
  return parts.join("+");
}

export function buildActiveAvailabilityBindMap<TBind extends AvailabilityBindLike>(binds: readonly TBind[]) {
  const bindMap = new Map<string, string>();

  binds.forEach(bind => {
    if (!bind.isActive) {
      return;
    }

    const normalizedKey = normalizeBindKey(bind.key);
    if (!normalizedKey) {
      return;
    }

    bindMap.set(normalizedKey, bind.value ?? "");
  });

  return bindMap;
}
