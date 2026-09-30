import { createPortal } from "react-dom";
import { dateTimeFormat, t } from "@shared/i18n";
import {
  useCallback,
  useDeferredValue,
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent,
} from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useEmployeeScheduleListQuery,
  type EmployeeSchedule,
  type EmployeeScheduleSlot,
} from "@entities/employee-schedule";
import {
  useEmployeeUiStateQuery,
  useSetEmployeeSwapPinMutation,
} from "@entities/employee-ui-state";
import {
  filterShiftSwaps,
  useAcceptEmployeeShiftSwapMutation,
  useCancelEmployeeShiftSwapMutation,
  useCreateEmployeeShiftSwapMutation,
  useEmployeeShiftSwapEmployeesQuery,
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
  type ShiftSwapEmployee,
} from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { EmployeeTargetCombobox } from "@shared/ui/components/EmployeeTargetCombobox";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ArrowIcon, CloseIcon, PinIcon, PlusIcon, SearchIcon, SwapHistoryIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeSwapPage.module.css";

const scheduleMonthOnlyFormatter = dateTimeFormat("en-GB", {
  month: "long",
  timeZone: "UTC",
});

const swapDateFormatter = dateTimeFormat("en-GB", {
  weekday: "short",
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const swapOfferDateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  timeZone: "UTC",
});

const slotWeekdayFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  timeZone: "UTC",
});

const TIME_STEP_MINUTES = 15;

type ShiftSelectionPeriod = {
  fromTime: string;
  toTime: string;
};

type ShiftPeriodMode = "full" | "custom";
type SwapFilter = "all" | "mine" | "available" | "pinned";

type SwapPreviewStats = {
  hoursBefore: number;
  hoursAfter: number;
  workDaysBefore: number;
  workDaysAfter: number;
  freeDaysBefore: number;
  freeDaysAfter: number;
};

function formatScheduleMonth(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return `${String(schedule.month).padStart(2, "0")}.${schedule.year}`;
}

function formatScheduleMonthOnly(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return scheduleMonthOnlyFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)));
}

function formatSwapDate(value: Pick<ShiftSwap, "year" | "month" | "dayOfMonth">) {
  return swapOfferDateFormatter
    .format(new Date(Date.UTC(value.year, value.month - 1, value.dayOfMonth)))
    .replaceAll("/", ".");
}

function getSwapCode(swap: ShiftSwap) {
  const source = swap.manualColumnName?.trim() || swap.scheduleName.trim() || swap.containerName.trim();
  const codeMatch = source.match(/\b[A-Za-z]+\d+\b/);
  if (codeMatch) {
    return codeMatch[0].slice(0, 4).toUpperCase();
  }

  const initials = source
    .split(/\s+/)
    .filter(Boolean)
    .map(part => part[0])
    .join("")
    .slice(0, 3)
    .toUpperCase();

  return initials || "SWP";
}

function formatSlotDate(schedule: Pick<EmployeeSchedule, "year" | "month">, slot: Pick<EmployeeScheduleSlot, "dayOfMonth">) {
  return swapDateFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, slot.dayOfMonth)));
}

function formatSlotWeekday(schedule: Pick<EmployeeSchedule, "year" | "month">, dayOfMonth: number) {
  return slotWeekdayFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, dayOfMonth)));
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

function formatHours(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  return Number.isInteger(roundedValue) ? `${roundedValue}h` : `${roundedValue.toFixed(1)}h`;
}

function formatDelta(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  const prefix = roundedValue > 0 ? "+" : "";
  return `${prefix}${formatHours(roundedValue)}`;
}

function formatIntegerDelta(value: number) {
  return value > 0 ? `+${value}` : String(value);
}

function getTargetEmployeeLabel(employee: ShiftSwapEmployee) {
  const fullName = `${employee.firstName} ${employee.lastName}`.trim();
  return employee.displayName?.trim() || fullName || t("Employee #{0}", employee.id);
}

function padTimePart(value: number) {
  return String(value).padStart(2, "0");
}

function parseTimeMinutes(value: string) {
  const [rawHour, rawMinute] = value.split(":");
  const hour = Number(rawHour);
  const minute = Number(rawMinute);

  if (!Number.isInteger(hour) || !Number.isInteger(minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
    return null;
  }

  return hour * 60 + minute;
}

function formatTimeMinutes(totalMinutes: number) {
  const normalizedMinutes = Math.min(Math.max(totalMinutes, 0), 23 * 60 + 59);
  const hour = Math.floor(normalizedMinutes / 60);
  const minute = normalizedMinutes % 60;

  return `${padTimePart(hour)}:${padTimePart(minute)}`;
}

function clampMinutes(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max);
}

function getSlotTimeBounds(slot: Pick<EmployeeScheduleSlot, "fromTime" | "toTime">) {
  const fromMinutes = parseTimeMinutes(slot.fromTime);
  const toMinutes = parseTimeMinutes(slot.toTime);

  if (fromMinutes === null || toMinutes === null || toMinutes <= fromMinutes) {
    return null;
  }

  return { fromMinutes, toMinutes };
}

function getSafeCustomPeriod(slot: EmployeeScheduleSlot, fromTime: string, toTime: string): ShiftSelectionPeriod {
  const bounds = getSlotTimeBounds(slot);
  if (!bounds) {
    return { fromTime: slot.fromTime, toTime: slot.toTime };
  }

  if (bounds.toMinutes - bounds.fromMinutes <= TIME_STEP_MINUTES) {
    return { fromTime: slot.fromTime, toTime: slot.toTime };
  }

  const currentFrom = parseTimeMinutes(fromTime) ?? bounds.fromMinutes;
  const currentTo = parseTimeMinutes(toTime) ?? bounds.toMinutes;
  const safeFrom = clampMinutes(currentFrom, bounds.fromMinutes, bounds.toMinutes - TIME_STEP_MINUTES);
  const safeTo = clampMinutes(currentTo, safeFrom + TIME_STEP_MINUTES, bounds.toMinutes);

  return {
    fromTime: formatTimeMinutes(safeFrom),
    toTime: formatTimeMinutes(safeTo),
  };
}

