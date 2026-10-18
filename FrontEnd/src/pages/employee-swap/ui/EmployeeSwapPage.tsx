import { createPortal } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import {
  useCallback,
  useDeferredValue,
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent,
} from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useEmployeeScheduleListQuery,
} from "@entities/employee-schedule";
import {
  useEmployeeUiStateQuery,
  useSetEmployeeSwapPinMutation,
} from "@entities/employee-ui-state";
import {
  filterShiftSwaps,
  useAcceptEmployeeShiftSwapMutation,
  useCancelEmployeeShiftSwapMutation,
  useCreateEmployeeShiftSwapMutation,
  useEmployeeShiftSwapEmployeesQuery,
  useEmployeeShiftSwapsQuery,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SwapCreateWizard } from "./SwapCreateWizard";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ArrowIcon, CloseIcon, PinIcon, PlusIcon, SearchIcon, SwapHistoryIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeSwapPage.module.css";

const swapOfferDateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  timeZone: "UTC",
});

type SwapFilter = "all" | "mine" | "available" | "pinned";

type SwapPreviewStats = {
  hoursBefore: number;
  hoursAfter: number;
  workDaysBefore: number;
  workDaysAfter: number;
  freeDaysBefore: number;
  freeDaysAfter: number;
};

function formatSwapDate(value: Pick<ShiftSwap, "year" | "month" | "dayOfMonth">) {
  return swapOfferDateFormatter
    .format(new Date(Date.UTC(value.year, value.month - 1, value.dayOfMonth)))
    .replaceAll("/", ".");
}

function getSwapCode(swap: ShiftSwap) {
  const source = swap.manualColumnName?.trim() || swap.scheduleName.trim() || swap.containerName.trim();
  const codeMatch = source.match(/\b[A-Za-z]+\d+\b/);
  if (codeMatch) {
    return codeMatch[0].slice(0, 4).toUpperCase();
  }

  const initials = source
    .split(/\s+/)
    .filter(Boolean)
    .map(part => part[0])
    .join("")
    .slice(0, 3)
    .toUpperCase();

  return initials || "SWP";
}

function formatHours(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  return Number.isInteger(roundedValue) ? `${roundedValue}h` : `${roundedValue.toFixed(1)}h`;
}

function formatDelta(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  const prefix = roundedValue > 0 ? "+" : "";
  return `${prefix}${formatHours(roundedValue)}`;
}

function formatIntegerDelta(value: number) {
  return value > 0 ? `+${value}` : String(value);
}

function getSwapOfferStats(swap: ShiftSwap): SwapPreviewStats {
  return {
    hoursBefore: swap.currentEmployeeHoursBefore,
    hoursAfter: swap.currentEmployeeHoursAfter,
    workDaysBefore: swap.currentEmployeeWorkDaysBefore,
    workDaysAfter: swap.currentEmployeeWorkDaysAfter,
    freeDaysBefore: swap.currentEmployeeFreeDaysBefore,
    freeDaysAfter: swap.currentEmployeeFreeDaysAfter,
  };
}

function StatsDeltaBadge({ value, kind }: { value: number; kind: "hours" | "days" }) {
  const isNeutral = Math.abs(value) < 0.001;
  const label = kind === "hours" ? formatDelta(value) : formatIntegerDelta(value);

  return (
    <span
      className={[
        styles.statsDelta,
        isNeutral ? styles.statsDeltaNeutral : value > 0 ? styles.statsDeltaPositive : styles.statsDeltaNegative,
      ].filter(Boolean).join(" ")}
    >
      {t(label)}
    </span>
  );
}

