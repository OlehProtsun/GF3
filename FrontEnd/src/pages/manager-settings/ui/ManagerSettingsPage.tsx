import { useState } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import { useLanguage } from "@app/providers/LanguageProvider";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { usePublicHolidaySettingsQuery, useUpdatePublicHolidaySettingsMutation } from "@entities/public-holidays/api/queries";
import { getErrorMessage } from "@shared/api/httpClient";
import { languageNames, t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { PageHeader } from "@shared/ui/PageHeader";
import { usePendingRegulationsQuery } from "@entities/regulations/api/queries";
import { PublicHolidayListDialog } from "./PublicHolidayListDialog";
import { LanguageSettingsDialog } from "./LanguageSettingsDialog";
import { SettingsSymbol } from "./SettingsSymbol";
import { ChevronRightIcon } from "@shared/ui/icons";
import styles from "./ManagerSettingsPage.module.css";

export function ManagerSettingsPage() {
  useLanguageRevision();
  const { session } = useAuth();
  const { language, ready } = useLanguage();
  const accountKey = session ? `${session.role}:${session.managerId ?? session.employeeId}` : "guest";
  const pending = usePendingRegulationsQuery(accountKey, Boolean(session));
  const query = usePublicHolidaySettingsQuery(pending.data?.length === 0 && !pending.isError);
  const mutation = useUpdatePublicHolidaySettingsMutation();
  const [open, setOpen] = useState(false);
  const [languageOpen, setLanguageOpen] = useState(false);
  const readOnly = session?.workspaceMode === "phone";

  return (
    <div className={styles.page}>
      <PageHeader title={t("Settings")} backTo={-1} />
      <div className={styles.settingsList}>
        <section className={styles.card} aria-labelledby="public-holiday-heading">
          <div className={styles.cardRow}>
            <span className={`${styles.iconTile} ${styles.calendarTile}`}><SettingsSymbol kind="calendar" /></span>
            <div className={styles.cardCopy}>
              <h2 id="public-holiday-heading" className={styles.cardTitle}>{t("Public holidays")}</h2>
              <p className={styles.description}>{t("Highlight official public holidays in schedules and availability tables.")}</p>
            </div>
            <label className={styles.switch} aria-busy={mutation.isPending}>
              <input type="checkbox" role="switch" aria-label={t("Show public holidays")} checked={query.data?.showPublicHolidays ?? true}
                disabled={readOnly || !query.data || query.isError || query.isFetching || mutation.isPending}
                onChange={event => mutation.mutate(event.target.checked)} />
              <span className={styles.switchTrack} aria-hidden="true"><span className={styles.switchThumb} /></span>
            </label>
          </div>
          <div className={styles.cardDetails}>
            <p className={styles.scopeNote}><SettingsSymbol kind="globe" size={17} /><span>{t("This setting applies to all managers and employees.")}</span></p>
            {query.isError ? <ErrorBanner dismissible={false}>{getErrorMessage(query.error, t("Could not load holiday settings."))}</ErrorBanner> : null}
            {query.isError ? <button className={styles.actionButton} type="button" onClick={() => void query.refetch()}>{t("Retry")}</button> : null}
            {mutation.error ? <ErrorBanner dismissible={false}>{getErrorMessage(mutation.error, t("Could not save holiday settings."))}</ErrorBanner> : null}
            {readOnly ? <p>{t("Switch to PC mode to change this setting.")}</p> : null}
            <div className={styles.cardActions}>
              <button className={styles.actionButton} type="button" onClick={event => { event.currentTarget.focus(); setOpen(true); }}>
                {t("View public holidays")}<span aria-hidden="true">→</span>
              </button>
            </div>
          </div>
        </section>
        <section className={styles.card} aria-labelledby="language-heading">
          <button type="button" className={`${styles.cardRow} ${styles.languageButton}`} disabled={readOnly || !ready}
            aria-labelledby="language-heading current-language" aria-haspopup="dialog" onClick={event => { event.currentTarget.focus(); setLanguageOpen(true); }}>
            <span className={styles.iconTile}><SettingsSymbol kind="globe" /></span>
            <span className={styles.cardCopy}>
              <span id="language-heading" className={styles.cardTitle}>{t("Language")}</span>
              <span className={styles.description}>{t("Choose your preferred application language.")}</span>
            </span>
            <span className={styles.languageValue} id="current-language" lang={language}>{languageNames[language]}</span>
            <ChevronRightIcon className={styles.chevron} size={17} />
          </button>
        </section>
      </div>
      <PublicHolidayListDialog open={open} onClose={() => setOpen(false)} />
      {languageOpen ? <LanguageSettingsDialog readOnly={readOnly} onClose={() => setLanguageOpen(false)} /> : null}
    </div>
  );
}