function getTimeRangeDurationHours(fromTime: string, toTime: string) {
  const fromMinutes = parseTimeMinutes(fromTime);
  const toMinutes = parseTimeMinutes(toTime);

  if (fromMinutes === null || toMinutes === null || toMinutes <= fromMinutes) {
    return 0;
  }

  return (toMinutes - fromMinutes) / 60;
}

function getSlotDurationHours(slot: Pick<EmployeeScheduleSlot, "fromTime" | "toTime">) {
  return getTimeRangeDurationHours(slot.fromTime, slot.toTime);
}

function buildOwnShiftLabel(schedule: EmployeeSchedule, slot: EmployeeScheduleSlot, period?: ShiftSelectionPeriod | null) {
  const date = formatSlotDate(schedule, slot);
  const fromTime = period?.fromTime ?? slot.fromTime;
  const toTime = period?.toTime ?? slot.toTime;

  return `${date} - ${fromTime} - ${toTime}`;
}

function getSelectedSchedule(schedules: EmployeeSchedule[], selectedScheduleId: number | null) {
  return schedules.find(schedule => schedule.id === selectedScheduleId) ?? null;
}

function getOwnShiftOptions(schedule: EmployeeSchedule | null, employeeId: number | null) {
  if (!schedule || !employeeId) {
    return [];
  }

  return schedule.slots
    .filter(slot => slot.employeeId === employeeId)
    .sort((left, right) =>
      left.dayOfMonth - right.dayOfMonth ||
      left.fromTime.localeCompare(right.fromTime) ||
      left.toTime.localeCompare(right.toTime));
}

function getEmployeeMonthStats(
  schedules: EmployeeSchedule[],
  selectedSchedule: EmployeeSchedule | null,
  employeeId: number | null,
  selectedSlot: EmployeeScheduleSlot | null,
  selectedPeriod: ShiftSelectionPeriod | null,
): SwapPreviewStats | null {
  if (!selectedSchedule || !employeeId) {
    return null;
  }

  const monthSchedules = schedules.filter(
    schedule => schedule.year === selectedSchedule.year && schedule.month === selectedSchedule.month,
  );
  const employeeSlots = monthSchedules.flatMap(schedule =>
    schedule.slots.filter(slot => slot.employeeId === employeeId),
  );
  const daysInMonth = getDaysInMonth(selectedSchedule.year, selectedSchedule.month);
  const workDaysBeforeSet = new Set(employeeSlots.map(slot => slot.dayOfMonth));
  const hoursBefore = employeeSlots.reduce((sum, slot) => sum + getSlotDurationHours(slot), 0);
  const giveAwayHours = selectedSlot
    ? getTimeRangeDurationHours(selectedPeriod?.fromTime ?? selectedSlot.fromTime, selectedPeriod?.toTime ?? selectedSlot.toTime)
    : 0;
  const workDaysAfterSet = new Set<number>();

  employeeSlots.forEach(slot => {
    if (!selectedSlot || slot.id !== selectedSlot.id) {
      workDaysAfterSet.add(slot.dayOfMonth);
      return;
    }

    if (getSlotDurationHours(slot) - giveAwayHours > 0) {
      workDaysAfterSet.add(slot.dayOfMonth);
    }
  });

  return {
    hoursBefore,
    hoursAfter: Math.max(0, hoursBefore - giveAwayHours),
    workDaysBefore: workDaysBeforeSet.size,
    workDaysAfter: workDaysAfterSet.size,
    freeDaysBefore: Math.max(0, daysInMonth - workDaysBeforeSet.size),
    freeDaysAfter: Math.max(0, daysInMonth - workDaysAfterSet.size),
  };
}

function getSwapOfferStats(swap: ShiftSwap): SwapPreviewStats {
  return {
    hoursBefore: swap.currentEmployeeHoursBefore,
    hoursAfter: swap.currentEmployeeHoursAfter,
    workDaysBefore: swap.currentEmployeeWorkDaysBefore,
    workDaysAfter: swap.currentEmployeeWorkDaysAfter,
    freeDaysBefore: swap.currentEmployeeFreeDaysBefore,
    freeDaysAfter: swap.currentEmployeeFreeDaysAfter,
  };
}

function StatsDeltaBadge({ value, kind }: { value: number; kind: "hours" | "days" }) {
  const isNeutral = Math.abs(value) < 0.001;
  const label = kind === "hours" ? formatDelta(value) : formatIntegerDelta(value);

  return (
    <span
      className={[
        styles.statsDelta,
        isNeutral ? styles.statsDeltaNeutral : value > 0 ? styles.statsDeltaPositive : styles.statsDeltaNegative,
      ].filter(Boolean).join(" ")}
    >
      {t(label)}
    </span>
  );
}

function StatsCompareItem({
  label,
  before,
  after,
  kind,
}: {
  label: string;
  before: string | number;
  after: string | number;
  kind: "hours" | "days";
}) {
  const delta = typeof before === "number" && typeof after === "number" ? after - before : 0;

  return (
    <div className={styles.statsCompareItem}>
      <span>{t(label)}</span>
      <div className={styles.statsCompareValues}>
        <strong>{kind === "hours" && typeof before === "number" ? formatHours(before) : before}</strong>
        <small>{t("before")}</small>
        <span className={styles.statsArrow}>{"->"}</span>
        <strong>{kind === "hours" && typeof after === "number" ? formatHours(after) : after}</strong>
        <small>{t("after")}</small>
        <StatsDeltaBadge value={delta} kind={kind} />
      </div>
    </div>
  );
}

function SwapStatisticsCard({
  stats,
  summary = t("Your month"),
  collapsible = false,
  defaultExpanded = true,
}: {
  stats: SwapPreviewStats | null;
  summary?: string;
  collapsible?: boolean;
  defaultExpanded?: boolean;
}) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  if (!stats) {
    return null;
  }

  const shouldShowBody = !collapsible || isExpanded;

  return (
    <div className={styles.statsPanel}>
      <div className={styles.statsHeader}>
        <div>
          <span>{t("Statistics")}</span>
          <strong>{summary}</strong>
        </div>

        {collapsible ? (
          <button
            type="button"
            className={styles.statsToggleButton}
            aria-label={isExpanded ? t("Collapse statistics") : t("Expand statistics")}
            aria-expanded={isExpanded}
            onClick={() => setIsExpanded(value => !value)}
          >
            <ArrowIcon size={12} />
          </button>
        ) : null}
      </div>

      {shouldShowBody ? (
        <div className={styles.statsCompareGrid}>
          <StatsCompareItem label={t("Total hours")} before={stats.hoursBefore} after={stats.hoursAfter} kind="hours" />
          <StatsCompareItem label={t("Work days")} before={stats.workDaysBefore} after={stats.workDaysAfter} kind="days" />
          <StatsCompareItem label={t("Free days")} before={stats.freeDaysBefore} after={stats.freeDaysAfter} kind="days" />
        </div>
      ) : null}
    </div>
  );
}

