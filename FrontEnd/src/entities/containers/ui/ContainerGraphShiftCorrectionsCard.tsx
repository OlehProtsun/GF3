import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useMemo, useState } from "react";
import { containersApi } from "@entities/containers/api/containersApi";
import {
  useApproveShiftCorrectionMutation,
  useGraphShiftCorrectionsQuery,
  useRejectShiftCorrectionMutation,
  useSaveShiftCorrectionSettingMutation,
  useShiftCorrectionSettingQuery,
  type ShiftCorrectionRequest,
} from "@entities/shift-corrections";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ArrowRightIcon, CheckIcon, ChevronRightIcon, ClockIcon, CloseIcon, GroupIcon, LockIcon, ScheduleIcon, StatisticsIcon } from "@shared/ui/icons";
import { ContainerGraphHighlightColorDialog } from "./ContainerGraphHighlightColorDialog";
import styles from "./ContainerGraphShiftCorrectionsCard.module.css";

type ContainerGraphShiftCorrectionsCardProps = {
  containerId: number | null;
  graphId: number | null;
  hasUnsavedChanges: boolean;
  disabled: boolean;
  onApproved: () => void;
  onPendingCountChange?: (count: number) => void;
};

const createdAtFormatter = dateTimeFormat("en-GB", {
  day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit",
});

function toMinutes(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  return hours * 60 + minutes;
}

