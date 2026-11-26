import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { containersApi } from "@entities/containers/api/containersApi";
import { formatHoursMinutes } from "@entities/containers/model/statistics";
import {
  useApproveShiftCorrectionMutation,
  useGraphShiftCorrectionsQuery,
  useRejectShiftCorrectionMutation,
  useSaveShiftCorrectionSettingMutation,
  useShiftCorrectionSettingQuery,
  type ShiftCorrectionRequest,
} from "@entities/shift-corrections";
import {
  formatShiftCorrectionDelta as formatDelta,
  shiftCorrectionOriginalMinutes,
  shiftCorrectionTimeMinutes as toMinutes,
} from "@entities/shift-corrections/model/presentation";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ArrowRightIcon, CheckIcon, ChevronRightIcon, ClockIcon, CloseIcon, GroupIcon, LockIcon, ScheduleDetailsIcon, ScheduleIcon, StatisticsIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import activity from "./ContainerGraphActivity.module.css";
import { ContainerGraphHighlightColorDialog } from "./ContainerGraphHighlightColorDialog";
import styles from "./ContainerGraphShiftCorrectionsCard.module.css";

type ContainerGraphShiftCorrectionsCardProps = {
  containerId: number | null;
  graphId: number | null;
  hasUnsavedChanges: boolean;
  disabled: boolean;
  onApproved: () => void;
  children?: ReactNode;
  onPendingCountChange?: (count: number) => void;
  reviewRequestId?: number;
  onReviewBusyChange?: (busy: boolean) => void;
  onReviewTypeChange?: (type: string) => void;
  reviewScheduleLink?: ReactNode;
};

const createdAtFormatter = dateTimeFormat("en-GB", {
  day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit",
});

function formatOriginalShifts(request: ShiftCorrectionRequest) {
  if (request.requestType === "removeShift")
    return request.originalShifts?.map(shift => `${shift.fromTime} – ${shift.toTime}`).join(", ") || "–";
  return request.isDayOffRequest ? t("Day off") : `${request.originalFromTime} – ${request.originalToTime}`;
}

function statusLabel(status: ShiftCorrectionRequest["status"]) {
  return status === "pending" ? t("Pending") : status === "approved" ? t("Approved") : t("Rejected");
}

function initials(name: string) {
  return (name.match(/\p{L}+/gu) ?? []).slice(0, 2).map(part => Array.from(part)[0]).join("").toLocaleUpperCase() || "?";
}

function statusIcon(status: ShiftCorrectionRequest["status"]) {
  return status === "pending" ? <ClockIcon size={14} /> : (
    <span className={styles.statusMark} aria-hidden="true">
      {status === "approved" ? <CheckIcon size={12} /> : <CloseIcon size={12} />}
    </span>
  );
}

