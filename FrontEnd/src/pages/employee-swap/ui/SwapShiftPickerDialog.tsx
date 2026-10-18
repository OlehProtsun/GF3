import { useState } from "react";
import type { ShiftSwapReturnCandidate } from "@entities/shift-swaps/model/types";
import { t } from "@shared/i18n";
import styles from "./SwapShiftPickerDialog.module.css";
export type SwapPeriod = { fromTime: string; toTime: string };
export type SwapShiftSelection = { shift: ShiftSwapReturnCandidate; period: SwapPeriod };
export const swapMinutes = (time: string) => Number(time.slice(0, 2)) * 60 + Number(time.slice(3));
export const swapTime = (minutes: number) => `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;
export const swapShiftDate = (shift: Pick<ShiftSwapReturnCandidate, "year" | "month" | "dayOfMonth">) =>
  `${String(shift.dayOfMonth).padStart(2, "0")}.${String(shift.month).padStart(2, "0")}.${shift.year}`;
// Rendered as a subview of the wizard's single native dialog.
export function SwapShiftPickerDialog({ shifts, selection, durationMinutes, returnMode = false, employeeName,
  loading, hasMore, onLoadMore, onConfirm }: {
  shifts: ShiftSwapReturnCandidate[]; selection?: SwapShiftSelection | null; durationMinutes?: number;
  returnMode?: boolean; employeeName?: string; loading?: boolean; hasMore?: boolean;
  onLoadMore?: () => void; onConfirm: (selection: SwapShiftSelection) => void; onBack: () => void;
}) {
  const [slotId, setSlotId] = useState(selection?.shift.slotId ?? null);
  const [period, setPeriod] = useState<SwapPeriod | null>(selection?.period ?? null);
  const [custom, setCustom] = useState(Boolean(selection && (selection.period.fromTime !== selection.shift.fromTime || selection.period.toTime !== selection.shift.toTime)));
  const shift = shifts.find(item => item.slotId === slotId);
  const fullAllowed = Boolean(shift && (!returnMode || shift.durationMinutes === durationMinutes) &&
    (!returnMode || shift.eligibleWindows?.some(window => window.fromTime === shift.fromTime && window.toTime === shift.toTime)));
  const valid = Boolean(shift?.canSelect && period && swapMinutes(period.fromTime) >= swapMinutes(shift.fromTime) &&
    swapMinutes(period.toTime) <= swapMinutes(shift.toTime) && swapMinutes(period.toTime) > swapMinutes(period.fromTime) &&
    (!returnMode || (swapMinutes(period.toTime) - swapMinutes(period.fromTime) === durationMinutes &&
      shift.eligibleWindows?.some(window => window.fromTime === period.fromTime && window.toTime === period.toTime))));
  const choose = (item: ShiftSwapReturnCandidate) => {
    setSlotId(item.slotId);
    const full = !returnMode || (item.durationMinutes === durationMinutes && item.eligibleWindows?.some(w => w.fromTime === item.fromTime));
    setCustom(!full);
    setPeriod({ fromTime: full ? item.fromTime : item.suggestedFromTime!, toTime: full ? item.toTime : item.suggestedToTime! });
  };
  const changeTime = (edge: "fromTime" | "toTime", time: string) => {
    if (!period) return;
    if (returnMode) setPeriod({ fromTime: time, toTime: swapTime(swapMinutes(time) + (durationMinutes ?? 0)) });
    else setPeriod({ ...period, [edge]: time });
  };
  const canAdjust = (edge: "fromTime" | "toTime", delta: number) => {
    if (!shift || !period) return false;
    const next = swapMinutes(period[edge]) + delta;
    if (returnMode) return shift.eligibleWindows?.some(w => w.fromTime === swapTime(next)) ?? false;
    return edge === "fromTime" ? next >= swapMinutes(shift.fromTime) && next < swapMinutes(period.toTime)
      : next <= swapMinutes(shift.toTime) && next > swapMinutes(period.fromTime);
  };
  return <section className={styles.picker} aria-label={returnMode ? t("Choose their shift") : t("Select a shift")}>
    <span className={styles.handle} aria-hidden="true" />
    <h2 tabIndex={-1} data-subview-heading>{returnMode ? t("Choose their shift") : t("Select a shift")}</h2>
    <p>{returnMode ? t("Select one of {0}'s shifts to swap with your shift.", employeeName ?? "") : t("Choose the shift you want to give away.")}</p>
    {returnMode && <div className={styles.info}><span aria-hidden="true">ⓘ</span><p>{t("Only compatible shifts are selectable. Shifts that overlap with your schedule are unavailable.")}</p></div>}
    <div className={styles.list} role="radiogroup" aria-label={t("Shifts")} onKeyDown={event => {
      if (!["ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight"].includes(event.key)) return;
      event.preventDefault();
      const rows = [...event.currentTarget.querySelectorAll<HTMLButtonElement>('[role="radio"]:not(:disabled)')];
      const index = rows.indexOf(event.target as HTMLButtonElement);
      const direction = event.key === "ArrowDown" || event.key === "ArrowRight" ? 1 : -1;
      const next = rows[(index + direction + rows.length) % rows.length];
      next?.focus(); next?.click();
    }}>
      {shifts.map(item => <button key={item.slotId} type="button" role="radio" aria-checked={item.slotId === slotId}
        tabIndex={item.slotId === slotId || (!shift?.canSelect && shifts.find(candidate => candidate.canSelect)?.slotId === item.slotId) ? 0 : -1}
        className={styles.row} disabled={!item.canSelect} onClick={() => choose(item)}>
        <span>{swapShiftDate(item)}</span>
        <strong>{item.fromTime} – {item.toTime}</strong>
        <span className={styles.radio} aria-hidden="true" />
        {!item.canSelect && <small>{t(item.unavailableReason ?? "Shift unavailable.")}</small>}
      </button>)}
      {!loading && shifts.length === 0 && <p>{t("No shifts available.")}</p>}
      {loading && <p role="status">{t("Loading...")}</p>}
      {hasMore && <button type="button" onClick={onLoadMore} disabled={loading}>{t("Load more")}</button>}
    </div>
    {(returnMode || (shift && period)) && <fieldset className={styles.period}>
      <legend>{returnMode ? t("Time to receive") : t("Time to give away")}</legend>
      <div className={styles.modes}>
        <button type="button" aria-pressed={!custom} disabled={!fullAllowed} onClick={() => { if (!shift) return; setCustom(false); setPeriod({ fromTime: shift.fromTime, toTime: shift.toTime }); }}>{t("Full shift")}</button>
        <button type="button" aria-pressed={custom} disabled={!shift} onClick={() => { setCustom(true); if (returnMode && shift) setPeriod({ fromTime: shift.suggestedFromTime!, toTime: shift.suggestedToTime! }); }}>{t("Custom period")}</button>
      </div>
      {custom && shift && period && <div className={styles.controls}><strong className={styles.date}>{swapShiftDate(shift)}</strong>
        {(["fromTime", "toTime"] as const).map(edge => <label key={edge}>
          <span>{edge === "fromTime" ? t("From") : t("To")}</span>
          <div className={styles.time}>
            <button type="button" aria-label={t("Decrease {0}", edge === "fromTime" ? t("From") : t("To"))} disabled={(returnMode && edge === "toTime") || !canAdjust(edge, -15)} onClick={() => changeTime(edge, swapTime(swapMinutes(period[edge]) - 15))}>−</button>
            <input type="time" step={900} value={period[edge]} min={shift.fromTime} max={shift.toTime}
              readOnly={returnMode && edge === "toTime"} onChange={event => changeTime(edge, event.target.value)} />
            <button type="button" aria-label={t("Increase {0}", edge === "fromTime" ? t("From") : t("To"))} disabled={(returnMode && edge === "toTime") || !canAdjust(edge, 15)} onClick={() => changeTime(edge, swapTime(swapMinutes(period[edge]) + 15))}>+</button>
          </div>
        </label>)}
      </div>}
      {!custom && shift && period && <div className={styles.summary}>
        <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">{returnMode ? <><rect x="4" y="5" width="16" height="16" rx="2" /><path d="M8 3v5m8-5v5M4 11h16" /></> : <><circle cx="12" cy="12" r="9" /><path d="M12 6v6h5" /></>}</svg>
        <div>{returnMode && <span>{swapShiftDate(shift)}</span>}<strong>{period.fromTime} – {period.toTime} ({Number(((swapMinutes(period.toTime) - swapMinutes(period.fromTime)) / 60).toFixed(2))}h)</strong>{!returnMode && <span>{t("Full shift")}</span>}</div>
      </div>}
    </fieldset>}
    <footer className={styles.footer}><button type="button" className={styles.confirm} disabled={!valid || loading} onClick={() => valid && shift && period && onConfirm({ shift, period })}>{returnMode ? t("Confirm selection") : t("Confirm shift")}</button></footer>
  </section>;
}
