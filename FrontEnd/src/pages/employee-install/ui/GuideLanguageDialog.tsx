import { useId, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { languageNames, translate, type Language } from "@shared/i18n";
import { AccountSettingsIcon, ArrowRightIcon, CheckIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./EmployeeInstallPage.module.css";

const options = [
  { id: "pl", label: "Polish", flag: "/guide/flags/poland.svg" },
  { id: "uk", label: "Ukrainian", flag: "/guide/flags/ukraine.svg" },
  { id: "en", label: "English", flag: "/guide/flags/united-kingdom.svg" },
] as const;

type Props = {
  displayLanguage: Language;
  initialLanguage: Language;
  onConfirm: (language: Language) => void;
  onDismiss?: () => void;
};

export function GuideLanguageDialog({ displayLanguage, initialLanguage, onConfirm, onDismiss }: Props) {
  const [selected, setSelected] = useState(initialLanguage);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const id = useId();
  const t = translate.bind(null, displayLanguage);

  useLayoutEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    const bodyStyle = document.body.style;
    const scrollX = window.scrollX;
    const scrollY = window.scrollY;
    const lockStyles = {
      "--guide-language-scroll-top": `${-scrollY}px`,
      "--guide-language-scroll-left": `${-scrollX}px`,
      "--guide-language-scrollbar-gap": `${window.innerWidth - document.documentElement.clientWidth}px`,
    };
    const previous = Object.keys(lockStyles).map(name =>
      [name, bodyStyle.getPropertyValue(name), bodyStyle.getPropertyPriority(name)] as const);
    for (const [name, value] of Object.entries(lockStyles)) bodyStyle.setProperty(name, value);
    dialog.showModal();
    dialog.querySelector<HTMLInputElement>("input:checked")?.focus({ preventScroll: true });
    return () => {
      if (dialog.open) dialog.close();
      for (const [name, value, priority] of previous) {
        if (value) bodyStyle.setProperty(name, value, priority);
        else bodyStyle.removeProperty(name);
      }
      window.scrollTo({ left: scrollX, top: scrollY, behavior: "instant" });
    };
  }, []);

  return createPortal(
    <dialog ref={dialogRef} className={styles.languageDialog} lang={displayLanguage}
      aria-labelledby={`${id}-title`} aria-describedby={`${id}-description`}
      onCancel={event => { event.preventDefault(); onDismiss?.(); }}>
      <form className={styles.languageForm} onSubmit={event => { event.preventDefault(); onConfirm(selected); }}>
        {onDismiss && (
          <button type="button" className={styles.dismissLanguage} aria-label={t("Close")} onClick={onDismiss}>
            <CloseIcon size={19} />
          </button>
        )}
        <header className={styles.languageHeader}>
          <span className={styles.languageHeaderIcon} aria-hidden="true">
            <AccountSettingsIcon name="globe" size={22} />
          </span>
          <h1 id={`${id}-title`}>{t("Choose your language")}</h1>
          <p id={`${id}-description`}>{t("Select the language for your GF App Guide.")}</p>
        </header>
        <fieldset className={styles.languageOptions} aria-labelledby={`${id}-title`}>
          {options.map(({ id: language, label, flag }) => {
            const nativeName = languageNames[language];
            const description = t(label);
            return (
              <label key={language} className={styles.languageOption}>
                <input type="radio" name={id} value={language} checked={selected === language}
                  onChange={() => setSelected(language)} />
                <span className={styles.languageOptionFlag} aria-hidden="true">
                  <img src={flag} width={24} height={16} alt="" />
                </span>
                <span className={styles.languageOptionText}>
                  <strong lang={language}>{nativeName}</strong>
                  {description !== nativeName && <small>{description}</small>}
                </span>
                <span className={styles.languageCheck} aria-hidden="true"><CheckIcon size={15} /></span>
              </label>
            );
          })}
        </fieldset>
        <button type="submit" className={styles.languageContinue}>
          {t("Continue")} <ArrowRightIcon size={18} />
        </button>
      </form>
    </dialog>,
    document.body,
  );
}
