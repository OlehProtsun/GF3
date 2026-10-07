import { useEffect, useId, useRef } from "react";
import { createPortal } from "react-dom";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { dateTimeFormat, t } from "@shared/i18n";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CheckIcon, CloseIcon, EmployeeIcon } from "@shared/ui/icons";
import styles from "./EmployeeScheduleSelectDialog.module.css";

type EmployeeScheduleSelectDialogProps = {
  open: boolean;
  schedules: EmployeeSchedule[];
  selectedScheduleId: number | null;
  onSelect: (scheduleId: number) => void;
  onClose: () => void;
};

const periodFormatter = dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" });

export function EmployeeScheduleSelectDialog({ open, schedules, selectedScheduleId, onSelect, onClose }: EmployeeScheduleSelectDialogProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const previousFocus = document.activeElement;
    const dialog = dialogRef.current;
    const buttons = () => Array.from(dialog?.querySelectorAll<HTMLButtonElement>("button") ?? []);
    (dialog?.querySelector<HTMLButtonElement>('[aria-pressed="true"]') ?? buttons()[0])?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
      if (event.key === "Tab") {
        const targets = buttons();
        const first = targets[0];
        const last = targets.at(-1);
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault();
          last?.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first?.focus();
        }
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      if (previousFocus instanceof HTMLElement) previousFocus.focus({ preventScroll: true });
    };
  }, [open, onClose]);

  if (!open) return null;

  return createPortal(
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId}
      onMouseDown={event => { if (event.target === event.currentTarget) { event.preventDefault(); onClose(); } }}>
      <div className={styles.sheet} ref={dialogRef}>
        <span className={styles.handle} aria-hidden="true" />
        <header className={styles.header}>
          <h2 id={titleId}>{t("Select schedule")}</h2>
          <button type="button" className={styles.close} aria-label={t("Close")} onClick={onClose}><CloseIcon size={18} /></button>
        </header>
        <div className={styles.list}>
          {schedules.map(schedule => {
            const selected = schedule.id === selectedScheduleId;
            const updated = formatScheduleLastUpdate(schedule.lastUpdatedAtUtc);
            return (
              <button key={schedule.id} type="button" className={`${styles.row} ${selected ? styles.selected : ""}`}
                aria-pressed={selected} onClick={() => onSelect(schedule.id)}>
                <span className={styles.icon} aria-hidden="true"><EmployeeIcon size={20} /></span>
                <span className={styles.copy}>
                  <strong>{schedule.name}</strong>
                  <span>{periodFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)))}</span>
                  <span className={styles.updated}>{schedule.lastUpdatedAtUtc ? t("Updated {0}", updated) : updated}</span>
                </span>
                <span className={styles.indicator} aria-hidden="true">{selected ? <CheckIcon size={14} /> : null}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>,
    document.body,
  );
}
