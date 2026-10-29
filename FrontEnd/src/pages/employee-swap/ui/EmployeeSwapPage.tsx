import { useEmployeeProfileQuery } from "@features/employee-profile/api/queries";
import { useEmploymentContractSettingsQuery } from "@entities/employment-contracts";
import { useEmployeeContractApprovalsQuery, useCreateContractClaimMutation, useCreateGiveawayExceptionMutation, useCancelContractApprovalMutation } from "@entities/shift-swap-approvals";
import { swapMinutes, type SwapPeriod } from "./SwapShiftPickerDialog";
import { createPortal, flushSync } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { dateTimeFormat, numberFormat, translateServerMessage, t } from "@shared/i18n";
import {
  useCallback,
  useDeferredValue,
  useEffect,
  useMemo,
  useRef,
  useState,
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
import { SwapOfferDetailsDialog } from "./SwapOfferDetailsDialog";
import { useSwapModalEnvironment } from "./useSwapModalEnvironment";
import { SwapCreateWizard } from "./SwapCreateWizard";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { CheckIcon, CloseIcon, EmployeeIcon, PinIcon, PlusIcon, ScheduleIcon, SearchIcon, SwapHistoryIcon, SwapOffersIcon, WarnIcon } from "@shared/ui/icons";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeSwapPage.module.css";
import { CopyLinkIcon } from "@shared/ui/icons";
import { createSwapLink, parseSwapLinkId } from "../model/swapLink";
import { useLocation } from "react-router-dom";

const swapOfferDateFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  timeZone: "UTC",
});

