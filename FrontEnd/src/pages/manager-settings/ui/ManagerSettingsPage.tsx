import { useEmploymentContractSettingsQuery, useUpdateEmploymentContractSettingsMutation } from "@entities/employment-contracts";
import type { EmploymentContractSettings } from "@entities/employment-contracts";
import { usePriorityGenerationSettingsQuery, useUpdatePriorityGenerationSettingsMutation } from "@entities/priority-generation/api/queries";
import { NumberStepperInput } from "@shared/ui/components/NumberStepperInput";
import { useRef, useState } from "react";
import type { PriorityGenerationSettings } from "@entities/priority-generation/model/types";
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
  const priorityQuery = usePriorityGenerationSettingsQuery(pending.data?.length === 0 && !pending.isError);
  const priorityMutation = useUpdatePriorityGenerationSettingsMutation();
  const priorityDisabled = session?.workspaceMode === "phone" || !priorityQuery.data || priorityQuery.isError || priorityQuery.isFetching || priorityMutation.isPending;
  const prioritySaving = useRef(false);
  const savePriority = (settings: PriorityGenerationSettings) => {
    if (priorityDisabled || prioritySaving.current) return;
    prioritySaving.current = true;
    priorityMutation.mutate(settings, {
      onSuccess: () => { prioritySaving.current = false; },
      onError: () => { prioritySaving.current = false; },
    });
  };
  const contractsQuery = useEmploymentContractSettingsQuery(pending.data?.length === 0 && !pending.isError);
  const contractsMutation = useUpdateEmploymentContractSettingsMutation();
  const contractRules: { key: keyof EmploymentContractSettings; label: string; description: string }[] = [
    { key: "restrictSwapsToEightHours", label: t("Whole eight-hour shifts only"), description: t("Give, receive and exchange only complete eight-hour shifts.") },
    { key: "requirePickupApproval", label: t("Manager approval for pickups"), description: t("Requested pickups change the schedule only after manager approval.") },
    { key: "requireBelowMinimumGiveApproval", label: t("Protect the monthly minimum"), description: t("Giving below the shop/month minimum requires a manager exception. Configure MinHoursMonth for the graph/shop.") },
    { key: "automaticWeekdayAvailability", label: t("Automatic weekday availability"), description: t("Weekdays available, weekends unavailable; employee editing is disabled.") },
  ];
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
        <section className={styles.card} aria-labelledby="priority-heading">
          <div className={styles.cardRow}>
            <span className={styles.iconTile}><SettingsSymbol kind="calendar" /></span>
            <div className={styles.cardCopy}>
              <h2 id="priority-heading" className={styles.cardTitle}>{t("Priority")}</h2>
              <p className={styles.description}>{t("Priority employees are scheduled first; positive availability may extend to the exact configured duration within graph hours and all other hard restrictions.")}</p>
            </div>
          </div>
          <div className={`${styles.cardDetails} ${styles.prioritySettings}`}>
            <div className={styles.prioritySettingRow}>
              <label htmlFor="priority-shift-hours">{t("Priority shift duration (hours)")}</label>
              <NumberStepperInput key={`${priorityQuery.data?.shiftHours ?? 8}:${priorityMutation.isPending}`} id="priority-shift-hours" ariaLabel={t("Priority shift duration")}
                value={priorityQuery.data?.shiftHours ?? 8} min={1} max={12} disabled={priorityDisabled}
                onChange={shiftHours => savePriority({ shiftHours, weekendsOff: priorityQuery.data?.weekendsOff ?? true,
                  respectAvailability: priorityQuery.data?.respectAvailability ?? true })} />
            </div>
            <div className={styles.prioritySettingRow}>
              <span id="priority-weekends-label">{t("Weekends off for priority employees")}</span>
              <label className={styles.switch} aria-busy={priorityMutation.isPending}>
                <input type="checkbox" role="switch" aria-labelledby="priority-weekends-label" checked={priorityQuery.data?.weekendsOff ?? true}
                  disabled={priorityDisabled} onChange={event => savePriority({ shiftHours: priorityQuery.data?.shiftHours ?? 8,
                    weekendsOff: event.target.checked, respectAvailability: priorityQuery.data?.respectAvailability ?? true })} />
                <span className={styles.switchTrack} aria-hidden="true"><span className={styles.switchThumb} /></span>
              </label>
            </div>
            <div className={styles.prioritySettingRow}>
              <label htmlFor="priority-respect-availability">{t("Respect priority employee availability")}</label>
              <label className={styles.switch} aria-busy={priorityMutation.isPending}>
                <input id="priority-respect-availability" type="checkbox" role="switch"
                  aria-label={t("Respect priority employee availability")} aria-describedby="priority-availability-description"
                  checked={priorityQuery.data?.respectAvailability ?? true} disabled={priorityDisabled}
                  onChange={event => savePriority({ shiftHours: priorityQuery.data?.shiftHours ?? 8,
                    weekendsOff: priorityQuery.data?.weekendsOff ?? true, respectAvailability: event.target.checked })} />
                <span className={styles.switchTrack} aria-hidden="true"><span className={styles.switchThumb} /></span>
              </label>
            </div>
            <p id="priority-availability-description" className={styles.description}>{t("When off, priority employees can work Monday–Friday across schedule hours, while saved work in other schedules remains protected. Weekends stay off even when the separate weekends setting is off.")}</p>
            {priorityQuery.isError ? <ErrorBanner dismissible={false}>{getErrorMessage(priorityQuery.error, t("Could not load priority settings."))}</ErrorBanner> : null}
            {priorityQuery.isError ? <button className={styles.actionButton} type="button" onClick={() => priorityQuery.refetch()}>{t("Retry")}</button> : null}
            {priorityMutation.error ? <ErrorBanner dismissible={false}>{getErrorMessage(priorityMutation.error, t("Could not save priority settings."))}</ErrorBanner> : null}
            {readOnly ? <p>{t("Switch to PC mode to change this setting.")}</p> : null}
          </div>
        </section>
        <section className={styles.card} aria-labelledby="employment-contract-heading">
          <h2 id="employment-contract-heading" className={styles.cardTitle}>{t("Employment contracts")}</h2>
          <p className={styles.description}>{t("These global rules apply only to Umowa o pracę employees.")}</p>
          <div className={styles.employmentSettings}>
            {contractRules.map(rule => <div key={rule.key} className={styles.prioritySettingRow}>
              <div><label htmlFor={`contract-${rule.key}`}>{rule.label}</label><p className={styles.description}>{rule.description}</p></div>
              <label className={styles.switch} aria-busy={contractsMutation.isPending}>
                <input id={`contract-${rule.key}`} type="checkbox" role="switch" checked={contractsQuery.data?.[rule.key] ?? true}
                  disabled={readOnly || !contractsQuery.data || contractsQuery.isError || contractsQuery.isFetching || contractsMutation.isPending}
                  onChange={event => { if (contractsQuery.data) contractsMutation.mutate({ ...contractsQuery.data, [rule.key]: event.target.checked }); }} />
                <span className={styles.switchTrack} aria-hidden="true"><span className={styles.switchThumb} /></span>
              </label>
            </div>)}
          </div>
          {contractsQuery.isError ? <><ErrorBanner dismissible={false}>{getErrorMessage(contractsQuery.error, t("Could not load contract settings."))}</ErrorBanner>
            <button type="button" className={styles.actionButton} onClick={() => contractsQuery.refetch()}>{t("Retry")}</button></> : null}
          {contractsMutation.error ? <ErrorBanner dismissible={false}>{getErrorMessage(contractsMutation.error, t("Could not save contract settings."))}</ErrorBanner> : null}
          {readOnly ? <p>{t("Switch to PC mode to change this setting.")}</p> : null}
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

