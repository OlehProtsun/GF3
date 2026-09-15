import { afterEach, describe, expect, it } from "vitest";
import { dateTimeFormat, getLanguage, numberFormat, setLanguage, t, translateServerMessage } from "./index";
import pl from "./pl.json";
import { normalizeBindKey } from "@entities/availability-binds/model/hotkeys";
import { getGraphWeekdayLabel, parseGraphCellContent } from "@entities/containers/model/graphWorkspace";
import { availabilityMonthOptions, getAvailabilityKindLabel } from "@entities/availability-groups/model/presentation";

afterEach(() => setLanguage("en"));

describe("account language", () => {
  it("switches translations, document language, preloaded month labels and formatters in both directions", () => {
    const formatter = dateTimeFormat("en-GB", { month: "long", timeZone: "UTC" });
    const date = new Date("2026-09-01T00:00:00Z");
    expect(formatter.format(date)).toBe("September");
    setLanguage("pl");
    expect(getLanguage()).toBe("pl");
    expect(document.documentElement.lang).toBe("pl");
    expect(t("Profile access")).toBe("Dostęp do profilu");
    expect(formatter.format(date)).toBe("wrzesień");
    expect(availabilityMonthOptions[8].label).toBe("wrzesień");
    expect(getAvailabilityKindLabel("Available")).toBe("Dowolna zmiana");
    expect(numberFormat("en-GB", { minimumFractionDigits: 2 }).format(12.5)).toBe("12,50");
    expect(getGraphWeekdayLabel(2026, 9, 14)).toBe("pn.");
    setLanguage("en");
    expect(t("Profile access")).toBe("Profile access");
    expect(formatter.format(date)).toBe("September");
    expect(availabilityMonthOptions[8].label).toBe("September");
  });

  it("preserves keyboard shortcuts and schedule values in Polish", () => {
    const english = parseGraphCellContent("09:00 - 15:00");
    setLanguage("pl");
    expect(normalizeBindKey("Control+Shift+Home")).toBe("Ctrl+Shift+Home");
    expect(parseGraphCellContent("09:00 - 15:00")).toEqual(english);
    expect(t("Manager account created for {0}.", "Home {1} & <b>")).toBe("Utworzono konto menedżera dla Home {1} & <b>.");
  });

  it("localizes known server messages while preserving record names and unknown details", () => {
    setLanguage("pl");
    expect(translateServerMessage("This username is already in use.")).toBe("Ta nazwa użytkownika jest już zajęta.");
    expect(translateServerMessage("Created manager account Home.")).toBe("Utworzono konto menedżera Home.");
    expect(translateServerMessage("Unrecognized technical detail")).toBe("Unrecognized technical detail");
  });

  it("has nonempty Polish translations with no invented interpolation parameters", () => {
    for (const [english, polish] of Object.entries(pl)) {
      expect(polish.trim(), english).not.toBe("");
      const parameters = new Set(english.match(/\{\d+\}/g) ?? []);
      for (const parameter of polish.match(/\{\d+\}/g) ?? []) expect(parameters.has(parameter), english).toBe(true);
      expect(/[А-Яа-яІіЇїЄє]/.test(polish), english).toBe(false);
    }
  });
});
