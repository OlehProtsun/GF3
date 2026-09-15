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
import { CheckIcon, CloseIcon } from "@shared/ui/icons";
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
  return `${sign}${hours ? `${hours}h` : ""}${hours && remainder ? " " : ""}${remainder ? `${remainder}m` : ""}`;
}

function statusLabel(status: ShiftCorrectionRequest["status"]) {
  return status === "pending" ? t("Pending") : status === "approved" ? t("Approved") : t("Rejected");
}

export function ContainerGraphShiftCorrectionsCard({
  containerId,
  graphId,
  hasUnsavedChanges,
  disabled,
  onApproved,
  onPendingCountChange,
}: ContainerGraphShiftCorrectionsCardProps) {
  const requestsQuery = useGraphShiftCorrectionsQuery(containerId, graphId);
  const settingQuery = useShiftCorrectionSettingQuery(containerId !== null && graphId !== null);
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
    setHighlightColor(value);
    setIsColorDialogOpen(false);
    setActionError(null);
    saveSettingMutation.mutate(value, {
      onError: error => setActionError(getErrorMessage(error, t("Could not save the correction highlight color."))),
    });
  };

  const handleApprove = () => {
    if (!selected || containerId === null || graphId === null) return;
    setActionError(null);
    approveMutation.mutate({ containerId, graphId, id: selected.id, highlightColor }, {
      onSuccess: () => {
        void containersApi.commitGraphVersion(containerId, graphId)
          .then(onApproved)
          .catch(error => setActionError(getErrorMessage(error, t("The correction was applied, but its schedule version could not be recorded."))));
      },
      onError: error => setActionError(getErrorMessage(error, t("Could not approve this correction."))),
    });
  };

  const handleReject = () => {
    if (!selected || containerId === null || graphId === null) return;
    setActionError(null);
    rejectMutation.mutate({ containerId, graphId, id: selected.id }, {
      onError: error => setActionError(getErrorMessage(error, t("Could not reject this correction."))),
    });
  };

  const isBusy = disabled || approveMutation.isPending || rejectMutation.isPending;
  const originalDuration = selected ? toMinutes(selected.originalToTime) - toMinutes(selected.originalFromTime) : 0;
  const requestedDuration = selected ? toMinutes(selected.requestedToTime) - toMinutes(selected.requestedFromTime) : 0;
  const selectedDetails = selected ? (
    <article key={selected.id} className={styles.details} role="listitem">
      <div className={styles.detailsHeader}>
        <div><span>{t("Selected request")}</span><strong>{selected.employeeName}</strong></div>
        <div className={styles.detailsHeaderActions}>
          <time dateTime={selected.createdAtUtc}>{createdAtFormatter.format(new Date(selected.createdAtUtc))}</time>
          <button type="button" aria-label={t("Collapse request from {0}", selected.employeeName)}
            onClick={() => setSelectedId(null)}><CloseIcon size={14} /></button>
        </div>
      </div>
      <dl className={styles.detailGrid}>
        <div><dt>{t("Schedule")}</dt><dd>{selected.scheduleName} · {selected.shopName}</dd></div>
        <div><dt>{t("Date")}</dt><dd>{String(selected.dayOfMonth).padStart(2, "0")}.{String(selected.month).padStart(2, "0")}.{selected.year}</dd></div>
      </dl>
      <div className={styles.comparison}>
        <div><span>{t("Current")}</span><strong>{selected.originalFromTime} – {selected.originalToTime}</strong></div>
        <i>→</i>
        <div><span>{t("Requested")}</span><strong>{selected.requestedFromTime} – {selected.requestedToTime}</strong></div>
      </div>
      <div className={styles.deltaRow}>
        <span>{t("Start")} {formatDelta(toMinutes(selected.requestedFromTime) - toMinutes(selected.originalFromTime))}</span>
        <span>{t("End")} {formatDelta(toMinutes(selected.requestedToTime) - toMinutes(selected.originalToTime))}</span>
        <strong>{t("Duration")} {formatDelta(requestedDuration - originalDuration)}</strong>
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
        <p className={styles.reviewed}>{t("Reviewed")} {selected.reviewedByManagerName ? `by ${selected.reviewedByManagerName}` : ""}</p>
      )}
    </article>
  ) : null;

  return (
    <>
      <div className={styles.headerTools}>
        <button type="button" className={styles.colorControl} title={t("Approved correction highlight color")}
          aria-label={t("Approved correction highlight color")} disabled={saveSettingMutation.isPending}
          onClick={() => setIsColorDialogOpen(true)}>
          <span className={styles.colorSwatch} style={{ backgroundColor: highlightColor }} aria-hidden="true" />
        </button>
      </div>

      <ContainerGraphHighlightColorDialog open={isColorDialogOpen} value={highlightColor}
        eyebrow="Shift corrections" title={t("Choose approval color")} inputLabel="Approval highlight hex color"
        isSaving={saveSettingMutation.isPending} onCancel={() => setIsColorDialogOpen(false)} onSave={handleColorChange} />

      {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

      <div className={styles.summary}>
        <span>{t("Employee requests")}</span><strong>{pendingCount}  {t("pending")}</strong>
      </div>

      {requestsQuery.isLoading ? <p className={styles.state}>{t("Loading requests...")}</p> : null}
      {requestsQuery.isError ? <p className={styles.state}>{t("Could not load shift corrections.")}</p> : null}
      {!requestsQuery.isLoading && !requestsQuery.isError && requests.length === 0 ? (
        <p className={styles.state}>{t("No correction requests yet.")}</p>
      ) : null}

      {requests.length > 0 ? (
        <div className={styles.list} role="list" aria-label={t("Shift correction requests")}>
          {requests.map(request => selected?.id === request.id ? selectedDetails : (
            <button key={request.id} type="button" role="listitem"
              className={`${styles.requestButton} ${selected?.id === request.id ? styles.requestButtonActive : ""}`}
              aria-expanded={selected?.id === request.id}
              onClick={() => { setSelectedId(current => current === request.id ? null : request.id); setActionError(null); }}>
              <span className={styles.avatar}>{request.employeeName.trim().charAt(0).toUpperCase()}</span>
              <span className={styles.requestCopy}>
                <strong>{request.employeeName}</strong>
                <small>{t("Day")} {request.dayOfMonth} · {request.originalFromTime}–{request.originalToTime}</small>
              </span>
              <em className={`${styles.status} ${styles[`status${request.status}`]}`}>{statusLabel(request.status)}</em>
            </button>
          ))}
        </div>
      ) : null}
    </>
  );
}
