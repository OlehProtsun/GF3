import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import type { Employee } from "@entities/employees";
import type { ShiftSwap } from "@entities/shift-swaps";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { CloseIcon, InformationIcon, PlusIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ContainerGraphManualColumnsCard.module.css";

type ManualColumnSummary = {
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
};

export type PendingManualColumnShiftPublication = ManualColumnShiftPublicationInput & {
  clientId: string;
};

type ContainerGraphManualColumnsCardProps = {
  columns: ManualColumnSummary[];
  allowSwap?: boolean;
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

type ManualColumnShiftGroup = {
  columnId: number;
  columnLabel: string;
  cells: ManualColumnShiftCell[];
  shiftsCount: number;
};

type ManualColumnShiftCell = {
  dayOfMonth: number;
  rawValue: string;
  option: ManualColumnShiftOption | null;
};

const timeRangePattern = /(\d{1,2}):(\d{2})\s*(?:-|\u2013|\u2014)\s*(\d{1,2}):(\d{2})/;

const dayFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const weekdayFormatter = dateTimeFormat("en-GB", {
  weekday: "short",
  timeZone: "UTC",
});

function normalizeTime(hourValue: string, minuteValue: string) {
  const hour = Number(hourValue);
  const minute = Number(minuteValue);
  if (!Number.isInteger(hour) || !Number.isInteger(minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
    return null;
  }

  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function parseManualShiftTime(value: string) {
  const match = value.match(timeRangePattern);
  if (!match) {
    return null;
  }

  const fromTime = normalizeTime(match[1], match[2]);
  const toTime = normalizeTime(match[3], match[4]);
  if (!fromTime || !toTime || toTime <= fromTime) {
    return null;
  }

  return { fromTime, toTime };
}

function formatDay(year: number, month: number, dayOfMonth: number) {
  return dayFormatter.format(new Date(Date.UTC(year, month - 1, dayOfMonth)));
}

function formatWeekday(year: number, month: number, dayOfMonth: number) {
  return weekdayFormatter.format(new Date(Date.UTC(year, month - 1, dayOfMonth))).toUpperCase();
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

function isWeekendDay(year: number, month: number, dayOfMonth: number) {
  const day = new Date(Date.UTC(year, month - 1, dayOfMonth)).getUTCDay();
  return day === 0 || day === 6;
}

function formatPublishedShiftTime(value: string) {
  return value.slice(0, 5);
}

function formatPublishedShiftHours(value: number) {
  const normalized = Math.abs(value) < 0.05 ? 0 : value;
  return `${Number.isInteger(normalized) ? normalized.toFixed(0) : normalized.toFixed(1)}h`;
}

function getTimeRangeDurationHours(fromTime: string, toTime: string) {
  const fromMatch = fromTime.match(/^(\d{1,2}):(\d{2})/);
  const toMatch = toTime.match(/^(\d{1,2}):(\d{2})/);
  if (!fromMatch || !toMatch) {
    return 0;
  }

  const fromMinutes = Number(fromMatch[1]) * 60 + Number(fromMatch[2]);
  const toMinutes = Number(toMatch[1]) * 60 + Number(toMatch[2]);

  return toMinutes > fromMinutes ? (toMinutes - fromMinutes) / 60 : 0;
}

function getEmployeeLabel(employee: Employee) {
  const fullName = `${employee.firstName} ${employee.lastName}`.trim();
  return fullName || t("Employee #{0}", employee.id);
}

function buildManualShiftGroups(columns: ManualColumnSummary[], year: number, month: number): ManualColumnShiftGroup[] {
  const dayNumbers = Array.from({ length: getDaysInMonth(year, month) }, (_, index) => index + 1);

  return columns.map((column, index) => {
    const columnLabel = column.label.trim() || t("Manual column {0}", index + 1);
    const cells = dayNumbers.map(dayOfMonth => {
      const rawValue = column.cells[String(dayOfMonth)]?.trim() || "";
      const timeRange = parseManualShiftTime(rawValue);
      const option = timeRange
        ? {
          id: `${column.columnId}:${dayOfMonth}:${timeRange.fromTime}:${timeRange.toTime}`,
          manualColumnId: column.columnId,
          columnLabel,
          dayOfMonth,
          fromTime: timeRange.fromTime,
          toTime: timeRange.toTime,
          rawValue,
        } satisfies ManualColumnShiftOption
        : null;

      return {
        dayOfMonth,
        rawValue,
        option,
      };
    });

    return {
      columnId: column.columnId,
      columnLabel,
      cells,
      shiftsCount: cells.filter(cell => cell.option !== null).length,
    };
  });
}

function ManualShiftPickerDialog({
  groups,
  selectedShiftId,
  year,
  month,
  onSelect,
  onClose,
}: {
  groups: ManualColumnShiftGroup[];
  selectedShiftId: string;
  year: number;
  month: number;
  onSelect: (shiftId: string) => void;
  onClose: () => void;
}) {
  const dayNumbers = useMemo(() => Array.from({ length: getDaysInMonth(year, month) }, (_, index) => index + 1), [month, year]);
  const matrixWidth = 65 + Math.max(1, groups.length) * 135;

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose]);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, []);

  const dialog = (
    <div
      className={styles.shiftDialogOverlay}
      role="presentation"
      onMouseDown={event => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <div className={styles.shiftDialog} role="dialog" aria-modal="true" aria-label={t("Choose manual shift")}>
        <div className={styles.shiftDialogHeader}>
          <div>
            <span>{`${String(month).padStart(2, "0")}.${year}`}</span>
            <strong>{t("Manual Columns")}</strong>
          </div>

          <button type="button" className={styles.shiftDialogClose} aria-label={t("Close")} onClick={onClose}>
            <CloseIcon size={16} />
          </button>
        </div>

        <div className={styles.shiftMatrixShell}>
          <table
            className={styles.shiftMatrixTable}
            style={{ width: `${matrixWidth}px`, minWidth: `${matrixWidth}px` }}
          >
            <colgroup>
              <col className={styles.shiftMatrixDayColumn} />
              {groups.map(group => <col key={group.columnId} className={styles.shiftMatrixValueColumn} />)}
            </colgroup>
            <thead>
              <tr>
                <th className={styles.shiftMatrixDayHeader}>{t("Day")}</th>
                {groups.map(group => (
                  <th key={group.columnId} className={styles.shiftMatrixHeader}>
                    <span>{group.columnLabel}</span>
                    <small>{group.shiftsCount}</small>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {dayNumbers.map(dayOfMonth => (
                <tr key={dayOfMonth} className={isWeekendDay(year, month, dayOfMonth) ? styles.shiftMatrixWeekendRow : undefined}>
                  <th className={styles.shiftMatrixDayCell}>
                    <span>{dayOfMonth}</span>
                    <small>{formatWeekday(year, month, dayOfMonth)}</small>
                  </th>
                  {groups.map(group => {
                    const cell = group.cells[dayOfMonth - 1];
                    const option = cell.option;
                    const isSelected = option?.id === selectedShiftId;

                    return (
                      <td
                        key={`${group.columnId}:${dayOfMonth}`}
                        className={[
                          styles.shiftMatrixCell,
                          option ? styles.shiftMatrixSelectableCell : styles.shiftMatrixMutedCell,
                          isSelected ? styles.shiftMatrixSelectedCell : "",
                        ].filter(Boolean).join(" ")}
                      >
                        {option ? (
                          <button
                            type="button"
                            className={styles.shiftMatrixCellButton}
                            aria-pressed={isSelected}
                            onClick={() => onSelect(option.id)}
                          >
                            {cell.rawValue}
                          </button>
                        ) : (
                          <span className={styles.shiftMatrixCellValue}>{cell.rawValue || "-"}</span>
                        )}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );

  return createPortal(dialog, document.body);
}

export function ContainerGraphManualColumnsCard({
  columns,
  allowSwap = true,
  year,
  month,
  employees,
  isPublishingShift = false,
  isCancellingPublishedShift = false,
  publishError = null,
  publishedShifts = [],
  pendingPublishedShifts = [],
  headerRightSlot,
  onAddColumn,
  onDeleteColumn,
  onPublishShift,
  onCancelPublishedShift,
  onCancelPendingPublishedShift,
}: ContainerGraphManualColumnsCardProps) {
  const columnsCountLabel = t("{0} column{1}", columns.length, columns.length === 1 ? "" : "s");
  const shiftGroups = useMemo(() => buildManualShiftGroups(columns, year, month), [columns, month, year]);
  const sortedEmployees = useMemo(
    () => [...employees].sort((left, right) => getEmployeeLabel(left).localeCompare(getEmployeeLabel(right))),
    [employees],
  );
  const employeeOptions = useMemo<SearchableSelectOption[]>(
    () => sortedEmployees.map(employee => {
      const label = getEmployeeLabel(employee);

      return {
        value: String(employee.id),
        label,
        hint: t("Employee ID: {0}", employee.id),
        keywords: [
          String(employee.id),
          employee.firstName,
          employee.lastName,
          employee.email ?? "",
          employee.phone ?? "",
          label,
        ].join(" "),
      };
    }),
    [sortedEmployees],
  );
  const shiftOptions = useMemo(() => shiftGroups.flatMap(group =>
    group.cells.flatMap(cell => (cell.option ? [cell.option] : [])),
  ).sort((left, right) =>
    left.dayOfMonth - right.dayOfMonth ||
    left.fromTime.localeCompare(right.fromTime) ||
    left.columnLabel.localeCompare(right.columnLabel)), [shiftGroups]);
  const [selectedShiftId, setSelectedShiftId] = useState("");
  const [isShiftDialogOpen, setIsShiftDialogOpen] = useState(false);
  const [targetMode, setTargetMode] = useState<"public" | "private">("public");
  const [targetEmployeeId, setTargetEmployeeId] = useState<number | null>(null);
  const selectedShift = shiftOptions.find(option => option.id === selectedShiftId) ?? shiftOptions[0] ?? null;
  const selectedEmployeeId = targetEmployeeId ?? sortedEmployees[0]?.id ?? null;
  const employeeNameById = useMemo(
    () => new Map(employees.map(employee => [employee.id, getEmployeeLabel(employee)] as const)),
    [employees],
  );
  const offerCount = publishedShifts.length + pendingPublishedShifts.length;

  useEffect(() => {
    if (shiftOptions.length === 0) {
      setSelectedShiftId("");
      return;
    }

    if (!shiftOptions.some(option => option.id === selectedShiftId)) {
      setSelectedShiftId(shiftOptions[0].id);
    }
  }, [selectedShiftId, shiftOptions]);

  const canPublishShift =
    allowSwap &&
    Boolean(selectedShift) &&
    !isPublishingShift &&
    (targetMode === "public" || selectedEmployeeId !== null);

  return (
    <CardSection
      className={styles.card}
      title={(
        <span className={styles.titleWrap}>
          <span>{t("Manual Columns")}</span>
          <span className={styles.titleMeta}>{columnsCountLabel}</span>
        </span>
      )}
      icon={<InformationIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <p className={styles.description}>
          {t("Add extra free-form columns and edit their header and cells directly inside Schedule Matrix.")}</p>

        {columns.length === 0 ? (
          <div className={styles.emptyState}>{t("No manual columns yet. Add one when you need an extra editable column.")}</div>
        ) : (
          <div className={styles.list}>
            {columns.map((column, index) => (
              <div key={column.columnId} className={styles.item}>
                <div className={styles.itemText}>
                  <span className={styles.itemTitle}>{column.label.trim() || t("Untitled column {0}", index + 1)}</span>
                  <span className={styles.itemHint}>{t("Edit header and values in the grid")}</span>
                </div>

                <button
                  type="button"
                  className={styles.deleteButton}
                  aria-label={t("Delete {0}", column.label.trim() || `manual column ${index + 1}`)}
                  onClick={() => onDeleteColumn(column.columnId)}
                >
                  <CloseIcon size={14} />
                </button>
              </div>
            ))}
          </div>
        )}

        <div className={styles.actions}>
          <IosButton label={t("Add Column")} icon={<PlusIcon size={16} />} onClick={onAddColumn} />
        </div>

        <div className={styles.publishBox}>
          <div className={styles.publishHeader}>
            <div>
              <span>{t("Publish manual shift")}</span>
              <strong>{t("Select one manual cell and publish it to Swap.")}</strong>
            </div>
            <span className={styles.publishCount}>{shiftOptions.length}</span>
          </div>

          {!allowSwap ? (
            <div className={styles.swapDisabledNotice}>{t("Swaps are not allowed for this schedule. Enable Allow swap in Publication first.")}</div>
          ) : shiftOptions.length === 0 ? (
            <div className={styles.emptyState}>{t("Write a time range like 09:00-15:00 in a manual column cell first.")}</div>
          ) : (
            <div className={styles.publishForm}>
              <div className={styles.field}>
                <span>{t("Shift")}</span>
                <button
                  type="button"
                  className={styles.shiftPickerButton}
                  onClick={() => setIsShiftDialogOpen(true)}
                >
                  <span className={styles.shiftPickerContent}>
                    {selectedShift ? (
                      <>
                        <span>{selectedShift.columnLabel}</span>
                        <strong>{`${formatDay(year, month, selectedShift.dayOfMonth)} ${selectedShift.fromTime}-${selectedShift.toTime}`}</strong>
                      </>
                    ) : (
                      <strong>{t("Choose shift")}</strong>
                    )}
                  </span>
                  <span className={styles.shiftPickerAction}>{t("Choose")}</span>
                </button>
              </div>

              <div className={styles.segmentRow}>
                <button
                  type="button"
                  className={[styles.segmentButton, targetMode === "public" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setTargetMode("public")}
                >
                  {t("Everyone")}</button>
                <button
                  type="button"
                  className={[styles.segmentButton, targetMode === "private" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setTargetMode("private")}
                >
                  {t("Specific employee")}</button>
              </div>

              {targetMode === "private" ? (
                <div className={styles.field}>
                  <span>{t("Employee")}</span>
                  <SearchableSelect
                    id="manual-shift-target-employee-select"
                    value={selectedEmployeeId !== null ? String(selectedEmployeeId) : ""}
                    options={employeeOptions}
                    placeholder={employeeOptions.length > 0 ? t("Select employee...") : t("No employees available")}
                    dropdownTitle="Employee list"
                    searchPlaceholder={t("Search employee...")}
                    emptyMessage={t("No employees match your search.")}
                    fallbackHint={t("{0} employees found", employeeOptions.length)}
                    ariaLabel="employee list"
                    onChange={value => setTargetEmployeeId(value ? Number(value) : null)}
                  />
                </div>
              ) : null}

              {publishError ? <p className={styles.errorText}>{publishError}</p> : null}

              <IosButton
                label={isPublishingShift ? t("Publishing...") : t("Publish shift")}
                icon={<PlusIcon size={16} />}
                disabled={!canPublishShift}
                onClick={() => {
                  if (!selectedShift) {
                    return;
                  }

                  onPublishShift({
                    manualColumnId: selectedShift.manualColumnId,
                    dayOfMonth: selectedShift.dayOfMonth,
                    fromTime: selectedShift.fromTime,
                    toTime: selectedShift.toTime,
                    targetEmployeeId: targetMode === "private" ? selectedEmployeeId : null,
                  });
                }}
              />

              {isShiftDialogOpen ? (
                <ManualShiftPickerDialog
                  groups={shiftGroups}
                  selectedShiftId={selectedShift?.id ?? ""}
                  year={year}
                  month={month}
                  onClose={() => setIsShiftDialogOpen(false)}
                  onSelect={shiftId => {
                    setSelectedShiftId(shiftId);
                    setIsShiftDialogOpen(false);
                  }}
                />
              ) : null}
            </div>
          )}
        </div>

        <div className={styles.offeredBox}>
          <div className={styles.publishHeader}>
            <div>
              <span>{t("Offered shifts")}</span>
              <strong>{t("Manual shifts waiting in employee Swap.")}</strong>
            </div>
            <span className={styles.publishCount}>{offerCount}</span>
          </div>

          {offerCount === 0 ? (
            <div className={styles.emptyState}>{t("Published manual shifts that are still waiting for an employee will appear here.")}</div>
          ) : (
            <div className={styles.offeredList}>
              {pendingPublishedShifts.map(shift => {
                const targetName = shift.targetEmployeeId ? employeeNameById.get(shift.targetEmployeeId) ?? t("Selected employee") : t("Everyone");
                const visibilityLabel = shift.targetEmployeeId ? t("Private") : t("Public");

                return (
                  <article key={shift.clientId} className={styles.offeredItem}>
                    <div className={styles.offeredMain}>
                      <strong>{`${formatDay(year, month, shift.dayOfMonth)} ${shift.fromTime}-${shift.toTime}`}</strong>
                      <span>{`Pending save / ${targetName}`}</span>
                      <small>{`${formatPublishedShiftHours(getTimeRangeDurationHours(shift.fromTime, shift.toTime))} / ${visibilityLabel}`}</small>
                    </div>

                    <IosButton
                      label={t("Remove")}
                      variant="secondary"
                      size="compact"
                      disabled={!onCancelPendingPublishedShift}
                      onClick={() => onCancelPendingPublishedShift?.(shift.clientId)}
                    />
                  </article>
                );
              })}

              {publishedShifts.map(shift => (
                <article key={shift.id} className={styles.offeredItem}>
                  <div className={styles.offeredMain}>
                    <strong>{`${formatDay(shift.year, shift.month, shift.dayOfMonth)} ${formatPublishedShiftTime(shift.fromTime)}-${formatPublishedShiftTime(shift.toTime)}`}</strong>
                    <span>{`${shift.scheduleName} / ${shift.targetEmployeeName ?? t("Everyone")}`}</span>
                    <small>{`${formatPublishedShiftHours(shift.shiftHours)} / ${shift.visibility === "private" ? t("Private") : t("Public")}`}</small>
                  </div>

                  <IosButton
                    label={isCancellingPublishedShift ? t("Cancelling...") : t("Cancel")}
                    variant="secondary"
                    size="compact"
                    disabled={isCancellingPublishedShift || !onCancelPublishedShift}
                    onClick={() => onCancelPublishedShift?.(shift.id)}
                  />
                </article>
              ))}
            </div>
          )}
        </div>
      </div>
    </CardSection>
  );
}