type SwapFilter = "all" | "mine" | "available" | "pinned";

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
  return t("{0}h", numberFormat("en-GB", { maximumFractionDigits: 1 }).format(roundedValue));
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
  const actionLabel = `${isPinned ? t("Unpin") : t("Pin")} ${label}`;
  return (
    <button
      type="button"
      className={[
        styles.collapseButton,
        styles.pinButton,
        styles.cardActionButton,
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

function SwapOfferCard({ swap, isPinned, highlighted, onTogglePin, onViewDetails }: {
  swap: ShiftSwap; isPinned: boolean; onTogglePin: (swapId: number) => void;
  highlighted?: boolean;
  onViewDetails: (swapId: number, trigger: HTMLElement) => void;
}) {
  const isOpen = swap.status === "open";
  const [shareStatus, setShareStatus] = useState<string | null>(null);
  useEffect(() => {
    if (!shareStatus) return;
    const timer = window.setTimeout(() => setShareStatus(null), 4000);
    return () => window.clearTimeout(timer);
  }, [shareStatus]);
  const share = async () => {
    setShareStatus(null);
    const url = createSwapLink(swap.id, window.location.origin);
    try {
      await navigator.clipboard.writeText(url);
      setShareStatus("copied");
    } catch {
      setShareStatus("failed");
    }
  };
  const isManagerManual = swap.isManagerCreated === true && typeof swap.manualColumnId === "number" && swap.manualColumnId > 0;
  const availabilityLabel = !isOpen ? swap.status === "accepted" ? t("Accepted") : t("Cancelled")
    : swap.isCreatedByCurrentEmployee ? t("My offer") : swap.canAccept ? t("Can") : t("Can’t");
  const unavailableReason = isOpen && !swap.canAccept ? swap.acceptanceUnavailableReason ||
    (swap.isScheduleLocked ? "Schedule is locked while a manager is editing it." : null) : null;
  return <article className={[styles.offerCard, isPinned ? styles.offerCardPinned : "", highlighted ? styles.deepLinkedOffer : ""].join(" ")} data-swap-id={swap.id} data-pinned={String(isPinned)} data-own={swap.isCreatedByCurrentEmployee}>
    <button type="button" className={styles.offerOpenButton} aria-label={t("View offer details for {0}", swap.scheduleName)}
      onClick={event => onViewDetails(swap.id, event.currentTarget)}>
      <span className={styles.offerAvatar} aria-hidden="true">{getSwapCode(swap)}</span>
      <span className={styles.offerSummary}>
        <span className={`${styles.offerTitle} ${styles.managerManualTitle}`}><strong>{swap.scheduleName}</strong>{isManagerManual && <span className={styles.managerManualIndicator} role="img" aria-label={t("Manager-posted shift")}><span aria-hidden="true">🔥</span></span>}</span>
        <span className={styles.offerMeta}>
          <span className={styles.offerDateLine}><ScheduleIcon size={12} /><span>{`${formatSwapDate(swap)} · ${swap.fromTime} – ${swap.toTime}`}</span></span>
          <small className={styles.offerFromLine}><EmployeeIcon size={12} /><span>{t("From: {0}", swap.isManagerCreated ? swap.manualColumnName || t("Open shift") : swap.fromEmployeeName)}</span></small>
        </span>
        <span className={styles.offerBadges}>
          {swap.requestKind === "exchange" && <span className={`${styles.badge} ${styles.badgeExchange}`}><SwapOffersIcon size={14} /><span>{t("Swap shifts")}</span></span>}
          <span className={`${styles.badge} ${swap.visibility === "private" ? styles.badgePrivate : styles.badgePublic}`}>
            {swap.visibility === "private" ? <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="3" /><path d="M8 10V7a4 4 0 0 1 8 0v3M12 14v3" /></svg>
              : <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="12" cy="12" r="9" /><ellipse cx="12" cy="12" rx="4" ry="9" /><path d="M3 12h18" /></svg>}
            <span>{t(swap.visibility === "private" ? "Private" : "Public")}</span>
          </span>
          <span className={`${styles.badge} ${!isOpen ? styles.badgeMuted : swap.isCreatedByCurrentEmployee ? styles.badgeMine : swap.canAccept ? styles.badgeCan : styles.badgeCant}`}>
            {isOpen && swap.isCreatedByCurrentEmployee ? <EmployeeIcon size={12} /> : (isOpen ? swap.canAccept : swap.status === "accepted") ? <CheckIcon size={13} /> : <WarnIcon size={13} />}
            <span>{availabilityLabel}</span>
          </span>
          {swap.allowPartialAcceptance === true && swap.requestKind !== "exchange" && <span className={`${styles.badge} ${styles.badgePartial}`}>
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
            <span>{t("Partial")}</span>
          </span>}
          {isOpen && swap.isScheduleLocked && <span className={`${styles.badge} ${styles.badgeLocked}`}><WarnIcon size={13} /><span>{t("Locked")}</span></span>}
        </span>
        {unavailableReason && <span className={styles.offerUnavailableReason}><WarnIcon size={13} /><span className={styles.unavailableText}>{translateServerMessage(unavailableReason)}</span></span>}
      </span>
    </button>
    <PinToggleButton isPinned={isPinned} label={t("{0} swap", swap.scheduleName)} onToggle={() => onTogglePin(swap.id)} />
    <button type="button" className={`${styles.collapseButton} ${styles.cardActionButton} ${styles.copyLinkButton}`} aria-label={t("Copy swap link for {0}", swap.scheduleName)} onClick={() => void share()}><CopyLinkIcon size={15} /></button>
    {shareStatus && <span className={styles.shareStatus} role="status">{shareStatus === "copied" ? t("Link copied") : t("Could not copy this link. Please try again.")}</span>}
  </article>;
}

export function EmployeeSwapPage() {
  const { search } = useLocation();
  const linkedId = parseSwapLinkId(search);
  const offerListRef = useRef<HTMLDivElement>(null);
  const historyListRef = useRef<HTMLDivElement>(null);
  const [highlightedId, setHighlightedId] = useState<number | null>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  const detailsLauncherRef = useRef<HTMLElement | null>(null);
  const [selectedDetailsSwapId, setSelectedDetailsSwapId] = useState<number | null>(null);
  const [detailsError, setDetailsError] = useState<string | null>(null);
  const createOfferTriggerRef = useRef<HTMLButtonElement>(null);
  const historyTriggerRef = useRef<HTMLButtonElement>(null);
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
  const profileQuery = useEmployeeProfileQuery();
  const contractSettingsQuery = useEmploymentContractSettingsQuery();
  const approvalsQuery = useEmployeeContractApprovalsQuery();
  const claimApprovalMutation = useCreateContractClaimMutation();
  const exceptionMutation = useCreateGiveawayExceptionMutation();
  const cancelApprovalMutation = useCancelContractApprovalMutation();
  const restrictToEightHours = profileQuery.data?.contractType === "o_prace" && contractSettingsQuery.data?.restrictSwapsToEightHours === true;
  const createSwapMutation = useCreateEmployeeShiftSwapMutation();
  const acceptSwapMutation = useAcceptEmployeeShiftSwapMutation();
  const cancelSwapMutation = useCancelEmployeeShiftSwapMutation();
  const setSwapPinMutation = useSetEmployeeSwapPinMutation();
  const schedules = schedulesQuery.data ?? [];
  const swaps = swapsQuery.data ?? [];
  const selectedDetailsSwap = swaps.find(swap => swap.id === selectedDetailsSwapId) ?? null;
  if (selectedDetailsSwapId !== null && !selectedDetailsSwap) setSelectedDetailsSwapId(null);
  const targetEmployees = targetEmployeesQuery.data ?? [];
  const [actionError, setActionError] = useState<string | null>(null);
  const [isGiveAwayExpanded, setIsGiveAwayExpanded] = useState(false);
  const [isHistoryExpanded, setIsHistoryExpanded] = useState(false);
  const [historySuspended, setHistorySuspended] = useState(false);
  useSwapModalEnvironment(historyDialogRef, historyTriggerRef, isHistoryExpanded && !historySuspended);
  useEffect(() => {
    if (isHistoryExpanded && !historySuspended && selectedDetailsSwapId === null && historyDialogRef.current?.contains(detailsLauncherRef.current)) {
      detailsLauncherRef.current?.focus({ preventScroll: true });
    }
  }, [isHistoryExpanded, historySuspended, selectedDetailsSwapId]);
  const [swapSearchQuery, setSwapSearchQuery] = useState("");
  const [swapFilter, setSwapFilter] = useState<SwapFilter>("all");
  const [pendingUnpinSwapId, setPendingUnpinSwapId] = useState<number | null>(null);
  const [pendingAcceptance, setPendingAcceptSwap] = useState<{ swap: ShiftSwap; claimedPeriod?: SwapPeriod } | null>(null);
  const pendingAcceptSwap = pendingAcceptance?.swap ?? null;
  const [isAcceptSubmitting, setIsAcceptSubmitting] = useState(false);
  const acceptInFlightRef = useRef(false);
  const cancelInFlightRef = useRef(false);
  useEffect(() => {
    if (selectedDetailsSwapId === null && detailsLauncherRef.current &&
      (!detailsLauncherRef.current.isConnected || detailsLauncherRef.current === createOfferTriggerRef.current))
      createOfferTriggerRef.current?.focus({ preventScroll: true });
  }, [selectedDetailsSwapId]);
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
  const linkedSwap = linkedId === null ? undefined : swaps.find(swap => swap.id === linkedId);
  const linkedStatus = linkedSwap?.status;
  const swapsReady = !swapsQuery.isLoading && !swapsQuery.error && swapsQuery.data !== undefined;
  useEffect(() => {
    setHighlightedId(null);
    if (!linkedId || !employeeId || !swapsReady || !linkedStatus) return;
    setSelectedDetailsSwapId(null);
    setHistorySuspended(false);
    setSwapSearchQuery("");
    setSwapFilter("all");
    setIsHistoryExpanded(linkedStatus !== "open");
    let frame = 0;
    let timer = 0;
    const locate = () => {
      const list = linkedStatus === "open" ? offerListRef.current : historyListRef.current;
      const card = list?.querySelector<HTMLElement>(`[data-swap-id="${linkedId}"]`);
      if (!card) { frame = requestAnimationFrame(locate); return; }
      let scroller = card.parentElement;
      while (scroller && scroller !== document.body) {
        const overflow = getComputedStyle(scroller).overflowY;
        if (/auto|scroll/.test(overflow) && scroller.scrollHeight > scroller.clientHeight) {
          const top = card.getBoundingClientRect().top - scroller.getBoundingClientRect().top + scroller.scrollTop - 8;
          scroller.scrollTop = Math.max(0, Math.min(top, scroller.scrollHeight - scroller.clientHeight));
          break;
        }
        scroller = scroller.parentElement;
      }
      card.querySelector<HTMLButtonElement>("button")?.focus({ preventScroll: true });
      setHighlightedId(linkedId);
      timer = window.setTimeout(() => setHighlightedId(null), 3500);
    };
    frame = requestAnimationFrame(locate);
    return () => { cancelAnimationFrame(frame); window.clearTimeout(timer); };
  }, [linkedId, linkedStatus, swapsReady, employeeId]);
  const linkError = new URLSearchParams(search).has("swapId") && linkedId === null
    ? t("This swap link is invalid.")
    : linkedId && swapsReady && !linkedSwap ? t("This swap is unavailable or you do not have access.") : null;
  const pendingUnpinSwap = pendingUnpinSwapId === null
    ? null
    : swaps.find(swap => swap.id === pendingUnpinSwapId) ?? null;
  const isActionBusy = claimApprovalMutation.isPending || exceptionMutation.isPending || cancelApprovalMutation.isPending || createSwapMutation.isPending || acceptSwapMutation.isPending || cancelSwapMutation.isPending || isAcceptSubmitting;
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

  const handleViewDetails = (swapId: number, trigger: HTMLElement) => {
    if (isHistoryExpanded) flushSync(() => setHistorySuspended(true));
    detailsLauncherRef.current = trigger;
    setDetailsError(null);
    setSelectedDetailsSwapId(swapId);
  };
  const closeDetails = () => {
    flushSync(() => setSelectedDetailsSwapId(null));
    setHistorySuspended(false);
    if (!detailsLauncherRef.current?.isConnected) detailsLauncherRef.current = createOfferTriggerRef.current;
  };

  const handleAccept = (swap: ShiftSwap, claimedPeriod?: SwapPeriod) => {
    if (swap.status !== "open" || !swap.canAccept || swap.approvalPendingForCurrentEmployee || isActionBusy || acceptInFlightRef.current) return;
    setActionError(null);
    flushSync(() => { closeDetails(); setIsHistoryExpanded(false); });
    setPendingAcceptSwap({ swap, claimedPeriod });
  };

  const handleConfirmAccept = () => {
    if (!pendingAcceptSwap || isActionBusy || acceptInFlightRef.current) return;
    const currentSwap = swaps.find(swap => swap.id === pendingAcceptSwap.id);
    if (!currentSwap || currentSwap.status !== "open" || !currentSwap.canAccept) {
      setPendingAcceptSwap(null);
      setActionError(t("This offer is no longer available. The swap list has been refreshed."));
      void queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
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
    const acceptAction = currentSwap.requiresManagerApproval ? claimApprovalMutation : acceptSwapMutation;
    acceptAction.mutate({ id: currentSwap.id, ...pendingAcceptance?.claimedPeriod }, {
      onSuccess: finish,
      onError: error => {
        finish();
        setActionError(getErrorMessage(error, t("Could not accept this swap offer.")));
        void queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
        void queryClient.invalidateQueries({ queryKey: queryKeys.employeeSchedules.all });
        void queryClient.invalidateQueries({ queryKey: queryKeys.containers.all });
      },
    });
  };

  const handleCancel = (swap: ShiftSwap) => {
    const currentSwap = swaps.find(item => item.id === swap.id);
    if (!currentSwap || currentSwap.status !== "open" || !currentSwap.canCancel || isActionBusy || cancelInFlightRef.current) return;
    cancelInFlightRef.current = true;
    setActionError(null);
    setDetailsError(null);
    cancelSwapMutation.mutate(currentSwap.id, {
      onSuccess: () => { cancelInFlightRef.current = false; closeDetails(); },
      onError: error => {
        cancelInFlightRef.current = false;
        const message = getErrorMessage(error, t("Could not cancel this swap offer."));
        setActionError(message);
        setDetailsError(message);
      },
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
          {linkError ? <ErrorBanner dismissible={false}>{linkError}</ErrorBanner> : null}

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

            <div ref={offerListRef} data-swap-offers-scroll role="region" aria-label={t("Swap offers")} tabIndex={0} data-motion-list className={[
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
                  highlighted={highlightedId === swap.id}
                  isPinned={pinnedSwapIdSet.has(String(swap.id))}
                  onTogglePin={handleToggleSwapPin}
                  onViewDetails={handleViewDetails}
                />
              ))}
            </div>
          </section>
        </main>
        </div>

        <button
          type="button"
          ref={historyTriggerRef}
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

      {createPortal(<dialog ref={setHistoryDialogRef} className={styles.creationDialog} aria-labelledby="swap-history-heading" onCancel={event => { if (pendingUnpinSwapId !== null) { event.preventDefault(); setPendingUnpinSwapId(null); } else setIsHistoryExpanded(false); }} onClick={event => { if (event.target === event.currentTarget) setIsHistoryExpanded(false); }}>
      {isHistoryExpanded ? (
        <section id="swap-history-panel" className={styles.historyPanel} aria-labelledby="swap-history-heading">
          <div className={styles.panelHeaderRow}>
            <div>
              <span className={workspaceStyles.panelEyebrow}>{t("History")}</span>
              <h2 id="swap-history-heading" className={workspaceStyles.panelTitle}>{t("Recent swap activity")}</h2>
            </div>
            <button type="button" className={styles.dialogCloseButton} aria-label={t("Close")} onClick={() => setIsHistoryExpanded(false)}><CloseIcon size={20} /></button>
          </div>
          <section aria-label={t("My approval requests")}>
            <h3>{t("My approval requests")}</h3>
            {approvalsQuery.error ? <ErrorBanner dismissible={false}>{getErrorMessage(approvalsQuery.error, t("Could not load approval requests."))}</ErrorBanner> : null}
            {approvalsQuery.data?.map(item => <article key={item.id}>
              <strong>{t(item.kind === "claim" ? "Shift pickup approval" : "Monthly minimum exception")}</strong>
              <p>{item.scheduleName} · {item.dayOfMonth}.{item.month}.{item.year} · {item.fromTime}–{item.toTime}</p>
              <span>{({ pending: t("Pending"), approved: t("Approved"), rejected: t("Rejected"), cancelled: t("Cancelled"), expired: t("Expired") } as const)[item.status]}</span>
              {item.failureReason ? <p>{translateServerMessage(item.failureReason)}</p> : null}
              {item.status === "pending" ? <><p>{t("Pending requests do not reserve offers. Schedules change only after approval.")}</p>
                <button type="button" disabled={cancelApprovalMutation.isPending} onClick={() => cancelApprovalMutation.mutate(item.id, {
                  onError: error => setActionError(getErrorMessage(error, t("Could not cancel this request."))),
                })}>{t("Cancel request")}</button></> : null}
            </article>)}
          </section>
          <div ref={historyListRef} data-swap-history-scroll role="region" aria-label={t("Swap history")} tabIndex={0} data-motion-list className={[styles.offerList, swapHistory.length >= 5 ? styles.offerListScrollable : ""].filter(Boolean).join(" ")}>
            {swapHistory.length === 0 ? (
              <p className={styles.emptyText}>{t("Accepted and cancelled swaps will appear here.")}</p>
            ) : swapHistory.map(swap => (
              <SwapOfferCard
                key={swap.id}
                swap={swap}
                highlighted={highlightedId === swap.id}
                isPinned={pinnedSwapIdSet.has(String(swap.id))}
                onTogglePin={handleToggleSwapPin}
                onViewDetails={handleViewDetails}
              />
            ))}
          </div>
        </section>
      ) : null}

      </dialog>, document.body)}
      {selectedDetailsSwap && <SwapOfferDetailsDialog key={selectedDetailsSwap.id} swap={selectedDetailsSwap} currentEmployeeId={employeeId}
        busy={isActionBusy} error={detailsError} onClose={closeDetails} onAccept={handleAccept} onCancel={handleCancel}
        returnFocusRef={detailsLauncherRef} />}
      {isGiveAwayExpanded && employeeId && <SwapCreateWizard
        returnFocusRef={createOfferTriggerRef}
        schedules={schedules} employees={targetEmployees} employeeId={employeeId} employeeName={session?.displayName ?? ""} swaps={swaps}
        restrictToEightHours={restrictToEightHours}
        busy={createSwapMutation.isPending || exceptionMutation.isPending} error={actionError || (loadError ? getErrorMessage(loadError, t("Could not load swap data.")) : null)}
        onClose={() => { setIsGiveAwayExpanded(false); createOfferTriggerRef.current?.focus({ preventScroll: true }); }}
        onRequestException={payload => { setActionError(null); exceptionMutation.mutate(payload, {
          onSuccess: () => { setIsGiveAwayExpanded(false); createOfferTriggerRef.current?.focus({ preventScroll: true }); },
          onError: error => { setActionError(getErrorMessage(error, t("Could not request a manager exception.")));
            void queryClient.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all }); },
        }); }}
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
        title={t(pendingAcceptSwap?.requiresManagerApproval ? "Request manager approval?" : "Accept this shift?")}
        message={pendingAcceptSwap?.requiresManagerApproval ? t("Pending requests do not reserve offers. Schedules change only after approval.") : pendingAcceptSwap?.requestKind === "exchange" ? t(
          "Accept exchange: you receive {0} on {1}, {2}–{3}, and give {4} on {5}, {6}–{7}, to {8}? Both schedules update together.",
          pendingAcceptSwap.scheduleName, formatSwapDate(pendingAcceptSwap), pendingAcceptSwap.fromTime, pendingAcceptSwap.toTime,
          pendingAcceptSwap.returnScheduleName ?? "", `${String(pendingAcceptSwap.returnDayOfMonth).padStart(2, "0")}.${String(pendingAcceptSwap.returnMonth).padStart(2, "0")}.${pendingAcceptSwap.returnYear}`,
          pendingAcceptSwap.returnFromTime ?? "", pendingAcceptSwap.returnToTime ?? "", pendingAcceptSwap.fromEmployeeName,
        ) : pendingAcceptSwap ? t(
          "Take the shift from {0} in {1}{2} on {3}, {4}–{5} ({6})? Acceptance updates your schedule.",
          pendingAcceptSwap.isManagerCreated ? pendingAcceptSwap.manualColumnName || t("Open shift") : pendingAcceptSwap.fromEmployeeName,
          pendingAcceptSwap.scheduleName,
          pendingAcceptSwap.shopName ? ` · ${pendingAcceptSwap.shopName}` : "",
          formatSwapDate(pendingAcceptSwap), pendingAcceptance?.claimedPeriod?.fromTime ?? pendingAcceptSwap.fromTime, pendingAcceptance?.claimedPeriod?.toTime ?? pendingAcceptSwap.toTime,
          formatHours(pendingAcceptance?.claimedPeriod ? (swapMinutes(pendingAcceptance.claimedPeriod.toTime) - swapMinutes(pendingAcceptance.claimedPeriod.fromTime)) / 60 : pendingAcceptSwap.shiftHours),
        ) : ""}
        confirmText={t(pendingAcceptSwap?.requiresManagerApproval ? "Request manager approval" : "Accept shift")}
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
        message={t("Remove {0} from your pinned swaps?", pendingUnpinSwap?.scheduleName ?? t("this swap"))}
        confirmText={t("Unpin")}
        onCancel={() => setPendingUnpinSwapId(null)}
        onConfirm={handleConfirmUnpin}
      />, isHistoryExpanded && historyDialogElement ? historyDialogElement : document.body)}
    </div>
  );
}
