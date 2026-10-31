import { enumerateManualShiftOptions } from "../model/manualShiftIntervals";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useRef, useState, type ReactNode } from "react";
import type { Employee } from "@entities/employees";
import type { ShiftSwap } from "@entities/shift-swaps";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { ChevronRightIcon, CloseIcon, PlusIcon, InformationIcon, ShareIcon, SwapHistoryIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ContainerGraphManualColumnsCard.module.css";

type ManualColumnSummary = {
  source?: "generated-unfilled";
  columnId: number;
  label: string;
  cells: Record<string, string>;
};

export type ManualColumnShiftPublicationInput = {
  manualColumnId: number;
  dayOfMonth: number;
  fromTime: string;
  toTime: string;
  targetEmployeeId?: number | null;
  allowPartialAcceptance: boolean;
};

export type PendingManualColumnShiftPublication = ManualColumnShiftPublicationInput & {
  clientId: string;
};

type ContainerGraphManualColumnsCardProps = {
  highlightColor: string;
  highlightSettingDisabled: boolean;
  onOpenHighlightColorPicker: () => void;
  columns: ManualColumnSummary[];
  allowSwap?: boolean;
  isSchedulePublished?: boolean;
  year: number;
  month: number;
  employees: Employee[];
  isPublishingShift?: boolean;
  isCancellingPublishedShift?: boolean;
  publishError?: string | null;
  publishedShifts?: ShiftSwap[];
  pendingPublishedShifts?: PendingManualColumnShiftPublication[];
  headerRightSlot?: ReactNode;
  onAddColumn: () => void;
  onDeleteColumn: (columnId: number) => void;
  onPublishShift: (input: ManualColumnShiftPublicationInput) => void;
  onCancelPublishedShift?: (shiftId: number) => void;
  onCancelPendingPublishedShift?: (clientId: string) => void;
};

type ManualColumnShiftOption = {
  id: string;
  manualColumnId: number;
  columnLabel: string;
  dayOfMonth: number;
  fromTime: string;
  toTime: string;
  rawValue: string;
};

const dayFormatter = dateTimeFormat("en-GB", { day: "2-digit", month: "short", timeZone: "UTC" });

function formatDay(year: number, month: number, dayOfMonth: number) {
  return dayFormatter.format(new Date(Date.UTC(year, month - 1, dayOfMonth)));
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

function getEmployeeLabel(employee: Employee) {
  const fullName = `${employee.firstName} ${employee.lastName}`.trim();
  return fullName || t("Employee #{0}", employee.id);
}

export function ContainerGraphManualColumnsCard({
  highlightColor, highlightSettingDisabled, onOpenHighlightColorPicker,
  columns, allowSwap = true, isSchedulePublished = true, year, month, employees,
  isPublishingShift = false, isCancellingPublishedShift = false, publishError = null,
  publishedShifts = [], pendingPublishedShifts = [], headerRightSlot,
  onAddColumn, onDeleteColumn, onPublishShift, onCancelPublishedShift, onCancelPendingPublishedShift,
}: ContainerGraphManualColumnsCardProps) {
  useLanguageRevision();
  const groups = columns.map((column, index) => ({ columnLabel: column.label.trim() || t("Manual column {0}", index + 1) }));
  const openOffers = publishedShifts.filter(shift => shift.isManagerCreated && shift.status === "open" && shift.manualColumnId != null);
  const options = columns.flatMap((column, columnIndex) => Object.entries(column.cells).flatMap(([day, rawValue]) => {
    const dayOfMonth = Number(day);
    if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > getDaysInMonth(year, month)) return [];
    const reserved = [...openOffers, ...pendingPublishedShifts].filter(shift => shift.manualColumnId === column.columnId && shift.dayOfMonth === dayOfMonth)
      .map(shift => ({ fromTime: shift.fromTime.slice(0, 5), toTime: shift.toTime.slice(0, 5) }));
    return enumerateManualShiftOptions(rawValue, reserved).map(period => ({ ...period, manualColumnId: column.columnId, dayOfMonth,
      columnIndex, columnLabel: groups[columnIndex].columnLabel, rawValue,
      id: `${column.columnId}:${day}:${period.fromTime}:${period.toTime}:${period.occurrence}:${period.combined}` } satisfies ManualColumnShiftOption & { columnIndex: number; combined: boolean; occurrence: number }));
  })).sort((a, b) => a.dayOfMonth - b.dayOfMonth || a.fromTime.localeCompare(b.fromTime) || a.columnIndex - b.columnIndex);
  const [selectedId, setSelectedId] = useState("");
  const [allowPartialAcceptance, setAllowPartialAcceptance] = useState(false);
  const [targetMode, setTargetMode] = useState<"public" | "private">("public");
  const [targetEmployeeId, setTargetEmployeeId] = useState<number | null>(null);
  const [offeredExpanded, setOfferedExpanded] = useState(false);
  const previousPendingIds = useRef(new Set(pendingPublishedShifts.map(shift => shift.clientId)));
  useEffect(() => {
    if (pendingPublishedShifts.some(shift => !previousPendingIds.current.has(shift.clientId))) setOfferedExpanded(true);
    previousPendingIds.current = new Set(pendingPublishedShifts.map(shift => shift.clientId));
  }, [pendingPublishedShifts]);
  const selected = options.find(option => option.id === selectedId) ?? options[0] ?? null;
  const sortedEmployees = [...employees].sort((a, b) => getEmployeeLabel(a).localeCompare(getEmployeeLabel(b)));
  const employeeNameById = new Map(employees.map(employee => [employee.id, getEmployeeLabel(employee)]));
  const employeeOptions: SearchableSelectOption[] = sortedEmployees.map(employee => ({
    value: String(employee.id), label: getEmployeeLabel(employee), keywords: `${employee.firstName} ${employee.lastName} ${employee.email ?? ""}`,
  }));
  const selectedEmployeeId = sortedEmployees.some(employee => employee.id === targetEmployeeId) ? targetEmployeeId : null;
  const canPublish = allowSwap && isSchedulePublished && !isPublishingShift && selected !== null &&
    (targetMode === "public" || selectedEmployeeId !== null);
  const offerCount = openOffers.length + pendingPublishedShifts.length;
  return (
    <CardSection className={styles.card}
      title={<span className={styles.title}>{t("Manual shifts")} <span className={styles.badge}>{options.length}</span></span>}
      icon={<span className={styles.calendarIcon}><InformationIcon size={18} /></span>}
      headerRightSlot={headerRightSlot}>
      <div className={styles.layout}>
        <div className={styles.colorSetting}>
          <span>{t("Accepted shift color")}</span>
          <button type="button" className={styles.colorButton} aria-label={t("Accepted manual shift highlight color")}
            disabled={highlightSettingDisabled} onClick={onOpenHighlightColorPicker}>
            <span style={{ backgroundColor: highlightColor }} aria-hidden="true" />
          </button>
        </div>
        <p className={styles.muted}>{t("Select a shift and publish it for employees to claim.")}</p>
        <section className={styles.panel} aria-label={t("Columns")}>
          <div className={styles.heading}><span>{t("Columns")} <span className={styles.badge}>{columns.length}</span></span>
            <IosButton className={styles.addButton} label={t("Add")} icon={<PlusIcon size={16} />} size="compact" variant="secondary" onClick={onAddColumn} />
          </div>
          {columns.length === 0 ? <p className={styles.muted}>{t("No manual columns yet.")}</p> : (
            <div className={styles.list}>{columns.map((column, index) => (
              <div key={column.columnId} className={styles.column}>
                <span className={styles.columnName} title={t("Edit header and values in the grid")}>{groups[index].columnLabel}</span>
                <span className={styles.badge}>{options.filter(option => option.manualColumnId === column.columnId).length}</span>
                {column.source === "generated-unfilled" ? <span className={styles.autoBadge}>{t("Auto")}</span> : null}
                <button type="button" className={styles.quietButton} aria-label={t("Delete {0}", groups[index].columnLabel)} onClick={() => onDeleteColumn(column.columnId)}><CloseIcon size={14} /></button>
              </div>
            ))}</div>
          )}
        </section>
        <section className={styles.panel}>
          <div className={styles.heading}><span>{t("Available shifts")} <span className={styles.badge}>{options.length}</span></span></div>
          {options.length === 0 ? <p className={styles.muted}>{t("No available shifts. Enter a time range in the matrix.")}</p> : (
            <div className={styles.shiftList} role="region" aria-label={t("Available shifts")} tabIndex={0}>{options.map(option => (
              <button type="button" key={option.id} className={styles.shift} aria-pressed={selected?.id === option.id} onClick={() => setSelectedId(option.id)}>
                <span aria-hidden="true" className={styles.indicator} />
                <span className={styles.shiftText}><strong>{`${formatDay(year, month, option.dayOfMonth)} · ${option.fromTime}–${option.toTime}`}</strong><small>{option.columnLabel}{option.combined ? ` · ${t("Combined coverage")}` : ""}</small></span>
                <ChevronRightIcon size={14} className={styles.rowChevron} />
              </button>
            ))}</div>
          )}
          {!allowSwap ? <p className={styles.notice}>{t("Swaps are not allowed for this schedule. Enable Allow swap in Publication first.")}</p> : null}
          {!isSchedulePublished ? <p className={styles.notice}>{t("Publish the schedule first.")}</p> : null}
          <div className={styles.segments}>
            <button type="button" aria-pressed={targetMode === "public"} onClick={() => setTargetMode("public")}>{t("Everyone")}</button>
            <button type="button" aria-pressed={targetMode === "private"} onClick={() => setTargetMode("private")}>{t("Specific employee")}</button>
          </div>
          {targetMode === "private" ? <SearchableSelect id="manual-shift-target-employee-select" value={selectedEmployeeId === null ? "" : String(selectedEmployeeId)}
            options={employeeOptions} placeholder={employeeOptions.length ? t("Select employee...") : t("No employees available")}
            dropdownTitle={t("Employee list")} searchPlaceholder={t("Search employee...")} emptyMessage={t("No employees match your search.")}
            ariaLabel={t("employee list")} onChange={value => setTargetEmployeeId(value ? Number(value) : null)} /> : null}
          <div className={styles.segments} role="group" aria-label={t("Acceptance mode")}>
            <button type="button" aria-pressed={!allowPartialAcceptance} onClick={() => setAllowPartialAcceptance(false)}>{t("Only the exact shift")}</button>
            <button type="button" aria-pressed={allowPartialAcceptance} onClick={() => setAllowPartialAcceptance(true)}>{t("Allow custom time")}</button>
          </div>
          <p className={styles.muted}>{t("Unclaimed time will remain available as a new offer.")}</p>
          {publishError ? <p className={styles.error} role="alert">{publishError}</p> : null}
          <IosButton className={styles.publishButton} icon={<ShareIcon size={18} />} label={isPublishingShift ? t("Publishing...") : t("Publish shift")} disabled={!canPublish} onClick={() => {
            if (!canPublish || !selected) return;
            onPublishShift({ manualColumnId: selected.manualColumnId, dayOfMonth: selected.dayOfMonth,
              fromTime: selected.fromTime, toTime: selected.toTime, allowPartialAcceptance, targetEmployeeId: targetMode === "private" ? selectedEmployeeId : null });
          }} />
        </section>
        <section className={styles.offeredPanel} aria-label={t("Offered")}>
          <button type="button" className={styles.offeredToggle} aria-expanded={offeredExpanded} onClick={() => setOfferedExpanded(value => !value)}><span className={styles.offeredTitle}><SwapHistoryIcon size={18} />{t("Offered")} ({offerCount})</span><ChevronRightIcon size={16} className={styles.offeredChevron} /></button>
          {offeredExpanded ? <div className={styles.list}>
            {offerCount === 0 ? <p className={styles.muted}>{t("No offered shifts.")}</p> : null}
            {pendingPublishedShifts.map(shift => <div className={styles.offer} key={shift.clientId}>
              <span className={styles.shiftText}><strong>{`${formatDay(year, month, shift.dayOfMonth)} · ${shift.fromTime}–${shift.toTime}`}</strong>
                <small>{t(shift.allowPartialAcceptance ? "Allow custom time" : "Only the exact shift")} · {t("Pending save")} · {shift.targetEmployeeId ? t("Private") : t("Public")}{shift.targetEmployeeId ? ` · ${employeeNameById.get(shift.targetEmployeeId) ?? t("Selected employee")}` : ""}</small></span>
              <button type="button" className={styles.quietButton} aria-label={t("Remove shift on {0}", formatDay(year, month, shift.dayOfMonth))} disabled={isPublishingShift || !onCancelPendingPublishedShift} onClick={() => onCancelPendingPublishedShift?.(shift.clientId)}>{t("Remove")}</button>
            </div>)}
            {openOffers.map(shift => <div className={styles.offer} key={shift.id}>
              <span className={styles.shiftText}><strong>{`${formatDay(shift.year, shift.month, shift.dayOfMonth)} · ${shift.fromTime.slice(0, 5)}–${shift.toTime.slice(0, 5)}`}</strong>
                <small>{t(shift.allowPartialAcceptance ? "Allow custom time" : "Only the exact shift")} · {shift.visibility === "private" ? t("Private") : t("Public")}{shift.targetEmployeeName ? ` · ${shift.targetEmployeeName}` : ""}</small></span>
              <button type="button" className={styles.quietButton} aria-label={t("Cancel shift on {0}", formatDay(shift.year, shift.month, shift.dayOfMonth))} disabled={isPublishingShift || isCancellingPublishedShift || !onCancelPublishedShift} onClick={() => onCancelPublishedShift?.(shift.id)}>{isCancellingPublishedShift ? t("Cancelling...") : t("Cancel")}</button>
            </div>)}
          </div> : null}
        </section>
      </div>
    </CardSection>
  );
}
