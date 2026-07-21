import {
  useDeferredValue,
  useEffect,
  useMemo,
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
import { ArrowIcon, CloseIcon, PinIcon, SearchIcon, ShiftGiveAwayIcon, SwapHistoryIcon, SwapOffersIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeSwapPage.module.css";

const scheduleMonthOnlyFormatter = new Intl.DateTimeFormat("en-GB", {
  month: "long",
  timeZone: "UTC",
});

const swapDateFormatter = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  day: "2-digit",
  month: "short",
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
  return swapDateFormatter.format(new Date(Date.UTC(value.year, value.month - 1, value.dayOfMonth)));
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
  return employee.displayName?.trim() || fullName || `Employee #${employee.id}`;
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
      {label}
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
      <span>{label}</span>
      <div className={styles.statsCompareValues}>
        <strong>{kind === "hours" && typeof before === "number" ? formatHours(before) : before}</strong>
        <small>before</small>
        <span className={styles.statsArrow}>{"->"}</span>
        <strong>{kind === "hours" && typeof after === "number" ? formatHours(after) : after}</strong>
        <small>after</small>
        <StatsDeltaBadge value={delta} kind={kind} />
      </div>
    </div>
  );
}

function SwapStatisticsCard({
  stats,
  summary = "Your month",
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
          <span>Statistics</span>
          <strong>{summary}</strong>
        </div>

        {collapsible ? (
          <button
            type="button"
            className={styles.statsToggleButton}
            aria-label={isExpanded ? "Collapse statistics" : "Expand statistics"}
            aria-expanded={isExpanded}
            onClick={() => setIsExpanded(value => !value)}
          >
            <ArrowIcon size={12} />
          </button>
        ) : null}
      </div>

      {shouldShowBody ? (
        <div className={styles.statsCompareGrid}>
          <StatsCompareItem label="Total hours" before={stats.hoursBefore} after={stats.hoursAfter} kind="hours" />
          <StatsCompareItem label="Work days" before={stats.workDaysBefore} after={stats.workDaysAfter} kind="days" />
          <StatsCompareItem label="Free days" before={stats.freeDaysBefore} after={stats.freeDaysAfter} kind="days" />
        </div>
      ) : null}
    </div>
  );
}

