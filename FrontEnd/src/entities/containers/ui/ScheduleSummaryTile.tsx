import { t } from "@shared/i18n";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CheckIcon, EmployeeIcon, ScheduleIcon, ShopIcon, SwapHistoryIcon } from "@shared/ui/icons";
import type { ContainerGraphSummary } from "../model/statistics";
import styles from "./ScheduleSummaryTile.module.css";

type ScheduleSummaryTileProps = {
  summary: ContainerGraphSummary;
  isMultiOpenEnabled: boolean;
  isSelected: boolean;
  onActivate: () => void;
};

const tones = [styles.toneBlue, styles.toneLavender, styles.tonePeach, styles.toneMint, styles.tonePink, styles.toneSky, styles.toneViolet];
const waves = [
  "M0 64 C62 29 94 92 184 55 S297 -5 400 31 V100 H0 Z",
  "M0 57 C72 100 134 53 215 20 S337 2 400 34 V100 H0 Z",
  "M0 64 C71 86 126 52 196 19 S323 6 400 34 V100 H0 Z",
  "M0 62 C79 40 114 98 220 59 S334 2 400 32 V100 H0 Z",
  "M0 52 C75 80 142 88 229 46 S339 9 400 33 V100 H0 Z",
  "M0 83 C50 8 114 26 202 55 S307 9 400 39 V100 H0 Z",
  "M0 82 C65 -1 122 23 204 53 S329 8 400 35 V100 H0 Z",
];

export function ScheduleSummaryTile({ summary, isMultiOpenEnabled, isSelected, onActivate }: ScheduleSummaryTileProps) {
  const selected = isMultiOpenEnabled && isSelected;
  const isPublic = summary.graph.publicationStatus === "public";
  const toneIndex = Math.abs(summary.graph.id) % 7;
  const fields = [
    { label: t("Shop"), value: summary.shopName, icon: <ShopIcon size={15} /> },
    { label: t("Employees"), value: String(summary.employeeCount), icon: <EmployeeIcon size={15} /> },
    { label: t("Hours"), value: summary.assignedHoursText, icon: <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg> },
    { label: t("Month Year"), value: summary.monthYearLabel, icon: <ScheduleIcon size={15} /> },
    { label: t("Status"), value: isPublic ? t("Public") : t("Private"), className: isPublic ? styles.statusPublic : styles.statusPrivate, icon: <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path fill="currentColor" fillOpacity=".12" d="m12 3 8 3v6c0 4-4 7-8 9-4-2-8-5-8-9V6l8-3Z" /><path d="m8 12 3 3 5-6" /></svg> },
    { label: t("Last Update"), value: formatScheduleLastUpdate(summary.graph.lastUpdatedAtUtc), className: styles.lastUpdate, icon: <SwapHistoryIcon size={15} /> },
  ];
  const chip = (field: typeof fields[number]) => (
    <span className={styles.chip} key={field.label}>
      <span className={styles.chipLabel}><span className={styles.chipIcon} aria-hidden="true">{field.icon}</span>{field.label}</span>
      <span className={[styles.chipValue, field.className].filter(Boolean).join(" ")}>{field.value}</span>
    </span>
  );

  return (
    <button
      type="button"
      data-schedule-graph-id={summary.graph.id}
      className={[styles.tile, tones[toneIndex], selected && styles.selectedTile].filter(Boolean).join(" ")}
      aria-label={isMultiOpenEnabled
        ? t("{0} schedule {1}", isSelected ? "Deselect" : "Select", summary.graph.name)
        : t("Open schedule {0}", summary.graph.name)}
      aria-pressed={isMultiOpenEnabled ? isSelected : undefined}
      onClick={onActivate}
    >
      <span className={styles.ribbon} aria-hidden="true">
        <svg className={styles.waveSvg} aria-hidden="true" viewBox="0 0 400 100" preserveAspectRatio="none">
          <path className={styles.waveReflection} d={waves[(toneIndex + 3) % 7]} transform="translate(0 -17)" />
          <path className={styles.waveFront} d={waves[toneIndex]} />
        </svg>
      </span>
      {isMultiOpenEnabled ? (
        <span data-schedule-selection-indicator aria-hidden="true" className={[styles.selectionIndicator, selected && styles.selectionIndicatorSelected].filter(Boolean).join(" ")}>
          {selected ? <CheckIcon size={14} /> : null}
        </span>
      ) : (
        <span className={`${styles.selectionIndicator} ${styles.decoration}`} aria-hidden="true" data-schedule-decoration>
          <svg width="16" height="20" viewBox="0 0 16 20" fill="currentColor"><circle cx="8" cy="4" r="1.6" /><circle cx="8" cy="10" r="1.6" /><circle cx="8" cy="16" r="1.6" /></svg>
        </span>
      )}
      <span className={styles.body}>
        <span className={styles.title}>{summary.graph.name}</span>
        <span className={styles.metaRow}>{fields.slice(0, 2).map(chip)}</span>
        {fields.slice(2).map(field => <span className={styles.metaRow} key={field.label}>{chip(field)}</span>)}
      </span>
    </button>
  );
}
