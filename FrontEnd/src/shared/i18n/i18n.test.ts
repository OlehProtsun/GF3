import { formatHoursMinutes } from "@entities/containers/model/statistics";
import { afterEach, vi, describe, expect, it } from "vitest";
import { dateTimeFormat, getLanguage, getLocale, numberFormat, setLanguage, t, translateServerMessage } from "./index";
import pl from "./pl.json";
import { getAvailabilityWeekdayLabel } from "@entities/availability-groups/model/matrix";
import { normalizeBindKey } from "@entities/availability-binds/model/hotkeys";
import { getGraphWeekdayLabel, parseGraphCellContent } from "@entities/containers/model/graphWorkspace";
import { availabilityMonthOptions, getAvailabilityKindLabel } from "@entities/availability-groups/model/presentation";

afterEach(() => setLanguage("pl"));

describe("account language", () => {
  it("starts a fresh module in Polish and synchronizes HTML even for the same language", async () => {
    vi.resetModules();
    const fresh = await import("./index");
    expect(fresh.getLanguage()).toBe("pl");
    document.documentElement.lang = "en";
    fresh.setLanguage("pl");
    expect(document.documentElement.lang).toBe("pl");
  });
  it("translates shift selection, notification fallbacks and duration interpolation", () => {
    setLanguage("pl");
    expect(t("Choose a shift")).toBe("Wybierz zmian\u0119");
    expect(t("your shop")).toBe("Twoim sklepie");
    expect(t("A coworker")).toBe("Wsp\u00f3\u0142pracownik");
    expect(t("{0}h", "7,5")).toBe("7,5 godz.");
    expect(formatHoursMinutes(450)).toBe("7 godz. 30 min");
    setLanguage("en");
    expect(t("Choose a shift")).toBe("Choose a shift");
    expect(formatHoursMinutes(450)).toBe("7h 30m");
    setLanguage("pl");
  });
  it("switches translations, document language, preloaded month labels and formatters in both directions", () => {
    setLanguage("en");
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

  it("switches Ukrainian labels, formatters, weekdays and known errors without changing user values", () => {
    const month = dateTimeFormat("en-GB", { month: "long", timeZone: "UTC" });
    const date = new Date("2026-09-01T00:00:00Z");
    const cell = parseGraphCellContent("09:00 - 15:00");
    setLanguage("uk");
    expect(getLocale()).toBe("uk-UA");
    expect(document.documentElement.lang).toBe("uk");
    expect(t("Application language")).toBe("Мова застосунку");
    expect(t("Choose a shift")).toBe("Виберіть зміну");
    expect(t("New schedule published")).toBe("Опубліковано новий графік");
    expect(t("{0}h", "7,5")).toBe("7,5 год");
    expect(formatHoursMinutes(450)).toBe("7 год 30 хв");
    expect(month.format(date)).toBe("вересень");
    expect(availabilityMonthOptions[8].label).toBe("вересень");
    expect(numberFormat("en-GB", { minimumFractionDigits: 2 }).format(12.5)).toBe("12,50");
    expect(getGraphWeekdayLabel(2026, 9, 17)).toBe("чт.");
    expect(getAvailabilityWeekdayLabel(2026, 9, 17)).toBe("чт.");
    expect(translateServerMessage("Created manager account Home.")).toBe("Створено акаунт менеджера Home.");
    expect(translateServerMessage("Unrecognized technical detail")).toBe("Unrecognized technical detail");
    expect(t("Manager account created for {0}.", "Home {1} & <b>")).toBe("Акаунт менеджера для Home {1} & <b> створено.");
    expect(parseGraphCellContent("09:00 - 15:00")).toEqual(cell);
    setLanguage("en");
    expect(t("Application language")).toBe("Application language");
    expect(month.format(date)).toBe("September");
    setLanguage("pl");
    expect(month.format(date)).toBe("wrzesień");
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
