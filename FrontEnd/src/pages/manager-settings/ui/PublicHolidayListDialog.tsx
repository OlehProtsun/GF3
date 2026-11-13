import { useState } from "react";
import { t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { getPolishPublicHolidaysForYear } from "@shared/lib/polishPublicHolidays";
import styles from "./ManagerSettingsPage.module.css";
import { SettingsDialog } from "./SettingsDialog";
import { SettingsSymbol } from "./SettingsSymbol";
import { ChevronLeftIcon, ChevronRightIcon, ExternalLinkIcon, InfoIcon } from "@shared/ui/icons";

type Props = { open: boolean; onClose: () => void; initialYear?: number };

export function PublicHolidayListDialog({ open, onClose, initialYear }: Props) {
  useLanguageRevision();
  // Mount the dialog contents on every opening so its year and focus start fresh.
  return open ? <OpenHolidayListDialog onClose={onClose} initialYear={initialYear} /> : null;
}

function OpenHolidayListDialog({ onClose, initialYear }: Omit<Props, "open">) {
  const [year, setYear] = useState(() => Number.isInteger(initialYear) && initialYear! >= 1 && initialYear! <= 9999
    ? initialYear! : new Date().getFullYear());
  return (
    <SettingsDialog title={t("Public holidays in {0}", year)} icon={<SettingsSymbol kind="calendar" />} onClose={onClose}>
        <div className={styles.yearControls}>
          <button type="button" aria-label={t("Previous year")} disabled={year <= 1} onClick={() => setYear(year - 1)}><ChevronLeftIcon size={17} /></button>
          <label><span className={styles.srOnly}>{t("Year")}</span><input type="number" min={1} max={9999} step={1} value={year}
            onChange={event => { const value = event.target.valueAsNumber; if (Number.isInteger(value) && value >= 1 && value <= 9999) setYear(value); }} /></label>
          <button type="button" aria-label={t("Next year")} disabled={year >= 9999} onClick={() => setYear(year + 1)}><ChevronRightIcon size={17} /></button>
        </div>
        <ol className={styles.holidayList}>
          {getPolishPublicHolidaysForYear(year).map(holiday => <li key={`${holiday.month}-${holiday.day}`}>
            <time dateTime={`${String(year).padStart(4, "0")}-${String(holiday.month).padStart(2, "0")}-${String(holiday.day).padStart(2, "0")}`}>
              {`${String(holiday.day).padStart(2, "0")}.${String(holiday.month).padStart(2, "0")}.${String(year).padStart(4, "0")}`}
            </time><span>{t(holiday.name)}</span>
          </li>)}
        </ol>
        <div className={styles.holidayInfo}>
          <InfoIcon size={18} />
          <div>
            <p>{t("All Sundays are also statutory days off and retain normal weekend styling.")}</p>
            <p>{t("Ordinary Sundays are not individually repeated in this list.")}</p>
          </div>
        </div>
        <a className={styles.legalLink} href="https://eli.gov.pl/eli/DU/2025/296/ogl/pol" target="_blank" rel="noopener noreferrer"><ExternalLinkIcon size={17} />{t("Official legal source")}</a>
    </SettingsDialog>
  );
}
