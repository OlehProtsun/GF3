import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import { useGraphContractApprovalsQuery, useReviewContractApprovalMutation, type ContractShiftApproval } from "@entities/shift-swap-approvals";
import { queryKeys } from "@shared/api/queryKeys";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { dateTimeFormat, t, translateServerMessage } from "@shared/i18n";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ArrowRightIcon, CheckIcon, ChevronRightIcon, ClockIcon, CloseIcon, GroupIcon, ScheduleIcon } from "@shared/ui/icons";
import activity from "./ContainerGraphActivity.module.css";
import styles from "./ContainerGraphContractApprovalsCard.module.css";

const submittedAt = dateTimeFormat("en-GB", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
function initials(name: string) {
  return (name.match(/\p{L}+/gu) ?? []).slice(0, 2).map(part => Array.from(part)[0]).join("").toLocaleUpperCase() || "?";
}
function participants(item: ContractShiftApproval) {
  const from = item.isManagerCreated ? t("Manager-created shift") : item.fromEmployeeName?.trim()
    || (item.kind === "below_minimum_give" ? item.employeeName.trim() : "") || t("Unknown employee");
  const to = item.toEmployeeName?.trim() || (item.kind === "claim" ? item.employeeName.trim() : "")
    || (item.kind === "below_minimum_give" && !item.targetEmployeeId ? t("Open recipient") : t("Unknown employee"));
  return { from, to };
}
function statusLabel(status: ContractShiftApproval["status"]) {
  return ({ pending: t("Pending"), approved: t("Approved"), rejected: t("Rejected"), cancelled: t("Cancelled"), expired: t("Expired") })[status];
}
function date(day: number | null | undefined, month: number, year?: number) {
  return [day ? String(day).padStart(2, "0") : null, String(month).padStart(2, "0"), year].filter(value => value != null).join(".");
}
function Transfer({ from, to, period, label, focused }: { from: string; to: string; period: string; label: string; focused: boolean }) {
  return <div className={styles.transfer} role="group" aria-label={label} data-review-part={focused ? "transfer" : undefined}>
    <div className={styles.participants} data-review-part={focused ? "participants" : undefined}>
      <div className={styles.person}><span>{t("Gives shift")}</span><strong>{from}</strong><span className={styles.avatar} aria-hidden="true">{initials(from)}</span></div>
      <span className={styles.arrow} aria-hidden="true"><ArrowRightIcon size={17} /></span>
      <div className={`${styles.person} ${styles.recipient}`}><span>{t("Receives shift")}</span><strong>{to}</strong><span className={styles.avatar} aria-hidden="true">{initials(to)}</span></div>
    </div>
    <span className={styles.period}><ClockIcon size={14} />{period}</span>
  </div>;
}

export function ContainerGraphContractApprovalsCard({ containerId, graphId, disabled, hasUnsavedChanges,
  onApproved, onPendingCountChange, reviewRequestId, onReviewBusyChange, onReviewTypeChange, reviewScheduleLink }: {
  containerId: number | null; graphId: number | null; disabled: boolean; hasUnsavedChanges: boolean;
  onApproved: () => void; onPendingCountChange?: (count: number) => void;
  reviewRequestId?: number; onReviewBusyChange?: (busy: boolean) => void;
  onReviewTypeChange?: (type: string) => void; reviewScheduleLink?: ReactNode;
}) {
  const focused = reviewRequestId !== undefined;
  const query = useGraphContractApprovalsQuery(containerId, graphId, reviewRequestId);
  const mutation = useReviewContractApprovalMutation();
  const client = useQueryClient();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [confirm, setConfirm] = useState<ContractShiftApproval | null>(null);
  const [error, setError] = useState<string | null>(null);
  const reviewInFlight = useRef(false);
  const confirmRef = useRef<HTMLDivElement>(null);
  const notFound = focused && query.error instanceof ApiError && query.error.status === 404;
  const isReading = focused && (query.isLoading || query.isFetching);
  const requests = notFound ? [] : (query.data ?? []).filter(item => !focused || item.id === reviewRequestId)
    .sort((a, b) => Number(b.status === "pending") - Number(a.status === "pending") || Date.parse(b.createdAtUtc) - Date.parse(a.createdAtUtc));
  const count = requests.filter(item => item.status === "pending").length;
  const blocked = disabled || hasUnsavedChanges || mutation.isPending || (focused && (isReading || query.isError));
  const confirmRequest = confirm && requests.find(item => item.id === confirm.id && item.status === "pending");
  const queryKey = focused ? queryKeys.shiftSwaps.graphApprovalById(containerId ?? 0, graphId ?? 0, reviewRequestId)
    : queryKeys.shiftSwaps.graphApprovals(containerId ?? 0, graphId ?? 0);
  const reviewKind = focused ? requests[0]?.kind : undefined;
  const reviewType = t(!reviewKind ? "Contract approvals" : reviewKind === "claim" ? "Shift pickup approval" : "Monthly minimum exception");
  useEffect(() => onPendingCountChange?.(count), [count, onPendingCountChange]);
  useEffect(() => {
    if (focused) onReviewTypeChange?.(reviewType);
  }, [focused, onReviewTypeChange, reviewType]);
  useLayoutEffect(() => {
    const root = confirmRef.current;
    if (!focused || !root) return;
    const previous = document.activeElement;
    root.querySelector<HTMLElement>("button:not(:disabled)")?.focus();
    return () => {
      if (previous instanceof HTMLElement && previous.isConnected) previous.focus({ preventScroll: true });
    };
  }, [focused, confirm?.id]);

  function finishReview() {
    reviewInFlight.current = false;
    onReviewBusyChange?.(false);
  }

  const review = (item: ContractShiftApproval, action: "approve" | "reject") => {
    if (!containerId || !graphId || blocked || reviewInFlight.current || item.status !== "pending") return;
    setError(null);
    reviewInFlight.current = true;
    onReviewBusyChange?.(true);
    mutation.mutate({ containerId, graphId, id: item.id, action }, {
      onSuccess: () => { setConfirm(null); finishReview(); if (action === "approve" && item.kind === "claim") onApproved(); },
      onError: failure => {
        setConfirm(null); setError(getErrorMessage(failure, t("Could not review this request.")));
        client.invalidateQueries({ queryKey });
        if (focused) {
          client.invalidateQueries({ queryKey: queryKeys.home.activity() });
          client.invalidateQueries({ queryKey: queryKeys.home.navBadges() });
        }
        finishReview();
      },
    });
  };
  if (!containerId || !graphId) return null;
  const confirmation = confirm && <ConfirmDialog open title={t("Approve shift transfer?")} message={t("Approval transfers the requested shifts and records schedule history.")}
    confirmText={t("Approve")} confirmDisabled={blocked || !confirmRequest} cancelDisabled={mutation.isPending}
    onCancel={() => setConfirm(null)} onConfirm={() => { if (confirmRequest) review(confirmRequest, "approve"); }} />;
  return <section className={styles.root} data-review-part={focused ? "panel" : undefined} aria-label={t("Contract approvals")}>
    {!focused && <header className={styles.summary}><strong>{t("Contract approvals")}</strong><span className={`${activity.badge} ${activity.pending}`}><ClockIcon size={13} />{t("{0} pending", count)}</span></header>}
    {hasUnsavedChanges ? <p className={`${activity.state} ${activity.pending}`} role="status">{t("Save your changes before reviewing requests.")}</p> : null}
    {error && <ErrorBanner dismissible={false}>{error}</ErrorBanner>}
    {query.error && !notFound && (focused || !error) && <ErrorBanner dismissible={false}>{getErrorMessage(query.error, t("Could not load approval requests."))}</ErrorBanner>}
    {query.isError && !notFound && <IosButton label={t("Retry")} variant="secondary" size="compact" onClick={() => client.invalidateQueries({ queryKey })} />}
    {(query.isLoading || isReading) && <p className={activity.state} role="status">{t("Loading...")}</p>}
    {focused ? (notFound || (!isReading && !query.isError && requests.length === 0)) &&
      <p className={activity.state} role="status">{t("This request is no longer available.")}</p>
      : query.data?.length === 0 && <p className={activity.state}>{t("No contract approval requests.")}</p>}
    <div className={styles.list} data-review-part={focused ? "list" : undefined}>{requests.map(item => {
      const { from, to } = participants(item);
      const requester = item.employeeName.trim() || t("Unknown employee");
      const StatusIcon = item.status === "approved" ? CheckIcon
      : item.status === "rejected" || item.status === "cancelled" ? CloseIcon : ClockIcon;
      const status = <span className={`${styles.status} ${styles[item.status] ?? ""}`}
        data-review-pending={focused && item.status === "pending" ? "" : undefined}>
        <StatusIcon size={12} />{focused && item.status === "pending" ? t("Pending review") : statusLabel(item.status)}
      </span>;
      const period = `${item.fromTime} – ${item.toTime}`;
      return !focused && selectedId !== item.id ? <button key={item.id} type="button" className={styles.requestButton}
        aria-expanded={false} aria-controls={`contract-approval-${item.id}`} onClick={() => setSelectedId(item.id)}>
        <span className={styles.avatar} aria-hidden="true">{initials(requester)}</span>
        <span className={styles.requestCopy}><strong>{requester}</strong><span>{from} → {to}</span><small>{date(item.dayOfMonth, item.month)} · {period}</small></span>
        {status}<span className={styles.chevron} aria-hidden="true"><ChevronRightIcon size={15} /></span>
      </button> : <article key={item.id} id={`contract-approval-${item.id}`} className={styles.details} aria-label={t("Request from {0}", requester)}
        data-review-part={focused ? "detail" : undefined}>
        <div className={styles.detailsHeader} data-review-part={focused ? "person" : undefined}>
          <span className={styles.avatar} aria-hidden="true">{initials(requester)}</span>
          <div className={styles.selectedCopy}>{!focused && <span>{t("Selected request")}</span>}<strong>{requester}</strong><time dateTime={item.createdAtUtc}>{submittedAt.format(new Date(item.createdAtUtc))}</time></div>
          {focused ? status : <button type="button" className={styles.collapse} aria-label={t("Collapse request from {0}", requester)} aria-expanded={true} onClick={() => setSelectedId(null)}><CloseIcon size={15} /></button>}
        </div>
        {!focused && <div className={styles.kindRow}><span>{t(item.kind === "claim" ? "Shift pickup approval" : "Monthly minimum exception")}</span>{status}</div>}
        <dl className={styles.detailGrid} data-review-part={focused ? "metadata" : undefined}>
          <div><dt><ScheduleIcon size={15} />{t("Date")}</dt><dd>{date(item.dayOfMonth, item.month, item.year)}</dd></div>
          <div><dt><GroupIcon size={15} />{t("Schedule")}</dt><dd>{item.shopName || t("Not available")} · {item.scheduleName || t("Not available")}</dd></div>
        </dl>
        <Transfer from={from} to={to} period={period} label={t("Shift transfer")} focused={focused} />
        {item.requestKind === "exchange" ? <div className={styles.returnShift}><strong>{t("Return shift")}</strong>
          {item.returnMonth && item.returnYear ? <span>{date(item.returnDayOfMonth, item.returnMonth, item.returnYear)}</span> : null}
          <Transfer from={to} to={from} period={`${item.returnFromTime ?? t("Not available")} – ${item.returnToTime ?? t("Not available")}`} label={t("Return shift")} focused={focused} />
        </div> : null}
        {item.minimumShopName ? <p className={styles.note}>{item.minimumShopName} · {item.minimumMonth}.{item.minimumYear}</p> : null}
        {item.projectedMinutes != null ? <p className={styles.note}>{t("Monthly minimum: {0} h; after transfer: {1} h.", item.minimumMinutes == null ? t("Not configured") : item.minimumMinutes / 60, item.projectedMinutes / 60)}</p> : null}
        {item.shiftSwapRequestId ? <span className={styles.related}>{t("Related offer")}: #{item.shiftSwapRequestId}</span> : null}
        {item.kind === "claim" && item.status === "pending" ? <p className={styles.note}>{t("The offer is reserved while this pickup request awaits manager approval.")}</p> : null}
        {item.failureReason ? <p className={styles.note}>{translateServerMessage(item.failureReason)}</p> : null}
        {focused && <div data-review-part="comment">
          <strong>{t("Comment")}</strong><p data-empty="">{t("No comment provided")}</p>
        </div>}
        {(item.status === "pending" || reviewScheduleLink) && <div data-review-part={focused ? "footer" : undefined}>
          {reviewScheduleLink}
          {item.status === "pending" && <div className={styles.actions} data-review-part={focused ? "decisions" : undefined}>
            <IosButton className={`${activity.action} ${activity.danger}`} label={mutation.isPending ? t("Applying...") : t("Reject")} variant="secondary" size="compact" icon={<CloseIcon size={15} />} disabled={blocked} onClick={() => review(item, "reject")} />
            <IosButton className={`${activity.action} ${activity.success}`} label={mutation.isPending ? t("Applying...") : t("Approve")} size="compact" icon={<CheckIcon size={15} />} disabled={blocked} onClick={() => item.kind === "claim" ? setConfirm(item) : review(item, "approve")} />
          </div>}
        </div>}
      </article>;
    })}</div>
    {focused && requests.length === 0 && reviewScheduleLink && <div data-review-part="footer">{reviewScheduleLink}</div>}
    {confirmation && createPortal(focused ? <div ref={confirmRef}>{confirmation}</div> : confirmation, document.body)}
  </section>;
}