function CollapseToggleButton({
  isExpanded,
  label,
  onToggle,
}: {
  isExpanded: boolean;
  label: string;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      className={styles.collapseButton}
      aria-label={isExpanded ? `Collapse ${label}` : `Expand ${label}`}
      aria-expanded={isExpanded}
      title={isExpanded ? `Collapse ${label}` : `Expand ${label}`}
      onClick={onToggle}
    >
      <ArrowIcon size={13} />
    </button>
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
  const actionLabel = `${isPinned ? "Unpin" : "Pin"} ${label}`;
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

  useEffect(() => {
    if (!schedule) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose, schedule]);

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
  const slotByDay = new Map<number, EmployeeScheduleSlot>();
  slots.forEach(slot => {
    if (!slotByDay.has(slot.dayOfMonth)) {
      slotByDay.set(slot.dayOfMonth, slot);
    }
  });
  const confirmSlot = slots.find(slot => slot.id === confirmSlotId) ?? null;
  const selectedPeriod = confirmSlot && periodMode === "custom"
    ? getSafeCustomPeriod(confirmSlot, periodFromTime, periodToTime)
    : confirmSlot
      ? { fromTime: confirmSlot.fromTime, toTime: confirmSlot.toTime }
      : null;
  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
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
    <div className={styles.dialogOverlay} role="dialog" aria-modal="true" aria-label="Choose shift" onMouseDown={handleOverlayMouseDown}>
      <div className={styles.shiftDialog}>
        <div className={styles.dialogHeader}>
          <div>
            <span className={styles.dialogEyebrow}>{formatScheduleMonth(schedule)}</span>
            <h3>{`Schedule name: ${schedule.name}`}</h3>
          </div>

          <button type="button" className={styles.iconButton} aria-label="Close" onClick={onClose}>
            <CloseIcon size={16} />
          </button>
        </div>

        <div className={styles.shiftScroll}>
          <div className={styles.shiftGrid}>
            {dayNumbers.map(dayOfMonth => {
              const slot = slotByDay.get(dayOfMonth) ?? null;
              const isSelected = Boolean(slot && slot.id === selectedSlotId);
              const isConfirming = Boolean(slot && slot.id === confirmSlotId);

              return (
                <button
                  key={dayOfMonth}
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
                <span>Confirm shift</span>
                <strong>{buildOwnShiftLabel(schedule, confirmSlot, selectedPeriod)}</strong>
              </div>
              <p>Choose the whole shift or only the time period you want to give away.</p>
              <div className={styles.periodModeRow}>
                <button
                  type="button"
                  className={[styles.segmentButton, periodMode === "full" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setPeriodMode("full")}
                >
                  Full shift
                </button>
                <button
                  type="button"
                  className={[styles.segmentButton, periodMode === "custom" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                  onClick={() => setPeriodMode("custom")}
                >
                  Custom period
                </button>
              </div>
              {periodMode === "custom" && selectedPeriod ? (
                <div className={styles.timePanel}>
                  <div className={styles.timeStepper}>
                    <button type="button" aria-label="Decrease start time" onClick={() => adjustCustomPeriod("from", -TIME_STEP_MINUTES)}>
                      -
                    </button>
                    <span>
                      <strong>{selectedPeriod.fromTime}</strong>
                      <small>From</small>
                    </span>
                    <button type="button" aria-label="Increase start time" onClick={() => adjustCustomPeriod("from", TIME_STEP_MINUTES)}>
                      +
                    </button>
                  </div>

                  <span className={styles.timeDash}>-</span>

                  <div className={styles.timeStepper}>
                    <button type="button" aria-label="Decrease end time" onClick={() => adjustCustomPeriod("to", -TIME_STEP_MINUTES)}>
                      -
                    </button>
                    <span>
                      <strong>{selectedPeriod.toTime}</strong>
                      <small>To</small>
                    </span>
                    <button type="button" aria-label="Increase end time" onClick={() => adjustCustomPeriod("to", TIME_STEP_MINUTES)}>
                      +
                    </button>
                  </div>
                </div>
              ) : null}
              <div className={styles.confirmActions}>
                <button type="button" className={styles.secondaryButton} onClick={() => onConfirmSlotChange(null)}>
                  Back
                </button>
                <button
                  type="button"
                  className={styles.primaryButton}
                  onClick={() => {
                    if (selectedPeriod) {
                      onChoose(confirmSlot, selectedPeriod);
                    }
                  }}
                >
                  Choose shift
                </button>
              </div>
            </div>
          </div>
        ) : null}
      </div>
    </div>
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
  const unavailableReason = isOpen && !swap.canAccept
    ? swap.acceptanceUnavailableReason
      ?? (isScheduleLocked
        ? "Schedule is locked while a manager is editing it."
        : swap.isCreatedByCurrentEmployee
          ? "This is your own swap offer."
          : null)
    : null;

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
      title={isExpanded ? "Click to collapse this swap" : "Click to expand this swap"}
      onClick={handleCardClick}
      onKeyDown={handleCardKeyDown}
    >
      <div className={styles.offerHeader}>
        <div className={styles.offerTitle}>
          <strong>{swap.scheduleName}</strong>
        </div>

        <div className={styles.offerHeaderActions}>
          <div className={styles.offerBadges}>
            {isOpen ? (
              <span className={[
                styles.badge,
                swap.canAccept ? styles.badgeCan : styles.badgeCant,
              ].join(" ")}>
                {swap.canAccept ? "Can" : "Can\u2019t"}
              </span>
            ) : (
              <span className={[styles.badge, styles.badgeMuted].join(" ")}>
                {swap.status === "accepted" ? "Accepted" : "Cancelled"}
              </span>
            )}
            <span className={[
              styles.badge,
              swap.visibility === "private" ? styles.badgePrivate : "",
            ].filter(Boolean).join(" ")}>
              {swap.visibility === "private" ? "Private" : "Public"}
            </span>
            {isScheduleLocked ? <span className={[styles.badge, styles.badgeLocked].join(" ")}>Locked</span> : null}
          </div>
          <PinToggleButton
            isPinned={isPinned}
            label={`${swap.scheduleName} swap`}
            onToggle={() => onTogglePin(swap.id)}
          />
        </div>
      </div>

      <div className={styles.offerMeta}>
        <span>{`${formatSwapDate(swap)} - ${swap.fromTime} - ${swap.toTime}`}</span>
      </div>

      {isExpanded ? (
        <>
          <div className={styles.detailGrid}>
            <div className={styles.detailItem}>
              <span>Location</span>
              <strong>{swap.shopName || swap.containerName || "Schedule"}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>From</span>
              <strong>{swap.fromEmployeeName}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>Target</span>
              <strong>{swap.targetEmployeeName ?? "Everyone"}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>Shift hours</span>
              <strong>{formatHours(swap.shiftHours)}</strong>
            </div>
            {swap.acceptedByEmployeeName ? (
              <div className={styles.detailItem}>
                <span>Accepted by</span>
                <strong>{swap.acceptedByEmployeeName}</strong>
              </div>
            ) : null}
          </div>

          <SwapStatisticsCard stats={stats} />

          {unavailableReason ? (
            <p className={styles.unavailableText}>{unavailableReason}</p>
          ) : null}

          {swap.canAccept || swap.canCancel ? (
            <div className={styles.cardActions}>
              {swap.canAccept ? (
                <button
                  type="button"
                  className={styles.primaryButton}
                  disabled={isBusy}
                  onClick={() => onAccept(swap)}
                >
                  Accept
                </button>
              ) : null}
              {swap.canCancel ? (
                <button
                  type="button"
                  className={styles.secondaryButton}
                  disabled={isBusy}
                  onClick={() => onCancel(swap)}
                >
                  Cancel offer
                </button>
              ) : null}
            </div>
          ) : null}
        </>
      ) : null}
    </article>
  );
}

export function EmployeeSwapPage() {
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
  const [swapSearchQuery, setSwapSearchQuery] = useState("");
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
  const filteredOpenSwaps = useMemo(
    () => filterShiftSwaps(openSwaps, deferredSwapSearchQuery),
    [deferredSwapSearchQuery, openSwaps],
  );
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
      { onError: error => setActionError(getErrorMessage(error, "Could not pin this swap.")) },
    );
  };

  const handleConfirmUnpin = () => {
    if (pendingUnpinSwapId !== null) {
      setSwapPinMutation.mutate(
        { swapId: pendingUnpinSwapId, pinned: false },
        { onError: error => setActionError(getErrorMessage(error, "Could not unpin this swap.")) },
      );
    }
    setPendingUnpinSwapId(null);
  };

  const handleOpenSchedule = (schedule: EmployeeSchedule) => {
    if (schedule.allowSwap === false) {
      setActionError(`Swaps are not allowed for “${schedule.name}”.`);
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
      setActionError("Choose a published schedule and one of your shifts.");
      return;
    }

    if (selectedSchedule.allowSwap === false) {
      setActionError("Swaps are not allowed for this schedule.");
      return;
    }

    if (targetMode === "private" && !resolvedTargetEmployeeId) {
      setActionError("Choose who should receive this private swap.");
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
          setSelectedSlotId(null);
          setSelectedPeriod(null);
        },
        onError: error => setActionError(getErrorMessage(error, "Could not create this swap offer.")),
      },
    );
  };

  const handleAccept = (swap: ShiftSwap) => {
    setActionError(null);
    acceptSwapMutation.mutate(swap.id, {
      onError: error => setActionError(getErrorMessage(error, "Could not accept this swap offer.")),
    });
  };

  const handleCancel = (swap: ShiftSwap) => {
    setActionError(null);
    cancelSwapMutation.mutate(swap.id, {
      onError: error => setActionError(getErrorMessage(error, "Could not cancel this swap offer.")),
    });
  };

  return (
    <div className={workspaceStyles.page}>
      {loadError ? <ErrorBanner dismissible={false}>{getErrorMessage(loadError, "Could not load swap data.")}</ErrorBanner> : null}
      {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

      <section className={workspaceStyles.panel}>
        <div className={styles.panelHeaderRow}>
          <div className={styles.sectionHeading}>
            <span className={styles.sectionIcon} aria-hidden="true">
              <ShiftGiveAwayIcon size={20} />
            </span>
            <div>
              <span className={workspaceStyles.panelEyebrow}>Give away a shift</span>
              <h1 className={workspaceStyles.panelTitle}>Create a swap offer</h1>
            </div>
          </div>

          <CollapseToggleButton
            isExpanded={isGiveAwayExpanded}
            label="give away a shift"
            onToggle={() => setIsGiveAwayExpanded(value => !value)}
          />
        </div>

        {isGiveAwayExpanded ? (
          <>
            {schedules.length === 0 ? (
              <p className={styles.emptyText}>No published schedules are available for swap.</p>
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
                          {`${schedule.shopName || `Shop ${schedule.shopId}`} / ${schedule.containerName || `Container ${schedule.containerId}`}${schedule.allowSwap === false ? " · Swaps disabled" : ""}`}
                        </span>
                      </span>
                    </button>
                  );
                })}
              </div>
            )}

            {selectedSchedule && selectedSlot ? (
              <div className={styles.selectedShiftCard}>
                <span>Selected shift</span>
                <strong>{buildOwnShiftLabel(selectedSchedule, selectedSlot, selectedPeriod)}</strong>
              </div>
            ) : (
              <p className={styles.helperText}>Choose a schedule, then select one of your shifts from the dialog.</p>
            )}

            <SwapStatisticsCard stats={swapPreviewStats} summary="Give away impact" collapsible defaultExpanded={false} />

            <div className={styles.formGrid}>
              <div className={`${styles.field} ${styles.fieldWide}`}>
                <span className={styles.fieldLabel}>Receiver</span>
                <div className={styles.segmentRow}>
                  <button
                    type="button"
                    className={[styles.segmentButton, targetMode === "public" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                    onClick={() => setTargetMode("public")}
                  >
                    Everyone
                  </button>
                  <button
                    type="button"
                    className={[styles.segmentButton, targetMode === "private" ? styles.segmentButtonActive : ""].filter(Boolean).join(" ")}
                    onClick={() => setTargetMode("private")}
                  >
                    Specific employee
                  </button>
                </div>
              </div>

              {targetMode === "private" ? (
                <div className={styles.field}>
                  <span className={styles.fieldLabel}>Employee</span>
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
                {createSwapMutation.isPending ? "Creating..." : "Offer shift"}
              </button>
            </div>
          </>
        ) : null}
      </section>

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

      <section className={workspaceStyles.panel}>
        <div className={styles.sectionHeading}>
          <span className={styles.sectionIcon} aria-hidden="true">
            <SwapOffersIcon size={20} />
          </span>
          <div>
            <span className={workspaceStyles.panelEyebrow}>Swap offers</span>
            <h2 className={workspaceStyles.panelTitle}>Open swaps</h2>
          </div>
        </div>
        <div className={styles.offerToolbar}>
          <label className={styles.swapSearchField} htmlFor="employee-swap-search">
            <SearchIcon size={16} className={styles.swapSearchIcon} />
            <input
              id="employee-swap-search"
              className={styles.swapSearchInput}
              type="search"
              value={swapSearchQuery}
              placeholder="Search employee, date or schedule"
              aria-label="Search swaps by giver, receiver, date or schedule"
              onChange={event => setSwapSearchQuery(event.target.value)}
            />
            {hasSwapSearch ? (
              <button
                type="button"
                className={styles.clearSearchButton}
                aria-label="Clear swap search"
                title="Clear search"
                onClick={() => setSwapSearchQuery("")}
              >
                <CloseIcon size={13} />
              </button>
            ) : null}
          </label>
          <span className={styles.searchResultText}>
            {hasSwapSearch ? `${filteredOpenSwaps.length} of ${openSwaps.length}` : `${openSwaps.length} offers`}
          </span>
        </div>
        <div className={[
          styles.offerList,
          filteredOpenSwaps.length >= 5 ? styles.offerListScrollable : "",
        ].filter(Boolean).join(" ")}>
          {openSwaps.length === 0 ? (
            <p className={styles.emptyText}>No open swap offers right now.</p>
          ) : filteredOpenSwaps.length === 0 ? (
            <p className={styles.emptyText}>{`No open swap offers match "${swapSearchQuery.trim()}".`}</p>
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

      <section className={workspaceStyles.panel}>
        <div className={styles.sectionHeading}>
          <span className={styles.sectionIcon} aria-hidden="true">
            <SwapHistoryIcon size={20} />
          </span>
          <div>
            <span className={workspaceStyles.panelEyebrow}>History</span>
            <h2 className={workspaceStyles.panelTitle}>Recent swap activity</h2>
          </div>
        </div>
        <div className={[
          styles.offerList,
          swapHistory.length >= 5 ? styles.offerListScrollable : "",
        ].filter(Boolean).join(" ")}>
          {swapHistory.length === 0 ? (
            <p className={styles.emptyText}>Accepted and cancelled swaps will appear here.</p>
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

      <ConfirmDialog
        open={pendingUnpinSwapId !== null}
        variant="confirm"
        title="Unpin swap?"
        message={`Remove ${pendingUnpinSwap?.scheduleName ?? "this swap"} from your pinned swaps?`}
        confirmText="Unpin"
        onCancel={() => setPendingUnpinSwapId(null)}
        onConfirm={handleConfirmUnpin}
      />
    </div>
  );
}
