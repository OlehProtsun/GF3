import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { AppRouter } from "@app/router";
import { AppUpdateNotice } from "./ui/AppUpdateNotice";

export function App() {
  useLanguageRevision();
  return <><AppRouter /><AppUpdateNotice /></>;
}
