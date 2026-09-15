import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { AppRouter } from "@app/router";

export function App() {
  useLanguageRevision();
  return <AppRouter />;
}
