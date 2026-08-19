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

const barColors = ["#2563eb", "#7c3aed", "#0891b2", "#db2777"];
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
  if (minutes === 0) return "No adjustment";
  const sign = minutes > 0 ? "+" : "−";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  return `${sign}${hours ? `${hours}h` : ""}${hours && remainder ? " " : ""}${remainder ? `${remainder}m` : ""}`;
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
  const visibleBounds = selectedSlots.flatMap(slot => slot.id === selectedSlot?.id
    ? [requestedFromMinutes, requestedToMinutes]
    : [toMinutes(slot.fromTime), toMinutes(slot.toTime)]);
  const rangeStart = visibleBounds.length > 0
    ? Math.max(0, Math.floor((Math.min(...visibleBounds) - 60) / 60) * 60)
    : 0;
  const rangeEnd = visibleBounds.length > 0
    ? Math.min(1440, Math.ceil((Math.max(...visibleBounds) + 60) / 60) * 60)
    : 1440;
  const rangeDuration = Math.max(60, rangeEnd - rangeStart);
  const ticks = Array.from({ length: Math.floor(rangeDuration / 60) + 1 }, (_, index) => rangeStart + index * 60);
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

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) onCancel();
  };

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId} onMouseDown={handleOverlayMouseDown}>
      <div className={styles.dialog}>
        <header className={styles.header}>
          <div className={styles.titleBlock}>
            <span className={styles.eyebrow}><ScheduleIcon size={16} /> Shift correction</span>
            <h2 id={titleId}>Adjust your shift</h2>
            <p>{schedule.name} · Choose a day, then select one edge of a shift.</p>
          </div>
          <button type="button" className={styles.closeButton} aria-label="Close shift correction dialog" onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

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

        {selectedSlots.length > 0 ? (
          <section className={styles.timelineSection} aria-label="Shift timeline">
            <div className={styles.axisLabels} aria-hidden="true">
              {ticks.map((tick, index) => (
                <span key={tick} style={{ left: `${(tick - rangeStart) / rangeDuration * 100}%` }}
                  className={index === ticks.length - 1 ? styles.lastTick : ""}>{toTime(Math.min(tick, 1439))}</span>
              ))}
            </div>
            <div className={styles.axis}>
              {ticks.map(tick => <i key={tick} style={{ left: `${(tick - rangeStart) / rangeDuration * 100}%` }} />)}
              {selectedSlots.map((slot, index) => {
                const from = slot.id === selectedSlot?.id ? requestedFromMinutes : toMinutes(slot.fromTime);
                const to = slot.id === selectedSlot?.id ? requestedToMinutes : toMinutes(slot.toTime);
                const pending = pendingSlotIds.has(slot.id);
                return (
                  <div key={slot.id} className={`${styles.shiftRow} ${pending ? styles.shiftPending : ""}`}>
                    <div className={styles.shiftBar} style={{
                      left: `${(from - rangeStart) / rangeDuration * 100}%`,
                      width: `${Math.max(1, (to - from) / rangeDuration * 100)}%`,
                      backgroundColor: barColors[index % barColors.length],
                    }}>
                      <button type="button" className={`${styles.handle} ${styles.handleStart} ${boundary?.slotId === slot.id && boundary.edge === "start" ? styles.handleSelected : ""}`}
                        aria-label={`Adjust start of ${slot.fromTime} to ${slot.toTime}`} disabled={pending} onClick={() => selectBoundary(slot.id, "start")} />
                      <span>{toTime(from)} – {toTime(to)}</span>
                      <button type="button" className={`${styles.handle} ${styles.handleEnd} ${boundary?.slotId === slot.id && boundary.edge === "end" ? styles.handleSelected : ""}`}
                        aria-label={`Adjust end of ${slot.fromTime} to ${slot.toTime}`} disabled={pending} onClick={() => selectBoundary(slot.id, "end")} />
                    </div>
                    {pending ? <em>Request pending</em> : null}
                  </div>
                );
              })}
            </div>
          </section>
        ) : <p className={styles.empty}>No shifts are available for correction.</p>}

        <section className={styles.adjustmentPanel}>
          {boundary && selectedSlot ? (
            <>
              <div className={styles.adjustmentHeader}>
                <span>{boundary.edge === "start" ? "Shift start" : "Shift end"}</span>
                <strong>{formatAdjustment(adjustmentMinutes)}</strong>
              </div>
              <div className={styles.stepButtons} role="group" aria-label="Adjust selected boundary">
                {[-60, -30, 30, 60].map(minutes => (
                  <button key={minutes} type="button" onClick={() => setAdjustmentMinutes(current => current + minutes)}>
                    {minutes > 0 ? "+" : "−"}{Math.abs(minutes) === 60 ? "1h" : "30m"}
                  </button>
                ))}
                <label><span>Minutes</span><input type="number" step="15" value={adjustmentMinutes}
                  onChange={event => setAdjustmentMinutes(Number(event.target.value) || 0)} /></label>
              </div>
              <div className={styles.comparison}>
                <span>{originalFrom} – {originalTo}</span><i>→</i><strong>{requestedFrom} – {requestedTo}</strong>
              </div>
              {overlaps ? <p className={styles.validation}>The adjusted shift overlaps another shift.</p> : null}
              {requestedFromMinutes >= requestedToMinutes ? <p className={styles.validation}>The shift end must be after its start.</p> : null}
            </>
          ) : <p className={styles.hint}>Click the round handle at the beginning or end of a colored shift.</p>}
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
