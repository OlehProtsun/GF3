import { useId, type Ref } from "react";
import type { EmployeeSchedule, EmployeeScheduleSlot } from "@entities/employee-schedule";
import { formatGraphIntervals, mergeGraphIntervalsForDisplay } from "@entities/containers/model/graphWorkspace";
import { dateTimeFormat, t } from "@shared/i18n";
import { getPolishPublicHoliday } from "@shared/lib/polishPublicHolidays";
import { getScheduleEmployeeLabel } from "./employeeScheduleMatrix";
import styles from "./EmployeeSchedulePage.module.css";
import { ScheduleShiftBadges } from "./PendingCorrectionBadge";
import { getScheduleStatusTone, type ScheduleShiftStatus } from "./scheduleShiftStatuses";

const scheduleWeekdayFormatter = dateTimeFormat("en-GB", {
  weekday: "long",
  timeZone: "UTC",
});

const scheduleWeekdayShortFormatter = dateTimeFormat("en-GB", {
  weekday: "short",
  timeZone: "UTC",
});

export function parseTimeMinutes(value: string) {
  const match = value.trim().match(/^(\d{1,2}):(\d{2})/);
  if (!match) {
    return null;
  }

  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (!Number.isInteger(hour) || !Number.isInteger(minute)) {
    return null;
  }

  return Math.min(23, Math.max(0, hour)) * 60 + Math.min(59, Math.max(0, minute));
}

type DailyScheduleWorker = {
  employeeId: number;
  label: string;
  shifts: EmployeeScheduleSlot[];
  relatedScheduleNames: string[];
  isAllDay: boolean;
  firstStartMinutes: number;
};

function isAllDayScheduleSlot(slot: EmployeeScheduleSlot) {
  const fromTime = slot.fromTime.trim();
  const toTime = slot.toTime.trim();
  return fromTime === "00:00" && (toTime === "00:00" || toTime === "23:59" || toTime === "24:00");
}

export function buildDailyScheduleWorkers(
  schedule: EmployeeSchedule,
  dayOfMonth: number,
  fallbackEmployeeId: number,
  fallbackLabel: string,
) {
  const employeesById = new Map((schedule.employees ?? []).map(employee => [employee.employeeId, employee] as const));
  const slotsByEmployeeId = new Map<number, EmployeeScheduleSlot[]>();
  for (const slot of schedule.slots) {
    const employeeId = slot.employeeId;
    if (slot.dayOfMonth !== dayOfMonth || !employeeId || employeeId < 0) continue;
    const employeeSlots = slotsByEmployeeId.get(employeeId) ?? [];
    employeeSlots.push(slot);
    slotsByEmployeeId.set(employeeId, employeeSlots);
  }

  const relatedNamesByEmployeeId = new Map<number, Set<string>>();
  for (const assignment of schedule.relatedScheduleAssignments ?? []) {
    if (assignment.dayOfMonth !== dayOfMonth || !(assignment.employeeId > 0)) continue;
    const names = relatedNamesByEmployeeId.get(assignment.employeeId) ?? new Set<string>();
    names.add(assignment.scheduleName.trim() || t("Schedule #{0}", assignment.scheduleId));
    relatedNamesByEmployeeId.set(assignment.employeeId, names);
  }

  const getWorkerLabel = (employeeId: number) => {
    const employee = employeesById.get(employeeId);
    return employee ? getScheduleEmployeeLabel(employee) : employeeId === fallbackEmployeeId
      ? fallbackLabel
      : t("Employee #{0}", employeeId);
  };

  const workers = [...slotsByEmployeeId.entries()].map<DailyScheduleWorker>(([employeeId, slots]) => {
    const sortedSlots = [...slots].sort((left, right) =>
      (parseTimeMinutes(left.fromTime) ?? Number.MAX_SAFE_INTEGER) -
        (parseTimeMinutes(right.fromTime) ?? Number.MAX_SAFE_INTEGER) ||
      left.toTime.localeCompare(right.toTime),
    );
    return {
      employeeId,
      label: getWorkerLabel(employeeId),
      shifts: sortedSlots,
      relatedScheduleNames: [...(relatedNamesByEmployeeId.get(employeeId) ?? [])]
        .sort((left, right) => left.localeCompare(right)),
      isAllDay: sortedSlots.some(isAllDayScheduleSlot),
      firstStartMinutes: parseTimeMinutes(sortedSlots[0]?.fromTime ?? "") ?? Number.MAX_SAFE_INTEGER,
    };
  });

  const timedWorkers = workers
    .filter(worker => !worker.isAllDay)
    .sort((left, right) =>
      left.firstStartMinutes - right.firstStartMinutes || left.label.localeCompare(right.label),
    );
  const allDayWorkers = workers
    .filter(worker => worker.isAllDay)
    .sort((left, right) => left.label.localeCompare(right.label));
  const middleIndex = Math.ceil(timedWorkers.length / 2);
  const relatedOnlyWorkers = [...relatedNamesByEmployeeId]
    .filter(([employeeId]) => !slotsByEmployeeId.has(employeeId))
    .map<DailyScheduleWorker>(([employeeId, names]) => ({
      employeeId,
      label: getWorkerLabel(employeeId),
      shifts: [],
      relatedScheduleNames: [...names].sort((left, right) => left.localeCompare(right)),
      isAllDay: false,
      firstStartMinutes: Number.MAX_SAFE_INTEGER,
    }))
    .sort((left, right) => left.label.localeCompare(right.label) || left.employeeId - right.employeeId);

  return [
    ...timedWorkers.slice(0, middleIndex),
    ...allDayWorkers,
    ...timedWorkers.slice(middleIndex),
    ...relatedOnlyWorkers,
  ];
}

