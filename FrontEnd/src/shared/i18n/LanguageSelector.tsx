import { useEffect, useId, useRef, useState } from "react";
import { useLanguage } from "@app/providers/LanguageProvider";
import { t, type Language } from "@shared/i18n";
import { SearchableSelect } from "@shared/ui/components/SearchableSelect/SearchableSelect";
import styles from "./LanguageSelector.module.css";

export function LanguageSelector({ disabled = false, selectClassName, appearance = "native" }: {
  disabled?: boolean;
  selectClassName?: string;
  appearance?: "native" | "rounded";
}) {
  const { language, ready, save } = useLanguage();
  const id = useId();
  const successTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const mounted = useRef(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(false);
  const [saved, setSaved] = useState(false);
  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; clearTimeout(successTimer.current); };
  }, []);
  const change = async (value: Language) => {
    if (pending || disabled || !ready || value === language) return;
    clearTimeout(successTimer.current);
    setSaved(false);
    setPending(true);
    setError(false);
    try {
      await save(value);
      if (mounted.current && appearance === "rounded") {
        setSaved(true);
        successTimer.current = setTimeout(() => setSaved(false), 2400);
      }
    }
    catch { if (mounted.current) setError(true); }
    finally { if (mounted.current) setPending(false); }
  };
  if (appearance === "rounded") return <>
    <label className={styles.label} htmlFor={id}>{t("Application language")}</label>
    <div className={styles.control} aria-busy={pending}>
      <SearchableSelect
        id={id}
        className={styles.select}
        value={language}
        options={[{ value: "en", label: "English" }, { value: "pl", label: "Polski" }]}
        placeholder={t("Application language")}
        dropdownTitle={t("Application language")}
        ariaLabel={t("Application language")}
        ariaDescribedBy={error ? `${id}-error` : undefined}
        searchEnabled={false}
        showSelectedHint={false}
        size="field"
        shadow="soft"
        disabled={disabled || pending || !ready}
        onChange={value => { if (value === "en" || value === "pl") void change(value); }}
      />
      <div className={styles.feedback} role="status" aria-live="polite">
        {saved ? <div className={styles.success}>
          <svg viewBox="0 0 32 32" aria-hidden="true">
            <circle cx="16" cy="16" r="14" />
            <path d="m9 16 4.5 4.5L23 11" pathLength="1" />
          </svg>
          <span className={styles.srOnly}>{t("Saved")}</span>
        </div> : null}
      </div>
    </div>
    {error ? <small id={`${id}-error`} role="alert">{t("Could not save language. Please try again.")}</small> : null}
  </>;
  return <>
    <span>{t("Application language")}</span>
    <select className={selectClassName} aria-label={t("Application language")} value={language} disabled={disabled || pending || !ready}
      onChange={event => void change(event.target.value as Language)}>
      <option value="en" lang="en">English</option>
      <option value="pl" lang="pl">Polski</option>
    </select>
    {error ? <small role="alert">{t("Could not save language. Please try again.")}</small> : null}
  </>;
}
