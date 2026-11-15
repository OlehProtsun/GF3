import { useId, useLayoutEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { filterShiftSwaps, type ShiftSwap } from "@entities/shift-swaps";
import { dateTimeFormat, numberFormat, t } from "@shared/i18n";
import { ArrowRightIcon, CheckIcon, ClockIcon, CloseIcon, EyeIcon, SearchIcon, StatisticsIcon, SwapOffersIcon } from "@shared/ui/icons";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { getSwapHistoryShifts, type SwapHistoryShift } from "./shiftSwapHistoryPresentation";
import historyStyles from "./ContainerGraphSwapLogPanel.module.css";
import styles from "./ContainerSwapsDialog.module.css";

type TabKey = "all" | "open" | "accepted" | "cancelled" | "exchange";
type Props = {
  open: boolean;
  swaps: ShiftSwap[];
  isLoading: boolean;
  isError: boolean;
  actionError: string | null;
  isActionPending: boolean;
  onRetry: () => void;
  onClose: () => void;
  onView: (swap: ShiftSwap) => void;
  onCancel: (swap: ShiftSwap) => void;
  onDelete: (swap: ShiftSwap) => void;
};

const tabKeys: TabKey[] = ["all", "open", "accepted", "cancelled", "exchange"];
const dateFormatter = dateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", timeZone: "UTC" });
const time = (value?: string | null) => value ? value.slice(0, 5) : t("Not available");
const schedule = (shift: SwapHistoryShift) => {
  const name = shift.scheduleName?.trim();
  const shop = shift.shopName?.trim();
  return [name, shop?.toLocaleLowerCase() === name?.toLocaleLowerCase() ? null : shop].filter(Boolean).join(" · ") || t("Not available");
};
function date(shift: SwapHistoryShift) {
  return shift.year && shift.month && shift.day
    ? dateFormatter.format(new Date(Date.UTC(shift.year, shift.month - 1, shift.day)))
    : t("Not available");
}
function initials(name: string) {
  return name.match(/\p{L}+/gu)?.slice(0, 2).map(part => Array.from(part)[0]).join("").toLocaleUpperCase() ?? "";
}
function filterForTab(items: ShiftSwap[], tab: TabKey) {
  if (tab === "all") return items;
  if (tab === "exchange") return items.filter(item => item.requestKind === "exchange");
  return items.filter(item => item.status === tab);
}

function ExchangeSide({ shift, recipient }: { shift: SwapHistoryShift; recipient: string }) {
  const name = shift.employeeName || t("Unknown employee");
  const duration = shift.hours != null && Number.isFinite(shift.hours)
    ? t("{0}h", numberFormat("en-GB", { maximumFractionDigits: 1 }).format(shift.hours))
    : null;
  return <div className={`${historyStyles.exchangeShift} ${styles.exchangeSide}`} role="group" aria-label={name}>
    <div className={`${historyStyles.giver} ${styles.exchangeGiver}`}><span className={`${historyStyles.avatar} ${styles.exchangeAvatar}`} aria-hidden="true">{initials(name)}</span><strong>{name}</strong></div>
    <div className={`${historyStyles.recipient} ${styles.exchangeRecipient}`}>{t("Gives to")} <strong>{recipient || t("Unknown employee")}</strong></div>
    <dl className={`${historyStyles.exchangeDetails} ${styles.exchangeDetails}`}>
      <div><dt>{t("Schedule")}</dt><dd className={historyStyles.exchangeLocation}>{schedule(shift)}</dd></div>
      <div><dt>{t("Date")}</dt><dd>{date(shift)}</dd></div>
      <div><dt>{t("Shift:")}</dt><dd>{shift.from && shift.to ? `${time(shift.from)}–${time(shift.to)}${duration ? ` (${duration})` : ""}` : t("Not available")}</dd></div>
    </dl>
  </div>;
}

function OpenContainerSwapsDialog(props: Omit<Props, "open">) {
  const { swaps, isLoading, isError, actionError, isActionPending, onRetry, onClose, onView, onCancel, onDelete } = props;
  const id = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const onCloseRef = useRef(onClose);
  const [tab, setTab] = useState<TabKey>("all");
  const [search, setSearch] = useState("");
  const searched = useMemo(() => filterShiftSwaps(swaps, search), [swaps, search]);
  const shown = useMemo(() => filterForTab(searched, tab), [searched, tab]);
  const counts: Record<TabKey, number> = {
    all: swaps.length,
    open: swaps.filter(item => item.status === "open").length,
    accepted: swaps.filter(item => item.status === "accepted").length,
    cancelled: swaps.filter(item => item.status === "cancelled").length,
    exchange: swaps.filter(item => item.requestKind === "exchange").length,
  };
  const metrics = [
    [t("Total swaps"), swaps.length, StatisticsIcon, styles.neutral],
    [t("Open status"), counts.open, ClockIcon, styles.pending],
    [t("Accepted"), counts.accepted, CheckIcon, styles.success],
    [t("Cancelled"), counts.cancelled, CloseIcon, styles.danger],
    [t("Exchange"), counts.exchange, SwapOffersIcon, styles.info],
  ] as const;

  useLayoutEffect(() => { onCloseRef.current = onClose; }, [onClose]);

  useLayoutEffect(() => {
    const root = rootRef.current!;
    const previousFocus = document.activeElement;
    const oldBodyOverflow = document.body.style.overflow;
    const oldRootOverflow = document.documentElement.style.overflow;
    const anotherModalOpen = () => Array.from(document.querySelectorAll<HTMLElement>('[aria-modal="true"]')).some(item => item !== root && !item.closest("[hidden], [inert], [aria-hidden='true']"));
    const onFocusIn = (event: FocusEvent) => {
      if (!anotherModalOpen() && event.target instanceof Node && !root.contains(event.target)) closeRef.current?.focus();
    };
    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";
    closeRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (anotherModalOpen()) return;
      if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); onCloseRef.current(); }
      if (event.key === "Tab") {
        const stops = Array.from(root.querySelectorAll<HTMLElement>('button, input, [tabindex="0"]')).filter(item => item.tabIndex >= 0 && !item.matches(":disabled"));
        const index = stops.indexOf(document.activeElement as HTMLElement);
        if (!stops.length || (event.shiftKey ? index <= 0 : index === stops.length - 1)) {
          event.preventDefault();
          (event.shiftKey ? stops.at(-1) : stops[0])?.focus();
        }
      }
    };
    window.addEventListener("keydown", onKeyDown, true);
    document.addEventListener("focusin", onFocusIn);
    return () => {
      window.removeEventListener("keydown", onKeyDown, true);
      document.removeEventListener("focusin", onFocusIn);
      document.body.style.overflow = oldBodyOverflow;
      document.documentElement.style.overflow = oldRootOverflow;
      if (previousFocus instanceof HTMLElement && previousFocus.isConnected) previousFocus.focus();
    };
  }, []);

  const tabLabels: Record<TabKey, string> = {
    all: t("All"), open: t("Open status"), accepted: t("Accepted"), cancelled: t("Cancelled"), exchange: t("Exchange"),
  };

  return createPortal(<div className={styles.overlay} onMouseDown={event => {
    if (event.target === event.currentTarget && !Array.from(document.querySelectorAll('[aria-modal="true"]')).some(node => node !== rootRef.current)) onClose();
  }}>
    <div ref={rootRef} className={styles.dialog} role="dialog" aria-modal="true" aria-labelledby={`${id}-title`} aria-describedby={`${id}-description`}>
      <header className={styles.header}>
        <span className={styles.icon}><SwapOffersIcon size={23} /></span>
        <div className={styles.heading}><h2 id={`${id}-title`}>{t("Container Swaps")}</h2><p id={`${id}-description`}>{t("Review and manage all schedule swaps in this container.")}</p></div>
        <button ref={closeRef} type="button" className={styles.close} aria-label={t("Close")} onClick={onClose}><CloseIcon size={18} /></button>
      </header>
      <div className={styles.tabs} role="tablist" aria-label={t("Swap filters")}>
        {tabKeys.map((key, index) => <button key={key} ref={element => { tabRefs.current[index] = element; }} id={`${id}-tab-${key}`} type="button" role="tab" aria-label={`${tabLabels[key]} ${counts[key]}`} aria-controls={`${id}-panel`} aria-selected={tab === key} tabIndex={tab === key ? 0 : -1} onClick={() => setTab(key)} onKeyDown={event => {
          const next = event.key === "ArrowRight" ? (index + 1) % tabKeys.length : event.key === "ArrowLeft" ? (index + tabKeys.length - 1) % tabKeys.length : event.key === "Home" ? 0 : event.key === "End" ? tabKeys.length - 1 : -1;
          if (next < 0) return;
          event.preventDefault();
          setTab(tabKeys[next]);
          tabRefs.current[next]?.focus();
        }}>
          {tabLabels[key]}<span>{counts[key]}</span>
        </button>)}
      </div>
      <dl className={styles.metrics}>{metrics.map(([label, count, Icon, tone]) => <div key={label} className={`${styles.metric} ${tone}`}>
        <dt><span className={styles.metricIcon}><Icon size={16} /></span>{label}</dt><dd>{count}</dd>
      </div>)}</dl>
      <label className={styles.search}><SearchIcon size={17} /><input type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder={t("Search by employee, date or schedule")} aria-label={t("Search swaps by giver, receiver, date or schedule")} /></label>
      <div id={`${id}-panel`} className={styles.listViewport} role="tabpanel" aria-labelledby={`${id}-tab-${tab}`} tabIndex={0}>
        {actionError ? <ErrorBanner className={styles.error}>{actionError}</ErrorBanner> : null}
        {isLoading ? <p className={styles.state}>{t("Loading swaps...")}</p> : isError ? <div className={styles.state}>{t("Could not load swaps for this container.")} <button type="button" onClick={onRetry}>{t("Retry")}</button></div> : shown.length === 0 ? <p className={styles.state}>{swaps.length === 0 ? t("No swaps in this container.") : t("No swaps match this search.")}</p> : (
          <div className={styles.list}>{shown.map(swap => {
            const exchange = swap.requestKind === "exchange" ? getSwapHistoryShifts(swap) : null;
            const recipient = swap.status === "accepted" ? swap.acceptedByEmployeeName ?? t("Employee") : swap.targetEmployeeName ?? t("Everyone");
            const statusTone = swap.status === "open" ? styles.statusOpen : swap.status === "accepted" ? styles.statusAccepted : styles.statusCancelled;
            const day = dateFormatter.format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
            return <article key={swap.id} className={`${styles.swapCard} ${exchange ? styles.exchangeCard : styles.regularCard}`}>
              <div className={styles.cardHeading}>
                <span className={styles.cardIcon}><SwapOffersIcon size={18} /></span>
                <div className={styles.cardTitle}>
                  <strong>{exchange ? `${swap.fromEmployeeName} ↔ ${exchange.returned.employeeName || t("Unknown employee")}` : t("Giveaway to {0}", recipient)}</strong>
                  {!exchange ? <div className={styles.transfer}><span>{t("By {0}", swap.fromEmployeeName)}</span><ArrowRightIcon size={14} /><span>{t("To {0}", recipient)}</span></div> : null}
                </div>
                {exchange ? <div className={styles.cardControls}>
                  <div className={styles.badges}><span className={`${styles.status} ${statusTone}`}>{t(swap.status === "open" ? "Open status" : swap.status === "accepted" ? "Accepted" : "Cancelled")}</span><span className={`${styles.status} ${styles.statusExchange}`}>{t("Exchange")}</span></div>
                  <button type="button" className={styles.viewIcon} onClick={() => onView(swap)} aria-label={t("View {0} swap", swap.scheduleName)}><EyeIcon size={15} /></button>
                </div> : null}
              </div>
              {exchange ? <div className={styles.exchange}>
                <ExchangeSide shift={exchange.source} recipient={exchange.returned.employeeName || t("Unknown employee")} />
                <span className={`${historyStyles.exchangeArrow} ${styles.exchangeArrow}`} aria-hidden="true"><SwapOffersIcon size={15} /></span>
                <ExchangeSide shift={exchange.returned} recipient={exchange.source.employeeName || t("Unknown employee")} />
              </div> : <div className={styles.giveawayDetails}>
                <div className={styles.pills}><span>{[swap.scheduleName, swap.shopName?.trim().toLocaleLowerCase() === swap.scheduleName?.trim().toLocaleLowerCase() ? null : swap.shopName].filter(Boolean).join(" · ") || t("Not available")}</span><span>{day}</span><span>{time(swap.fromTime)}–{time(swap.toTime)}</span><span>{t("{0}h", numberFormat("en-GB", { maximumFractionDigits: 1 }).format(swap.shiftHours))}</span></div>
              </div>}
              <div className={styles.cardFooter}>{!exchange ? <div className={styles.badges}><span className={`${styles.status} ${statusTone}`}>{t(swap.status === "open" ? "Open status" : swap.status === "accepted" ? "Accepted" : "Cancelled")} · {t(swap.visibility === "public" ? "Public" : "Private")}</span></div> : null}<div className={styles.actions}>
                <button type="button" onClick={() => onView(swap)}><EyeIcon size={14} />{exchange ? t("View Comparison") : t("View")}</button>
                {swap.canCancel && swap.status === "open" ? <button type="button" disabled={isActionPending} onClick={() => onCancel(swap)}>{t("Cancel")}</button> : null}
                <button type="button" className={styles.delete} disabled={isActionPending} onClick={() => onDelete(swap)}>{t("Delete")}</button>
              </div></div>
            </article>;
          })}</div>
        )}
      </div>
    </div>
  </div>, document.body);
}

export function ContainerSwapsDialog(props: Props) {
  return props.open ? <OpenContainerSwapsDialog {...props} /> : null;
}
