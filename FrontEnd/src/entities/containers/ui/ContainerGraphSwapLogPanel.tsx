import { useState } from "react";
import type { ShiftSwap } from "@entities/shift-swaps";
import { filterAcceptedShiftSwapHistory } from "@entities/shift-swaps/model/history";
import { dateTimeFormat, numberFormat, t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { ArrowRightIcon, CheckIcon, ClockIcon, EyeIcon, SearchIcon, SwapHistoryIcon, SwapOffersIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import activity from "./ContainerGraphActivity.module.css";
import { formatHistoryDate, formatHistoryPeriod, getSwapHistoryShifts, type SwapHistoryShift } from "./shiftSwapHistoryPresentation";
import styles from "./ContainerGraphSwapLogPanel.module.css";

type ContainerGraphSwapLogPanelProps = {
  acceptedSwaps: ShiftSwap[];
  isLoading: boolean;
  highlightColor: string;
  highlightSettingDisabled: boolean;
  highlightError: string | null;
  onOpenHighlightColorPicker: () => void;
  onViewComparison: (swap: ShiftSwap) => void;
};

const shiftSwapLogDayFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const shiftSwapLogAcceptedAtFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

function formatShiftSwapLogDay(item: ShiftSwap) {
  return shiftSwapLogDayFormatter.format(new Date(Date.UTC(item.year, item.month - 1, item.dayOfMonth)));
}

function formatShiftSwapLogTime(value: string) {
  return value.slice(0, 5);
}

function formatShiftSwapLogHours(value: number) {
  const normalized = Math.abs(value) < 0.05 ? 0 : value;
  return t("{0}h", numberFormat("en-GB", { maximumFractionDigits: 1 }).format(normalized));
}

function formatShiftSwapAcceptedAt(value?: string | null) {
  if (!value) {
    return t("Unknown");
  }

  return shiftSwapLogAcceptedAtFormatter.format(new Date(value));
}

function formatShiftSwapLogShift(item: ShiftSwap) {
  const locationLabel = item.shopName || item.containerName || item.scheduleName;
  const timeLabel = `${formatShiftSwapLogTime(item.fromTime)}-${formatShiftSwapLogTime(item.toTime)}`;
  const baseLabel = `${formatShiftSwapLogDay(item)} ${timeLabel} (${formatShiftSwapLogHours(item.shiftHours)})`;

  return locationLabel ? `${baseLabel} - ${locationLabel}` : baseLabel;
}

function ExchangeShift({ shift, recipient }: { shift: SwapHistoryShift; recipient?: string | null }) {
  const giver = shift.employeeName || t("Unknown employee");
  const initials = shift.employeeName?.match(/\p{L}+/gu)?.slice(0, 2)
    .map(part => Array.from(part)[0]).join("").toLocaleUpperCase();
  const schedule = shift.scheduleName?.trim();
  const shop = shift.shopName?.trim();
  const location = [schedule, shop?.toLocaleLowerCase() === schedule?.toLocaleLowerCase() ? null : shop]
    .filter(Boolean).join(" · ");
  const duration = shift.hours != null && Number.isFinite(shift.hours) ? formatShiftSwapLogHours(shift.hours) : null;

  return (
    <div className={styles.exchangeShift} role="group" aria-label={giver}>
      <div className={styles.giver}>
        {initials && <span className={styles.avatar} aria-hidden="true">{initials}</span>}
        <strong>{giver}</strong>
      </div>
      <div className={styles.recipient}>
        <span>{t("Gives to")}</span>{" "}<strong>{recipient || t("Unknown employee")}</strong>
      </div>
      <dl className={styles.exchangeDetails}>
        <div><dt>{t("Schedule")}</dt><dd className={styles.exchangeLocation}>{location || t("Not available")}</dd></div>
        <div><dt>{t("Date")}</dt><dd>{formatHistoryDate(shift)}</dd></div>
        <div><dt>{t("Shift:")}</dt><dd>{formatHistoryPeriod(shift)}{duration && ` (${duration})`}</dd></div>
      </dl>
    </div>
  );
}

export function ContainerGraphSwapLogPanel({
  acceptedSwaps,
  isLoading,
  highlightColor,
  highlightSettingDisabled,
  highlightError,
  onOpenHighlightColorPicker,
  onViewComparison,
}: ContainerGraphSwapLogPanelProps) {
  const [search, setSearch] = useState("");
  const accepted = acceptedSwaps.filter(item => item.status === "accepted");
  const filtered = filterAcceptedShiftSwapHistory(accepted, search);

  return (
    <CardSection className={activity.card} headerClassName={activity.header}
      title={<span className={activity.title} role="heading" aria-level={3}>{t("Swap log")}
        <span className={activity.badge}>{search.trim() ? `${filtered.length}/${accepted.length}` : accepted.length}</span>
      </span>}
      icon={<span className={activity.sectionIcon} aria-hidden="true"><SwapHistoryIcon size={18} /></span>}
      headerRightSlot={
        <button type="button" className={activity.iconButton}
          title={t("Accepted swap highlight color")} aria-label={t("Accepted swap highlight color")}
          disabled={highlightSettingDisabled} onClick={onOpenHighlightColorPicker}>
          <span className={activity.swatch} style={{ backgroundColor: highlightColor }} aria-hidden="true" />
        </button>
      }>
      <div className={activity.stack}>
        {highlightError && <ErrorBanner dismissible={false}>{highlightError}</ErrorBanner>}
        <div className={activity.panel}>
          <label className={styles.search}>
            <SearchIcon size={16} />
            <input
              type="search"
              value={search}
              placeholder={t("Search employee, date or schedule...")}
              aria-label={t("Search accepted swaps by giver, receiver, date or schedule")}
              onChange={event => setSearch(event.target.value)}
            />
          </label>
        </div>
        <section className={styles.history} aria-label={t("Swap history")}>
          <h4 className={activity.panelHeader}>{t("Swap history")}</h4>
          {isLoading ? (
            <p className={activity.state}>{t("Loading swap log...")}</p>
          ) : accepted.length === 0 ? (
            <p className={activity.state}>{t("No accepted swaps yet.")}</p>
          ) : filtered.length === 0 ? (
            <p className={activity.state}>{t("No swaps match this search.")}</p>
          ) : (
            <div className={styles.list}>
              {filtered.map(item => {
                const exchange = item.requestKind === "exchange" ? getSwapHistoryShifts(item) : null;
                return (
                  <article key={item.id} className={styles.swap}>
                    <div className={styles.swapHeader}>
                      <div className={styles.people}>
                        <strong>{item.fromEmployeeName}</strong>
                        {exchange ? <SwapOffersIcon size={14} aria-hidden="true" /> : <ArrowRightIcon size={14} aria-hidden="true" />}
                        <span className={styles.visuallyHidden}>{t(exchange ? "Exchange" : "to")}</span>
                        <strong>{exchange ? exchange.returned.employeeName || t("Unknown employee") : item.acceptedByEmployeeName ?? t("Employee")}</strong>
                      </div>
                      <button
                        type="button"
                        className={activity.iconButton}
                        aria-label={t("View swap comparison for {0}", item.fromEmployeeName)}
                        title={t("View before and after")}
                        onClick={() => onViewComparison(item)}
                      >
                        <EyeIcon size={17} />
                      </button>
                    </div>
                    <div className={styles.badges}>
                      <span className={`${activity.badge} ${activity.success}`}><CheckIcon size={12} />{t("Accepted")}</span>
                      {exchange && <span className={`${activity.badge} ${activity.info}`}>{t("Exchange")}</span>}
                    </div>
                    {exchange && (
                      <div className={styles.exchange}>
                        <ExchangeShift shift={exchange.source} recipient={exchange.returned.employeeName} />
                        <span className={styles.exchangeArrow} aria-hidden="true"><SwapOffersIcon size={15} /></span>
                        <ExchangeShift shift={exchange.returned} recipient={exchange.source.employeeName} />
                      </div>
                    )}
                    <dl className={styles.details}>
                      {!exchange && <div><dt>{t("Shift:")}</dt><dd>{formatShiftSwapLogShift(item)}</dd></div>}
                      <div>
                        <dt><ClockIcon size={13} aria-hidden="true" />{t("When was accepted:")}</dt>
                        <dd>{formatShiftSwapAcceptedAt(item.acceptedAtUtc)}</dd>
                      </div>
                    </dl>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </div>
    </CardSection>
  );
}
