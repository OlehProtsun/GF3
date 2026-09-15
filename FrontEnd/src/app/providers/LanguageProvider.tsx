import { getLanguage, setLanguage, subscribeLanguage, t, type Language } from "@shared/i18n";
import { createContext, useContext, useEffect, useRef, useState, useSyncExternalStore, type PropsWithChildren } from "react";
import { useAuth } from "./AuthProvider";
import { request } from "@shared/api/httpClient";

type LanguageContextValue = { language: Language; ready: boolean; save: (language: Language) => Promise<void> };
const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: PropsWithChildren) {
  const { session } = useAuth();
  const language = useSyncExternalStore(subscribeLanguage, getLanguage);
  const accountKey = session ? `${session.role}:${session.managerId ?? session.employeeId}` : "guest";
  const isAuthenticated = Boolean(session);
  const [loadedAccount, setLoadedAccount] = useState<string | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [retry, setRetry] = useState(0);
  const activeRequest = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    activeRequest.current = controller;
    setLanguage("en");
    setLoadError(false);
    if (!isAuthenticated) {
      setLoadedAccount(accountKey);
      return () => controller.abort();
    }
    void request<{ language: Language }>("account-language", { signal: controller.signal })
      .then(result => {
        if (!controller.signal.aborted) {
          setLanguage(result.language === "pl" ? "pl" : "en");
          setLoadedAccount(accountKey);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setLoadError(true);
          setLoadedAccount(accountKey);
        }
      });
    return () => controller.abort();
  }, [accountKey, isAuthenticated, retry]);

  const save = async (next: Language) => {
    const controller = activeRequest.current;
    const result = await request<{ language: Language }>("account-language", {
      method: "PUT", body: { language: next }, signal: controller?.signal,
    });
    if (!controller?.signal.aborted) {
      setLanguage(result.language === "pl" ? "pl" : "en");
      setLoadError(false);
    }
  };
  const ready = loadedAccount === accountKey;
  return <LanguageContext.Provider value={{ language, ready, save }}>
    {loadError ? <div role="alert">{t("Could not load your language preference.")} <button type="button" onClick={() => setRetry(value => value + 1)}>{t("Retry")}</button></div> : null}
    {ready ? <LanguageWorkspace key={accountKey}>{children}</LanguageWorkspace> : <div role="status">{t("Loading...")}</div>}
  </LanguageContext.Provider>;
}

function LanguageWorkspace({ children }: PropsWithChildren) { return children; }

// eslint-disable-next-line react-refresh/only-export-components
export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error("LanguageProvider is missing");
  return context;
}
