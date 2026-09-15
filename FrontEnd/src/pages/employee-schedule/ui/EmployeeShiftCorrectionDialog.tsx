import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useId, useMemo, useState, type MouseEvent } from "react";
import type { EmployeeSchedule, EmployeeScheduleSlot } from "@entities/employee-schedule";
import type { CreateShiftCorrectionInput, ShiftCorrectionRequest } from "@entities/shift-corrections";
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CloseIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./EmployeeShiftCorrectionDialog.module.css";

type Boundary = { slotId: number; edge: "start" | "end" };

type EmployeeShiftCorrectionDialogProps = {
  open: boolean;
  schedule: EmployeeSchedule | null;
  employeeId: number | null;
  existingRequests: ShiftCorrectionRequest[];
  isSending: boolean;
  errorMessage?: string | null;
  onCancel: () => void;
  onSubmit: (input: CreateShiftCorrectionInput) => void;
};

const weekdayFormatter = dateTimeFormat("en-GB", { weekday: "short", timeZone: "UTC" });

function toMinutes(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  return hours * 60 + minutes;
}

function toTime(minutes: number) {
  const safe = Math.max(0, Math.min(1439, minutes));
  return `${String(Math.floor(safe / 60)).padStart(2, "0")}:${String(safe % 60).padStart(2, "0")}`;
}

function ownsSlot(slot: EmployeeScheduleSlot, employeeId: number | null) {
  return employeeId === null
    ? slot.employeeId === undefined || slot.employeeId === null
    : slot.employeeId === undefined || slot.employeeId === null || slot.employeeId === employeeId;
}

function formatAdjustment(minutes: number) {
  if (minutes === 0) return t("No change yet");
  const direction = minutes > 0 ? "later" : "earlier";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  const amount = [
    hours ? `${hours} ${hours === 1 ? "hour" : "hours"}` : "",
    remainder ? t("{0} min", remainder) : "",
  ].filter(Boolean).join(" ");
  return `${amount} ${direction}`;
}