export function ContainerGraphShiftCorrectionsCard({
  containerId,
  graphId,
  hasUnsavedChanges,
  disabled,
  onApproved,
  onPendingCountChange,
  reviewRequestId,
  onReviewBusyChange,
  onReviewTypeChange,
  reviewScheduleLink,
  children,
}: ContainerGraphShiftCorrectionsCardProps) {
  const hasPersistedGraph = Number.isInteger(containerId) && containerId !== null && containerId > 0
    && Number.isInteger(graphId) && graphId !== null && graphId > 0;
  const focused = reviewRequestId !== undefined;
  const requestsQuery = useGraphShiftCorrectionsQuery(containerId, graphId, reviewRequestId);
  const settingQuery = useShiftCorrectionSettingQuery(hasPersistedGraph && !focused);
  const client = useQueryClient();
  const saveSettingMutation = useSaveShiftCorrectionSettingMutation();
  const approveMutation = useApproveShiftCorrectionMutation();
  const rejectMutation = useRejectShiftCorrectionMutation();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [highlightColor, setHighlightColor] = useState("#FDE68A");
  const [isColorDialogOpen, setIsColorDialogOpen] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isVersionSaving, setIsVersionSaving] = useState(false);
  const reviewInFlight = useRef(false);

  const requests = useMemo(() => [...(requestsQuery.data ?? [])].sort((left, right) => {
    if (left.status === "pending" && right.status !== "pending") return -1;
    if (left.status !== "pending" && right.status === "pending") return 1;
    return Date.parse(right.createdAtUtc) - Date.parse(left.createdAtUtc);
  }), [requestsQuery.data]);
  const notFound = requestsQuery.error instanceof ApiError && requestsQuery.error.status === 404;
  const selected = focused && notFound ? null : requests.find(request => request.id === (reviewRequestId ?? selectedId)) ?? null;
  const reviewType = selected?.requestType === "removeShift" ? t("Day off requested")
    : selected?.requestType === "addShift" || selected?.isDayOffRequest ? t("Add shift")
    : t("Schedule correction");
  const pendingCount = requests.filter(request => request.status === "pending").length;
  const exactKey = queryKeys.shiftSwaps.graphCorrectionById(containerId ?? 0, graphId ?? 0, reviewRequestId ?? 0);
  const isReading = focused && (requestsQuery.isLoading || requestsQuery.isFetching);
  const isBusy = !hasPersistedGraph || disabled || approveMutation.isPending || rejectMutation.isPending || isVersionSaving ||
    (focused && (isReading || requestsQuery.isError));

  useEffect(() => {
    if (settingQuery.data?.highlightColor) setHighlightColor(settingQuery.data.highlightColor);
  }, [settingQuery.data?.highlightColor]);

  useEffect(() => {
    if (selectedId !== null && !requests.some(request => request.id === selectedId)) setSelectedId(null);
  }, [requests, selectedId]);

  useEffect(() => {
    onPendingCountChange?.(pendingCount);
  }, [onPendingCountChange, pendingCount]);

  useEffect(() => {
    if (focused) onReviewTypeChange?.(reviewType);
  }, [focused, onReviewTypeChange, reviewType]);

  const handleColorChange = (value: string) => {
    if (!hasPersistedGraph) return;
    setHighlightColor(value);
    setIsColorDialogOpen(false);
    setActionError(null);
    saveSettingMutation.mutate(value, {
      onError: error => setActionError(getErrorMessage(error, t("Could not save the correction highlight color."))),
    });
  };

  function finishReview() {
    reviewInFlight.current = false;
    onReviewBusyChange?.(false);
  }

  function handleReviewError(error: unknown, message: string) {
    setActionError(getErrorMessage(error, message));
    if (focused) {
      client.invalidateQueries({ queryKey: exactKey });
      client.invalidateQueries({ queryKey: queryKeys.home.activity() });
      client.invalidateQueries({ queryKey: queryKeys.home.navBadges() });
    }
    finishReview();
  }

  const handleApprove = () => {
    if (selected?.status !== "pending" || isBusy || hasUnsavedChanges || reviewInFlight.current || containerId === null || graphId === null) return;
    setActionError(null);
    reviewInFlight.current = true;
    onReviewBusyChange?.(true);
    approveMutation.mutate({ containerId, graphId, id: selected.id, ...(focused ? {} : { highlightColor }) }, {
      onSuccess: () => {
        setSelectedId(current => current === selected.id ? null : current);
        setIsVersionSaving(true);
        void containersApi.commitGraphVersion(containerId, graphId)
          .then(onApproved)
          .catch(() => setActionError(t("The correction was applied, but its schedule version could not be recorded.")))
          .finally(() => { setIsVersionSaving(false); finishReview(); });
      },
      onError: error => handleReviewError(error, t("Could not approve this correction.")),
    });
  };

  const handleReject = () => {
    if (selected?.status !== "pending" || isBusy || reviewInFlight.current || containerId === null || graphId === null) return;
    setActionError(null);
    reviewInFlight.current = true;
    onReviewBusyChange?.(true);
    rejectMutation.mutate({ containerId, graphId, id: selected.id }, {
      onSuccess: () => { setSelectedId(current => current === selected.id ? null : current); finishReview(); },
      onError: error => handleReviewError(error, t("Could not reject this correction.")),
    });
  };

  const removesShift = selected?.requestType === "removeShift";
  const requestedFrom = selected?.requestedFromTime;
  const requestedTo = selected?.requestedToTime;
  const originalShift = selected && !selected.isDayOffRequest && (!focused || selected.requestType !== "addShift")
    && selected.originalFromTime && selected.originalToTime
    ? { fromTime: selected.originalFromTime, toTime: selected.originalToTime } : null;
  const requestedShift = !removesShift && requestedFrom && requestedTo ? { fromTime: requestedFrom, toTime: requestedTo } : null;
  const originalDuration = removesShift ? shiftCorrectionOriginalMinutes(selected?.originalShifts ?? [])
    : !originalShift ? 0 : focused ? shiftCorrectionOriginalMinutes([originalShift])
      : toMinutes(originalShift.toTime) - toMinutes(originalShift.fromTime);
  const requestedDuration = !requestedShift ? 0 : focused ? shiftCorrectionOriginalMinutes([requestedShift])
    : toMinutes(requestedShift.toTime) - toMinutes(requestedShift.fromTime);
  const durationDelta = requestedDuration - originalDuration;
  const selectedStatus = selected && <em className={`${styles.status} ${styles[`status${selected.status}`]}`}
    data-review-pending={focused && selected.status === "pending" ? "" : undefined}>
    {statusIcon(selected.status)}{focused && selected.status === "pending" ? t("Pending review") : statusLabel(selected.status)}
  </em>;
  const selectedDetails = selected ? (
    <article key={selected.id} className={styles.details} role="listitem" data-review-part={focused ? "detail" : undefined}>
      <div className={styles.detailsHeader} data-review-part={focused ? "person" : undefined}>
        <span className={`${styles.avatar} ${styles.selectedAvatar}`} aria-hidden="true">{focused ? initials(selected.employeeName) : Array.from(initials(selected.employeeName))[0]}</span>
        <div className={styles.selectedCopy}>{!focused && <span>{t("Selected request")}</span>}<strong>{selected.employeeName}</strong>
          <time dateTime={selected.createdAtUtc}>{createdAtFormatter.format(new Date(selected.createdAtUtc))}</time>
        </div>
        {focused ? selectedStatus : <div className={styles.detailsHeaderActions}>
          <button type="button" aria-label={t("Collapse request from {0}", selected.employeeName)}
            onClick={() => setSelectedId(null)}><CloseIcon size={14} /></button>
        </div>}
      </div>
      {!focused && selectedStatus}
      {!focused && removesShift && <strong>{t("Day off requested")}</strong>}
      <dl className={styles.detailGrid} data-review-part={focused ? "metadata" : undefined}>
        <div><dt><span className={styles.metadataIcon}><ScheduleIcon size={16} /></span>{t("Date")}</dt><dd>{String(selected.dayOfMonth).padStart(2, "0")}.{String(selected.month).padStart(2, "0")}.{selected.year}</dd></div>
        <div><dt><span className={styles.metadataIcon}><GroupIcon size={16} /></span>{t("Schedule")}</dt><dd>{selected.scheduleName} · {selected.shopName}</dd></div>
      </dl>
      <div className={styles.comparison} data-review-part={focused ? "comparison" : undefined}>
        <div><span>{t("Current shift")}</span><strong>{focused && !removesShift && !originalShift && !selected.isDayOffRequest ? "–" : formatOriginalShifts(selected)}</strong>
          {focused && originalDuration > 0 && <span>{formatHoursMinutes(originalDuration)}</span>}
        </div>
        <span className={styles.comparisonArrow} aria-hidden="true"><ArrowRightIcon size={17} /></span>
        <div><span>{t("Requested shift")}</span><strong>{requestedShift ? `${requestedShift.fromTime} – ${requestedShift.toTime}` : "–"}</strong>
          {focused && requestedDuration > 0 && <span>{formatHoursMinutes(requestedDuration)}</span>}
          {focused && durationDelta !== 0 && <small className={styles.reviewDelta} aria-label={t("Duration change")}>{formatDelta(durationDelta)}</small>}
        </div>
      </div>
      {focused ? originalShift && requestedShift && <p className={styles.reviewChangeSummary}>
        {t("Start")} {formatDelta(toMinutes(requestedShift.fromTime) - toMinutes(originalShift.fromTime))} · {t("End")} {formatDelta(toMinutes(requestedShift.toTime) - toMinutes(originalShift.toTime))}
      </p> : <div className={styles.deltaRow}>
        {!removesShift && !selected.isDayOffRequest && selected.originalFromTime && selected.originalToTime && requestedFrom && requestedTo ? <>
          <span className={requestedFrom === selected.originalFromTime ? styles.deltaNeutral : styles.deltaStart}><ClockIcon size={15} />{t("Start")} {formatDelta(toMinutes(requestedFrom) - toMinutes(selected.originalFromTime))}</span>
          <span className={styles.deltaNeutral}><LockIcon size={15} />{t("End")} {formatDelta(toMinutes(requestedTo) - toMinutes(selected.originalToTime))}</span>
        </> : null}
        <strong className={requestedDuration === originalDuration ? styles.deltaNeutral : styles.deltaDuration}><StatisticsIcon size={15} />{t(removesShift ? "Duration change" : "Duration")} {formatDelta(requestedDuration - originalDuration)}</strong>
      </div>}
      {focused || selected.comment?.trim() ? (
        <div className={styles.comment} data-review-part={focused ? "comment" : undefined}>
          <strong>{t("Comment")}</strong>
          <p data-empty={!selected.comment?.trim() ? "" : undefined}>{selected.comment?.trim() ? selected.comment : t("No comment provided")}</p>
        </div>
      ) : null}
      {selected.status === "pending" && hasUnsavedChanges && <p className={styles.saveHint}>{t("Save your current schedule edits before approving.")}</p>}
      <div data-review-part={focused ? "footer" : undefined}>
        {reviewScheduleLink}
        {selected.status === "pending" ? (
          <div className={styles.actions} data-review-part={focused ? "decisions" : undefined}>
            <IosButton className={`${activity.action} ${activity.danger}`} label={rejectMutation.isPending ? t("Rejecting...") : t("Reject")} variant="secondary" size="compact"
              icon={<CloseIcon size={15} />} disabled={isBusy} onClick={handleReject} />
            <IosButton className={`${activity.action} ${activity.success}`} label={approveMutation.isPending ? t("Applying...") : t("Approve")} size="compact"
              icon={<CheckIcon size={15} />} disabled={isBusy || hasUnsavedChanges} onClick={handleApprove} />
          </div>
        ) : (
        <p className={`${styles.reviewed} ${selected.status === "approved" ? activity.success : activity.danger}`}><span className={styles.reviewMark} aria-hidden="true">{selected.status === "approved" ? <CheckIcon size={12} /> : <CloseIcon size={12} />}</span><span>{selected.reviewedByManagerName
          ? t("Reviewed by {0}", selected.reviewedByManagerName) : t("Reviewed")}</span></p>
        )}
      </div>
    </article>
  ) : null;

  if (focused) return <section className={styles.requestsPanel} data-review-part="panel" aria-label={t("Shift Corrections")}>
    {actionError && <ErrorBanner dismissible={false}>{actionError}</ErrorBanner>}
    {isReading && <p className={activity.state} role="status">{t("Loading requests...")}</p>}
    {notFound || (!isReading && !requestsQuery.isError && !selected)
      ? <p className={activity.state} role="status">{t("This request is no longer available.")}</p>
      : requestsQuery.isError && <ErrorBanner dismissible={false}>
        {getErrorMessage(requestsQuery.error, t("Could not load shift corrections."))}
        <IosButton label={t("Retry")} variant="secondary" size="compact" onClick={() => client.invalidateQueries({ queryKey: exactKey })} />
      </ErrorBanner>}
    {selectedDetails ? <div className={styles.list} data-review-part="list" role="list" aria-label={t("Shift correction requests")}>{selectedDetails}</div>
      : reviewScheduleLink && <div data-review-part="footer">{reviewScheduleLink}</div>}
  </section>;

  return (
    <CardSection className={activity.card} headerClassName={activity.header}
      title={<span className={activity.title} role="heading" aria-level={3}>{t("Shift Corrections")} <span className={activity.badge}>{requests.length}</span></span>}
      icon={<span className={activity.sectionIcon} aria-hidden="true"><ScheduleDetailsIcon size={18} /></span>}
      headerRightSlot={
        <button type="button" className={activity.iconButton} title={t("Approved correction highlight color")}
          aria-label={t("Approved correction highlight color")} disabled={!hasPersistedGraph || saveSettingMutation.isPending}
          onClick={() => setIsColorDialogOpen(true)}>
          <span className={activity.swatch} style={{ backgroundColor: highlightColor }} aria-hidden="true" />
        </button>
      }>

      <ContainerGraphHighlightColorDialog open={isColorDialogOpen} value={highlightColor}
        eyebrow={t("Shift corrections")} title={t("Choose approval color")} inputLabel={t("Approval highlight hex color")}
        isSaving={saveSettingMutation.isPending} onCancel={() => setIsColorDialogOpen(false)} onSave={handleColorChange} />

      <div className={activity.stack}>
        <section className={styles.requestsPanel} aria-label={t("Employee requests")}>
          {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

          <header className={activity.panelHeader}>
            <span>{t("Employee requests")}</span>
            <span className={`${activity.badge} ${activity.pending}`}><ClockIcon size={13} />{pendingCount} {t("pending")}</span>
          </header>

          {requestsQuery.isLoading ? <p className={activity.state}>{t("Loading requests...")}</p> : null}
          {requestsQuery.isError ? <p className={`${activity.state} ${activity.danger}`}>{t("Could not load shift corrections.")}</p> : null}
          {!requestsQuery.isLoading && !requestsQuery.isError && requests.length === 0 ? (
            <p className={activity.state}>{t("No correction requests yet.")}</p>
          ) : null}

          {requests.length > 0 ? (
            <div className={styles.list} role="list" tabIndex={0} aria-label={t("Shift correction requests")}>
              {requests.map(request => selected?.id === request.id ? selectedDetails : (
                <button key={request.id} type="button" role="listitem"
                  className={styles.requestButton}
                  aria-expanded={selected?.id === request.id}
                  onClick={() => { setSelectedId(current => current === request.id ? null : request.id); setActionError(null); }}>
                  <span className={styles.avatar} aria-hidden="true">{initials(request.employeeName)}</span>
                  <span className={styles.requestCopy}>
                    <strong>{request.employeeName}</strong>
                    <small>{request.requestType === "removeShift"
                      ? `${t("Day off requested")} · ${t("Day")} ${request.dayOfMonth} · ${formatOriginalShifts(request)} → –`
                      : request.isDayOffRequest
                      ? `${String(request.dayOfMonth).padStart(2, "0")}.${String(request.month).padStart(2, "0")}.${request.year} · ${t("Day off")} → ${request.requestedFromTime}–${request.requestedToTime} · ${request.scheduleName} · ${request.shopName}`
                      : `${t("Day")} ${request.dayOfMonth} · ${request.originalFromTime}–${request.originalToTime}`}</small>
                  </span>
                  <em className={`${styles.status} ${styles[`status${request.status}`]}`}>{statusIcon(request.status)}{statusLabel(request.status)}</em>
                  <span className={styles.chevron} aria-hidden="true"><ChevronRightIcon size={15} /></span>
                </button>
              ))}
            </div>
          ) : null}
        </section>
        {children}
      </div>
    </CardSection>
  );
}
