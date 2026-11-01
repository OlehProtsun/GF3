import { useId, useRef, useState, type RefObject } from "react";
import { createPortal } from "react-dom";
import type { ShiftSwap } from "@entities/shift-swaps";
import { getLocale, translateServerMessage, t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CloseIcon, EmployeeIcon, EyeIcon, InfoIcon, NoteIcon, ShopIcon, SwapOffersIcon } from "@shared/ui/icons";
import { useSwapModalEnvironment } from "./useSwapModalEnvironment";
import { swapMinutes, swapTime, type SwapPeriod } from "./SwapShiftPickerDialog";
import styles from "./SwapOfferDetailsDialog.module.css";

export function SwapOfferDetailsDialog({ swap, currentEmployeeId, busy, error, onClose, onAccept, onCancel, returnFocusRef }: {
  swap: ShiftSwap; currentEmployeeId: number | null; busy: boolean; error?: string | null;
  onClose: () => void; onAccept: (swap: ShiftSwap, claimedPeriod?: SwapPeriod) => void; onCancel: (swap: ShiftSwap) => void;
  returnFocusRef?: RefObject<HTMLElement | null>;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const headingId = useId();
  useSwapModalEnvironment(dialogRef, returnFocusRef);
  const exchange = swap.requestKind === "exchange";
  const flexible = !exchange && swap.allowPartialAcceptance;
  const windows = swap.eligibleClaimWindows ?? [];
  const fullAllowed = swap.canAcceptFull ?? swap.canAccept;
  const [custom, setCustom] = useState(Boolean(flexible && !fullAllowed));
  const [period, setPeriod] = useState<SwapPeriod>(flexible && !fullAllowed && windows[0] ? windows[0] : { fromTime: swap.fromTime, toTime: swap.toTime });
  const allowed = (value: SwapPeriod) => /^([01]\d|2[0-3]):[0-5]\d$/.test(value.fromTime) && /^([01]\d|2[0-3]):[0-5]\d$/.test(value.toTime) &&
    value.fromTime < value.toTime && windows.some(window => value.fromTime >= window.fromTime && value.toTime <= window.toTime);
  const held = swap.isAwaitingManagerApproval || swap.approvalPendingForCurrentEmployee;
  const valid = !held && (!swap.requiresManagerApproval || swap.canRequestManagerApproval === true) && swap.canAccept && (!flexible || (custom ? allowed(period) : fullAllowed));
  const selectedWindow = windows.find(window => period.fromTime >= window.fromTime && period.toTime <= window.toTime);
  const creator = swap.isCreatedByCurrentEmployee || (currentEmployeeId !== null && currentEmployeeId === swap.fromEmployeeId);
  const recipient = !creator && currentEmployeeId !== null && (currentEmployeeId === swap.targetEmployeeId ||
    currentEmployeeId === swap.acceptedByEmployeeId || (!exchange && swap.status === "open" && swap.visibility === "public"));
  const giver = swap.isManagerCreated ? swap.manualColumnName || t("Open shift") : swap.fromEmployeeName;
  const unavailable = swap.acceptanceUnavailableReason || (creator ? "This is your own swap offer." :
    swap.isScheduleLocked ? "Schedule is locked while a manager is editing it." : "Not available");
  const status = swap.status === "open" ? t(held ? "Awaiting manager approval" : "Open offer") : swap.status === "accepted" ? t("Accepted") : t("Cancelled");
  const panels = [{ schedule: swap.scheduleName, location: swap.shopName || swap.containerName, employee: giver,
    year: swap.year, month: swap.month, day: swap.dayOfMonth, from: swap.fromTime, to: swap.toTime, hours: swap.shiftHours,
    heading: creator ? t("Your shift (you give)") : recipient ? t("Shift you receive") : t("Offered shift"),
    role: creator ? "give" : recipient ? "receive" : null },
  ...(exchange ? [{ schedule: swap.returnScheduleName, location: swap.returnShopName, employee: swap.targetEmployeeName || swap.acceptedByEmployeeName,
    year: swap.returnYear, month: swap.returnMonth, day: swap.returnDayOfMonth, from: swap.returnFromTime, to: swap.returnToTime,
    hours: swap.returnShiftHours ?? hoursFromBounds(swap.returnFromTime, swap.returnToTime),
    heading: creator ? t("Shift you receive") : recipient ? t("Your shift (you give)") : t("Return shift"),
    role: creator ? "receive" : recipient ? "give" : null }] : [])];
  return createPortal(<dialog ref={dialogRef} className={styles.dialog} aria-labelledby={headingId}
    onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}
    onClick={event => { if (event.target === event.currentTarget && !busy) onClose(); }}>
    <div className={styles.shell}>
      <header className={styles.header}><h2 id={headingId}>{t("Offer details")}</h2>
        <button type="button" disabled={busy} aria-label={t("Close")} onClick={onClose}><CloseIcon size={20} /></button></header>
      <div className={styles.body}>
        {error && <ErrorBanner dismissible={false}>{error}</ErrorBanner>}
        {panels.map((panel, index) => {
          const date = panel.year && panel.month && panel.day ? new Date(Date.UTC(panel.year, panel.month - 1, panel.day)) : null;
          return <div key={index}>
            {index > 0 && <div className={styles.exchangeArrows} aria-label={t("Reciprocal exchange")}>⇅</div>}
            <section className={styles.transferCard} aria-label={panel.heading}>
              <header><div><h3>{panel.heading}</h3><p>{panel.employee || t("Not available")}</p></div>
                {panel.role && <span className={panel.role === "receive" ? styles.receiveBadge : styles.giveBadge}>{t(panel.role === "receive" ? "Receive" : "Give")}</span>}
              </header>
              <div className={styles.transferPeriod}>
                <div className={styles.dateBadge}>{date ? <><strong>{String(panel.day).padStart(2, "0")}</strong>
                  <span>{new Intl.DateTimeFormat(getLocale(), { month: "short", timeZone: "UTC" }).format(date)}</span><small>{panel.year}</small></> : t("Not available")}</div>
                <div className={styles.transferMeta}><strong>{panel.schedule || t("Not available")}</strong>
                  {panel.location && <span className={styles.detailLine}><ShopIcon size={16} className={styles.shopIcon} />{panel.location}</span>}
                  <strong className={styles.detailLine}><ClockIcon />{panel.from && panel.to ? `${panel.from}–${panel.to}` : t("Not available")}</strong>
                  {typeof panel.hours === "number" && Number.isFinite(panel.hours) && <small>{t("Shift hours")}: {t("{0}h", new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 2 }).format(panel.hours))}</small>}
                </div>
              </div>
            </section>
          </div>;
        })}
        {flexible && swap.status === "open" && <p className={styles.info}><InfoIcon size={20} />{t("Allow custom time")}</p>}
        {swap.status === "open" && flexible && swap.canAccept && <section className={styles.claim} aria-label={t("Claim period")}>
          {!fullAllowed && <p>{t("You already work during part of this shift; you can claim {0}.", windows.map(window => `${window.fromTime}–${window.toTime}`).join(", "))}</p>}
          <div className={styles.claimModes}>
            {fullAllowed && <button type="button" aria-pressed={!custom} onClick={() => setCustom(false)}>{t("Full shift")}</button>}
            <button type="button" aria-pressed={custom} onClick={() => { setCustom(true); if (!allowed(period) && windows[0]) setPeriod(windows[0]); }}>{t("Custom period")}</button>
          </div>
          {custom && <>
            <output className={styles.selectedPeriod} aria-label={t("Claim period")}>{period.fromTime}–{period.toTime}</output>
            {windows.length > 1 && <div className={styles.claimWindows} role="group" aria-label={t("Available claim windows")}>
              {windows.map(window => <button type="button" key={`${window.fromTime}:${window.toTime}`} aria-pressed={selectedWindow === window} onClick={() => setPeriod(window)}>{window.fromTime}–{window.toTime}</button>)}
            </div>}
            <div className={styles.timeControls}>
              <div className={styles.timeCards}>
              {(["fromTime", "toTime"] as const).map(edge => <div className={styles.timeCard} key={edge}>
                <label htmlFor={`${headingId}-${edge}`}>{t(edge === "fromTime" ? "From" : "To")}</label>
                <div className={styles.timeValue}>
                  <input id={`${headingId}-${edge}`} type="time" step={900} min={selectedWindow?.fromTime} max={selectedWindow?.toTime} value={period[edge]} onChange={event => setPeriod({ ...period, [edge]: event.target.value })} />
                  <ClockIcon />
                </div>
                <div className={styles.timeButtons}>
                  <button type="button" aria-label={t("Decrease {0}", t(edge === "fromTime" ? "From" : "To"))} disabled={!allowed({ ...period, [edge]: swapTime(swapMinutes(period[edge]) - 15) })} onClick={() => setPeriod({ ...period, [edge]: swapTime(swapMinutes(period[edge]) - 15) })}>−</button>
                  <span aria-hidden="true" />
                  <button type="button" aria-label={t("Increase {0}", t(edge === "fromTime" ? "From" : "To"))} disabled={!allowed({ ...period, [edge]: swapTime(swapMinutes(period[edge]) + 15) })} onClick={() => setPeriod({ ...period, [edge]: swapTime(swapMinutes(period[edge]) + 15) })}>+</button>
                </div>
              </div>)}
              </div>
              <p className={styles.helper}>{t("Unclaimed time will remain available as a new offer.")}</p>
            </div>
            {!allowed(period) && <p role="alert">{t("Choose one continuous period inside an available claim window.")}</p>}
          </>}
        </section>}
        <dl className={styles.metadata}>
          {([[t("Request type"), t(exchange ? "Swap shifts" : "Give shift"), SwapOffersIcon], [t("From"), giver, EmployeeIcon],
            [t("Target"), swap.targetEmployeeName || t("Everyone"), EmployeeIcon], [t("Location"), swap.shopName || swap.containerName, ShopIcon],
            [t("Visibility"), t(swap.visibility === "private" ? "Private" : "Public"), EyeIcon], [t("Status"), status, NoteIcon],
            ...(swap.acceptedByEmployeeName ? [[t("Accepted by"), swap.acceptedByEmployeeName, EmployeeIcon] as const] : [])] as const).map(([label, value, Icon]) =>
            <div key={label}><dt><Icon size={17} className={Icon === ShopIcon ? styles.shopIcon : undefined} /><span>{label}</span></dt><dd>{value}</dd></div>)}
        </dl>
        {swap.status === "open" && !swap.canAccept && <p className={styles.info}>{translateServerMessage(unavailable)}</p>}
      </div>
      {held ? <p role="status">{t(swap.approvalPendingForCurrentEmployee ? "Awaiting manager approval" : "This offer is awaiting manager approval.")}</p> : null}
      {held || swap.requiresManagerApproval ? <p>{t("This offer is unavailable to others while manager approval is pending.")}</p> : null}
      <footer className={styles.footer}>
        {swap.status === "open" ? <>
          <button type="button" className={styles.primary} aria-busy={busy} disabled={!valid || busy} onClick={() => onAccept(swap, flexible && custom ? period : undefined)}>{t(held ? "Awaiting manager approval" : swap.requiresManagerApproval ? swap.canRequestManagerApproval ? "Request manager approval" : "Not available" : "Accept shift")}<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><path d="M4 12h16m-6-6 6 6-6 6" /></svg></button>
          {swap.canCancel && <button type="button" disabled={busy} onClick={() => onCancel(swap)}>{t("Cancel offer")}</button>}
        </> : <span>{status}</span>}
      </footer>
    </div>
  </dialog>, document.body);
}

function ClockIcon() {
  return <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 6v6l4 2" /></svg>;
}

function hoursFromBounds(from?: string | null, to?: string | null): number | null {
  if (!from || !to || !/^([01]\d|2[0-3]):[0-5]\d$/.test(from) || !/^([01]\d|2[0-3]):[0-5]\d$/.test(to)) return null;
  const minutes = (value: string) => Number(value.slice(0, 2)) * 60 + Number(value.slice(3));
  const duration = minutes(to) - minutes(from);
  return duration > 0 ? duration / 60 : null;
}
