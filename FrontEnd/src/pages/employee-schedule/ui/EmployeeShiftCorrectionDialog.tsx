import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useId, useRef, useState, type CSSProperties } from "react";
import { createPortal } from "react-dom";
import { useSwapModalEnvironment } from "@pages/employee-swap/ui/useSwapModalEnvironment";
import type { EmployeeSchedule, EmployeeScheduleSlot } from "@entities/employee-schedule";
import type { CreateShiftCorrectionInput, ShiftCorrectionRequest } from "@entities/shift-corrections";
import { formatShiftCorrectionDelta, shiftCorrectionOriginalMinutes } from "@entities/shift-corrections/model/presentation";
import type { ShiftSwap } from "@entities/shift-swaps/model/types";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { parseFlexibleTimeSegment } from "@shared/lib/timeRange";
import { IosButton } from "@shared/ui/components/IosButton";
import { ArrowRightIcon, CheckIcon, ChevronLeftIcon, ChevronRightIcon, ClockIcon, CloseIcon, NoteIcon, ScheduleDetailsIcon, ScheduleIcon } from "@shared/ui/icons";
import scheduleStyles from "./EmployeeScheduleSelectDialog.module.css";
import styles from "./EmployeeShiftCorrectionDialog.module.css";

type EmployeeShiftCorrectionDialogProps = {
  open: boolean;
  schedules: EmployeeSchedule[];
  employeeId: number | null;
  existingRequests: ShiftCorrectionRequest[];
  activeSwaps: readonly Pick<ShiftSwap, "status" | "scheduleSlotId" | "returnScheduleSlotId">[];
  isSending: boolean;
  submitted?: boolean;
  errorMessage?: string | null;
  onCancel: () => void;
  onSubmit: (input: CreateShiftCorrectionInput) => void;
};

const steps = [
  { label: "Schedule", title: "Choose a schedule" },
  { label: "Day", title: "Choose a day" },
  { label: "Shift", title: "Adjust shift time" },
  { label: "Comment", title: "Add a comment" },
  { label: "Review", title: "Review your request" },
] as const;

const commentMaxLength = 1000;
const timePattern = /^(?:[01]\d|2[0-3]):[0-5]\d$/;
const periodFormatter = dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" });
const dateFormatter = dateTimeFormat("en-GB", { dateStyle: "full", timeZone: "UTC" });

function formatShift(from: string, to: string) {
  return from && to ? from + " – " + to : "—";
}

function moveTime(value: string, delta: number) {
  const time = parseFlexibleTimeSegment(value);
  if (!time) return value;
  const minutes = time.totalMinutes + delta;
  if (minutes < 0 || minutes > 1439) return value;
  return String(Math.floor(minutes / 60)).padStart(2, "0") + ":" + String(minutes % 60).padStart(2, "0");
}

export function EmployeeShiftCorrectionDialog({ open, ...props }: EmployeeShiftCorrectionDialogProps) {
  return open ? <ShiftCorrectionWizard {...props} /> : null;
}