function StatsCompareItem({
  label,
  before,
  after,
  kind,
}: {
  label: string;
  before: string | number;
  after: string | number;
  kind: "hours" | "days";
}) {
  const delta = typeof before === "number" && typeof after === "number" ? after - before : 0;

  return (
    <div className={styles.statsCompareItem}>
      <span>{t(label)}</span>
      <div className={styles.statsCompareValues}>
        <strong>{kind === "hours" && typeof before === "number" ? formatHours(before) : before}</strong>
        <small>{t("before")}</small>
        <span className={styles.statsArrow}>{"->"}</span>
        <strong>{kind === "hours" && typeof after === "number" ? formatHours(after) : after}</strong>
        <small>{t("after")}</small>
        <StatsDeltaBadge value={delta} kind={kind} />
      </div>
    </div>
  );
}

function SwapStatisticsCard({
  stats,
  summary = t("Your month"),
  collapsible = false,
  defaultExpanded = true,
}: {
  stats: SwapPreviewStats | null;
  summary?: string;
  collapsible?: boolean;
  defaultExpanded?: boolean;
}) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  if (!stats) {
    return null;
  }

  const shouldShowBody = !collapsible || isExpanded;

  return (
    <div className={styles.statsPanel}>
      <div className={styles.statsHeader}>
        <div>
          <span>{t("Statistics")}</span>
          <strong>{summary}</strong>
        </div>

        {collapsible ? (
          <button
            type="button"
            className={styles.statsToggleButton}
            aria-label={isExpanded ? t("Collapse statistics") : t("Expand statistics")}
            aria-expanded={isExpanded}
            onClick={() => setIsExpanded(value => !value)}
          >
            <ArrowIcon size={12} />
          </button>
        ) : null}
      </div>

      {shouldShowBody ? (
        <div className={styles.statsCompareGrid}>
          <StatsCompareItem label={t("Total hours")} before={stats.hoursBefore} after={stats.hoursAfter} kind="hours" />
          <StatsCompareItem label={t("Work days")} before={stats.workDaysBefore} after={stats.workDaysAfter} kind="days" />
          <StatsCompareItem label={t("Free days")} before={stats.freeDaysBefore} after={stats.freeDaysAfter} kind="days" />
        </div>
      ) : null}
    </div>
  );
}

function PinToggleButton({
  isPinned,
  label,
  onToggle,
}: {
  isPinned: boolean;
  label: string;
  onToggle: () => void;
}) {
  const actionLabel = `${isPinned ? t("Unpin") : t("Pin")} ${t(label)}`;
  return (
    <button
      type="button"
      className={[
        styles.collapseButton,
        styles.pinButton,
        isPinned ? styles.pinButtonActive : "",
      ].filter(Boolean).join(" ")}
      aria-label={actionLabel}
      aria-pressed={isPinned}
      title={actionLabel}
      onClick={onToggle}
    >
      <PinIcon size={15} />
    </button>
  );
}

