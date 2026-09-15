import pl from "./pl.json";

export type Language = "en" | "pl";
let language: Language = "en";
const listeners = new Set<() => void>();

export function getLanguage(): Language { return language; }
export function getLocale() { return language === "pl" ? "pl-PL" : "en-GB"; }
export function subscribeLanguage(listener: () => void) {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}
export function setLanguage(value: Language) {
  document.documentElement.lang = value;
  if (language === value) return;
  language = value;
  listeners.forEach(listener => listener());
}

/** Only application-owned messages belong here; never translate user content. */
export function t(message: string, ...values: unknown[]): string {
  const translated = language === "pl" ? (pl as Record<string, string>)[message] ?? message : message;
  return translated.replace(/\{(\d+)\}/g, (match, index: string) => Number(index) < values.length ? String(values[Number(index)]) : match);
}

export function dateTimeFormat(englishLocale?: Intl.LocalesArgument, options?: Intl.DateTimeFormatOptions) {
  return {
    format: (value?: Date | number) => new Intl.DateTimeFormat(language === "pl" ? "pl-PL" : englishLocale, options).format(value),
    formatToParts: (value?: Date | number) => new Intl.DateTimeFormat(language === "pl" ? "pl-PL" : englishLocale, options).formatToParts(value),
  };
}
export function numberFormat(englishLocale?: Intl.LocalesArgument, options?: Intl.NumberFormatOptions) {
  return { format: (value: number | bigint) => new Intl.NumberFormat(language === "pl" ? "pl-PL" : englishLocale, options).format(value) };
}
import serverMessages from "./serverMessages.json";

const serverPatterns = serverMessages.map(message => {
  const indexes: number[] = [];
  const parts = message.split(/(\{\d+\})/g).map(part => {
    const parameter = /^\{(\d+)\}$/.exec(part);
    if (parameter) { indexes.push(Number(parameter[1])); return "(.*?)"; }
    return part.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  });
  return { message, indexes, pattern: new RegExp(`^${parts.join("")}$`) };
});

/** Translate only known server messages, preserving all interpolated account and record data. */
export function translateServerMessage(message: string): string {
  if (language !== "pl") return message;
  const exact = (pl as Record<string, string>)[message];
  if (exact !== undefined) return exact;
  for (const entry of serverPatterns) {
    const match = entry.pattern.exec(message);
    if (!match) continue;
    const values: string[] = [];
    entry.indexes.forEach((index, position) => { values[index] = match[position + 1]; });
    return t(entry.message, ...values);
  }
  return message;
}