function formatDailyWorkerShifts(worker: DailyScheduleWorker) {
  const relatedNames = worker.relatedScheduleNames.join(", ");
  if (worker.shifts.length === 0) return relatedNames;

  const ownShifts = worker.isAllDay
    ? t("All day")
    : formatGraphIntervals(mergeGraphIntervalsForDisplay(worker.shifts, { mergeTouching: true }));
  return relatedNames && ownShifts ? `${ownShifts}, ${relatedNames}` : ownShifts || relatedNames;
}

export function buildDailyScheduleDays(selectedSchedule: EmployeeSchedule | null, currentEmployeeId?: number | null) {
  if (!selectedSchedule) return [];

  const { year, month, slots, relatedScheduleAssignments } = selectedSchedule;
  const daysInMonth = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;
    const date = new Date(Date.UTC(year, month - 1, dayOfMonth));
    const isWorkingDay = currentEmployeeId === undefined
      ? slots.some(slot => slot.dayOfMonth === dayOfMonth && (slot.employeeId ?? 0) > 0)
      : currentEmployeeId !== null && currentEmployeeId > 0 && (
        slots.some(slot => slot.dayOfMonth === dayOfMonth && slot.employeeId === currentEmployeeId) ||
        (relatedScheduleAssignments ?? []).some(assignment =>
          assignment.dayOfMonth === dayOfMonth && assignment.employeeId === currentEmployeeId)
      );

    return {
      dayOfMonth,
      weekdayLong: scheduleWeekdayFormatter.format(date),
      weekdayShort: scheduleWeekdayShortFormatter.format(date).replace(".", ""),
      isWorkingDay,
    };
  });
}

export function getDefaultDailyScheduleDay(selectedSchedule: EmployeeSchedule | null, dailyScheduleDays: ReturnType<typeof buildDailyScheduleDays>) {
  if (!selectedSchedule || dailyScheduleDays.length === 0) {
    return 1;
  }

  const now = new Date();
  if (now.getUTCFullYear() === selectedSchedule.year && now.getUTCMonth() + 1 === selectedSchedule.month) {
    return Math.min(now.getUTCDate(), dailyScheduleDays.length);
  }

  return dailyScheduleDays.find(day => day.isWorkingDay)?.dayOfMonth ?? 1;
}

