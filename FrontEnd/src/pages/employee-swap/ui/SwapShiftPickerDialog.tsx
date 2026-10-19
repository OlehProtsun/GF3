import { useId, useState } from "react";
import type { ShiftSwapReturnCandidate } from "@entities/shift-swaps/model/types";
import { t } from "@shared/i18n";
import { SwapSelectionContext } from "./SwapSelectionContext";
import { SwapScrollRegion } from "./SwapScrollRegion";
import styles from "./SwapShiftPickerDialog.module.css";
export type SwapPeriod = { fromTime: string; toTime: string };
export type SwapShiftSelection = { shift: ShiftSwapReturnCandidate; period: SwapPeriod };
export const swapMinutes = (time: string) => Number(time.slice(0, 2)) * 60 + Number(time.slice(3));
export const swapTime = (minutes: number) => `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;
export const swapShiftDate = (shift: Pick<ShiftSwapReturnCandidate, "year" | "month" | "dayOfMonth">) =>
  `${String(shift.dayOfMonth).padStart(2, "0")}.${String(shift.month).padStart(2, "0")}.${shift.year}`;
// Rendered as a subview of the wizard's single native dialog.
export function SwapShiftPickerDialog({ shifts, selection, outgoing, returnMode = false, employeeName,
  loading, hasMore, onLoadMore, onConfirm }: {
  shifts: ShiftSwapReturnCandidate[]; selection?: SwapShiftSelection | null; outgoing?: SwapShiftSelection;
  returnMode?: boolean; employeeName?: string; loading?: boolean; hasMore?: boolean;
  onLoadMore?: () => void; onConfirm: (selection: SwapShiftSelection) => void; onBack: () => void;
}) {
  const periodHeadingId = useId();
  const [slotId, setSlotId] = useState(selection?.shift.slotId ?? null);
  const [period, setPeriod] = useState<SwapPeriod | null>(selection?.period ?? null);
  const [custom, setCustom] = useState(Boolean(selection && (selection.period.fromTime !== selection.shift.fromTime || selection.period.toTime !== selection.shift.toTime)));
  const shift = shifts.find(item => item.slotId === slotId);
  const allowed = (item: ShiftSwapReturnCandidate, value: SwapPeriod) => {
    const from = swapMinutes(value.fromTime), to = swapMinutes(value.toTime);
    if (from < swapMinutes(item.fromTime) || to > swapMinutes(item.toTime) || from >= to) return false;
    if (!returnMode) return true;
    if (!item.eligibleWindows?.some(window => from >= swapMinutes(window.fromTime) && to <= swapMinutes(window.toTime))) return false;
    // The target must give away any part of this shift that overlaps the incoming source period.
    const give = outgoing?.shift;
    if (give && give.year === item.year && give.month === item.month && give.dayOfMonth === item.dayOfMonth) {
      const overlapFrom = Math.max(swapMinutes(outgoing.period.fromTime), swapMinutes(item.fromTime));
      const overlapTo = Math.min(swapMinutes(outgoing.period.toTime), swapMinutes(item.toTime));
      if (overlapFrom < overlapTo && (from > overlapFrom || to < overlapTo)) return false;
    }
    return true;
  };
  const fullAllowed = Boolean(shift && allowed(shift, shift));
  const valid = Boolean(shift?.canSelect && period && allowed(shift, period));
  const suggestedPeriod = (item: ShiftSwapReturnCandidate): SwapPeriod | null => {
    const suggested = item.suggestedFromTime && item.suggestedToTime
      ? { fromTime: item.suggestedFromTime, toTime: item.suggestedToTime } : null;
    return suggested && allowed(item, suggested) ? suggested : item.eligibleWindows?.find(window => allowed(item, window)) ?? null;
  };
  const choose = (item: ShiftSwapReturnCandidate) => {
    if (!item.canSelect) return;
    setSlotId(item.slotId);
    const full = allowed(item, item);
    setCustom(!full);
    setPeriod(full ? { fromTime: item.fromTime, toTime: item.toTime } : suggestedPeriod(item));
  };
  const changeTime = (edge: "fromTime" | "toTime", time: string) => {
    if (!period || !/^([01]\d|2[0-3]):[0-5]\d$/.test(time)) return;
    setPeriod({ ...period, [edge]: time });
  };
  const canAdjust = (edge: "fromTime" | "toTime", delta: number) => {
    if (!shift || !period) return false;
    const next = swapMinutes(period[edge]) + delta;
    return allowed(shift, { ...period, [edge]: swapTime(next) });
  };
  return <section className={styles.picker} aria-label={returnMode ? t("Choose their shift") : t("Select your shift")}>
    <span className={styles.handle} aria-hidden="true" />
    <h2 tabIndex={-1} data-subview-heading>{returnMode ? t("Choose their shift") : t("Select your shift")}</h2>
    <p><SwapSelectionContext message={returnMode ? "Select a shift from {0}'s schedule to swap with yours." : "Choose a shift from {0}'s schedule."} employeeName={employeeName ?? ""} own={!returnMode} /></p>
    {returnMode && <div className={styles.info}><span aria-hidden="true">ⓘ</span><p>{t("Only compatible shifts are selectable. Shifts that overlap with your schedule are unavailable.")}</p></div>}
    <SwapScrollRegion className={styles.list} role="radiogroup" aria-label={t("Shifts")} onKeyDown={event => {
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
    </SwapScrollRegion>
    {(returnMode || (shift && period)) && <div className={styles.period} role="group" aria-labelledby={periodHeadingId}>
      <h3 id={periodHeadingId} data-period-heading>{returnMode ? t("Time to receive") : t("Time to give away")}</h3>
      <div className={styles.modes}>
        <button type="button" aria-pressed={!custom} disabled={!fullAllowed} onClick={() => { if (!shift) return; setCustom(false); setPeriod({ fromTime: shift.fromTime, toTime: shift.toTime }); }}>{t("Full shift")}</button>
        <button type="button" aria-pressed={custom} disabled={!shift} onClick={() => { setCustom(true); if (returnMode && shift && !valid) setPeriod(suggestedPeriod(shift)); }}>{t("Custom period")}</button>
      </div>
      {custom && shift && period && <div className={styles.controls}><strong className={styles.date}>{swapShiftDate(shift)}</strong>
        {(["fromTime", "toTime"] as const).map(edge => <label key={edge}>
          <span>{edge === "fromTime" ? t("From") : t("To")}</span>
          <div className={styles.time}>
            <button type="button" aria-label={t("Decrease {0}", edge === "fromTime" ? t("From") : t("To"))} disabled={!canAdjust(edge, -15)} onClick={() => changeTime(edge, swapTime(swapMinutes(period[edge]) - 15))}>−</button>
            <input type="time" step={900} value={period[edge]} min={shift.fromTime} max={shift.toTime}
              onChange={event => changeTime(edge, event.target.value)} />
            <button type="button" aria-label={t("Increase {0}", edge === "fromTime" ? t("From") : t("To"))} disabled={!canAdjust(edge, 15)} onClick={() => changeTime(edge, swapTime(swapMinutes(period[edge]) + 15))}>+</button>
          </div>
        </label>)}
      </div>}
      {!custom && shift && period && <div className={styles.summary}>
        <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">{returnMode ? <><rect x="4" y="5" width="16" height="16" rx="2" /><path d="M8 3v5m8-5v5M4 11h16" /></> : <><circle cx="12" cy="12" r="9" /><path d="M12 6v6h5" /></>}</svg>
        <div>{returnMode && <span>{swapShiftDate(shift)}</span>}<strong>{period.fromTime} – {period.toTime} ({Number(((swapMinutes(period.toTime) - swapMinutes(period.fromTime)) / 60).toFixed(2))}h)</strong>{!returnMode && <span>{t("Full shift")}</span>}</div>
      </div>}
    </div>}
    <footer className={styles.footer}><button type="button" className={styles.confirm} disabled={!valid || loading} onClick={() => valid && shift && period && onConfirm({ shift, period })}>{returnMode ? t("Confirm selection") : t("Confirm shift")}</button></footer>
  </section>;
}