function PinToggleButton({
  isPinned,
  label,
  onToggle,
}: {
  isPinned: boolean;
  label: string;
  onToggle: () => void;
}) {
  const actionLabel = `${isPinned ? t("Unpin") : t("Pin")} ${t(label)}`;
  return (
    <button
      type="button"
      className={[
        styles.collapseButton,
        styles.pinButton,
        isPinned ? styles.pinButtonActive : "",
      ].filter(Boolean).join(" ")}
      aria-label={actionLabel}
      aria-pressed={isPinned}
      title={actionLabel}
      onClick={onToggle}
    >
      <PinIcon size={15} />
    </button>
  );
}

function ShiftPickerDialog({
  schedule,
  slots,
  selectedSlotId,
  confirmSlotId,
  onConfirmSlotChange,
  onChoose,
  onClose,
}: {
  schedule: EmployeeSchedule | null;
  slots: EmployeeScheduleSlot[];
  selectedSlotId: number | null;
  confirmSlotId: number | null;
  onConfirmSlotChange: (slotId: number | null) => void;
  onChoose: (slot: EmployeeScheduleSlot, period: ShiftSelectionPeriod) => void;
  onClose: () => void;
}) {
  const [periodMode, setPeriodMode] = useState<ShiftPeriodMode>("full");
  const [periodFromTime, setPeriodFromTime] = useState("09:00");
  const [periodToTime, setPeriodToTime] = useState("15:00");

  const dialogRef = useRef<HTMLDialogElement>(null);
  const isOpen = schedule !== null;
  useEffect(() => {
    const dialog = dialogRef.current;
    if (!isOpen || !dialog) return;
    dialog.showModal();
    return () => dialog.close();
  }, [isOpen]);
  useEffect(() => {
    const confirmSlot = slots.find(slot => slot.id === confirmSlotId) ?? null;
    if (!confirmSlot) {
      return;
    }

    setPeriodMode("full");
    setPeriodFromTime(confirmSlot.fromTime);
    setPeriodToTime(confirmSlot.toTime);
  }, [confirmSlotId, slots]);

  if (!schedule) {
    return null;
  }

  const dayNumbers = Array.from({ length: getDaysInMonth(schedule.year, schedule.month) }, (_, index) => index + 1);
  const pickerEntries = dayNumbers.flatMap<{ dayOfMonth: number; slot: EmployeeScheduleSlot | null }>(dayOfMonth => {
    const daySlots = slots.filter(slot => slot.dayOfMonth === dayOfMonth);
    return daySlots.length > 0
      ? daySlots.map(slot => ({ dayOfMonth, slot }))
      : [{ dayOfMonth, slot: null }];
  });
  const confirmSlot = slots.find(slot => slot.id === confirmSlotId) ?? null;
  const selectedPeriod = confirmSlot && periodMode === "custom"
    ? getSafeCustomPeriod(confirmSlot, periodFromTime, periodToTime)
    : confirmSlot
      ? { fromTime: confirmSlot.fromTime, toTime: confirmSlot.toTime }
      : null;
  const handleOverlayMouseDown = (event: MouseEvent<HTMLDialogElement>) => {
    if (event.target === event.currentTarget) {
      onClose();
    }
  };
  const adjustCustomPeriod = (edge: "from" | "to", deltaMinutes: number) => {
    if (!confirmSlot) {
      return;
    }

    const bounds = getSlotTimeBounds(confirmSlot);
    if (!bounds) {
      return;
    }

    if (bounds.toMinutes - bounds.fromMinutes <= TIME_STEP_MINUTES) {
      return;
    }

    const currentPeriod = getSafeCustomPeriod(confirmSlot, periodFromTime, periodToTime);
    const currentFrom = parseTimeMinutes(currentPeriod.fromTime) ?? bounds.fromMinutes;
    const currentTo = parseTimeMinutes(currentPeriod.toTime) ?? bounds.toMinutes;

    setPeriodMode("custom");
    if (edge === "from") {
      const maxFrom = Math.min(bounds.toMinutes - TIME_STEP_MINUTES, currentTo - TIME_STEP_MINUTES);
      setPeriodFromTime(formatTimeMinutes(clampMinutes(currentFrom + deltaMinutes, bounds.fromMinutes, maxFrom)));
      return;
    }

    const minTo = Math.max(bounds.fromMinutes + TIME_STEP_MINUTES, currentFrom + TIME_STEP_MINUTES);
    setPeriodToTime(formatTimeMinutes(clampMinutes(currentTo + deltaMinutes, minTo, bounds.toMinutes)));
  };

  return (
    <dialog ref={dialogRef} className={styles.dialogOverlay} aria-label={t("Choose shift")} onCancel={event => { event.preventDefault(); onClose(); }} onMouseDown={handleOverlayMouseDown}>
      <div className={styles.shiftDialog}>
        <div className={styles.dialogHeader}>
          <div>
            <span className={styles.dialogEyebrow}>{formatScheduleMonth(schedule)}</span>
            <h3>{t("Schedule name: {0}", schedule.name)}</h3>
          </div>

          <button type="button" className={styles.iconButton} aria-label={t("Close")} onClick={onClose}>
            <CloseIcon size={16} />
          </button>
        </div>

        <div className={styles.shiftScroll}>
          <div className={styles.shiftGrid}>
            {pickerEntries.map(({ dayOfMonth, slot }) => {
              const isSelected = Boolean(slot && slot.id === selectedSlotId);
              const isConfirming = Boolean(slot && slot.id === confirmSlotId);

              return (
                <button
                  key={slot ? `slot-${slot.id}` : `day-${dayOfMonth}`}
                  type="button"
                  className={[
                    styles.shiftButton,
                    !slot ? styles.shiftButtonEmpty : "",
                    isSelected ? styles.shiftButtonSelected : "",
                    isConfirming ? styles.shiftButtonConfirming : "",
                  ].filter(Boolean).join(" ")}
                  aria-pressed={isSelected || isConfirming}
                  disabled={!slot}
                  onClick={() => {
                    if (slot) {
                      onConfirmSlotChange(slot.id);
                    }
                  }}
                >
                  <span className={styles.shiftDay}>{dayOfMonth}</span>
                  <span className={styles.shiftWeekday}>{formatSlotWeekday(schedule, dayOfMonth)}</span>
                  <span className={styles.shiftTime}>{slot ? `${slot.fromTime} - ${slot.toTime}` : "-"}</span>
                </button>
              );
            })}
          </div>
        </div>

        {confirmSlot ? (
          <div className={styles.confirmOverlay}>
            <div className={styles.confirmBox}>
              <div>
                <span>{t("Confirm shift")}</span>
                <strong>{buildOwnShiftLabel(schedule, confirmSlot, selectedPeriod)}</strong>
              </div>
              <p>{t("Choose the whole shift or only the time period you want to give away.")}</p>
              <div className={styles.periodModeRow}>
                <button
                  type="button"
                  className={[styles.segmentButton, periodMode === "full" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setPeriodMode("full")}
                >
                  {t("Full shift")}</button>
                <button
                  type="button"
                  className={[styles.segmentButton, periodMode === "custom" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setPeriodMode("custom")}
                >
                  {t("Custom period")}</button>
              </div>
              {periodMode === "custom" && selectedPeriod ? (
                <div className={styles.timePanel}>
                  <div className={styles.timeStepper}>
                    <button type="button" aria-label={t("Decrease start time")} onClick={() => adjustCustomPeriod("from", -TIME_STEP_MINUTES)}>
                      -
                    </button>
                    <span>
                      <strong>{selectedPeriod.fromTime}</strong>
                      <small>{t("From")}</small>
                    </span>
                    <button type="button" aria-label={t("Increase start time")} onClick={() => adjustCustomPeriod("from", TIME_STEP_MINUTES)}>
                      +
                    </button>
                  </div>

                  <span className={styles.timeDash}>-</span>

                  <div className={styles.timeStepper}>
                    <button type="button" aria-label={t("Decrease end time")} onClick={() => adjustCustomPeriod("to", -TIME_STEP_MINUTES)}>
                      -
                    </button>
                    <span>
                      <strong>{selectedPeriod.toTime}</strong>
                      <small>{t("To")}</small>
                    </span>
                    <button type="button" aria-label={t("Increase end time")} onClick={() => adjustCustomPeriod("to", TIME_STEP_MINUTES)}>
                      +
                    </button>
                  </div>
                </div>
              ) : null}
              <div className={styles.confirmActions}>
                <button type="button" className={styles.secondaryButton} onClick={() => onConfirmSlotChange(null)}>
                  {t("Back")}</button>
                <button
                  type="button"
                  className={styles.primaryButton}
                  onClick={() => {
                    if (selectedPeriod) {
                      onChoose(confirmSlot, selectedPeriod);
                    }
                  }}
                >
                  {t("Choose shift")}</button>
              </div>
            </div>
          </div>
        ) : null}
      </div>
    </dialog>
  );
}

function SwapOfferCard({
  swap,
  stats,
  isBusy,
  isPinned,
  onTogglePin,
  onAccept,
  onCancel,
}: {
  swap: ShiftSwap;
  stats: SwapPreviewStats | null;
  isBusy: boolean;
  isPinned: boolean;
  onTogglePin: (swapId: number) => void;
  onAccept: (swap: ShiftSwap) => void;
  onCancel: (swap: ShiftSwap) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(false);
  const isOpen = swap.status === "open";
  const isScheduleLocked = isOpen && swap.isScheduleLocked;
  const unavailableReason = isOpen && !swap.canAccept && !swap.isCreatedByCurrentEmployee
    ? swap.acceptanceUnavailableReason
      ?? (isScheduleLocked
        ? t("Schedule is locked while a manager is editing it.")
        : null)
    : null;
  const availabilityLabel = !isOpen
    ? swap.status === "accepted" ? t("Accepted") : t("Cancelled")
    : swap.isCreatedByCurrentEmployee
      ? t("My offer")
      : swap.canAccept
        ? t("Can")
        : t("Can’t");

  const handleCardClick = (event: MouseEvent<HTMLElement>) => {
    if ((event.target as Element).closest("button, a, input, select, textarea")) {
      return;
    }

    setIsExpanded(value => !value);
  };

  const handleCardKeyDown = (event: ReactKeyboardEvent<HTMLElement>) => {
    if (event.target !== event.currentTarget || (event.key !== "Enter" && event.key !== " ")) {
      return;
    }

    event.preventDefault();
    setIsExpanded(value => !value);
  };

  return (
    <article
      className={[styles.offerCard, isPinned ? styles.offerCardPinned : ""].filter(Boolean).join(" ")}
      data-pinned={isPinned ? "true" : "false"}
      data-expanded={isExpanded ? "true" : "false"}
      tabIndex={0}
      title={isExpanded ? t("Click to collapse this swap") : t("Click to expand this swap")}
      onClick={handleCardClick}
      onKeyDown={handleCardKeyDown}
    >
      <span className={styles.offerAvatar} aria-hidden="true">{getSwapCode(swap)}</span>

      <div className={styles.offerSummary}>
        <div className={styles.offerHeader}>
          <div className={styles.offerTitle}>
            <strong>{swap.scheduleName}</strong>
          </div>

          <div className={styles.offerHeaderActions}>
            <PinToggleButton
              isPinned={isPinned}
              label={t("{0} swap", swap.scheduleName)}
              onToggle={() => onTogglePin(swap.id)}
            />
            <div className={styles.offerBadges}>
              <span className={[
                styles.badge,
                swap.visibility === "private" ? styles.badgePrivate : styles.badgePublic,
              ].join(" ")}>
                {swap.visibility === "private" ? t("Private") : t("Public")}
              </span>
              <span className={[
                styles.badge,
                !isOpen ? styles.badgeMuted : swap.isCreatedByCurrentEmployee
                  ? styles.badgeMine
                  : swap.canAccept ? styles.badgeCan : styles.badgeCant,
              ].join(" ")}>
                {availabilityLabel}
              </span>
              {isScheduleLocked ? <span className={[styles.badge, styles.badgeLocked].join(" ")}>{t("Locked")}</span> : null}
            </div>
          </div>
        </div>

        <div className={styles.offerMeta}>
          <span>{`${formatSwapDate(swap)} → ${swap.fromTime} – ${swap.toTime}`}</span>
          <small>{t("From: {0}", swap.fromEmployeeName)}</small>
        </div>
      </div>

      {swap.canAccept || swap.canCancel ? (
        <div className={styles.cardActions}>
          {swap.canAccept ? (
            <button
              type="button"
              className={styles.primaryButton}
              disabled={isBusy}
              onClick={() => onAccept(swap)}
            >
              {t("Accept")}
            </button>
          ) : null}
          {swap.canCancel ? (
            <button
              type="button"
              className={styles.secondaryButton}
              aria-label={t("Cancel offer")}
              disabled={isBusy}
              onClick={() => onCancel(swap)}
            >
              {t("Cancel")}
            </button>
          ) : null}
        </div>
      ) : null}

      {isExpanded ? (
        <div className={styles.offerExpandedContent} data-employee-motion>
          <div className={styles.detailGrid}>
            <div className={styles.detailItem}>
              <span>{t("Location")}</span>
              <strong>{swap.shopName || swap.containerName || t("Schedule")}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("From")}</span>
              <strong>{swap.fromEmployeeName}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("Target")}</span>
              <strong>{swap.targetEmployeeName ?? t("Everyone")}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("Shift hours")}</span>
              <strong>{formatHours(swap.shiftHours)}</strong>
            </div>
            {swap.acceptedByEmployeeName ? (
              <div className={styles.detailItem}>
                <span>{t("Accepted by")}</span>
                <strong>{swap.acceptedByEmployeeName}</strong>
              </div>
            ) : null}
          </div>

          <div className={styles.offerExpandedStats}>
            <SwapStatisticsCard stats={stats} />
          </div>
        </div>
      ) : null}

      <div className={styles.offerFooter}>
        <span className={styles.cardChevron} aria-hidden="true"><ArrowIcon size={13} /></span>
      </div>
      {unavailableReason ? (
        <div className={styles.offerUnavailableReason} title={unavailableReason}>
          <span className={styles.unavailableText}>{unavailableReason}</span>
        </div>
      ) : null}
    </article>
  );
}

