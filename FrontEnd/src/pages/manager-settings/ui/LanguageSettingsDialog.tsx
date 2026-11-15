import { useEffect, useId, useRef, useState } from "react";
import { useLanguage } from "@app/providers/LanguageProvider";
import { getErrorMessage } from "@shared/api/httpClient";
import { languageNames, t, type Language } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CheckIcon } from "@shared/ui/icons";
import { SettingsDialog } from "./SettingsDialog";
import { SettingsSymbol } from "./SettingsSymbol";
import styles from "./ManagerSettingsPage.module.css";

export function LanguageSettingsDialog({ onClose, readOnly }: { onClose: () => void; readOnly: boolean }) {
  useLanguageRevision();
  const { language, ready, save } = useLanguage();
  const name = useId();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const busy = useRef(false);
  const mounted = useRef(true);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);

  const choose = async (value: Language) => {
    if (busy.current || readOnly || !ready || value === language) return;
    busy.current = true;
    setPending(true);
    setError(null);
    try { await save(value); }
    catch (reason) { if (mounted.current) setError(reason); }
    finally { busy.current = false; if (mounted.current) setPending(false); }
  };

  return <SettingsDialog title={t("Language")} description={t("Choose your preferred application language.")} icon={<SettingsSymbol kind="globe" />} className={styles.languageDialog} onClose={onClose}>
    <fieldset className={styles.languageChoices} disabled={readOnly || !ready || pending} aria-busy={pending}>
      <legend className={styles.srOnly}>{t("Application language")}</legend>
      {(["pl", "en", "uk"] as const).map(value => <label key={value} className={styles.languageOption}>
        <input className={styles.languageRadio} type="radio" name={name} value={value} checked={language === value} onChange={() => void choose(value)} />
        <span aria-hidden="true" className={[styles.languageMarker, value === "pl" ? styles.flagPolish : value === "uk" ? styles.flagUkrainian : styles.flagEnglish].join(" ")}>
          {value === "en" ? <svg viewBox="0 0 60 40" preserveAspectRatio="xMidYMid slice" focusable="false"><path d="M0 0h60v40H0z" fill="#17458b" /><path d="m0 0 60 40M60 0 0 40" stroke="#fff" strokeWidth="9" /><path d="m0 0 60 40M60 0 0 40" stroke="#e52b49" strokeWidth="4" /><path d="M30 0v40M0 20h60" stroke="#fff" strokeWidth="13" /><path d="M30 0v40M0 20h60" stroke="#e52b49" strokeWidth="7" /></svg> : null}
        </span>
        <span className={styles.languageName} lang={value}>{languageNames[value]}</span>
        <span className={`${styles.selectionIndicator} ${language === value ? styles.selectionSelected : ""}`} aria-hidden="true">
          {language === value ? <CheckIcon size={16} /> : null}
        </span>
      </label>)}
    </fieldset>
    {error ? <ErrorBanner dismissible={false}>{getErrorMessage(error, t("Could not save language. Please try again."))}</ErrorBanner> : null}
  </SettingsDialog>;
}
