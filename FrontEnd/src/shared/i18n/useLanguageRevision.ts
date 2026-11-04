import { useSyncExternalStore } from "react";
import { getLanguage, subscribeLanguage } from "./index";

/** Subscribe persistent screens to language changes without discarding their drafts. */
export function useLanguageRevision() {
  return useSyncExternalStore(subscribeLanguage, getLanguage);
}