export function EmployeeSwapPage() {
  const stageRef = useRef<HTMLDivElement>(null);
  const creationDialogRef = useRef<HTMLDialogElement>(null);
  const createOfferTriggerRef = useRef<HTMLButtonElement>(null);
  const historyDialogRef = useRef<HTMLDialogElement>(null);
  const [historyDialogElement, setHistoryDialogElement] = useState<HTMLDialogElement | null>(null);
  const setHistoryDialogRef = useCallback((element: HTMLDialogElement | null) => {
    historyDialogRef.current = element;
    setHistoryDialogElement(element);
  }, []);
  const [stageWidth, setStageWidth] = useState(430);
  const [stageHeight, setStageHeight] = useState(900);
  const backdropPath = `M34 22H150C161 22 168 31 168 48V65C168 87 181 99 203 99H${stageWidth - 54}C${stageWidth - 22} 99 ${stageWidth - 4} 117 ${stageWidth - 4} 148V${stageHeight - 52}C${stageWidth - 4} ${stageHeight - 35} ${stageWidth - 17} ${stageHeight - 22} ${stageWidth - 34} ${stageHeight - 22}H${stageWidth - 150}C${stageWidth - 161} ${stageHeight - 22} ${stageWidth - 168} ${stageHeight - 31} ${stageWidth - 168} ${stageHeight - 48}V${stageHeight - 65}C${stageWidth - 168} ${stageHeight - 87} ${stageWidth - 181} ${stageHeight - 99} ${stageWidth - 203} ${stageHeight - 99}H54C22 ${stageHeight - 99} 4 ${stageHeight - 117} 4 ${stageHeight - 148}V52C4 35 17 22 34 22Z`;

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(([entry]) => {
      if (entry.contentRect.width > 0) setStageWidth(entry.contentRect.width);
      if (entry.contentRect.height > 0) setStageHeight(entry.contentRect.height);
    });
    observer.observe(stage);
    return () => observer.disconnect();
  }, []);

  const { session } = useAuth();
  const employeeId = session?.employeeId && session.employeeId > 0 ? session.employeeId : null;
  const schedulesQuery = useEmployeeScheduleListQuery();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const targetEmployeesQuery = useEmployeeShiftSwapEmployeesQuery();
  const uiStateQuery = useEmployeeUiStateQuery(Boolean(employeeId));
  const createSwapMutation = useCreateEmployeeShiftSwapMutation();
  const acceptSwapMutation = useAcceptEmployeeShiftSwapMutation();
  const cancelSwapMutation = useCancelEmployeeShiftSwapMutation();
  const setSwapPinMutation = useSetEmployeeSwapPinMutation();
  const schedules = schedulesQuery.data ?? [];
  const swaps = swapsQuery.data ?? [];
  const targetEmployees = targetEmployeesQuery.data ?? [];
  const [selectedScheduleId, setSelectedScheduleId] = useState<number | null>(null);
  const [selectedSlotId, setSelectedSlotId] = useState<number | null>(null);
  const [selectedPeriod, setSelectedPeriod] = useState<ShiftSelectionPeriod | null>(null);
  const [shiftDialogScheduleId, setShiftDialogScheduleId] = useState<number | null>(null);
  const [confirmSlotId, setConfirmSlotId] = useState<number | null>(null);
  const [targetMode, setTargetMode] = useState<"public" | "private">("public");
  const [targetEmployeeId, setTargetEmployeeId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isGiveAwayExpanded, setIsGiveAwayExpanded] = useState(false);
  useEffect(() => {
    const dialog = creationDialogRef.current;
    if (!isGiveAwayExpanded || !dialog) return;
    const previousOverflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      createOfferTriggerRef.current?.focus({ preventScroll: true });
      document.body.style.overflow = previousOverflow;
    };
  }, [isGiveAwayExpanded]);
  const [isHistoryExpanded, setIsHistoryExpanded] = useState(false);
  useEffect(() => {
    const dialog = historyDialogRef.current;
    if (!isHistoryExpanded || !dialog) return;
    const previousOverflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      document.body.style.overflow = previousOverflow;
    };
  }, [isHistoryExpanded]);
  const [swapSearchQuery, setSwapSearchQuery] = useState("");
  const [swapFilter, setSwapFilter] = useState<SwapFilter>("all");
  const [pendingUnpinSwapId, setPendingUnpinSwapId] = useState<number | null>(null);
  const deferredSwapSearchQuery = useDeferredValue(swapSearchQuery);
  const selectedSchedule = useMemo(
    () => getSelectedSchedule(schedules, selectedScheduleId),
    [schedules, selectedScheduleId],
  );
  const dialogSchedule = useMemo(
    () => getSelectedSchedule(schedules, shiftDialogScheduleId),
    [schedules, shiftDialogScheduleId],
  );
  const selectedScheduleShiftOptions = useMemo(
    () => getOwnShiftOptions(selectedSchedule, employeeId),
    [employeeId, selectedSchedule],
  );
  const dialogShiftOptions = useMemo(
    () => getOwnShiftOptions(dialogSchedule, employeeId),
    [dialogSchedule, employeeId],
  );
  const selectedSlot = selectedScheduleShiftOptions.find(slot => slot.id === selectedSlotId) ?? null;
  const sortedTargetEmployees = useMemo(
    () => targetEmployees
      .filter(employee => employee.id !== employeeId)
      .sort((left, right) => getTargetEmployeeLabel(left).localeCompare(getTargetEmployeeLabel(right))),
    [employeeId, targetEmployees],
  );
  const pinnedSwapIdSet = useMemo(
    () => new Set((uiStateQuery.data?.pinnedSwapIds ?? []).map(String)),
    [uiStateQuery.data?.pinnedSwapIds],
  );
  const sortedSwaps = useMemo(() => {
    const pinnedSwaps: ShiftSwap[] = [];
    const regularSwaps: ShiftSwap[] = [];
    swaps.forEach(swap => {
      (pinnedSwapIdSet.has(String(swap.id)) ? pinnedSwaps : regularSwaps).push(swap);
    });
    return [...pinnedSwaps, ...regularSwaps];
  }, [pinnedSwapIdSet, swaps]);
  const openSwaps = useMemo(() => sortedSwaps.filter(swap => swap.status === "open"), [sortedSwaps]);
  const filteredOpenSwaps = useMemo(() => {
    const swapsForFilter = openSwaps.filter(swap => {
      if (swapFilter === "mine") {
        return swap.isCreatedByCurrentEmployee;
      }
      if (swapFilter === "available") {
        return swap.canAccept;
      }
      if (swapFilter === "pinned") {
        return pinnedSwapIdSet.has(String(swap.id));
      }
      return true;
    });

    return filterShiftSwaps(swapsForFilter, deferredSwapSearchQuery);
  }, [deferredSwapSearchQuery, openSwaps, pinnedSwapIdSet, swapFilter]);
  const swapHistory = useMemo(() => sortedSwaps.filter(swap => swap.status !== "open"), [sortedSwaps]);
  const pendingUnpinSwap = pendingUnpinSwapId === null
    ? null
    : swaps.find(swap => swap.id === pendingUnpinSwapId) ?? null;
  const isActionBusy = createSwapMutation.isPending || acceptSwapMutation.isPending || cancelSwapMutation.isPending;
  const loadError = schedulesQuery.error ?? swapsQuery.error ?? targetEmployeesQuery.error ?? uiStateQuery.error;
  const resolvedTargetEmployeeId = targetMode === "private" ? targetEmployeeId ?? sortedTargetEmployees[0]?.id ?? null : null;
  const hasSwapSearch = swapSearchQuery.trim().length > 0;
  const swapPreviewStats = useMemo(
    () => getEmployeeMonthStats(schedules, selectedSchedule, employeeId, selectedSlot, selectedPeriod),
    [employeeId, schedules, selectedPeriod, selectedSchedule, selectedSlot],
  );

  const handleToggleSwapPin = (swapId: number) => {
    if (pinnedSwapIdSet.has(String(swapId))) {
      setPendingUnpinSwapId(swapId);
      return;
    }

    setSwapPinMutation.mutate(
      { swapId, pinned: true },
      { onError: error => setActionError(getErrorMessage(error, t("Could not pin this swap."))) },
    );
  };

  const handleConfirmUnpin = () => {
    if (pendingUnpinSwapId !== null) {
      setSwapPinMutation.mutate(
        { swapId: pendingUnpinSwapId, pinned: false },
        { onError: error => setActionError(getErrorMessage(error, t("Could not unpin this swap."))) },
      );
    }
    setPendingUnpinSwapId(null);
  };

  const handleOpenSchedule = (schedule: EmployeeSchedule) => {
    if (schedule.allowSwap === false) {
      setActionError(t("Swaps are not allowed for “{0}”.", schedule.name));
      return;
    }

    setSelectedScheduleId(schedule.id);
    setShiftDialogScheduleId(schedule.id);
    setConfirmSlotId(null);
    setActionError(null);
  };

  const handleChooseShift = (slot: EmployeeScheduleSlot, period: ShiftSelectionPeriod) => {
    if (!dialogSchedule) {
      return;
    }

    setSelectedScheduleId(dialogSchedule.id);
    setSelectedSlotId(slot.id);
    setSelectedPeriod(period);
    setShiftDialogScheduleId(null);
    setConfirmSlotId(null);
    setActionError(null);
  };

  const handleCreateOffer = () => {
    setActionError(null);

    if (!selectedSchedule || !selectedSlot) {
      setActionError(t("Choose a published schedule and one of your shifts."));
      return;
    }

    if (selectedSchedule.allowSwap === false) {
      setActionError(t("Swaps are not allowed for this schedule."));
      return;
    }

    if (targetMode === "private" && !resolvedTargetEmployeeId) {
      setActionError(t("Choose who should receive this private swap."));
      return;
    }

    createSwapMutation.mutate(
      {
        scheduleId: selectedSchedule.id,
        scheduleSlotId: selectedSlot.id,
        fromTime: selectedPeriod?.fromTime ?? selectedSlot.fromTime,
        toTime: selectedPeriod?.toTime ?? selectedSlot.toTime,
        targetEmployeeId: resolvedTargetEmployeeId,
      },
      {
        onSuccess: () => {
          setIsGiveAwayExpanded(false);
          setSelectedSlotId(null);
          setSelectedPeriod(null);
        },
        onError: error => setActionError(getErrorMessage(error, t("Could not create this swap offer."))),
      },
    );
  };

  const handleAccept = (swap: ShiftSwap) => {
    setActionError(null);
    acceptSwapMutation.mutate(swap.id, {
      onError: error => setActionError(getErrorMessage(error, t("Could not accept this swap offer."))),
    });
  };

  const handleCancel = (swap: ShiftSwap) => {
    setActionError(null);
    cancelSwapMutation.mutate(swap.id, {
      onError: error => setActionError(getErrorMessage(error, t("Could not cancel this swap offer."))),
    });
  };

  return (
    <div className={styles.swapPage}>
      <div ref={stageRef} className={styles.swapStage}>
        <svg className={styles.swapBackdrop} viewBox={`0 0 ${stageWidth} ${stageHeight}`} preserveAspectRatio="none" aria-hidden="true">
          <path d={backdropPath} />
        </svg>

        <header className={styles.swapHeader} data-employee-motion>
          <div className={styles.swapIdentity}>
            <span className={styles.swapLogo} aria-hidden="true">
              <span className={styles.swapLogoMark}>⇄</span>
            </span>
            <div>
              <h1>{t("Swap")}</h1>
              <span>{t("Shift exchange")}</span>
            </div>
          </div>

          <button
            type="button"
            ref={createOfferTriggerRef}
            className={styles.createOfferButton}
            aria-expanded={isGiveAwayExpanded}
            aria-controls="create-swap-panel"
            aria-label={t("Create offer")}
            title={t("Create offer")}
            onClick={() => setIsGiveAwayExpanded(value => !value)}
          >
            <PlusIcon size={25} />
            <span className={styles.fullActionLabel}>{t("Create offer")}</span>
            <span className={styles.shortActionLabel}>{t("Add")}</span>
          </button>
        </header>

        <div className={styles.swapContentClip} style={{ clipPath: `path("${backdropPath}")` }}>
        <main className={styles.swapBody}>
          {loadError ? <ErrorBanner dismissible={false}>{getErrorMessage(loadError, t("Could not load swap data."))}</ErrorBanner> : null}
          {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

          <section className={styles.offersSection} aria-labelledby="open-swaps-heading">
            <h2 id="open-swaps-heading" className={styles.visuallyHidden}>{t("Open swaps")}</h2>
            <div className={styles.offerToolbar} data-employee-motion>
          <label className={styles.swapSearchField} htmlFor="employee-swap-search">
            <SearchIcon size={16} className={styles.swapSearchIcon} />
            <input
              id="employee-swap-search"
              className={styles.swapSearchInput}
              type="search"
              value={swapSearchQuery}
              placeholder={t("Search employee, date or schedule")}
              aria-label={t("Search swaps by giver, receiver, date or schedule")}
              onChange={event => setSwapSearchQuery(event.target.value)}
            />
            {hasSwapSearch ? (
              <button
                type="button"
                className={styles.clearSearchButton}
                aria-label={t("Clear swap search")}
                title={t("Clear search")}
                onClick={() => setSwapSearchQuery("")}
              >
                <CloseIcon size={13} />
              </button>
            ) : null}
          </label>

              <div className={styles.filterRow} role="group" aria-label={t("Filter swap offers")}>
                {([
                  ["all", "All"],
                  ["mine", "My offers"],
                  ["available", "Can"],
                  ["pinned", "Pinned"],
                ] as const).map(([value, label]) => (
                  <button
                    key={value}
                    type="button"
                    className={[styles.filterButton, swapFilter === value ? styles.filterButtonActive : ""].filter(Boolean).join(" ")}
                    aria-pressed={swapFilter === value}
                    onClick={() => setSwapFilter(value)}
                  >
                    {t(label)}
                  </button>
                ))}
                <span className={styles.searchResultText}>
                  {hasSwapSearch || swapFilter !== "all" ? t("{0} of {1}", filteredOpenSwaps.length, openSwaps.length) : t("{0} offers", openSwaps.length)}
                </span>
              </div>
            </div>

            <div data-motion-list className={[
              styles.offerList,
              filteredOpenSwaps.length >= 5 ? styles.offerListScrollable : "",
            ].filter(Boolean).join(" ")}>
              {openSwaps.length === 0 ? (
                <p className={styles.emptyText}>{t("No open swap offers right now.")}</p>
              ) : filteredOpenSwaps.length === 0 ? (
                <p className={styles.emptyText}>{hasSwapSearch ? t("No open swap offers match \"{0}\".", swapSearchQuery.trim()) : t("No swap offers match this filter.")}</p>
              ) : filteredOpenSwaps.map(swap => (
                <SwapOfferCard
                  key={swap.id}
                  swap={swap}
                  stats={getSwapOfferStats(swap)}
                  isBusy={isActionBusy}
                  isPinned={pinnedSwapIdSet.has(String(swap.id))}
                  onTogglePin={handleToggleSwapPin}
                  onAccept={handleAccept}
                  onCancel={handleCancel}
                />
              ))}
            </div>
          </section>
        </main>
        </div>

        <button
          type="button"
          className={styles.historyButton}
          aria-expanded={isHistoryExpanded}
          aria-controls="swap-history-panel"
          aria-label={t("Swap history")}
          title={t("Swap history")}
          onClick={() => setIsHistoryExpanded(value => !value)}
        >
          <span aria-hidden="true"><SwapHistoryIcon size={22} /></span>
          <span className={`${styles.historyButtonLabel} ${styles.fullActionLabel}`}>{t("Swap history")}</span>
          <span className={styles.shortActionLabel}>{t("History")}</span>
        </button>
      </div>

      <dialog ref={setHistoryDialogRef} className={styles.creationDialog} aria-labelledby="swap-history-heading" onCancel={event => { if (pendingUnpinSwapId !== null) { event.preventDefault(); setPendingUnpinSwapId(null); } else setIsHistoryExpanded(false); }} onClick={event => { if (event.target === event.currentTarget) setIsHistoryExpanded(false); }}>
      {isHistoryExpanded ? (
        <section id="swap-history-panel" className={styles.historyPanel} aria-labelledby="swap-history-heading">
          <div className={styles.panelHeaderRow}>
            <div>
              <span className={workspaceStyles.panelEyebrow}>{t("History")}</span>
              <h2 id="swap-history-heading" className={workspaceStyles.panelTitle}>{t("Recent swap activity")}</h2>
            </div>
            <button type="button" className={styles.dialogCloseButton} aria-label={t("Close")} onClick={() => setIsHistoryExpanded(false)}><CloseIcon size={20} /></button>
          </div>
          <div data-motion-list className={[styles.offerList, swapHistory.length >= 5 ? styles.offerListScrollable : ""].filter(Boolean).join(" ")}>
            {swapHistory.length === 0 ? (
              <p className={styles.emptyText}>{t("Accepted and cancelled swaps will appear here.")}</p>
            ) : swapHistory.map(swap => (
              <SwapOfferCard
                key={swap.id}
                swap={swap}
                stats={getSwapOfferStats(swap)}
                isBusy={isActionBusy}
                isPinned={pinnedSwapIdSet.has(String(swap.id))}
                onTogglePin={handleToggleSwapPin}
                onAccept={handleAccept}
                onCancel={handleCancel}
              />
            ))}
          </div>
        </section>
      ) : null}

      </dialog>
      <ShiftPickerDialog
        schedule={dialogSchedule}
        slots={dialogShiftOptions}
        selectedSlotId={selectedSlotId}
        confirmSlotId={confirmSlotId}
        onConfirmSlotChange={setConfirmSlotId}
        onChoose={handleChooseShift}
        onClose={() => {
          setShiftDialogScheduleId(null);
          setConfirmSlotId(null);
        }}
      />

      <dialog ref={creationDialogRef} className={styles.creationDialog} aria-labelledby="create-swap-title" onCancel={() => setIsGiveAwayExpanded(false)} onClick={event => { if (event.target === event.currentTarget) setIsGiveAwayExpanded(false); }}>
          {isGiveAwayExpanded ? (
            <section id="create-swap-panel" className={styles.creationPanel} aria-label={t("Create a swap offer")}>
              <div className={styles.panelHeaderRow}>
                <div>
                  <span className={workspaceStyles.panelEyebrow}>{t("Give away a shift")}</span>
                  <h2 id="create-swap-title" className={workspaceStyles.panelTitle}>{t("Create a swap offer")}</h2>
                </div>
                <button type="button" className={styles.dialogCloseButton} aria-label={t("Close")} onClick={() => setIsGiveAwayExpanded(false)}><CloseIcon size={20} /></button>
              </div>

            {loadError ? <ErrorBanner dismissible={false}>{getErrorMessage(loadError, t("Could not load swap data."))}</ErrorBanner> : null}
            {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}
            {schedules.length === 0 ? (
              <p className={styles.emptyText}>{t("No published schedules are available for swap.")}</p>
            ) : (
              <div className={styles.scheduleSwitcher}>
                {schedules.map(schedule => {
                  const isSelected = schedule.id === selectedSchedule?.id;

                  return (
                    <button
                      key={schedule.id}
                      type="button"
                      className={[styles.scheduleButton, isSelected ? styles.scheduleButtonActive : ""].filter(Boolean).join(" ")}
                      aria-pressed={isSelected}
                      onClick={() => handleOpenSchedule(schedule)}
                    >
                      <span className={styles.scheduleDateBadge}>
                        <span>{formatScheduleMonthOnly(schedule).slice(0, 3)}</span>
                        <strong>{schedule.year}</strong>
                      </span>

                      <span className={styles.scheduleButtonContent}>
                        <span className={styles.scheduleButtonName}>{schedule.name}</span>
                        <span className={styles.scheduleButtonMeta}>
                          {`${schedule.shopName || t("Shop {0}", schedule.shopId)} / ${schedule.containerName || t("Container {0}", schedule.containerId)}${schedule.allowSwap === false ? t(" · Swaps disabled") : ""}`}
                        </span>
                      </span>
                    </button>
                  );
                })}
              </div>
            )}

            {selectedSchedule && selectedSlot ? (
              <div className={styles.selectedShiftCard}>
                <span>{t("Selected shift")}</span>
                <strong>{buildOwnShiftLabel(selectedSchedule, selectedSlot, selectedPeriod)}</strong>
              </div>
            ) : (
              <p className={styles.helperText}>{t("Choose a schedule, then select one of your shifts from the dialog.")}</p>
            )}

            <SwapStatisticsCard stats={swapPreviewStats} summary={t("Give away impact")} collapsible defaultExpanded={false} />

            <div className={styles.formGrid}>
              <div className={`${styles.field} ${styles.fieldWide}`}>
                <span className={styles.fieldLabel}>{t("Receiver")}</span>
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
              </div>

              {targetMode === "private" ? (
                <div className={styles.field}>
                  <span className={styles.fieldLabel}>{t("Employee")}</span>
                  <EmployeeTargetCombobox
                    employees={sortedTargetEmployees}
                    selectedEmployeeId={resolvedTargetEmployeeId}
                    loading={targetEmployeesQuery.isLoading}
                    onChange={setTargetEmployeeId}
                  />
                </div>
              ) : null}

              <button
                type="button"
                className={styles.primaryButton}
                disabled={
                  isActionBusy ||
                  !selectedSchedule ||
                  selectedSchedule.allowSwap === false ||
                  !selectedSlot ||
                  (targetMode === "private" && !resolvedTargetEmployeeId)
                }
                onClick={handleCreateOffer}
              >
                {createSwapMutation.isPending ? t("Creating...") : t("Offer shift")}
              </button>
            </div>
            </section>
          ) : null}

      </dialog>
      {createPortal(<ConfirmDialog
        open={pendingUnpinSwapId !== null}
        variant="confirm"
        title={t("Unpin swap?")}
        message={t("Remove {0} from your pinned swaps?", pendingUnpinSwap?.scheduleName ?? "this swap")}
        confirmText={t("Unpin")}
        onCancel={() => setPendingUnpinSwapId(null)}
        onConfirm={handleConfirmUnpin}
      />, isHistoryExpanded && historyDialogElement ? historyDialogElement : document.body)}
    </div>
  );
}