function SwapOfferCard({
  swap,
  stats,
  isBusy,
  isPinned,
  onTogglePin,
  onAccept,
  onCancel,
}: {
  swap: ShiftSwap;
  stats: SwapPreviewStats | null;
  isBusy: boolean;
  isPinned: boolean;
  onTogglePin: (swapId: number) => void;
  onAccept: (swap: ShiftSwap) => void;
  onCancel: (swap: ShiftSwap) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(false);
  const isOpen = swap.status === "open";
  const isScheduleLocked = isOpen && swap.isScheduleLocked;
  const unavailableReason = isOpen && !swap.canAccept && (!swap.isCreatedByCurrentEmployee ||
    (swap.requestKind === "exchange" && swap.acceptanceUnavailableReason !== "This is your own swap offer."))
    ? swap.acceptanceUnavailableReason
      ?? (isScheduleLocked
        ? t("Schedule is locked while a manager is editing it.")
        : null)
    : null;
  const availabilityLabel = !isOpen
    ? swap.status === "accepted" ? t("Accepted") : t("Cancelled")
    : swap.isCreatedByCurrentEmployee
      ? t("My offer")
      : swap.canAccept
        ? t("Can")
        : t("Can’t");

  const handleCardClick = (event: MouseEvent<HTMLElement>) => {
    if ((event.target as Element).closest("button, a, input, select, textarea")) {
      return;
    }

    setIsExpanded(value => !value);
  };

  const handleCardKeyDown = (event: ReactKeyboardEvent<HTMLElement>) => {
    if (event.target !== event.currentTarget || (event.key !== "Enter" && event.key !== " ")) {
      return;
    }

    event.preventDefault();
    setIsExpanded(value => !value);
  };

  return (
    <article
      className={[styles.offerCard, isPinned ? styles.offerCardPinned : ""].filter(Boolean).join(" ")}
      data-pinned={isPinned ? "true" : "false"}
      data-expanded={isExpanded ? "true" : "false"}
      tabIndex={0}
      title={isExpanded ? t("Click to collapse this swap") : t("Click to expand this swap")}
      onClick={handleCardClick}
      onKeyDown={handleCardKeyDown}
    >
      <span className={styles.offerAvatar} aria-hidden="true">{getSwapCode(swap)}</span>

      <div className={styles.offerSummary}>
        <div className={styles.offerHeader}>
          <div className={styles.offerTitle}>
            <strong>{swap.scheduleName}</strong>
          </div>

          <div className={styles.offerHeaderActions}>
            <PinToggleButton
              isPinned={isPinned}
              label={t("{0} swap", swap.scheduleName)}
              onToggle={() => onTogglePin(swap.id)}
            />
            <div className={styles.offerBadges}>
              <span className={[
                styles.badge,
                swap.visibility === "private" ? styles.badgePrivate : styles.badgePublic,
              ].join(" ")}>
                {swap.visibility === "private" ? t("Private") : t("Public")}
              </span>
              <span className={[
                styles.badge,
                !isOpen ? styles.badgeMuted : swap.isCreatedByCurrentEmployee
                  ? styles.badgeMine
                  : swap.canAccept ? styles.badgeCan : styles.badgeCant,
              ].join(" ")}>
                {availabilityLabel}
              </span>
              {isScheduleLocked ? <span className={[styles.badge, styles.badgeLocked].join(" ")}>{t("Locked")}</span> : null}
            </div>
          </div>
        </div>

        <div className={styles.offerMeta}>
          <span>{`${formatSwapDate(swap)} → ${swap.fromTime} – ${swap.toTime}`}</span>
          <small>{t("From: {0}", swap.fromEmployeeName)}</small>
          {swap.requestKind === "exchange" && <>
            <small>{t("Swap shifts")} · {swap.targetEmployeeName}</small>
            <small>{t("In return")}: {swap.returnScheduleName} · {String(swap.returnDayOfMonth).padStart(2, "0")}.{String(swap.returnMonth).padStart(2, "0")}.{swap.returnYear} · {swap.returnFromTime}–{swap.returnToTime}</small>
          </>}
        </div>
      </div>

      {swap.canAccept || swap.canCancel ? (
        <div className={styles.cardActions}>
          {swap.canAccept ? (
            <button
              type="button"
              className={styles.primaryButton}
              disabled={isBusy}
              onClick={() => onAccept(swap)}
            >
              {t("Accept")}
            </button>
          ) : null}
          {swap.canCancel ? (
            <button
              type="button"
              className={styles.secondaryButton}
              aria-label={t("Cancel offer")}
              disabled={isBusy}
              onClick={() => onCancel(swap)}
            >
              {t("Cancel")}
            </button>
          ) : null}
        </div>
      ) : null}

      {isExpanded ? (
        <div className={styles.offerExpandedContent} data-employee-motion>
          <div className={styles.detailGrid}>
            <div className={styles.detailItem}>
              <span>{t("Location")}</span>
              <strong>{swap.shopName || swap.containerName || t("Schedule")}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("From")}</span>
              <strong>{swap.fromEmployeeName}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("Target")}</span>
              <strong>{swap.targetEmployeeName ?? t("Everyone")}</strong>
            </div>
            <div className={styles.detailItem}>
              <span>{t("Shift hours")}</span>
              <strong>{formatHours(swap.shiftHours)}</strong>
            </div>
            {swap.acceptedByEmployeeName ? (
              <div className={styles.detailItem}>
                <span>{t("Accepted by")}</span>
                <strong>{swap.acceptedByEmployeeName}</strong>
              </div>
            ) : null}
          </div>

          <div className={styles.offerExpandedStats}>
            <SwapStatisticsCard stats={stats} />
          </div>
        </div>
      ) : null}

      <div className={styles.offerFooter}>
        <span className={styles.cardChevron} aria-hidden="true"><ArrowIcon size={13} /></span>
      </div>
      {unavailableReason ? (
        <div className={styles.offerUnavailableReason} title={t(unavailableReason)}>
          <span className={styles.unavailableText}>{t(unavailableReason)}</span>
        </div>
      ) : null}
    </article>
  );
}

