import { useId, useMemo, useState, type ReactNode } from "react";
import type { AvailabilityPublicationStatus } from "@entities/availability-groups/model/types";
import { ErrorPill } from "@shared/ui/forms/Field";
import { EyeIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./AvailabilityPublicationCard.module.css";

export type AvailabilityPublicationErrors = {
  visibleFrom?: string;
  visibleTo?: string;
};

type AvailabilityPublicationCardProps = {
  publicationStatus: AvailabilityPublicationStatus;
  visibleFrom: string;
  visibleTo: string;
  errors?: AvailabilityPublicationErrors;
  headerRightSlot?: ReactNode;
  onPublicationStatusChange: (value: AvailabilityPublicationStatus) => void;
  onVisibleFromChange: (value: string) => void;
  onVisibleToChange: (value: string) => void;
};

const monthYearFormatter = new Intl.DateTimeFormat("en-US", { month: "long", year: "numeric" });
const weekdayLabels = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"] as const;
const quickTimes = ["09:00", "12:00", "18:00", "23:59"] as const;

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function padDatePart(value: number) {
  return String(value).padStart(2, "0");
}

function toLocalDatePart(date: Date) {
  return `${date.getFullYear()}-${padDatePart(date.getMonth() + 1)}-${padDatePart(date.getDate())}`;
}

function toLocalDateTimeValue(date: string, hour: number, minute: number) {
  return `${date}T${padDatePart(hour)}:${padDatePart(minute)}`;
}

function splitLocalDateTime(value: string) {
  const [date = "", time = ""] = value.split("T");

  return {
    date,
    time: time.slice(0, 5),
  };
}

function parseTime(value: string, fallback: string) {
  const [rawHour, rawMinute] = (value || fallback).split(":");
  const hour = Number(rawHour);
  const minute = Number(rawMinute);

  return {
    hour: Number.isInteger(hour) ? Math.min(23, Math.max(0, hour)) : 9,
    minute: Number.isInteger(minute) ? Math.min(59, Math.max(0, minute)) : 0,
  };
}

function getPickerParts(value: string, defaultTime: string) {
  const parts = splitLocalDateTime(value);
  const fallbackDate = toLocalDatePart(new Date());
  const time = parseTime(parts.time, defaultTime);

  return {
    date: parts.date || fallbackDate,
    ...time,
  };
}

function getPickerBaseDate(value: string) {
  const parts = splitLocalDateTime(value);
  const date = parts.date ? new Date(`${parts.date}T00:00`) : new Date();

  return Number.isNaN(date.getTime()) ? new Date() : date;
}

function buildCalendarCells(year: number, monthIndex: number) {
  const firstDay = new Date(year, monthIndex, 1);
  const startOffset = (firstDay.getDay() + 6) % 7;
  const daysInCurrentMonth = new Date(year, monthIndex + 1, 0).getDate();
  const daysInPreviousMonth = new Date(year, monthIndex, 0).getDate();

  return Array.from({ length: 42 }, (_, index) => {
    const currentDay = index - startOffset + 1;
    const date =
      currentDay < 1
        ? new Date(year, monthIndex - 1, daysInPreviousMonth + currentDay)
        : currentDay > daysInCurrentMonth
          ? new Date(year, monthIndex + 1, currentDay - daysInCurrentMonth)
          : new Date(year, monthIndex, currentDay);

    return {
      key: toLocalDatePart(date),
      day: date.getDate(),
      value: toLocalDatePart(date),
      isCurrentMonth: date.getMonth() === monthIndex,
      isToday: toLocalDatePart(date) === toLocalDatePart(new Date()),
    };
  });
}

function wrapTimePart(value: number, maxExclusive: number) {
  return (value + maxExclusive) % maxExclusive;
}

function formatLocalDateTime(value: string) {
  if (!value) {
    return "Select date and time";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Select date and time";
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(date);
}

function PublicationDateTimeField({
  id,
  label,
  value,
  error,
  defaultTime,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  error?: string;
  defaultTime: string;
  onChange: (value: string) => void;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [viewDate, setViewDate] = useState(() => getPickerBaseDate(value));
  const pickerParts = useMemo(() => getPickerParts(value, defaultTime), [defaultTime, value]);
  const calendarCells = useMemo(
    () => buildCalendarCells(viewDate.getFullYear(), viewDate.getMonth()),
    [viewDate],
  );
  const selectedTime = `${padDatePart(pickerParts.hour)}:${padDatePart(pickerParts.minute)}`;
  const errorId = error ? `${id}-error` : undefined;

  const openPicker = () => {
    setIsOpen(current => {
      if (!current) {
        setViewDate(getPickerBaseDate(value));
      }

      return !current;
    });
  };

  const commitDateTime = (date: string, hour = pickerParts.hour, minute = pickerParts.minute) => {
    onChange(toLocalDateTimeValue(date, hour, minute));
  };

  const commitQuickTime = (time: string) => {
    const nextTime = parseTime(time, defaultTime);
    commitDateTime(pickerParts.date, nextTime.hour, nextTime.minute);
  };

  const stepHour = (delta: number) => {
    commitDateTime(pickerParts.date, wrapTimePart(pickerParts.hour + delta, 24), pickerParts.minute);
  };

  const stepMinute = (delta: number) => {
    commitDateTime(pickerParts.date, pickerParts.hour, wrapTimePart(pickerParts.minute + delta, 60));
  };

  return (
    <div className={styles.field}>
      <span className={styles.label}>{label}</span>
      <button
        type="button"
        className={joinClassNames(styles.dateButton, isOpen && styles.dateButtonOpen, Boolean(error) && styles.dateButtonInvalid)}
        aria-expanded={isOpen}
        aria-invalid={Boolean(error)}
        aria-describedby={errorId}
        onClick={openPicker}
      >
        <span className={styles.dateButtonValue}>{formatLocalDateTime(value)}</span>
        <span className={styles.dateButtonHint}>Edit</span>
      </button>

      {isOpen ? (
        <div className={styles.dateDialog} role="dialog" aria-label={`${label} picker`}>
          <div className={styles.calendarHeader}>
            <button
              type="button"
              className={styles.calendarNavButton}
              aria-label="Previous month"
              onClick={() => setViewDate(current => new Date(current.getFullYear(), current.getMonth() - 1, 1))}
            >
              {"<"}
            </button>
            <strong>{monthYearFormatter.format(viewDate)}</strong>
            <button
              type="button"
              className={styles.calendarNavButton}
              aria-label="Next month"
              onClick={() => setViewDate(current => new Date(current.getFullYear(), current.getMonth() + 1, 1))}
            >
              {">"}
            </button>
          </div>

          <div className={styles.weekdayGrid} aria-hidden="true">
            {weekdayLabels.map(day => (
              <span key={day}>{day}</span>
            ))}
          </div>

          <div className={styles.calendarGrid}>
            {calendarCells.map(cell => (
              <button
                key={cell.key}
                type="button"
                className={joinClassNames(
                  styles.calendarDay,
                  !cell.isCurrentMonth && styles.calendarDayMuted,
                  cell.isToday && styles.calendarDayToday,
                  cell.value === pickerParts.date && styles.calendarDaySelected,
                )}
                aria-pressed={cell.value === pickerParts.date}
                onClick={() => commitDateTime(cell.value)}
              >
                {cell.day}
              </button>
            ))}
          </div>

          <div className={styles.timePanel}>
            <div className={styles.timeStepper}>
              <button type="button" aria-label="Decrease hour" onClick={() => stepHour(-1)}>
                -
              </button>
              <span>
                <strong>{padDatePart(pickerParts.hour)}</strong>
                <small>Hour</small>
              </span>
              <button type="button" aria-label="Increase hour" onClick={() => stepHour(1)}>
                +
              </button>
            </div>

            <span className={styles.timeColon}>:</span>

            <div className={styles.timeStepper}>
              <button type="button" aria-label="Decrease minute" onClick={() => stepMinute(-5)}>
                -
              </button>
              <span>
                <strong>{padDatePart(pickerParts.minute)}</strong>
                <small>Min</small>
              </span>
              <button type="button" aria-label="Increase minute" onClick={() => stepMinute(5)}>
                +
              </button>
            </div>
          </div>

          <div className={styles.quickTimeGrid}>
            {quickTimes.map(time => (
              <button
                key={time}
                type="button"
                className={joinClassNames(styles.quickTimeButton, selectedTime === time && styles.quickTimeButtonActive)}
                onClick={() => commitQuickTime(time)}
              >
                {time}
              </button>
            ))}
          </div>

          <div className={styles.dateDialogActions}>
            <button
              type="button"
              className={styles.dateDialogAction}
              onClick={() => {
                onChange("");
                setIsOpen(false);
              }}
            >
              Clear
            </button>
            <button type="button" className={styles.dateDialogActionPrimary} onClick={() => setIsOpen(false)}>
              Done
            </button>
          </div>
        </div>
      ) : null}

      {error ? <ErrorPill id={errorId}>{error}</ErrorPill> : null}
    </div>
  );
}

export function AvailabilityPublicationCard({
  publicationStatus,
  visibleFrom,
  visibleTo,
  errors = {},
  headerRightSlot,
  onPublicationStatusChange,
  onVisibleFromChange,
  onVisibleToChange,
}: AvailabilityPublicationCardProps) {
  const fieldId = useId();

  return (
    <CardSection
      className={styles.card}
      title="Publication"
      icon={<EyeIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <div className={styles.segmentedControl} role="radiogroup" aria-label="Publication status">
          <button
            type="button"
            className={joinClassNames(styles.segmentButton, publicationStatus === "private" && styles.segmentButtonActive)}
            role="radio"
            aria-checked={publicationStatus === "private"}
            onClick={() => onPublicationStatusChange("private")}
          >
            Private
          </button>
          <button
            type="button"
            className={joinClassNames(styles.segmentButton, publicationStatus === "public" && styles.segmentButtonActive)}
            role="radio"
            aria-checked={publicationStatus === "public"}
            onClick={() => onPublicationStatusChange("public")}
          >
            Public
          </button>
        </div>

        <p className={styles.hint}>Public availability is visible only to assigned employees after saving.</p>

        <PublicationDateTimeField
          id={`${fieldId}-visible-from`}
          label="Visible From"
          value={visibleFrom}
          error={errors.visibleFrom}
          defaultTime="09:00"
          onChange={onVisibleFromChange}
        />

        <PublicationDateTimeField
          id={`${fieldId}-visible-to`}
          label="Visible To"
          value={visibleTo}
          error={errors.visibleTo}
          defaultTime="23:59"
          onChange={onVisibleToChange}
        />
      </div>
    </CardSection>
  );
}