export function EmployeeScheduleDailyBody({
  selectedSchedule, dailyScheduleDays, activeDailyScheduleDay, dailyScheduleWorkers,
  activeDailyScheduleDayRef, showPublicHolidays, onSelectDay, panelId, className, shiftStatuses,
}: {
  selectedSchedule: EmployeeSchedule;
  dailyScheduleDays: ReturnType<typeof buildDailyScheduleDays>;
  activeDailyScheduleDay: number;
  dailyScheduleWorkers: ReturnType<typeof buildDailyScheduleWorkers>;
  activeDailyScheduleDayRef: Ref<HTMLButtonElement>;
  showPublicHolidays: boolean;
  onSelectDay: (day: number) => void;
  panelId?: string;
  className?: string;
  shiftStatuses?: ReadonlyMap<number, ScheduleShiftStatus>;
}) {
  const generatedPanelId = useId();
  const contentId = panelId ?? generatedPanelId;
  const activeDayInfo = dailyScheduleDays.find(day => day.dayOfMonth === activeDailyScheduleDay);
  const historicalWorkers = shiftStatuses ? [...shiftStatuses].filter(([id]) => id > 0 &&
    !dailyScheduleWorkers.some(worker => worker.employeeId === id)) : [];
  return (
    <div className={className ? `${styles.dailyScheduleShell} ${className}` : styles.dailyScheduleShell}>
      <div className={styles.dailyScheduleDays} role="tablist" aria-label={t("Schedule days")}>
        {dailyScheduleDays.map(day => {
          const isSelected = day.dayOfMonth === activeDailyScheduleDay;
          const holiday = showPublicHolidays && selectedSchedule
            ? getPolishPublicHoliday(selectedSchedule.year, selectedSchedule.month, day.dayOfMonth) : null;
          return (
            <button
              key={day.dayOfMonth}
              ref={isSelected ? activeDailyScheduleDayRef : undefined}
              type="button"
              role="tab"
              aria-selected={isSelected}
              aria-controls={contentId}
              aria-label={`${day.weekdayLong} ${day.dayOfMonth}, ${day.isWorkingDay ? t("working day") : t("day off")}${holiday ? `, ${t("Holiday")}: ${t(holiday.name)}` : ""}`}
              title={holiday ? t(holiday.name) : undefined}
              className={[
                styles.dailyScheduleDay,
                holiday ? styles.dailyScheduleDayHoliday : "",
                day.isWorkingDay ? styles.dailyScheduleDayWorking : styles.dailyScheduleDayOff,
                isSelected ? styles.dailyScheduleDaySelected : "",
              ].filter(Boolean).join(" ")}
              onClick={() => onSelectDay(day.dayOfMonth)}
            >
              <span>{day.weekdayShort}</span>
              <strong>{day.dayOfMonth}</strong>
            </button>
          );
        })}
      </div>

      <div
        id={contentId}
        data-employee-motion="schedule-content"
        data-motion-key={`${selectedSchedule.id}:${activeDailyScheduleDay}`}
        className={styles.dailyScheduleBody}
        role="tabpanel"
        aria-label={`${activeDayInfo?.weekdayLong ?? t("Day")} ${activeDailyScheduleDay}`}
      >
        <div className={styles.dailyScheduleDate} aria-hidden="true">
          <span>{activeDayInfo?.weekdayLong ?? t("Day")}</span>
          <strong>{activeDailyScheduleDay}</strong>
        </div>

        <div className={styles.dailyScheduleWorkers}>
          {dailyScheduleWorkers.map(worker => {
            const status = shiftStatuses?.get(worker.employeeId);
            return <article key={worker.employeeId} className={styles.dailyScheduleWorker}
              data-schedule-status={status && getScheduleStatusTone(status)}>
              <span>{worker.label}</span>
              <strong>{formatDailyWorkerShifts(worker)}</strong>
              {status && <span className={styles.dailyScheduleStatusBadges}><ScheduleShiftBadges status={status} /></span>}
            </article>;
          })}
          {historicalWorkers.map(([id, status]) => (
            <article key={id} className={styles.dailyScheduleWorker} data-schedule-status={getScheduleStatusTone(status)}>
              <span>{status.label || t("Employee #{0}", id)}</span>
              <strong>{t(status.correctionPending || status.correctionApproved ? "Day off" : "No current shift")}</strong>
              <span className={styles.dailyScheduleStatusBadges}><ScheduleShiftBadges status={status} /></span>
            </article>
          ))}
          {dailyScheduleWorkers.length === 0 && historicalWorkers.length === 0 && (
            <div className={styles.dailyScheduleEmpty}>
              <strong>{t("No one is scheduled.")}</strong>
              <span>{t("This day has no assigned shifts.")}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
