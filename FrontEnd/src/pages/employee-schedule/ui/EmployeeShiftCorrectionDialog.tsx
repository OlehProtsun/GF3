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

const weekdayFormatter = new Intl.DateTimeFormat("en-GB", { weekday: "short", timeZone: "UTC" });

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
  if (minutes === 0) return "No change yet";
  const direction = minutes > 0 ? "later" : "earlier";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  const amount = [
    hours ? `${hours} ${hours === 1 ? "hour" : "hours"}` : "",
    remainder ? `${remainder} min` : "",
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
            <span className={styles.eyebrow}><ScheduleIcon size={16} /> Correction request</span>
            <h2 id={titleId}>Request a shift change</h2>
            <p>{schedule.name} · Change the start or end time of one shift.</p>
          </div>
          <button type="button" className={styles.closeButton} aria-label="Close shift correction dialog" onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

        <section className={styles.stepSection}>
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>1</span>
            <div><strong>Choose a workday</strong><small>Only days with your shifts are shown.</small></div>
          </div>
          <div className={styles.dayList} role="tablist" aria-label="Working days">
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

        <section className={styles.stepSection} aria-label="Choose shift and time">
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>2</span>
            <div><strong>Choose what to change</strong><small>Select the start or end time of a shift.</small></div>
          </div>
          {selectedSlots.length > 0 ? (
            <div className={styles.shiftList}>
              {selectedSlots.map((slot, index) => {
                const pending = pendingSlotIds.has(slot.id);
                const selectedEdge = boundary?.slotId === slot.id ? boundary.edge : null;
                return (
                  <article key={slot.id} className={`${styles.shiftCard} ${pending ? styles.shiftPending : ""}`}>
                    <div className={styles.shiftCardHeader}>
                      <span>Shift {index + 1}</span>
                      <strong>{slot.fromTime} – {slot.toTime}</strong>
                      {pending ? <em>Pending approval</em> : null}
                    </div>
                    <div className={styles.boundaryChoices} role="group" aria-label={`Change ${slot.fromTime} to ${slot.toTime}`}>
                      <button type="button" className={selectedEdge === "start" ? styles.boundarySelected : ""}
                        aria-label={`Change start time from ${slot.fromTime}`} aria-pressed={selectedEdge === "start"}
                        disabled={pending} onClick={() => selectBoundary(slot.id, "start")}>
                        <span>Start</span><strong>{slot.fromTime}</strong>
                      </button>
                      <i aria-hidden="true">→</i>
                      <button type="button" className={selectedEdge === "end" ? styles.boundarySelected : ""}
                        aria-label={`Change end time from ${slot.toTime}`} aria-pressed={selectedEdge === "end"}
                        disabled={pending} onClick={() => selectBoundary(slot.id, "end")}>
                        <span>End</span><strong>{slot.toTime}</strong>
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          ) : <p className={styles.empty}>No shifts are available for correction.</p>}
        </section>

        <section className={`${styles.stepSection} ${styles.adjustmentPanel}`}>
          <div className={styles.stepHeader}>
            <span className={styles.stepNumber}>3</span>
            <div><strong>Set the new time</strong><small>You can type the exact time or use a quick adjustment.</small></div>
          </div>
          {boundary && selectedSlot ? (
            <>
              <div className={styles.timeEditor}>
                <button type="button" aria-label="Move selected time 30 minutes earlier" onClick={() => moveRequestedBoundary(-30)}>−30 min</button>
                <label>
                  <span>New {boundary.edge} time</span>
                  <input type="time" step="900" value={boundary.edge === "start" ? requestedFrom : requestedTo}
                    onChange={event => setRequestedBoundaryTime(event.target.value)} />
                </label>
                <button type="button" aria-label="Move selected time 30 minutes later" onClick={() => moveRequestedBoundary(30)}>+30 min</button>
              </div>
              <p className={styles.movement}>{boundary.edge === "start" ? "Start time" : "End time"}: {formatAdjustment(adjustmentMinutes)}</p>
              <div className={styles.comparison}>
                <div><span>Current shift</span><strong>{originalFrom} – {originalTo}</strong></div>
                <i>→</i>
                <div><span>Requested shift</span><strong>{requestedFrom} – {requestedTo}</strong></div>
              </div>
              {overlaps ? <p className={styles.validation}>This time overlaps another shift on the same day.</p> : null}
              {requestedFromMinutes >= requestedToMinutes ? <p className={styles.validation}>The shift end must be after its start.</p> : null}
            </>
          ) : <p className={styles.hint}>Select a Start or End time above to continue.</p>}
          {errorMessage ? <p className={styles.validation}>{errorMessage}</p> : null}
        </section>

        <footer className={styles.footer}>
          <IosButton label="Cancel" variant="secondary" size="compact" onClick={onCancel} />
          <IosButton label={isSending ? "Sending..." : "Send request"} size="compact" icon={<CheckIcon size={15} />}
            disabled={!isValid || isSending}
            onClick={() => selectedSlot && onSubmit({ scheduleId: schedule.id, scheduleSlotId: selectedSlot.id, requestedFromTime: requestedFrom, requestedToTime: requestedTo })} />
        </footer>
      </div>
    </div>
  );
}
