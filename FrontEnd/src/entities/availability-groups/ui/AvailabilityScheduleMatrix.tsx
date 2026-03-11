import { useRef, type KeyboardEvent, type ReactNode } from "react";
import {
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_NONE_MARK,
  getAvailabilityCellKey,
  getDaysInMonth,
  getAvailabilityWeekdayLabel,
  isAvailabilityWeekend,
  parseAvailabilityCode,
  type AvailabilityMatrixCellMap,
  type AvailabilityMatrixColumn,
} from "@entities/availability-groups/model/editor";
import {
  formatBindKeyFromKeyboardEvent,
  isBindNavigationKey,
  isCommonEditorShortcut,
} from "@entities/availability-binds";
import { AvailabilityIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./AvailabilityScheduleMatrix.module.css";

type AvailabilityScheduleMatrixProps = {
  year: number;
  month: number;
  columns: AvailabilityMatrixColumn[];
  cellMap: AvailabilityMatrixCellMap;
  title?: string;
  helperText?: string;
  readOnly?: boolean;
  footer?: ReactNode;
  emptyMessage?: string;
  cellErrors?: Record<string, string>;
  className?: string;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  bindValueByKey?: ReadonlyMap<string, string>;
  onCellChange?: (employeeId: number, dayOfMonth: number, value: string) => void;
};

export function AvailabilityScheduleMatrix({
  year,
  month,
  columns,
  cellMap,
  title = "Availability Schedule",
  helperText = `Use ${AVAILABILITY_ANY_MARK} for any shift, ${AVAILABILITY_NONE_MARK} for unavailable, or a time interval like 08:00 - 16:00.`,
  readOnly = false,
  footer,
  emptyMessage,
  cellErrors = {},
  className,
  headerCenterSlot,
  headerRightSlot,
  bindValueByKey,
  onCellChange,
}: AvailabilityScheduleMatrixProps) {
  const inputRefs = useRef<Record<string, HTMLInputElement | null>>({});
  const days = Array.from({ length: getDaysInMonth(year, month) }, (_, index) => {
    const dayOfMonth = index + 1;

    return {
      dayOfMonth,
      label: `${getAvailabilityWeekdayLabel(year, month, dayOfMonth)}/${String(dayOfMonth).padStart(2, "0")}`,
      isWeekend: isAvailabilityWeekend(year, month, dayOfMonth),
    };
  });
  const cardClassName = [styles.card, className ?? ""].filter(Boolean).join(" ");
  const resolvedEmptyMessage = emptyMessage ??
    (readOnly
      ? "No employees are assigned to this availability group yet."
      : "Add at least one employee to start filling the schedule.");

  const handleCellKeyDown = (event: KeyboardEvent<HTMLInputElement>, employeeId: number, dayOfMonth: number) => {
    if (readOnly || !onCellChange || !bindValueByKey || bindValueByKey.size === 0) {
      return;
    }

    if (isBindNavigationKey(event.key) || isCommonEditorShortcut(event)) {
      return;
    }

    const bindToken = formatBindKeyFromKeyboardEvent(event);
    if (!bindToken) {
      return;
    }

    const bindValue = bindValueByKey.get(bindToken);
    if (bindValue === undefined) {
      return;
    }

    event.preventDefault();

    const parsedBindValue = parseAvailabilityCode(bindValue);
    const nextValue = parsedBindValue.ok ? parsedBindValue.value.normalizedCode : bindValue;
    onCellChange(employeeId, dayOfMonth, nextValue);

    const nextInput = inputRefs.current[getAvailabilityCellKey(employeeId, dayOfMonth + 1)];
    if (nextInput) {
      requestAnimationFrame(() => {
        nextInput.focus();
        nextInput.select();
      });
    }
  };

  return (
    <CardSection
      className={cardClassName}
      title={title}
      icon={<AvailabilityIcon size={18} style={{ transform: "scaleY(-1)" }} />}
      headerCenterSlot={headerCenterSlot}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <p className={styles.helperText}>{helperText}</p>

        {columns.length === 0 ? (
          <div className={styles.emptyState}>{resolvedEmptyMessage}</div>
        ) : (
          <div className={styles.tableShell}>
            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th className={styles.dayHeader}>Day</th>
                    {columns.map(column => (
                      <th key={column.employeeId}>{column.label}</th>
                    ))}
                  </tr>
                </thead>

                <tbody>
                  {days.map(({ dayOfMonth, label, isWeekend }) => (
                    <tr key={dayOfMonth} className={isWeekend ? styles.weekendRow : undefined}>
                      <th className={styles.dayCell}>{label}</th>

                      {columns.map(column => {
                        const cellKey = getAvailabilityCellKey(column.employeeId, dayOfMonth);
                        const cellValue = cellMap[cellKey] ?? AVAILABILITY_NONE_MARK;
                        const error = cellErrors[cellKey];
                        const isUnavailable = cellValue.trim() === AVAILABILITY_NONE_MARK;

                        return (
                          <td
                            key={cellKey}
                            className={[
                              isUnavailable ? styles.unavailableCell : "",
                              error ? styles.errorCell : "",
                            ].filter(Boolean).join(" ")}
                          >
                            {readOnly ? (
                              <span
                                className={[
                                  styles.readonlyValue,
                                  isUnavailable ? styles.readonlyUnavailable : "",
                                ].filter(Boolean).join(" ")}
                              >
                                {cellValue}
                              </span>
                            ) : (
                              <input
                                ref={element => {
                                  inputRefs.current[cellKey] = element;
                                }}
                                className={[
                                  styles.input,
                                  isUnavailable ? styles.inputUnavailable : "",
                                  error ? styles.inputInvalid : "",
                                ].filter(Boolean).join(" ")}
                                value={cellValue}
                                onChange={event => onCellChange?.(column.employeeId, dayOfMonth, event.target.value)}
                                onKeyDown={event => handleCellKeyDown(event, column.employeeId, dayOfMonth)}
                                aria-label={`${column.label} day ${dayOfMonth}`}
                                aria-invalid={Boolean(error)}
                                title={error ?? undefined}
                              />
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
        )}

        {footer ? <div className={styles.footer}>{footer}</div> : null}
      </div>
    </CardSection>
  );
}




