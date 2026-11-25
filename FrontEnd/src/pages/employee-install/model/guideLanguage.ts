import { createContext, useContext } from "react";
import { translate, type Language } from "@shared/i18n";

const storageKey = "gf3:guide-language";

export const GuideLanguageContext = createContext<Language>("pl");

export function useGuideTranslation() {
  return translate.bind(null, useContext(GuideLanguageContext));
}

export function readGuideLanguage(fallback: Language): Language {
  try {
    const saved = localStorage.getItem(storageKey);
    if (saved === "pl" || saved === "uk" || saved === "en") return saved;
  } catch {
    // Storage can be unavailable in private browsing or embedded browsers.
  }
  return fallback;
}

export function rememberGuideLanguage(language: Language) {
  try {
    localStorage.setItem(storageKey, language);
  } catch {
    // The selected language still applies for this visit.
  }
}
