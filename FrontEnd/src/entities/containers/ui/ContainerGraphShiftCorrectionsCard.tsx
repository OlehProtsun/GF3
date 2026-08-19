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
import styles from "./ContainerGraphShiftCorrectionsCard.module.css";

type ContainerGraphShiftCorrectionsCardProps = {
  containerId: number | null;
  graphId: number | null;
  hasUnsavedChanges: boolean;
  disabled: boolean;
  onApproved: () => void;
};

const createdAtFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit",
});

function toMinutes(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  return hours * 60 + minutes;
}

function formatDelta(minutes: number) {
  if (minutes === 0) return "unchanged";
  const sign = minutes > 0 ? "+" : "−";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  return `${sign}${hours ? `${hours}h` : ""}${hours && remainder ? " " : ""}${remainder ? `${remainder}m` : ""}`;
}

function statusLabel(status: ShiftCorrectionRequest["status"]) {
  return status === "pending" ? "Pending" : status === "approved" ? "Approved" : "Rejected";
}

export function ContainerGraphShiftCorrectionsCard({
  containerId,
  graphId,
  hasUnsavedChanges,
  disabled,
  onApproved,
}: ContainerGraphShiftCorrectionsCardProps) {
  const requestsQuery = useGraphShiftCorrectionsQuery(containerId, graphId);
  const settingQuery = useShiftCorrectionSettingQuery(containerId !== null && graphId !== null);
  const saveSettingMutation = useSaveShiftCorrectionSettingMutation();
  const approveMutation = useApproveShiftCorrectionMutation();
  const rejectMutation = useRejectShiftCorrectionMutation();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [highlightColor, setHighlightColor] = useState("#FDE68A");
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

  const handleColorChange = (value: string) => {
    setHighlightColor(value);
    setActionError(null);
    saveSettingMutation.mutate(value, {
      onError: error => setActionError(getErrorMessage(error, "Could not save the correction highlight color.")),
    });
  };

  const handleApprove = () => {
    if (!selected || containerId === null || graphId === null) return;
    setActionError(null);
    approveMutation.mutate({ containerId, graphId, id: selected.id, highlightColor }, {
      onSuccess: () => {
        void containersApi.commitGraphVersion(containerId, graphId)
          .then(onApproved)
          .catch(error => setActionError(getErrorMessage(error, "The correction was applied, but its schedule version could not be recorded.")));
      },
      onError: error => setActionError(getErrorMessage(error, "Could not approve this correction.")),
    });
  };

  const handleReject = () => {
    if (!selected || containerId === null || graphId === null) return;
    setActionError(null);
    rejectMutation.mutate({ containerId, graphId, id: selected.id }, {
      onError: error => setActionError(getErrorMessage(error, "Could not reject this correction.")),
    });
  };

  const isBusy = disabled || approveMutation.isPending || rejectMutation.isPending;
  const originalDuration = selected ? toMinutes(selected.originalToTime) - toMinutes(selected.originalFromTime) : 0;
  const requestedDuration = selected ? toMinutes(selected.requestedToTime) - toMinutes(selected.requestedFromTime) : 0;

  return (
    <>
      <div className={styles.headerTools}>
        <label className={styles.colorControl} title="Approved correction highlight color">
          <input type="color" value={highlightColor} aria-label="Approved correction highlight color"
            disabled={saveSettingMutation.isPending} onChange={event => void handleColorChange(event.target.value)} />
          <span className={styles.colorSwatch} style={{ backgroundColor: highlightColor }} aria-hidden="true" />
        </label>
      </div>

      {actionError ? <ErrorBanner dismissible={false}>{actionError}</ErrorBanner> : null}

      <div className={styles.summary}>
        <span>Employee requests</span><strong>{pendingCount} pending</strong>
      </div>

      {requestsQuery.isLoading ? <p className={styles.state}>Loading requests...</p> : null}
      {requestsQuery.isError ? <p className={styles.state}>Could not load shift corrections.</p> : null}
      {!requestsQuery.isLoading && !requestsQuery.isError && requests.length === 0 ? (
        <p className={styles.state}>No correction requests yet.</p>
      ) : null}

      {requests.length > 0 ? (
        <div className={styles.list} role="list" aria-label="Shift correction requests">
          {requests.map(request => (
            <button key={request.id} type="button" role="listitem"
              className={`${styles.requestButton} ${selected?.id === request.id ? styles.requestButtonActive : ""}`}
              aria-expanded={selected?.id === request.id}
              onClick={() => { setSelectedId(current => current === request.id ? null : request.id); setActionError(null); }}>
              <span className={styles.avatar}>{request.employeeName.trim().charAt(0).toUpperCase()}</span>
              <span className={styles.requestCopy}>
                <strong>{request.employeeName}</strong>
                <small>Day {request.dayOfMonth} · {request.originalFromTime}–{request.originalToTime}</small>
              </span>
              <em className={`${styles.status} ${styles[`status${request.status}`]}`}>{statusLabel(request.status)}</em>
            </button>
          ))}
        </div>
      ) : null}

      {selected ? (
        <article className={styles.details}>
          <div className={styles.detailsHeader}>
            <div><span>Selected request</span><strong>{selected.employeeName}</strong></div>
            <time dateTime={selected.createdAtUtc}>{createdAtFormatter.format(new Date(selected.createdAtUtc))}</time>
          </div>
          <dl className={styles.detailGrid}>
            <div><dt>Schedule</dt><dd>{selected.scheduleName} · {selected.shopName}</dd></div>
            <div><dt>Date</dt><dd>{String(selected.dayOfMonth).padStart(2, "0")}.{String(selected.month).padStart(2, "0")}.{selected.year}</dd></div>
          </dl>
          <div className={styles.comparison}>
            <div><span>Current</span><strong>{selected.originalFromTime} – {selected.originalToTime}</strong></div>
            <i>→</i>
            <div><span>Requested</span><strong>{selected.requestedFromTime} – {selected.requestedToTime}</strong></div>
          </div>
          <div className={styles.deltaRow}>
            <span>Start {formatDelta(toMinutes(selected.requestedFromTime) - toMinutes(selected.originalFromTime))}</span>
            <span>End {formatDelta(toMinutes(selected.requestedToTime) - toMinutes(selected.originalToTime))}</span>
            <strong>Duration {formatDelta(requestedDuration - originalDuration)}</strong>
          </div>
          {selected.status === "pending" ? (
            <>
              {hasUnsavedChanges ? <p className={styles.saveHint}>Save your current schedule edits before approving.</p> : null}
              <div className={styles.actions}>
                <IosButton label={rejectMutation.isPending ? "Rejecting..." : "Reject"} variant="secondary" size="compact"
                  icon={<CloseIcon size={15} />} disabled={isBusy} onClick={handleReject} />
                <IosButton label={approveMutation.isPending ? "Applying..." : "Approve"} size="compact"
                  icon={<CheckIcon size={15} />} disabled={isBusy || hasUnsavedChanges} onClick={handleApprove} />
              </div>
            </>
          ) : (
            <p className={styles.reviewed}>Reviewed {selected.reviewedByManagerName ? `by ${selected.reviewedByManagerName}` : ""}</p>
          )}
        </article>
      ) : null}
    </>
  );
}