function ShiftCorrectionWizard({
  schedules, employeeId, existingRequests, activeSwaps, isSending, submitted, errorMessage, onCancel, onSubmit,
}: Omit<EmployeeShiftCorrectionDialogProps, "open">) {
  const stepTitleId = useId();
  const commentId = useId();
  const commentHelpId = useId();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const bodyRef = useRef<HTMLDivElement>(null);
  const selectedDayRef = useRef<HTMLButtonElement>(null);
  const stepTitleRef = useRef<HTMLHeadingElement>(null);
  const [step, setStep] = useState(0);
  const [scheduleId, setScheduleId] = useState<number | null>(null);
  const [selectedDay, setSelectedDay] = useState<number | null>(null);
  const [slotId, setSlotId] = useState<number | null>(null);
  const [requestedFrom, setRequestedFrom] = useState("");
  const [requestedTo, setRequestedTo] = useState("");
  const [comment, setComment] = useState("");
  const [requestDayOff, setRequestDayOff] = useState(false);
  useSwapModalEnvironment(dialogRef);

  useEffect(() => {
    if (bodyRef.current) bodyRef.current.scrollTop = 0;
    stepTitleRef.current?.focus({ preventScroll: true });
  }, [step, submitted]);

  useEffect(() => {
    if (step === 1) selectedDayRef.current?.scrollIntoView?.({ block: "nearest", inline: "nearest" });
  }, [step, selectedDay, slotId]);

  const publishedSchedules = schedules.filter(item => item.publicationStatus === "public");
  const schedule = publishedSchedules.find(item => item.id === scheduleId);
  const monthSchedules = schedule ? publishedSchedules.filter(item =>
    item.year === schedule.year && item.month === schedule.month) : [];
  const daysInMonth = schedule ? new Date(Date.UTC(schedule.year, schedule.month, 0)).getUTCDate() : 0;
  const pendingRequests = existingRequests.filter(request => request.status === "pending" && request.employeeId === employeeId);
  const pendingForMonth = pendingRequests.filter(request => request.year === schedule?.year && request.month === schedule?.month);

  function getDayDetails(day: number | null) {
    const ownSlotsOnDay = (item: EmployeeSchedule) => item.slots.filter(slot =>
      employeeId !== null && slot.employeeId === employeeId && slot.dayOfMonth === day);
    const slots = schedule ? ownSlotsOnDay(schedule).sort((a, b) =>
      a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime) || a.id - b.id) : [];
    const otherSlots: EmployeeScheduleSlot[] = [];
    const conflictsBySchedule = new Map<number, string>();
    for (const other of monthSchedules) {
      if (other.id !== scheduleId) {
        const assigned = ownSlotsOnDay(other);
        if (assigned.length) conflictsBySchedule.set(other.id, other.name);
        otherSlots.push(...assigned);
      }
      for (const assignment of other.relatedScheduleAssignments ?? []) {
        if (assignment.employeeId === employeeId && assignment.dayOfMonth === day && assignment.scheduleId !== scheduleId)
          conflictsBySchedule.set(assignment.scheduleId, assignment.scheduleName);
      }
    }
    const conflictNames = [...new Set([...conflictsBySchedule].map(([id, name]) => name.trim() || t("Schedule #{0}", id)))]
      .sort((a, b) => a.localeCompare(b));
    return { slots, otherSlots, conflictNames, pending: pendingForMonth.filter(request => request.dayOfMonth === day) };
  }

  const { slots: selectedSlots, otherSlots, conflictNames, pending: pendingOnDay } = getDayDetails(selectedDay);
  const selectedSlot = selectedSlots.find(slot => slot.id === slotId);
  const isPendingSlot = (slot: EmployeeScheduleSlot) => pendingRequests.some(request => request.scheduleSlotId === slot.id) ||
    pendingForMonth.some(request => request.dayOfMonth === slot.dayOfMonth && request.scheduleId === scheduleId &&
      (request.requestType === "removeShift" ||
        (request.originalFromTime === slot.fromTime && request.originalToTime === slot.toTime)));
  const dayOptions = step === 1 && schedule ? Array.from({ length: daysInMonth }, (_, index) => {
    const day = index + 1;
    const date = dateFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, day)));
    const details = getDayDetails(day);
    return (details.slots.length > 0 ? details.slots : [null]).map(slot => {
      const conflicts = slot === null ? details.conflictNames : [];
      const pending = slot ? isPendingSlot(slot) : details.pending.length > 0;
      return {
        day, date, slot,
        selected: selectedDay === day && slotId === (slot?.id ?? null),
        pending,
        disabled: pending || conflicts.length > 0,
        label: slot ? formatShift(slot.fromTime, slot.toTime) : conflicts.length > 0
          ? t(conflicts.length === 1 ? "Already working on another schedule ({0})" : "Already working on other schedules ({0})", conflicts.join(", "))
          : t("Day off"),
        detail: conflicts.length > 0 ? t("Choose another day or go back to select the schedule with the assigned shift.")
          : pending ? t("Pending approval") : slot ? null : t("Request a shift on this day"),
      };
    });
  }).flat() : [];
  const tabStop = dayOptions.find(option => option.selected && !option.disabled) ?? dayOptions.find(option => !option.disabled);

  const hasDay = Boolean(schedule && selectedDay !== null && selectedDay >= 1 && selectedDay <= daysInMonth);
  const isDayOff = hasDay && selectedSlots.length === 0;
  const canChooseDay = hasDay && employeeId !== null && (isDayOff
    ? slotId === null && conflictNames.length === 0 && pendingOnDay.length === 0
    : Boolean(selectedSlot && !isPendingSlot(selectedSlot)));
  const validPeriod = timePattern.test(requestedFrom) && timePattern.test(requestedTo) && requestedFrom < requestedTo;
  const unchanged = Boolean(selectedSlot && requestedFrom === (parseFlexibleTimeSegment(selectedSlot.fromTime)?.label ?? selectedSlot.fromTime)
    && requestedTo === (parseFlexibleTimeSegment(selectedSlot.toTime)?.label ?? selectedSlot.toTime));
  const overlaps = validPeriod && [...selectedSlots.filter(slot => slot.id !== selectedSlot?.id), ...otherSlots]
    .some(slot => {
      const from = parseFlexibleTimeSegment(slot.fromTime)?.label ?? slot.fromTime;
      const to = parseFlexibleTimeSegment(slot.toTime)?.label ?? slot.toTime;
      return from < requestedTo && requestedFrom < to;
    });
  const hasOpenSwap = activeSwaps.some(swap => swap.status === "open" && selectedSlots.some(slot =>
    slot.id === swap.scheduleSlotId || slot.id === swap.returnScheduleSlotId));
  const canRemoveShifts = Boolean(selectedSlot) && !hasOpenSwap && !selectedSlots.some(isPendingSlot) &&
    !pendingOnDay.some(request => request.scheduleId === scheduleId);
  const removalBlockedReason = hasOpenSwap
    ? t("Resolve the open swap or pending shift approval before requesting or approving this day off.")
    : t("This day already has a pending correction request.");
  const canSubmit = canChooseDay && (requestDayOff ? canRemoveShifts : validPeriod && !unchanged && !overlaps);
  const canContinue = step === 0 ? Boolean(schedule && employeeId !== null) : step === 1 ? canChooseDay : canSubmit;
  const monthLabel = schedule ? periodFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1))) : "";
  const dateLabel = schedule && selectedDay !== null
    ? dateFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, selectedDay))) : "";
  const originalShift = requestDayOff ? selectedSlots.map(slot => formatShift(slot.fromTime, slot.toTime)).join(", ")
    : selectedSlot ? formatShift(selectedSlot.fromTime, selectedSlot.toTime) : t("Day off");
  const requestedShift = requestDayOff ? "–" : formatShift(requestedFrom, requestedTo);

  function resetShift() {
    setSlotId(null);
    setRequestedFrom("");
    setRequestedTo("");
    setRequestDayOff(false);
  }

  function chooseSchedule(id: number) {
    if (id === scheduleId) return;
    setScheduleId(id);
    setSelectedDay(null);
    resetShift();
  }

  function chooseDay(day: number) {
    if (day === selectedDay) return;
    setSelectedDay(day);
    resetShift();
  }

  function chooseSlot(slot: EmployeeScheduleSlot) {
    if (slot.id === slotId) return;
    if (slot.dayOfMonth !== selectedDay) setRequestDayOff(false);
    setSelectedDay(slot.dayOfMonth);
    setSlotId(slot.id);
    setRequestedFrom(parseFlexibleTimeSegment(slot.fromTime)?.label ?? "");
    setRequestedTo(parseFlexibleTimeSegment(slot.toTime)?.label ?? "");
  }

  function cancel() {
    if (!isSending) onCancel();
  }

  function back() {
    if (!isSending && step > 0) setStep(step - 1);
  }

  function advance() {
    if (submitted || isSending || !canContinue) return;
    if (step < steps.length - 1) {
      setStep(step + 1);
      return;
    }
    if (!schedule || selectedDay === null) return;
    const request = {
      scheduleId: schedule.id,
      ...(comment.trim() ? { comment: comment.trim() } : {}),
    };
    if (requestDayOff) {
      onSubmit({ ...request, requestType: "removeShift", dayOfMonth: selectedDay });
      return;
    }
    const payload = {
      ...request,
      requestedFromTime: requestedFrom,
      requestedToTime: requestedTo,
    };
    if (selectedSlot) onSubmit({ ...payload, scheduleSlotId: selectedSlot.id });
    else if (isDayOff) onSubmit({ ...payload, dayOfMonth: selectedDay });
  }

  return createPortal(
    <dialog ref={dialogRef} className={styles.overlay} aria-label={t("Request a shift change")}
      onCancel={event => { event.preventDefault(); cancel(); }}
      onMouseDown={event => { if (event.target === event.currentTarget) cancel(); }}>
      <div className={styles.dialog}>
        <header className={styles.header}>
          {!submitted && step > 0 && <button type="button" className={styles.backButton} aria-label={t("Back")} disabled={isSending} onClick={back}>
            <ChevronLeftIcon size={18} />
          </button>}
          <ol className={styles.progress} aria-label={t("Request progress")}
            style={{ "--progress-complete": `${step * 100 / (steps.length - 1)}%` } as CSSProperties}>
            {steps.map((item, index) => <li key={item.label} aria-current={index === step ? "step" : undefined} data-complete={index < step}>
              <span className={styles.stepMarker} aria-hidden="true">{index < step ? <CheckIcon size={13} /> : index + 1}</span>
              <span className={styles.stepLabel}>{t(item.label)}</span>
            </li>)}
          </ol>
          {!submitted && <button type="button" className={styles.closeButton} aria-label={t("Close shift correction dialog")} disabled={isSending} onClick={cancel}>
            <CloseIcon size={18} />
          </button>}
        </header>

        <div className={styles.body} ref={bodyRef}>
          {submitted ? <div className={styles.success}>
            <span className={styles.successIcon} aria-hidden="true"><CheckIcon size={28} /></span>
            <h3 tabIndex={-1} ref={stepTitleRef}>{t("Request sent successfully!")}</h3>
            <p>{t("Your shift correction request has been sent to your manager for approval.")}</p>
            <span className={styles.successStatus}><ClockIcon size={14} />{t("Pending approval")}</span>
          </div> : <>
          <h3 id={stepTitleId} tabIndex={-1} ref={stepTitleRef}>{t(steps[step].title)}</h3>

          {step === 0 && <div className={styles.choices} role="group" aria-labelledby={stepTitleId}>
            {publishedSchedules.length === 0 && <p className={styles.hint}>{t("No published schedules")}</p>}
            {publishedSchedules.map(item => {
              const selected = item.id === scheduleId;
              const updated = formatScheduleLastUpdate(item.lastUpdatedAtUtc);
              return <button key={item.id} type="button" className={scheduleStyles.row + (selected ? " " + scheduleStyles.selected : "")}
                aria-pressed={selected} onClick={() => chooseSchedule(item.id)}>
                <span className={scheduleStyles.icon} aria-hidden="true"><ScheduleIcon size={20} /></span>
                <span className={scheduleStyles.iconDivider} aria-hidden="true" />
                <span className={scheduleStyles.copy}>
                  <strong>{item.name}</strong>
                  <span>{periodFormatter.format(new Date(Date.UTC(item.year, item.month - 1, 1)))}</span>
                  <span className={scheduleStyles.updated}>{item.lastUpdatedAtUtc ? t("Updated {0}", updated) : updated}</span>
                </span>
                <span className={scheduleStyles.indicator} aria-hidden="true">{selected && <CheckIcon size={15} />}</span>
              </button>;
            })}
          </div>}

          {step === 1 && schedule && <>
            <p className={styles.hint}>{schedule.name} — {monthLabel}</p>
            <div className={styles.shiftChoices} role="radiogroup" aria-labelledby={stepTitleId} onKeyDown={event => {
              if (!["ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight"].includes(event.key)) return;
              const rows = [...event.currentTarget.querySelectorAll<HTMLButtonElement>('[role="radio"]:not(:disabled)')];
              if (rows.length === 0) return;
              event.preventDefault();
              const index = rows.indexOf(event.target as HTMLButtonElement);
              const direction = event.key === "ArrowDown" || event.key === "ArrowRight" ? 1 : -1;
              const next = rows[(index + direction + rows.length) % rows.length];
              next.focus();
              next.click();
            }}>
              {dayOptions.map(option => (
                <button key={option.slot ? `shift-${option.slot.id}` : `day-${option.day}`} type="button" role="radio"
                  className={styles.shiftChoice} aria-checked={option.selected && !option.disabled}
                  data-pending={option.pending || undefined}
                  tabIndex={option === tabStop ? 0 : -1} disabled={option.disabled}
                  ref={option.selected ? selectedDayRef : undefined}
                  onClick={() => { if (option.slot) chooseSlot(option.slot); else chooseDay(option.day); }}>
                  <span className={styles.shiftDetails}>
                    <span>{option.date}</span>
                    <strong>{option.label}</strong>
                    {option.detail && <small>{option.detail}</small>}
                    {option.pending && option.detail !== t("Pending approval") && <small>{t("Pending approval")}</small>}
                  </span>
                  <span className={styles.shiftIndicator} aria-hidden="true" />
                </button>
              ))}
            </div>
          </>}

          {step === 2 && <div className={styles.stepSection}>
            <div className={styles.comparison}>
              <div><span>{t("Original shift")}</span><strong>{originalShift}</strong></div>
              <span aria-hidden="true">→</span>
              <div><span>{t("Requested shift")}</span><strong>{requestedShift}</strong></div>
            </div>
            {!requestDayOff && <div className={styles.timeEditor}>
              {([
                { label: "Start time", value: requestedFrom, setValue: setRequestedFrom },
                { label: "End time", value: requestedTo, setValue: setRequestedTo },
              ] as const).map(({ label, value, setValue }) => <div key={label}>
                <label>{t(label)}<input type="time" step="60" value={value} onChange={event => setValue(event.target.value)} /></label>
                <div className={styles.quickTimes}>
                  <button type="button" disabled={moveTime(value, -15) === value} aria-label={t(label) + " · " + t("Move selected time 15 minutes earlier")} onClick={() => setValue(moveTime(value, -15))}>−15</button>
                  <button type="button" disabled={moveTime(value, 15) === value} aria-label={t(label) + " · " + t("Move selected time 15 minutes later")} onClick={() => setValue(moveTime(value, 15))}>+15</button>
                </div>
              </div>)}
            </div>}
            {selectedSlot && <button type="button" className={styles.dayOffOption} aria-pressed={requestDayOff}
              disabled={!requestDayOff && !canRemoveShifts} onClick={() => setRequestDayOff(value => !value)}>
              <ScheduleIcon size={19} />
              <span className={styles.shiftDetails}>
                <strong>{t("Request day off")}</strong>
                <span>{t(selectedSlots.length === 1 ? "Remove your scheduled shift for this day." : "Remove all your shifts in this schedule for this day.")}</span>
              </span>
              <span className={styles.shiftIndicator} aria-hidden="true">{requestDayOff && <CheckIcon size={14} />}</span>
            </button>}
            {selectedSlot && !canRemoveShifts && <p className={styles.hint}>{removalBlockedReason}</p>}
            {requestDayOff && <p className={styles.hint}>{t("This request requires manager approval.")}</p>}
            {requestDayOff && conflictNames.length > 0 &&
              <p className={styles.hint}>{t("Your shifts in other schedules will remain unchanged ({0}).", conflictNames.join(", "))}</p>}
            {!requestDayOff && requestedFrom && requestedTo && !validPeriod && <p className={styles.validation}>{t("The shift end must be after its start.")}</p>}
            {!requestDayOff && unchanged && <p className={styles.hint}>{t("Change at least one boundary before sending the request.")}</p>}
            {!requestDayOff && overlaps && <p className={styles.validation}>{t("This time overlaps another shift on the same day.")}</p>}
          </div>}

          {step === 3 && <div className={styles.stepSection}>
            <label htmlFor={commentId}>{t("Comment (optional)")}</label>
            <p className={styles.hint} id={commentHelpId}>{t("Add any details your manager should know.")}</p>
            <textarea id={commentId} className={styles.commentInput} aria-describedby={commentHelpId}
              maxLength={commentMaxLength} rows={4} value={comment} onChange={event => setComment(event.target.value)} />
            <small className={styles.commentCount}>{t("{0} / {1} characters", comment.length, commentMaxLength)}</small>
          </div>}

          {step === 4 && schedule && <div className={styles.reviewCard}>
            <span className={styles.reviewBadge} data-kind={requestDayOff ? "remove" : isDayOff ? "add" : "adjust"}>
              {requestDayOff ? <ScheduleDetailsIcon size={15} /> : isDayOff ? <ScheduleIcon size={15} /> : <ClockIcon size={15} />}
              {t(requestDayOff ? "Request day off" : isDayOff ? "Request shift on a day off" : "Shift correction")}
            </span>
            <div className={styles.reviewRow}><ScheduleIcon size={17} /><div><small>{t("Schedule")}</small><strong>{schedule.name}</strong><span>{monthLabel}</span></div></div>
            <div className={styles.reviewRow}><ScheduleDetailsIcon size={17} /><div><small>{t("Date")}</small><strong>{dateLabel}</strong></div></div>
            <div className={styles.reviewTimes}>
              <div><small>{t("Original shift")}</small><strong>{originalShift}</strong></div>
              <ArrowRightIcon size={16} />
              <div><small>{t("Requested shift")}</small><strong>{requestedShift}</strong></div>
            </div>
            {requestDayOff && <p className={styles.hint}>{t("Duration change")} · {formatShiftCorrectionDelta(-shiftCorrectionOriginalMinutes(selectedSlots))}</p>}
            {comment.trim() && <div className={styles.reviewRow}><NoteIcon size={17} /><div><small>{t("Comment")}</small><span className={styles.reviewComment}>{comment.trim()}</span></div></div>}
            {requestDayOff && conflictNames.length > 0 &&
              <p className={styles.hint}>{t("Your shifts in other schedules will remain unchanged ({0}).", conflictNames.join(", "))}</p>}
          </div>}

          {step > 0 && (!schedule || (step > 1 && !canChooseDay)) &&
            <p className={styles.validation} role="status">{t("The selected schedule or shift is no longer available for correction. Go back to make another selection.")}</p>}
          {step === 4 && requestDayOff && canChooseDay && !canRemoveShifts &&
            <p className={styles.validation}>{removalBlockedReason}</p>}
          {step === 4 && !requestDayOff && overlaps && <p className={styles.validation}>{t("This time overlaps another shift on the same day.")}</p>}
          {step === 4 && !requestDayOff && unchanged && <p className={styles.validation}>{t("Change at least one boundary before sending the request.")}</p>}
          {step === 4 && errorMessage && <p className={styles.validation} role="alert">{errorMessage}</p>}
          </>}
        </div>

        <footer className={styles.footer} data-submitted={submitted || undefined}>
          {submitted ? <IosButton label={t("Done")} size="compact" onClick={onCancel} /> : <>
          <IosButton label={t(step === 0 ? "Cancel" : "Back")} variant="secondary" size="compact" disabled={isSending}
            className={styles.footerSecondary} customColor="#f1f5f9" customBorderColor="#dfe6ef"
            icon={step === 0 ? <CloseIcon size={16} /> : <ChevronLeftIcon size={16} />}
            onClick={step === 0 ? cancel : back} />
          <IosButton label={isSending ? t("Sending...") : t(step === 4 ? "Send request" : "Continue")} size="compact"
            className={styles.footerPrimary} icon={step === 4 ? <CheckIcon size={15} /> : <ChevronRightIcon size={16} />}
            disabled={!canContinue || isSending} onClick={advance} />
          </>}
        </footer>
      </div>
    </dialog>, document.body,
  );
}