function formatDelta(minutes: number) {
  if (minutes === 0) return t("unchanged");
  const sign = minutes > 0 ? "+" : "−";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  return `${sign}${hours ? t("{0}h", hours) : ""}${hours && remainder ? " " : ""}${remainder ? t("{0}m", remainder) : ""}`;
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
}: ContainerGraphShiftCorrectionsCardProps) {
  const hasPersistedGraph = Number.isInteger(containerId) && containerId !== null && containerId > 0
    && Number.isInteger(graphId) && graphId !== null && graphId > 0;
  const requestsQuery = useGraphShiftCorrectionsQuery(containerId, graphId);
  const settingQuery = useShiftCorrectionSettingQuery(hasPersistedGraph);
  const saveSettingMutation = useSaveShiftCorrectionSettingMutation();
  const approveMutation = useApproveShiftCorrectionMutation();
  const rejectMutation = useRejectShiftCorrectionMutation();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [highlightColor, setHighlightColor] = useState("#FDE68A");
  const [isColorDialogOpen, setIsColorDialogOpen] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const requests = useMemo(() => [...(requestsQuery.data ?? [])].sort((left, right) => {
    if (left.status === "pending" && right.status !== "pending") return -1;
    if (left.status !== "pending" && right.status === "pending") return 1;
    return Date.parse(right.createdAtUtc) - Date.parse(left.createdAtUtc);
  }), [requestsQuery.data]);
  const selected = requests.find(request => request.id === selectedId) ?? null;
  const pendingCount = requests.filter(request => request.status === "pending").length;

  useEffect(() => {
    if (settingQuery.data?.highlightColor) setHighlightColor(settingQuery.data.highlightColor);
  }, [settingQuery.data?.highlightColor]);

  useEffect(() => {
    if (selectedId !== null && !requests.some(request => request.id === selectedId)) setSelectedId(null);
  }, [requests, selectedId]);

  useEffect(() => {
    onPendingCountChange?.(pendingCount);
  }, [onPendingCountChange, pendingCount]);

  const handleColorChange = (value: string) => {
    if (!hasPersistedGraph) return;
    setHighlightColor(value);
    setIsColorDialogOpen(false);
    setActionError(null);
    saveSettingMutation.mutate(value, {
      onError: error => setActionError(getErrorMessage(error, t("Could not save the correction highlight color."))),
    });
  };

  const handleApprove = () => {
    if (!selected || !hasPersistedGraph || containerId === null || graphId === null) return;
    setActionError(null);
    approveMutation.mutate({ containerId, graphId, id: selected.id, highlightColor }, {
      onSuccess: () => {
        setSelectedId(current => current === selected.id ? null : current);
        void containersApi.commitGraphVersion(containerId, graphId)
          .then(onApproved)
          .catch(error => setActionError(getErrorMessage(error, t("The correction was applied, but its schedule version could not be recorded."))));
      },
      onError: error => setActionError(getErrorMessage(error, t("Could not approve this correction."))),
    });
  };

  const handleReject = () => {
    if (!selected || !hasPersistedGraph || containerId === null || graphId === null) return;
    setActionError(null);
    rejectMutation.mutate({ containerId, graphId, id: selected.id }, {
      onSuccess: () => setSelectedId(current => current === selected.id ? null : current),
      onError: error => setActionError(getErrorMessage(error, t("Could not reject this correction."))),
    });
  };

  const isBusy = !hasPersistedGraph || disabled || approveMutation.isPending || rejectMutation.isPending;
  const originalDuration = selected ? toMinutes(selected.originalToTime) - toMinutes(selected.originalFromTime) : 0;
  const requestedDuration = selected ? toMinutes(selected.requestedToTime) - toMinutes(selected.requestedFromTime) : 0;
  const selectedDetails = selected ? (
    <article key={selected.id} className={styles.details} role="listitem">
      <div className={styles.detailsHeader}>
        <span className={`${styles.avatar} ${styles.selectedAvatar}`} aria-hidden="true">{Array.from(initials(selected.employeeName))[0]}</span>
        <div className={styles.selectedCopy}><span>{t("Selected request")}</span><strong>{selected.employeeName}</strong>
          <time dateTime={selected.createdAtUtc}>{createdAtFormatter.format(new Date(selected.createdAtUtc))}</time>
        </div>
        <div className={styles.detailsHeaderActions}>
          <button type="button" aria-label={t("Collapse request from {0}", selected.employeeName)}
            onClick={() => setSelectedId(null)}><CloseIcon size={14} /></button>
        </div>
      </div>
      <em className={`${styles.status} ${styles[`status${selected.status}`]}`}>{statusIcon(selected.status)}{statusLabel(selected.status)}</em>
      <dl className={styles.detailGrid}>
        <div><dt><span className={styles.metadataIcon}><ScheduleIcon size={16} /></span>{t("Date")}</dt><dd>{String(selected.dayOfMonth).padStart(2, "0")}.{String(selected.month).padStart(2, "0")}.{selected.year}</dd></div>
        <div><dt><span className={styles.metadataIcon}><GroupIcon size={16} /></span>{t("Schedule")}</dt><dd>{selected.scheduleName} · {selected.shopName}</dd></div>
      </dl>
      <div className={styles.comparison}>
        <div><span>{t("Current shift")}</span><strong>{selected.originalFromTime} – {selected.originalToTime}</strong></div>
        <span className={styles.comparisonArrow} aria-hidden="true"><ArrowRightIcon size={17} /></span>
        <div><span>{t("Requested shift")}</span><strong>{selected.requestedFromTime} – {selected.requestedToTime}</strong></div>
      </div>
      <div className={styles.deltaRow}>
        <span className={selected.requestedFromTime === selected.originalFromTime ? styles.deltaNeutral : styles.deltaStart}><ClockIcon size={15} />{t("Start")} {formatDelta(toMinutes(selected.requestedFromTime) - toMinutes(selected.originalFromTime))}</span>
        <span className={styles.deltaNeutral}><LockIcon size={15} />{t("End")} {formatDelta(toMinutes(selected.requestedToTime) - toMinutes(selected.originalToTime))}</span>
        <strong className={requestedDuration === originalDuration ? styles.deltaNeutral : styles.deltaDuration}><StatisticsIcon size={15} />{t("Duration")} {formatDelta(requestedDuration - originalDuration)}</strong>
      </div>
      {selected.status === "pending" ? (
        <>
          {hasUnsavedChanges ? <p className={styles.saveHint}>{t("Save your current schedule edits before approving.")}</p> : null}
          <div className={styles.actions}>
            <IosButton label={rejectMutation.isPending ? t("Rejecting...") : t("Reject")} variant="secondary" size="compact"
              icon={<CloseIcon size={15} />} disabled={isBusy} onClick={handleReject} />
            <IosButton label={approveMutation.isPending ? t("Applying...") : t("Approve")} size="compact"
              icon={<CheckIcon size={15} />} disabled={isBusy || hasUnsavedChanges} onClick={handleApprove} />
          </div>
        </>
      ) : (
        <p className={styles.reviewed}><span className={styles.reviewMark} aria-hidden="true"><CheckIcon size={12} /></span><span>{selected.reviewedByManagerName
          ? t("Reviewed by {0}", selected.reviewedByManagerName) : t("Reviewed")}</span></p>
      )}
    </article>
  ) : null;

  return (
    <>
      <div className={styles.headerTools}>
        <button type="button" className={styles.colorControl} title={t("Approved correction highlight color")}
          aria-label={t("Approved correction highlight color")} disabled={!hasPersistedGraph || saveSettingMutation.isPending}
          onClick={() => setIsColorDialogOpen(true)}>
          <span className={styles.colorSwatch} style={{ backgroundColor: highlightColor }} aria-hidden="true" />
        </button>
      </div>

      <ContainerGraphHighlightColorDialog open={isColorDialogOpen} value={highlightColor}
        eyebrow={t("Shift corrections")} title={t("Choose approval color")} inputLabel={t("Approval highlight hex color")}
        isSaving={saveSettingMutation.isPending} onCancel={() => setIsColorDialogOpen(false)} onSave={handleColorChange} />

      {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

      <div className={styles.summary}>
        <span>{t("Employee requests")}</span><strong><ClockIcon size={14} />{pendingCount}  {t("pending")}</strong>
      </div>

      {requestsQuery.isLoading ? <p className={styles.state}>{t("Loading requests...")}</p> : null}
      {requestsQuery.isError ? <p className={styles.state}>{t("Could not load shift corrections.")}</p> : null}
      {!requestsQuery.isLoading && !requestsQuery.isError && requests.length === 0 ? (
        <p className={styles.state}>{t("No correction requests yet.")}</p>
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
                <small>{t("Day")} {request.dayOfMonth} · {request.originalFromTime}–{request.originalToTime}</small>
              </span>
              <em className={`${styles.status} ${styles[`status${request.status}`]}`}>{statusIcon(request.status)}{statusLabel(request.status)}</em>
              <span className={styles.chevron} aria-hidden="true"><ChevronRightIcon size={15} /></span>
            </button>
          ))}
        </div>
      ) : null}
    </>
  );
}