export function EmployeeShiftCorrectionDialog({
  open,
  schedule,
  employeeId,
  existingRequests,
  isSending,
  errorMessage,
  onCancel,
  onSubmit,
}: EmployeeShiftCorrectionDialogProps) {
  const titleId = useId();
  const [selectedDay, setSelectedDay] = useState<number | null>(null);
  const [boundary, setBoundary] = useState<Boundary | null>(null);
  const [adjustmentMinutes, setAdjustmentMinutes] = useState(0);

  const slotsByDay = useMemo(() => {
    const map = new Map<number, EmployeeScheduleSlot[]>();
    schedule?.slots.filter(slot => ownsSlot(slot, employeeId)).forEach(slot => {
      const daySlots = map.get(slot.dayOfMonth) ?? [];
      daySlots.push(slot);
      map.set(slot.dayOfMonth, daySlots);
    });
    map.forEach(daySlots => daySlots.sort((left, right) => left.fromTime.localeCompare(right.fromTime)));
    return map;
  }, [employeeId, schedule]);

  const days = useMemo(() => [...slotsByDay.keys()].sort((left, right) => left - right), [slotsByDay]);
  const selectedSlots = selectedDay === null ? [] : slotsByDay.get(selectedDay) ?? [];
  const selectedSlot = boundary ? selectedSlots.find(slot => slot.id === boundary.slotId) ?? null : null;
  const pendingSlotIds = useMemo(
    () => new Set(existingRequests.filter(request => request.status === "pending").map(request => request.scheduleSlotId)),
    [existingRequests],
  );

  useEffect(() => {
    if (!open) return;
    setSelectedDay(days[0] ?? null);
    setBoundary(null);
    setAdjustmentMinutes(0);
  }, [days, open, schedule?.id]);

  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => event.key === "Escape" && onCancel();
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onCancel, open]);

  if (!open || !schedule) return null;

  const originalFrom = selectedSlot?.fromTime ?? "";
  const originalTo = selectedSlot?.toTime ?? "";
  const requestedFrom = selectedSlot && boundary
    ? toTime(toMinutes(originalFrom) + (boundary.edge === "start" ? adjustmentMinutes : 0))
    : "";
  const requestedTo = selectedSlot && boundary
    ? toTime(toMinutes(originalTo) + (boundary.edge === "end" ? adjustmentMinutes : 0))
    : "";
  const requestedFromMinutes = requestedFrom ? toMinutes(requestedFrom) : 0;
  const requestedToMinutes = requestedTo ? toMinutes(requestedTo) : 0;
  const overlaps = selectedSlot
    ? selectedSlots.some(slot => slot.id !== selectedSlot.id
      && requestedFromMinutes < toMinutes(slot.toTime)
      && requestedToMinutes > toMinutes(slot.fromTime))
    : false;
  const isValid = Boolean(selectedSlot && boundary && adjustmentMinutes !== 0
    && requestedFromMinutes < requestedToMinutes && !overlaps && !pendingSlotIds.has(selectedSlot.id));

  const selectBoundary = (slotId: number, edge: Boundary["edge"]) => {
    if (pendingSlotIds.has(slotId)) return;
    setBoundary({ slotId, edge });
    setAdjustmentMinutes(0);
  };

  const setRequestedBoundaryTime = (value: string) => {
    if (!selectedSlot || !boundary) return;
    const originalTime = boundary.edge === "start" ? selectedSlot.fromTime : selectedSlot.toTime;
    setAdjustmentMinutes(toMinutes(value) - toMinutes(originalTime));
  };

  const moveRequestedBoundary = (deltaMinutes: number) => {
    if (!selectedSlot || !boundary) return;
    const originalTime = boundary.edge === "start" ? selectedSlot.fromTime : selectedSlot.toTime;
    const nextMinutes = Math.max(0, Math.min(1439, toMinutes(originalTime) + adjustmentMinutes + deltaMinutes));
    setAdjustmentMinutes(nextMinutes - toMinutes(originalTime));
  };

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) onCancel();
  };

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId} onMouseDown={handleOverlayMouseDown}>
      <div className={styles.dialog}>
        <header className={styles.header}>
          <div className={styles.titleBlock}>
            <span className={styles.eyebrow}><ScheduleIcon size={16} />  {t("Correction request")}</span>
            <h2 id={titleId}>{t("Request a shift change")}</h2>
            <p>{schedule.name}  {t("· Change the start or end time of one shift.")}</p>
          </div>
          <button type="button" className={styles.closeButton} aria-label={t("Close shift correction dialog")} onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

        <section className={styles.stepSection}>
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>1</span>
            <div><strong>{t("Choose a workday")}</strong><small>{t("Only days with your shifts are shown.")}</small></div>
          </div>
          <div className={styles.dayList} role="tablist" aria-label={t("Working days")}>
            {days.map(day => {
              const weekday = weekdayFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, day))).replace(".", "");
              return (
                <button key={day} type="button" role="tab" aria-selected={day === selectedDay}
                  className={day === selectedDay ? styles.dayActive : ""}
                  onClick={() => { setSelectedDay(day); setBoundary(null); setAdjustmentMinutes(0); }}>
                  <span>{weekday}</span><strong>{day}</strong>
                </button>
              );
            })}
          </div>
        </section>

        <section className={styles.stepSection} aria-label={t("Choose shift and time")}>
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>2</span>
            <div><strong>{t("Choose what to change")}</strong><small>{t("Select the start or end time of a shift.")}</small></div>
          </div>
          {selectedSlots.length > 0 ? (
            <div className={styles.shiftList}>
              {selectedSlots.map((slot, index) => {
                const pending = pendingSlotIds.has(slot.id);
                const selectedEdge = boundary?.slotId === slot.id ? boundary.edge : null;
                return (
                  <article key={slot.id} className={`${styles.shiftCard} ${pending ? styles.shiftPending : ""}`}>
                    <div className={styles.shiftCardHeader}>
                      <span>{t("Shift")} {index + 1}</span>
                      <strong>{slot.fromTime} – {slot.toTime}</strong>
                      {pending ? <em>{t("Pending approval")}</em> : null}
                    </div>
                    <div className={styles.boundaryChoices} role="group" aria-label={t("Change {0} to {1}", slot.fromTime, slot.toTime)}>
                      <button type="button" className={selectedEdge === "start" ? styles.boundarySelected : ""}
                        aria-label={t("Change start time from {0}", slot.fromTime)} aria-pressed={selectedEdge === "start"}
                        disabled={pending} onClick={() => selectBoundary(slot.id, "start")}>
                        <span>{t("Start")}</span><strong>{slot.fromTime}</strong>
                      </button>
                      <i aria-hidden="true">→</i>
                      <button type="button" className={selectedEdge === "end" ? styles.boundarySelected : ""}
                        aria-label={t("Change end time from {0}", slot.toTime)} aria-pressed={selectedEdge === "end"}
                        disabled={pending} onClick={() => selectBoundary(slot.id, "end")}>
                        <span>{t("End")}</span><strong>{slot.toTime}</strong>
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          ) : <p className={styles.empty}>{t("No shifts are available for correction.")}</p>}
        </section>

        <section className={`${styles.stepSection} ${styles.adjustmentPanel}`}>
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>3</span>
            <div><strong>{t("Set the new time")}</strong><small>{t("You can type the exact time or use a quick adjustment.")}</small></div>
          </div>
          {boundary && selectedSlot ? (
            <>
              <div className={styles.timeEditor}>
                <button type="button" aria-label={t("Move selected time 30 minutes earlier")} onClick={() => moveRequestedBoundary(-30)}>−30 min</button>
                <label>
                  <span>{boundary.edge === "start" ? t("New start time") : t("New end time")}</span>
                  <input type="time" step="900" value={boundary.edge === "start" ? requestedFrom : requestedTo}
                    onChange={event => setRequestedBoundaryTime(event.target.value)} />
                </label>
                <button type="button" aria-label={t("Move selected time 30 minutes later")} onClick={() => moveRequestedBoundary(30)}>+30 min</button>
              </div>
              <p className={styles.movement}>{boundary.edge === "start" ? t("Start time") : t("End time")}: {formatAdjustment(adjustmentMinutes)}</p>
              <div className={styles.comparison}>
                <div><span>{t("Current shift")}</span><strong>{originalFrom} – {originalTo}</strong></div>
                <i>→</i>
                <div><span>{t("Requested shift")}</span><strong>{requestedFrom} – {requestedTo}</strong></div>
              </div>
              {overlaps ? <p className={styles.validation}>{t("This time overlaps another shift on the same day.")}</p> : null}
              {requestedFromMinutes >= requestedToMinutes ? <p className={styles.validation}>{t("The shift end must be after its start.")}</p> : null}
            </>
          ) : <p className={styles.hint}>{t("Select a Start or End time above to continue.")}</p>}
          {errorMessage ? <p className={styles.validation}>{errorMessage}</p> : null}
        </section>

        <footer className={styles.footer}>
          <IosButton label={t("Cancel")} variant="secondary" size="compact" onClick={onCancel} />
          <IosButton label={isSending ? t("Sending...") : t("Send request")} size="compact" icon={<CheckIcon size={15} />}
            disabled={!isValid || isSending}
            onClick={() => selectedSlot && onSubmit({ scheduleId: schedule.id, scheduleSlotId: selectedSlot.id, requestedFromTime: requestedFrom, requestedToTime: requestedTo })} />
        </footer>
      </div>
    </div>
  );
}
