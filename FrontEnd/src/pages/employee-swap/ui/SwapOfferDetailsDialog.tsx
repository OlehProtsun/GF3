import { useId, useRef, type RefObject } from "react";
import { createPortal } from "react-dom";
import type { ShiftSwap } from "@entities/shift-swaps";
import { getLocale, t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CloseIcon } from "@shared/ui/icons";
import { useSwapModalEnvironment } from "./useSwapModalEnvironment";
import review from "./SwapCreateWizard.module.css";
import styles from "./SwapOfferDetailsDialog.module.css";

export function SwapOfferDetailsDialog({ swap, currentEmployeeId, busy, error, onClose, onAccept, onCancel, returnFocusRef }: {
  swap: ShiftSwap; currentEmployeeId: number | null; busy: boolean; error?: string | null;
  onClose: () => void; onAccept: (swap: ShiftSwap) => void; onCancel: (swap: ShiftSwap) => void;
  returnFocusRef?: RefObject<HTMLElement | null>;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const headingId = useId();
  useSwapModalEnvironment(dialogRef, returnFocusRef);
  const exchange = swap.requestKind === "exchange";
  const creator = swap.isCreatedByCurrentEmployee || (currentEmployeeId !== null && currentEmployeeId === swap.fromEmployeeId);
  const recipient = !creator && currentEmployeeId !== null && (currentEmployeeId === swap.targetEmployeeId ||
    currentEmployeeId === swap.acceptedByEmployeeId || (!exchange && swap.status === "open" && swap.visibility === "public"));
  const giver = swap.isManagerCreated ? swap.manualColumnName || t("Open shift") : swap.fromEmployeeName;
  const unavailable = swap.acceptanceUnavailableReason || (creator ? "This is your own swap offer." :
    swap.isScheduleLocked ? "Schedule is locked while a manager is editing it." : "Not available");
  const status = swap.status === "open" ? t("Open offer") : swap.status === "accepted" ? t("Accepted") : t("Cancelled");
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
            {index > 0 && <div className={review.exchangeArrows} aria-label={t("Reciprocal exchange")}>⇅</div>}
            <section className={review.transferCard} aria-label={panel.heading}>
              <header><div><h3>{panel.heading}</h3><p>{panel.employee || t("Not available")}</p></div>
                {panel.role && <span className={panel.role === "receive" ? review.receiveBadge : review.giveBadge}>{t(panel.role === "receive" ? "Receive" : "Give")}</span>}
              </header>
              <div className={review.transferPeriod}>
                <div className={review.dateBadge}>{date ? <><strong>{String(panel.day).padStart(2, "0")}</strong>
                  <span>{new Intl.DateTimeFormat(getLocale(), { month: "short", timeZone: "UTC" }).format(date)}</span><small>{panel.year}</small></> : t("Not available")}</div>
                <div className={review.transferMeta}><strong>{panel.schedule || t("Not available")}</strong>
                  {panel.location && <span>{panel.location}</span>}
                  <strong>{panel.from && panel.to ? `${panel.from}–${panel.to}` : t("Not available")}</strong>
                  {typeof panel.hours === "number" && Number.isFinite(panel.hours) && <span>{t("Shift hours")}: {new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 2 }).format(panel.hours)}h</span>}
                </div>
              </div>
            </section>
          </div>;
        })}
        <dl className={`${review.requestSummary} ${styles.metadata}`}>
          {[[t("Request type"), t(exchange ? "Swap shifts" : "Give shift")], [t("From"), giver],
            [t("Target"), swap.targetEmployeeName || t("Everyone")], [t("Location"), swap.shopName || swap.containerName],
            [t("Visibility"), t(swap.visibility === "private" ? "Private" : "Public")], [t("Status"), status],
            ...(swap.acceptedByEmployeeName ? [[t("Accepted by"), swap.acceptedByEmployeeName]] : [])].map(([label, value]) =>
            <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}
        </dl>
        {swap.status === "open" && !swap.canAccept && <p className={review.reviewInfo}>{t(unavailable)}</p>}
      </div>
      <footer className={styles.footer}>
        {swap.status === "open" ? <>
          <button type="button" className={styles.primary} disabled={!swap.canAccept || busy} onClick={() => onAccept(swap)}>{t("Accept shift")}</button>
          {swap.canCancel && <button type="button" disabled={busy} onClick={() => onCancel(swap)}>{t("Cancel offer")}</button>}
        </> : <span>{status}</span>}
      </footer>
    </div>
  </dialog>, document.body);
}

function hoursFromBounds(from?: string | null, to?: string | null): number | null {
  if (!from || !to || !/^([01]\d|2[0-3]):[0-5]\d$/.test(from) || !/^([01]\d|2[0-3]):[0-5]\d$/.test(to)) return null;
  const minutes = (value: string) => Number(value.slice(0, 2)) * 60 + Number(value.slice(3));
  const duration = minutes(to) - minutes(from);
  return duration > 0 ? duration / 60 : null;
}