export function EmployeeSwapPage() {
  const stageRef = useRef<HTMLDivElement>(null);
  const createOfferTriggerRef = useRef<HTMLButtonElement>(null);
  const historyDialogRef = useRef<HTMLDialogElement>(null);
  const [historyDialogElement, setHistoryDialogElement] = useState<HTMLDialogElement | null>(null);
  const setHistoryDialogRef = useCallback((element: HTMLDialogElement | null) => {
    historyDialogRef.current = element;
    setHistoryDialogElement(element);
  }, []);
  const [stageWidth, setStageWidth] = useState(430);
  const [stageHeight, setStageHeight] = useState(900);
  const backdropPath = `M34 22H150C161 22 168 31 168 48V65C168 87 181 99 203 99H${stageWidth - 54}C${stageWidth - 22} 99 ${stageWidth - 4} 117 ${stageWidth - 4} 148V${stageHeight - 52}C${stageWidth - 4} ${stageHeight - 35} ${stageWidth - 17} ${stageHeight - 22} ${stageWidth - 34} ${stageHeight - 22}H${stageWidth - 150}C${stageWidth - 161} ${stageHeight - 22} ${stageWidth - 168} ${stageHeight - 31} ${stageWidth - 168} ${stageHeight - 48}V${stageHeight - 65}C${stageWidth - 168} ${stageHeight - 87} ${stageWidth - 181} ${stageHeight - 99} ${stageWidth - 203} ${stageHeight - 99}H54C22 ${stageHeight - 99} 4 ${stageHeight - 117} 4 ${stageHeight - 148}V52C4 35 17 22 34 22Z`;

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(([entry]) => {
      if (entry.contentRect.width > 0) setStageWidth(entry.contentRect.width);
      if (entry.contentRect.height > 0) setStageHeight(entry.contentRect.height);
    });
    observer.observe(stage);
    return () => observer.disconnect();
  }, []);

  const { session } = useAuth();
  const employeeId = session?.employeeId && session.employeeId > 0 ? session.employeeId : null;
  const schedulesQuery = useEmployeeScheduleListQuery();
  const swapsQuery = useEmployeeShiftSwapsQuery();
  const targetEmployeesQuery = useEmployeeShiftSwapEmployeesQuery();
  const uiStateQuery = useEmployeeUiStateQuery(Boolean(employeeId));
  const createSwapMutation = useCreateEmployeeShiftSwapMutation();
  const acceptSwapMutation = useAcceptEmployeeShiftSwapMutation();
  const cancelSwapMutation = useCancelEmployeeShiftSwapMutation();
  const setSwapPinMutation = useSetEmployeeSwapPinMutation();
  const schedules = schedulesQuery.data ?? [];
  const swaps = swapsQuery.data ?? [];
  const targetEmployees = targetEmployeesQuery.data ?? [];
  const [actionError, setActionError] = useState<string | null>(null);
  const [isGiveAwayExpanded, setIsGiveAwayExpanded] = useState(false);
  const [isHistoryExpanded, setIsHistoryExpanded] = useState(false);
  useEffect(() => {
    const dialog = historyDialogRef.current;
    if (!isHistoryExpanded || !dialog) return;
    const previousOverflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      document.body.style.overflow = previousOverflow;
    };
  }, [isHistoryExpanded]);
  const [swapSearchQuery, setSwapSearchQuery] = useState("");
  const [swapFilter, setSwapFilter] = useState<SwapFilter>("all");
  const [pendingUnpinSwapId, setPendingUnpinSwapId] = useState<number | null>(null);
  const [pendingAcceptSwap, setPendingAcceptSwap] = useState<ShiftSwap | null>(null);
  const [isAcceptSubmitting, setIsAcceptSubmitting] = useState(false);
  const acceptInFlightRef = useRef(false);
  const queryClient = useQueryClient();
  const deferredSwapSearchQuery = useDeferredValue(swapSearchQuery);
  const pinnedSwapIdSet = useMemo(
    () => new Set((uiStateQuery.data?.pinnedSwapIds ?? []).map(String)),
    [uiStateQuery.data?.pinnedSwapIds],
  );
  const sortedSwaps = useMemo(() => {
    const pinnedSwaps: ShiftSwap[] = [];
    const regularSwaps: ShiftSwap[] = [];
    swaps.forEach(swap => {
      (pinnedSwapIdSet.has(String(swap.id)) ? pinnedSwaps : regularSwaps).push(swap);
    });
    return [...pinnedSwaps, ...regularSwaps];
  }, [pinnedSwapIdSet, swaps]);
  const openSwaps = useMemo(() => sortedSwaps.filter(swap => swap.status === "open"), [sortedSwaps]);
  const filteredOpenSwaps = useMemo(() => {
    const swapsForFilter = openSwaps.filter(swap => {
      if (swapFilter === "mine") {
        return swap.isCreatedByCurrentEmployee;
      }
      if (swapFilter === "available") {
        return swap.canAccept;
      }
      if (swapFilter === "pinned") {
        return pinnedSwapIdSet.has(String(swap.id));
      }
      return true;
    });

    return filterShiftSwaps(swapsForFilter, deferredSwapSearchQuery);
  }, [deferredSwapSearchQuery, openSwaps, pinnedSwapIdSet, swapFilter]);
  const swapHistory = useMemo(() => sortedSwaps.filter(swap => swap.status !== "open"), [sortedSwaps]);
  const pendingUnpinSwap = pendingUnpinSwapId === null
    ? null
    : swaps.find(swap => swap.id === pendingUnpinSwapId) ?? null;
  const isActionBusy = createSwapMutation.isPending || acceptSwapMutation.isPending || cancelSwapMutation.isPending || isAcceptSubmitting;
  const loadError = schedulesQuery.error ?? swapsQuery.error ?? targetEmployeesQuery.error ?? uiStateQuery.error;
  const hasSwapSearch = swapSearchQuery.trim().length > 0;
  const handleToggleSwapPin = (swapId: number) => {
    if (pinnedSwapIdSet.has(String(swapId))) {
      setPendingUnpinSwapId(swapId);
      return;
    }

    setSwapPinMutation.mutate(
      { swapId, pinned: true },
      { onError: error => setActionError(getErrorMessage(error, t("Could not pin this swap."))) },
    );
  };

  const handleConfirmUnpin = () => {
    if (pendingUnpinSwapId !== null) {
      setSwapPinMutation.mutate(
        { swapId: pendingUnpinSwapId, pinned: false },
        { onError: error => setActionError(getErrorMessage(error, t("Could not unpin this swap."))) },
      );
    }
    setPendingUnpinSwapId(null);
  };

  const handleAccept = (swap: ShiftSwap) => {
    if (swap.status !== "open" || !swap.canAccept || isActionBusy || acceptInFlightRef.current) return;
    setActionError(null);
    setPendingAcceptSwap(swap);
  };

  const handleConfirmAccept = () => {
    if (!pendingAcceptSwap || isActionBusy || acceptInFlightRef.current) return;
    const currentSwap = swaps.find(swap => swap.id === pendingAcceptSwap.id);
    if (!currentSwap || currentSwap.status !== "open" || !currentSwap.canAccept) {
      setPendingAcceptSwap(null);
      setActionError(t("This offer is no longer available. The swap list has been refreshed."));
      void queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employee() });
      return;
    }
    acceptInFlightRef.current = true;
    setIsAcceptSubmitting(true);
    setActionError(null);
    const finish = () => {
      acceptInFlightRef.current = false;
      setIsAcceptSubmitting(false);
      setPendingAcceptSwap(null);
    };
    acceptSwapMutation.mutate(currentSwap.id, {
      onSuccess: finish,
      onError: error => {
        finish();
        setActionError(getErrorMessage(error, t("Could not accept this swap offer.")));
        void queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.employee() });
        void queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
      },
    });
  };

  const handleCancel = (swap: ShiftSwap) => {
    setActionError(null);
    cancelSwapMutation.mutate(swap.id, {
      onError: error => setActionError(getErrorMessage(error, t("Could not cancel this swap offer."))),
    });
  };

  return (
    <div className={styles.swapPage}>
      <div ref={stageRef} className={styles.swapStage}>
        <svg className={styles.swapBackdrop} viewBox={`0 0 ${stageWidth} ${stageHeight}`} preserveAspectRatio="none" aria-hidden="true">
          <path d={backdropPath} />
        </svg>

        <header className={styles.swapHeader} data-employee-motion>
          <div className={styles.swapIdentity}>
            <span className={styles.swapLogo} aria-hidden="true">
              <span className={styles.swapLogoMark}>⇄</span>
            </span>
            <div>
              <h1>{t("Swap")}</h1>
              <span>{t("Shift exchange")}</span>
            </div>
          </div>

          <button
            type="button"
            ref={createOfferTriggerRef}
            className={styles.createOfferButton}
            aria-expanded={isGiveAwayExpanded}
            aria-controls="create-swap-panel"
            aria-label={t("Create offer")}
            title={t("Create offer")}
            onClick={() => setIsGiveAwayExpanded(value => !value)}
          >
            <PlusIcon size={25} />
            <span className={styles.fullActionLabel}>{t("Create offer")}</span>
            <span className={styles.shortActionLabel}>{t("Add")}</span>
          </button>
        </header>

        <div className={styles.swapContentClip} style={{ clipPath: `path("${backdropPath}")` }}>
        <main className={styles.swapBody}>
          {loadError ? <ErrorBanner dismissible={false}>{getErrorMessage(loadError, t("Could not load swap data."))}</ErrorBanner> : null}
          {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

          <section className={styles.offersSection} aria-labelledby="open-swaps-heading">
            <h2 id="open-swaps-heading" className={styles.visuallyHidden}>{t("Open swaps")}</h2>
            <div className={styles.offerToolbar} data-employee-motion>
          <label className={styles.swapSearchField} htmlFor="employee-swap-search">
            <SearchIcon size={16} className={styles.swapSearchIcon} />
            <input
              id="employee-swap-search"
              className={styles.swapSearchInput}
              type="search"
              value={swapSearchQuery}
              placeholder={t("Search employee, date or schedule")}
              aria-label={t("Search swaps by giver, receiver, date or schedule")}
              onChange={event => setSwapSearchQuery(event.target.value)}
            />
            {hasSwapSearch ? (
              <button
                type="button"
                className={styles.clearSearchButton}
                aria-label={t("Clear swap search")}
                title={t("Clear search")}
                onClick={() => setSwapSearchQuery("")}
              >
                <CloseIcon size={13} />
              </button>
            ) : null}
          </label>

              <div className={styles.filterRow} role="group" aria-label={t("Filter swap offers")}>
                {([
                  ["all", "All"],
                  ["mine", "My offers"],
                  ["available", "Can"],
                  ["pinned", "Pinned"],
                ] as const).map(([value, label]) => (
                  <button
                    key={value}
                    type="button"
                    className={[styles.filterButton, swapFilter === value ? styles.filterButtonActive : ""].filter(Boolean).join(" ")}
                    aria-pressed={swapFilter === value}
                    onClick={() => setSwapFilter(value)}
                  >
                    {t(label)}
                  </button>
                ))}
                <span className={styles.searchResultText}>
                  {hasSwapSearch || swapFilter !== "all" ? t("{0} of {1}", filteredOpenSwaps.length, openSwaps.length) : t("{0} offers", openSwaps.length)}
                </span>
              </div>
            </div>

            <div data-motion-list className={[
              styles.offerList,
              filteredOpenSwaps.length >= 5 ? styles.offerListScrollable : "",
            ].filter(Boolean).join(" ")}>
              {openSwaps.length === 0 ? (
                <p className={`${styles.emptyText} ${styles.emptyState}`}><span aria-hidden="true">↔</span>{t("Your next shift swap starts here.")}</p>
              ) : filteredOpenSwaps.length === 0 ? (
                <p className={styles.emptyText}>{hasSwapSearch ? t("No open swap offers match \"{0}\".", swapSearchQuery.trim()) : t("No swap offers match this filter.")}</p>
              ) : filteredOpenSwaps.map(swap => (
                <SwapOfferCard
                  key={swap.id}
                  swap={swap}
                  stats={getSwapOfferStats(swap)}
                  isBusy={isActionBusy}
                  isPinned={pinnedSwapIdSet.has(String(swap.id))}
                  onTogglePin={handleToggleSwapPin}
                  onAccept={handleAccept}
                  onCancel={handleCancel}
                />
              ))}
            </div>
          </section>
        </main>
        </div>

        <button
          type="button"
          className={styles.historyButton}
          aria-expanded={isHistoryExpanded}
          aria-controls="swap-history-panel"
          aria-label={t("Swap history")}
          title={t("Swap history")}
          onClick={() => setIsHistoryExpanded(value => !value)}
        >
          <span aria-hidden="true"><SwapHistoryIcon size={22} /></span>
          <span className={`${styles.historyButtonLabel} ${styles.fullActionLabel}`}>{t("Swap history")}</span>
          <span className={styles.shortActionLabel}>{t("History")}</span>
        </button>
      </div>

      <dialog ref={setHistoryDialogRef} className={styles.creationDialog} aria-labelledby="swap-history-heading" onCancel={event => { if (pendingUnpinSwapId !== null) { event.preventDefault(); setPendingUnpinSwapId(null); } else setIsHistoryExpanded(false); }} onClick={event => { if (event.target === event.currentTarget) setIsHistoryExpanded(false); }}>
      {isHistoryExpanded ? (
        <section id="swap-history-panel" className={styles.historyPanel} aria-labelledby="swap-history-heading">
          <div className={styles.panelHeaderRow}>
            <div>
              <span className={workspaceStyles.panelEyebrow}>{t("History")}</span>
              <h2 id="swap-history-heading" className={workspaceStyles.panelTitle}>{t("Recent swap activity")}</h2>
            </div>
            <button type="button" className={styles.dialogCloseButton} aria-label={t("Close")} onClick={() => setIsHistoryExpanded(false)}><CloseIcon size={20} /></button>
          </div>
          <div data-motion-list className={[styles.offerList, swapHistory.length >= 5 ? styles.offerListScrollable : ""].filter(Boolean).join(" ")}>
            {swapHistory.length === 0 ? (
              <p className={styles.emptyText}>{t("Accepted and cancelled swaps will appear here.")}</p>
            ) : swapHistory.map(swap => (
              <SwapOfferCard
                key={swap.id}
                swap={swap}
                stats={getSwapOfferStats(swap)}
                isBusy={isActionBusy}
                isPinned={pinnedSwapIdSet.has(String(swap.id))}
                onTogglePin={handleToggleSwapPin}
                onAccept={handleAccept}
                onCancel={handleCancel}
              />
            ))}
          </div>
        </section>
      ) : null}

      </dialog>
      {isGiveAwayExpanded && employeeId && <SwapCreateWizard
        returnFocusRef={createOfferTriggerRef}
        schedules={schedules} employees={targetEmployees} employeeId={employeeId} employeeName={session?.displayName ?? ""} swaps={swaps}
        busy={createSwapMutation.isPending} error={actionError || (loadError ? getErrorMessage(loadError, t("Could not load swap data.")) : null)}
        onClose={() => { setIsGiveAwayExpanded(false); createOfferTriggerRef.current?.focus({ preventScroll: true }); }}
        onSubmit={payload => { setActionError(null); createSwapMutation.mutate(payload, {
          onSuccess: () => { setIsGiveAwayExpanded(false); createOfferTriggerRef.current?.focus({ preventScroll: true }); },
          onError: error => {
            setActionError(getErrorMessage(error, t("Could not create this swap offer.")));
            void queryClient.invalidateQueries({ queryKey: ["shiftSwaps", "preview"] });
            void queryClient.invalidateQueries({ queryKey: ["shiftSwaps", "returnCandidates"] });
            void queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
          },
        }); }}
      />}
      {createPortal(<ConfirmDialog
        open={pendingAcceptSwap !== null}
        variant="confirm"
        title={t("Accept this shift?")}
        message={pendingAcceptSwap?.requestKind === "exchange" ? t(
          "Accept exchange: you receive {0} on {1}, {2}–{3}, and give {4} on {5}, {6}–{7}, to {8}? Both schedules update together.",
          pendingAcceptSwap.scheduleName, formatSwapDate(pendingAcceptSwap), pendingAcceptSwap.fromTime, pendingAcceptSwap.toTime,
          pendingAcceptSwap.returnScheduleName ?? "", `${String(pendingAcceptSwap.returnDayOfMonth).padStart(2, "0")}.${String(pendingAcceptSwap.returnMonth).padStart(2, "0")}.${pendingAcceptSwap.returnYear}`,
          pendingAcceptSwap.returnFromTime ?? "", pendingAcceptSwap.returnToTime ?? "", pendingAcceptSwap.fromEmployeeName,
        ) : pendingAcceptSwap ? t(
          "Take the shift from {0} in {1}{2} on {3}, {4}–{5} ({6})? Acceptance updates your schedule.",
          pendingAcceptSwap.isManagerCreated ? pendingAcceptSwap.manualColumnName || t("Open shift") : pendingAcceptSwap.fromEmployeeName,
          pendingAcceptSwap.scheduleName,
          pendingAcceptSwap.shopName ? ` · ${pendingAcceptSwap.shopName}` : "",
          formatSwapDate(pendingAcceptSwap), pendingAcceptSwap.fromTime, pendingAcceptSwap.toTime,
          formatHours(pendingAcceptSwap.shiftHours),
        ) : ""}
        confirmText={t("Accept shift")}
        cancelText={t("Back")}
        confirmDisabled={acceptSwapMutation.isPending || isAcceptSubmitting}
        cancelDisabled={acceptSwapMutation.isPending || isAcceptSubmitting}
        onCancel={() => { if (!acceptInFlightRef.current && !acceptSwapMutation.isPending) setPendingAcceptSwap(null); }}
        onConfirm={handleConfirmAccept}
      />, document.body)}
      {createPortal(<ConfirmDialog
        open={pendingUnpinSwapId !== null}
        variant="confirm"
        title={t("Unpin swap?")}
        message={t("Remove {0} from your pinned swaps?", pendingUnpinSwap?.scheduleName ?? "this swap")}
        confirmText={t("Unpin")}
        onCancel={() => setPendingUnpinSwapId(null)}
        onConfirm={handleConfirmUnpin}
      />, isHistoryExpanded && historyDialogElement ? historyDialogElement : document.body)}
    </div>
  );
}
